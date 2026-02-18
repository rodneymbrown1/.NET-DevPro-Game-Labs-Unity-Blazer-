using Amazon.CDK;

var app = new App();

var env = app.Node.TryGetContext("env")?.ToString() ?? "dev";
var stackName = env == "prod" ? "UnityGameDeploymentsStack" : $"UnityGameDeployments-{char.ToUpper(env[0])}{env[1..]}";

new InfraStack(app, stackName, env, new StackProps
{
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
    }
});

app.Synth();
