# Unity Game Deployments

AWS infrastructure for hosting Unity WebGL game builds and a static portal website featuring "AI based Game Development."

## Architecture

```
unity-game-deployments/
├── infra/                     # AWS CDK (C#) - S3 buckets for portal + game builds
├── portal/                    # Static website (HTML/CSS)
├── tools/GameDeployer/        # Console tool to upload game builds to S3
└── games/                     # Local game build folders (not committed)
```

**S3 Buckets:**
- `ai-game-portal-668191889297` - Static portal website with hero section and game links
- `unity-game-builds-668191889297` - Unity WebGL builds (one subfolder per game)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [AWS CLI v2](https://aws.amazon.com/cli/)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html) (`npm install -g aws-cdk`)
- AWS SSO profile `AdministratorAccess-668191889297` configured

## Setup & Deploy

```bash
# 1. Login to AWS via SSO
aws sso login --profile AdministratorAccess-668191889297

# 2. Bootstrap CDK (first time only)
cd infra && cdk bootstrap --profile AdministratorAccess-668191889297

# 3. Deploy the stack
cd infra && cdk deploy --profile AdministratorAccess-668191889297
```

The deploy output will print the portal URL and game builds bucket URL.

## Deploying a Game Build

Place your Unity WebGL build output in `games/<game-name>/`, then run:

```bash
cd tools/GameDeployer
dotnet run -- <game-name> ../../games/<game-name>
```

Example:

```bash
dotnet run -- my-platformer ../../games/my-platformer
```

The tool uploads all files with correct content types and prints the game URL.

After deploying a game, update `portal/index.html` to add a card linking to the new game, then redeploy the CDK stack to push portal changes.

## Portal

The portal (`portal/index.html`) is a dark-themed static site with:
- A hero section titled "AI Based Game Development"
- A responsive grid of game cards linking to hosted WebGL builds
- Deployed automatically to the portal S3 bucket via CDK `BucketDeployment`

## Useful CDK Commands

```bash
cd infra
cdk synth     # Emit CloudFormation template
cdk diff      # Compare deployed stack with local changes
cdk deploy    # Deploy stack to AWS
cdk destroy   # Tear down all resources
```
