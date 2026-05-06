# Quickstart: Deploy to Azure

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
git clone https://github.com/PlagueHO/marginalia.git
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

## Environment configuration

All infrastructure parameters are read from `azd` environment variables and passed to `infra/main.bicepparam` at provisioning time. Use `azd env set <KEY> <VALUE>` to configure any of these before running `azd up` or `azd provision`.

### Required

These values are set automatically by `azd` and do not need to be configured manually.

| Variable | Description |
| --- | --- |
| `AZURE_ENV_NAME` | Environment name; used as a prefix for all resource names |
| `AZURE_LOCATION` | Primary Azure region (default: `EastUS2`) |
| `AZURE_PRINCIPAL_ID` | Object ID of the user or service principal running the deployment |
| `AZURE_PRINCIPAL_ID_TYPE` | `User` or `ServicePrincipal` (default: `User`) |

### Optional

| Variable | Default | Description |
| --- | --- | --- |
| `AZURE_LOCATION` | `EastUS2` | Azure region for all resources |
| `AZURE_STATIC_WEB_APP_LOCATION` | *(same as primary)* | Override region for the Static Web App. Must be one of: `centralus`, `eastasia`, `eastus2`, `westeurope`, `westus2` |
| `AZURE_CONTAINER_APP_IMAGE` | `ghcr.io/plagueho/marginalia-service:latest` | Container image to deploy to the backend Container App |
| `ENABLE_PUBLIC_NETWORK_ACCESS` | `true` | Set to `false` to restrict all resources to private network access only |

### Authentication

| Variable | Default | Description |
| --- | --- | --- |
| `ACCESS_CODE` | *(empty)* | Optional access code for single-user deployments. Leave empty for Anonymous mode |
| `ENABLE_ENTRA_AUTH` | `false` | Set to `true` to enable Entra ID multi-user authentication |
| `AZURE_AD_API_CLIENT_ID` | *(set by preprovision hook)* | API app registration client ID; written automatically when `ENABLE_ENTRA_AUTH=true` |
| `AZURE_AD_SPA_CLIENT_ID` | *(set by preprovision hook)* | SPA app registration client ID; written automatically when `ENABLE_ENTRA_AUTH=true` |

See [Authentication](./AUTHENTICATION.md) for full details on each authentication mode and how to configure it.

## Authentication

Marginalia supports three authentication modes for Azure deployments. Configure the appropriate variables before running `azd up`:

### Anonymous mode (default)

No additional configuration is needed. All requests are accepted without an access check and attributed to the `_anonymous` user:

```bash
azd up
```

### Access Code mode

Protect the deployment with a shared password. All requests must include the correct code:

```bash
azd env set ACCESS_CODE "your-access-code"
azd up
```

### Entra ID mode

Enable multi-user authentication backed by your Entra ID tenant. The pre-provision hook creates the required app registrations automatically:

```bash
azd env set ENABLE_ENTRA_AUTH true
azd up
```

The hook requires the `Application.ReadWrite.All` Microsoft Graph permission on the deploying identity.

For detailed instructions on each mode, see [Authentication](./AUTHENTICATION.md).

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
- **Authentication** — see [Authentication](./AUTHENTICATION.md) for detailed configuration of Anonymous, Access Code, and Entra ID modes.
- **Architecture** — read the [PRD](./design/PRD.md) for product requirements and design context.
