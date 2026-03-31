# Quickstart: Deploy to Azure

> ⚠️ **Not yet implemented.** Azure Developer CLI deployment is planned but the infrastructure (`azure.yaml`, Bicep/Terraform templates) has not been created yet. This guide documents the intended workflow for when it becomes available. For now, use [Local Development with Aspire](QUICKSTART-LOCAL.md).

Deploy Marginalia to Azure using the Azure Developer CLI (`azd`). This provisions all required infrastructure and deploys the application with a single command.

> **Looking for local development?** See [Local Development with Aspire](QUICKSTART-LOCAL.md).

## Prerequisites

### Azure Developer CLI

Install the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd):

```bash
# Windows (winget)
winget install Microsoft.Azd

# macOS (Homebrew)
brew install azd

# Linux (script)
curl -fsSL https://aka.ms/install-azd.sh | bash
```

Verify the installation:

```bash
azd version
```

### Azure CLI

Install the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli).

### Azure subscription

You need an active Azure subscription with quota for `gpt-5.3-chat` (GlobalStandard SKU) in the target region (default: **swedencentral**).

## 1. Clone the repository

```bash
git clone https://github.com/marymacgregorreid/marginalia.git
cd marginalia
```

## 2. Authenticate

Sign in to both the Azure CLI and Azure Developer CLI:

```bash
az login
azd auth login
```

## 3. Create an environment

Create a new `azd` environment. This stores your deployment configuration (subscription, region, resource group):

```bash
azd env new <env-name>
```

When prompted, select your Azure subscription and target region. The default region is **swedencentral**.

## 4. Deploy

Provision infrastructure and deploy the application:

```bash
azd up
```

This single command will:

1. Provision all Azure resources via Bicep templates
1. Build the .NET API and React frontend
1. Deploy the API to Azure Container Apps
1. Deploy the frontend to Azure Static Web Apps
1. Configure service connections and environment variables

### What gets provisioned

| Resource | Type | Purpose |
| --- | --- | --- |
| `rg-<env-name>` | Resource Group | Contains all Marginalia resources |
| Container App | Azure Container Apps | Hosts the Marginalia API |
| Static Web App | Azure Static Web Apps | Hosts the React frontend |
| AI Foundry (AIServices) | Azure AI Services | Provides `gpt-5.3-chat` chat model deployment |

### Access the deployed app

After `azd up` completes, the CLI displays the deployed URLs:

```text
Deploying services (azd deploy)

  (✓) Done: Deploying service api
  - Endpoint: https://api.<env-name>.<region>.azurecontainerapps.io

  (✓) Done: Deploying service frontend
  - Endpoint: https://<static-web-app-name>.azurestaticapps.net
```

Open the frontend endpoint in your browser to start using Marginalia.

## Model configuration

The deployment provisions these AI model deployments by default:

| Deployment | Model | Version | SKU | Capacity |
| --- | --- | --- | --- | --- |
| `foundry` | `gpt-5.3-chat` | `2026-03-03` | GlobalStandard | 50 |

To override the model configuration, set environment variables before deploying:

```bash
azd env set MicrosoftFoundry__modelName "gpt-4o"
azd env set MicrosoftFoundry__modelVersion "2026-03-03"
azd up
```

## Update and redeploy

After making code changes, redeploy with:

```bash
azd deploy
```

To update infrastructure (Bicep template changes):

```bash
azd provision
```

To update everything (infrastructure + code):

```bash
azd up
```

## Tear down

Remove all Azure resources created by the deployment:

```bash
azd down
```

> **Warning:** This permanently deletes all resources in the `rg-<env-name>` resource group, including any data stored in the application.

To force deletion without confirmation:

```bash
azd down --force --purge
```

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `azd` command not found | Install Azure Developer CLI: `winget install Microsoft.Azd` (Windows) or see [install docs](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) |
| Quota error during provisioning | Ensure your subscription has `gpt-5.3-chat` GlobalStandard quota in the target region. Try `swedencentral` or another region with available capacity. |
| `azd auth login` fails | Run `az login` first, then retry `azd auth login`. Ensure your account has Contributor access to the target subscription. |
| Deployment times out | AI Foundry model deployments can take several minutes. Re-run `azd up` — it will resume from where it left off. |
| Frontend can't reach API | Check that the Container App is running in the Azure Portal. Verify environment variables are set correctly with `azd env get-values`. |

## Next steps

- **Local development** — see [Local Development with Aspire](QUICKSTART-LOCAL.md) for running locally.
- **Architecture** — read the [PRD](../PRD.md) for product requirements and design context.
