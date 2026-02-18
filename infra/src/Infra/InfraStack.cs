using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.CDK.AWS.IAM;
using Constructs;

public class InfraStack : Stack
{
    public InfraStack(Construct scope, string id, string env, IStackProps? props = null) : base(scope, id, props)
    {
        var accountId = this.Account;
        var bucketPrefix = env == "prod" ? "" : $"{env}-";

        // Portal S3 Bucket
        var portalBucket = new Bucket(this, "PortalBucket", new BucketProps
        {
            BucketName = $"{bucketPrefix}ai-game-portal-{accountId}",
            WebsiteIndexDocument = "index.html",
            WebsiteErrorDocument = "index.html",
            BlockPublicAccess = new BlockPublicAccess(new BlockPublicAccessOptions
            {
                BlockPublicAcls = false,
                BlockPublicPolicy = false,
                IgnorePublicAcls = false,
                RestrictPublicBuckets = false
            }),
            ObjectOwnership = ObjectOwnership.BUCKET_OWNER_ENFORCED,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });

        portalBucket.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = new[] { "s3:GetObject" },
            Resources = new[] { portalBucket.ArnForObjects("*") },
            Principals = new[] { new AnyPrincipal() }
        }));

        // Deploy portal static files
        new BucketDeployment(this, "PortalDeployment", new BucketDeploymentProps
        {
            Sources = new[] { Source.Asset("../portal/bin/Release/net9.0/publish/wwwroot") },
            DestinationBucket = portalBucket
        });

        // Game Builds S3 Bucket
        var gameBucket = new Bucket(this, "GameBuildsBucket", new BucketProps
        {
            BucketName = $"{bucketPrefix}unity-game-builds-{accountId}",
            WebsiteIndexDocument = "index.html",
            BlockPublicAccess = new BlockPublicAccess(new BlockPublicAccessOptions
            {
                BlockPublicAcls = false,
                BlockPublicPolicy = false,
                IgnorePublicAcls = false,
                RestrictPublicBuckets = false
            }),
            ObjectOwnership = ObjectOwnership.BUCKET_OWNER_ENFORCED,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            Cors = new[]
            {
                new CorsRule
                {
                    AllowedOrigins = new[] { "*" },
                    AllowedMethods = new[] { HttpMethods.GET },
                    AllowedHeaders = new[] { "*" }
                }
            }
        });

        gameBucket.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = new[] { "s3:GetObject" },
            Resources = new[] { gameBucket.ArnForObjects("*") },
            Principals = new[] { new AnyPrincipal() }
        }));

        // Deploy game build - non-Brotli files (index.html, loader.js, TemplateData, StreamingAssets)
        new BucketDeployment(this, "GameBuildNonBrotli", new BucketDeploymentProps
        {
            Sources = new[] { Source.Asset("../games/mcp-unity-1-build-optimized", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Exclude = new[] { "Build/*.br" }
            }) },
            DestinationBucket = gameBucket,
            DestinationKeyPrefix = "mcp-unity-1"
        });

        // Deploy Brotli-compressed .wasm.br file with correct headers
        new BucketDeployment(this, "GameBuildWasm", new BucketDeploymentProps
        {
            Sources = new[] { Source.Asset("../games/mcp-unity-1-build-optimized/Build", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Exclude = new[] { "*.data.br", "*.framework.js.br", "*.loader.js" }
            }) },
            DestinationBucket = gameBucket,
            DestinationKeyPrefix = "mcp-unity-1/Build",
            ContentEncoding = "br",
            ContentType = "application/wasm"
        });

        // Deploy Brotli-compressed .framework.js.br file with correct headers
        new BucketDeployment(this, "GameBuildFramework", new BucketDeploymentProps
        {
            Sources = new[] { Source.Asset("../games/mcp-unity-1-build-optimized/Build", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Exclude = new[] { "*.data.br", "*.wasm.br", "*.loader.js" }
            }) },
            DestinationBucket = gameBucket,
            DestinationKeyPrefix = "mcp-unity-1/Build",
            ContentEncoding = "br",
            ContentType = "application/javascript"
        });

        // Deploy Brotli-compressed .data.br file with correct headers
        // Increased memory/timeout/storage for the large data file (~75MB)
        new BucketDeployment(this, "GameBuildData", new BucketDeploymentProps
        {
            Sources = new[] { Source.Asset("../games/mcp-unity-1-build-optimized/Build", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Exclude = new[] { "*.wasm.br", "*.framework.js.br", "*.loader.js" }
            }) },
            DestinationBucket = gameBucket,
            DestinationKeyPrefix = "mcp-unity-1/Build",
            ContentEncoding = "br",
            ContentType = "application/octet-stream",
            MemoryLimit = 1024,
            EphemeralStorageSize = Size.Mebibytes(512)
        });

        // Stack Outputs
        new CfnOutput(this, "PortalUrl", new CfnOutputProps
        {
            Value = portalBucket.BucketWebsiteUrl,
            Description = "Portal website URL"
        });

        new CfnOutput(this, "GameBuildsBucketUrl", new CfnOutputProps
        {
            Value = gameBucket.BucketWebsiteUrl,
            Description = "Game builds bucket URL"
        });
    }
}
