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
var bucketName = Environment.GetEnvironmentVariable("GAME_BUILDS_BUCKET")
    ?? throw new InvalidOperationException("GAME_BUILDS_BUCKET environment variable is required");
var profileName = Environment.GetEnvironmentVariable("AWS_PROFILE")
    ?? throw new InvalidOperationException("AWS_PROFILE environment variable is required");

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
    var contentEncoding = GetContentEncoding(filePath);

    var request = new TransferUtilityUploadRequest
    {
        BucketName = bucketName,
        Key = s3Key,
        FilePath = filePath,
        ContentType = contentType
    };

    // Set Content-Encoding for pre-compressed files so browsers auto-decompress
    if (contentEncoding != null)
    {
        request.Headers.ContentEncoding = contentEncoding;
    }

    await transferUtility.UploadAsync(request);
    var encodingInfo = contentEncoding != null ? $", encoding: {contentEncoding}" : "";
    Console.WriteLine($"  Uploaded: {s3Key} ({contentType}{encodingInfo})");
}

var gameUrl = $"http://{bucketName}.s3-website-us-east-1.amazonaws.com/{gameName}/index.html";
Console.WriteLine();
Console.WriteLine($"Done! Game URL: {gameUrl}");
return 0;

static string GetContentType(string filePath)
{
    var name = filePath.ToLowerInvariant();

    // For .br files, return the content type of the underlying file (not the compression format)
    if (name.EndsWith(".br"))
    {
        return name switch
        {
            _ when name.EndsWith(".wasm.br") => "application/wasm",
            _ when name.EndsWith(".js.br") => "application/javascript",
            _ when name.EndsWith(".data.br") => "application/octet-stream",
            _ when name.EndsWith(".json.br") => "application/json",
            _ => "application/octet-stream"
        };
    }

    // For .gz files, return the content type of the underlying file
    if (name.EndsWith(".gz"))
    {
        return name switch
        {
            _ when name.EndsWith(".wasm.gz") => "application/wasm",
            _ when name.EndsWith(".js.gz") => "application/javascript",
            _ when name.EndsWith(".data.gz") => "application/octet-stream",
            _ when name.EndsWith(".json.gz") => "application/json",
            _ => "application/octet-stream"
        };
    }

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
        _ => "application/octet-stream"
    };
}

static string? GetContentEncoding(string filePath)
{
    var name = filePath.ToLowerInvariant();
    if (name.EndsWith(".br")) return "br";
    if (name.EndsWith(".gz")) return "gzip";
    return null;
}
