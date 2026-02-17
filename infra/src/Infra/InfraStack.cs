using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.CDK.AWS.IAM;
using Constructs;

public class InfraStack : Stack
{
    public InfraStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        var accountId = this.Account;

        // Portal S3 Bucket
        var portalBucket = new Bucket(this, "PortalBucket", new BucketProps
        {
            BucketName = $"ai-game-portal-{accountId}",
            WebsiteIndexDocument = "index.html",
            BlockPublicAccess = BlockPublicAccess.NONE,
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
            Sources = new[] { Source.Asset("../portal") },
            DestinationBucket = portalBucket
        });

        // Game Builds S3 Bucket
        var gameBucket = new Bucket(this, "GameBuildsBucket", new BucketProps
        {
            BucketName = $"unity-game-builds-{accountId}",
            WebsiteIndexDocument = "index.html",
            BlockPublicAccess = BlockPublicAccess.NONE,
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
