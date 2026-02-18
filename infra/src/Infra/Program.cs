using Amazon.CDK;

var app = new App();

var env = app.Node.TryGetContext("env")?.ToString() ?? "dev";
var stackName = $"UnityGameDeployments-{char.ToUpper(env[0])}{env[1..]}";

new InfraStack(app, stackName, env, new StackProps
{
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT") ?? "668191889297",
        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1"
    }
});

app.Synth();
