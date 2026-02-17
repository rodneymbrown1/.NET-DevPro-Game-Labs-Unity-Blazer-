using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Transfer;

if (args.Length < 2)
{
    Console.WriteLine("Usage: GameDeployer <game-name> <path-to-build-folder>");
    Console.WriteLine("Example: GameDeployer my-game ./games/my-game");
    return 1;
}

var gameName = args[0];
var buildPath = args[1];
var bucketName = "unity-game-builds-668191889297";
var profileName = "AdministratorAccess-668191889297";

if (!Directory.Exists(buildPath))
{
    Console.Error.WriteLine($"Error: Build folder not found: {buildPath}");
    return 1;
}

// Load AWS SSO credentials
var chain = new CredentialProfileStoreChain();
if (!chain.TryGetAWSCredentials(profileName, out var credentials))
{
    Console.Error.WriteLine($"Error: AWS profile '{profileName}' not found. Run: aws sso login --profile {profileName}");
    return 1;
}

using var s3Client = new AmazonS3Client(credentials, Amazon.RegionEndpoint.USEast1);
var transferUtility = new TransferUtility(s3Client);

var files = Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories);
Console.WriteLine($"Uploading {files.Length} files to s3://{bucketName}/{gameName}/...");

foreach (var filePath in files)
{
    var relativePath = Path.GetRelativePath(buildPath, filePath).Replace('\\', '/');
    var s3Key = $"{gameName}/{relativePath}";
    var contentType = GetContentType(filePath);

    var request = new TransferUtilityUploadRequest
    {
        BucketName = bucketName,
        Key = s3Key,
        FilePath = filePath,
        ContentType = contentType
    };

    await transferUtility.UploadAsync(request);
    Console.WriteLine($"  Uploaded: {s3Key} ({contentType})");
}

var gameUrl = $"http://{bucketName}.s3-website-us-east-1.amazonaws.com/{gameName}/index.html";
Console.WriteLine();
Console.WriteLine($"Done! Game URL: {gameUrl}");
return 0;

static string GetContentType(string filePath)
{
    return Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".html" => "text/html",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".json" => "application/json",
        ".wasm" => "application/wasm",
        ".data" => "application/octet-stream",
        ".unityweb" => "application/octet-stream",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".ico" => "image/x-icon",
        ".br" => "application/x-brotli",
        ".gz" => "application/gzip",
        _ => "application/octet-stream"
    };
}
