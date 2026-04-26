# Quickstart: Local Development with Aspire

Get Marginalia running locally using .NET Aspire. Aspire orchestrates all services (API, frontend, Azure AI Foundry) with a single command — including dependency installation, builds, and service startup.

> **Looking to deploy to Azure?** See [Deploy to Azure with Azure Developer CLI](QUICKSTART-AZURE.md).

## Prerequisites

### .NET SDK 10.0

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later.

```bash
dotnet --version
# Expected: 10.0.x or later
```

### Aspire CLI

Install the Aspire CLI as a .NET global tool:

```bash
dotnet tool install --global Microsoft.Aspire.Cli --prerelease
```

Verify the installation:

```bash
aspire --version
```

### Node.js and pnpm

Install [Node.js 22 LTS](https://nodejs.org/) or later, then enable pnpm:

```bash
corepack enable
corepack prepare pnpm@latest --activate
pnpm --version
```

### Azure CLI

Install the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and sign in:

```bash
az login
```

### Azure subscription

You need an active Azure subscription with access to deploy Azure AI Foundry resources. The default region is **swedencentral** — ensure your subscription has quota for `gpt-5.3-chat` (GlobalStandard SKU) in that region.

## 1. Clone the repository

```bash
git clone https://github.com/marymacgregorreid/marginalia.git
cd marginalia
```

## 2. Configure Azure credentials

Aspire provisions Azure AI Foundry resources on your behalf. You need to configure your Azure subscription and tenant so Aspire knows where to create them.

Set user secrets for the AppHost project:

```bash
cd marginalia-service/src/Orchestration/AppHost

dotnet user-secrets set "Azure:SubscriptionId" "<your-subscription-id>" \
  --project src/Orchestration/AppHost
dotnet user-secrets set "Azure:TenantId" "<your-tenant-id>" \
  --project src/Orchestration/AppHost

cd ../../../..
```

> **Tip:** Find your subscription and tenant IDs with `az account show --query "{subscriptionId:id, tenantId:tenantId}"`.

## 3. Run the app

From the repository root, start Aspire:

```bash
cd marginalia-service/src/Orchestration/AppHost
aspire run
```

Aspire will:

1. Install frontend dependencies (`pnpm install`)
1. Build the .NET API project
1. Provision Azure Microsoft Foundry resource with Foundry Project.
1. Deploy a `gpt-5.3-chat` model to the provisioned Foundry Project.
1. Start the API and frontend dev server.
1. Open the Aspire Dashboard

First run takes several minutes while Azure AI Foundry resources are provisioned. Subsequent runs are much faster.

### Default ports

| Service           | URL                         |
| ----------------- | --------------------------- |
| Aspire Dashboard  | `https://localhost:17280`   |
| API (HTTP)        | `http://localhost:5279`     |
| API (HTTPS)       | `https://localhost:7022`    |
| Frontend (Vite)   | `http://localhost:5173`     |

## 4. First usage walkthrough

1. **Open the Aspire Dashboard** — the URL is shown in the terminal output when Aspire starts (typically `https://localhost:17280`).
1. **Find the frontend URL** — in the dashboard, locate the `frontend` resource and click its endpoint link (default: `http://localhost:5173`).
1. **Upload a document** — upload a `.docx` file or paste text directly into the editor.
1. **Configure AI model settings** — if you're not using the Aspire-provisioned model, update your endpoint and API key in the UI.
1. **Click "Analyze"** — Marginalia sends your text to the AI model and returns suggestions for improving your narrative flow.

## Model configuration

Aspire provisions these AI model deployments by default:

| Deployment | Model | Version | SKU | Capacity |
| --- | --- | --- | --- | --- |
| `foundry` | `gpt-5.3-chat` | `2026-03-03` | GlobalStandard | 50 |

### Override model settings

You can change the model name and version via environment variables or user secrets:

| Setting | Environment variable | Default |
| --- | --- | --- |
| Model name | `MicrosoftFoundry__modelName` | `gpt-5.3-chat` |
| Model version | `MicrosoftFoundry__modelVersion` | `2026-03-03` |

Set overrides with user secrets:

```bash
cd marginalia-service/src/Orchestration/AppHost
dotnet user-secrets set MicrosoftFoundry:modelName "gpt-5.3-chat"
dotnet user-secrets set MicrosoftFoundry:modelVersion "2026-03-03"
```

Or with environment variables:

```bash
export MicrosoftFoundry__modelName="gpt-5.3-chat"
export MicrosoftFoundry__modelVersion="2026-03-03"
```

## Access code protection

When running in single-user mode (without Entra ID authentication), you can optionally protect the application with an access code. When configured, the frontend displays an access code dialog on first load and the backend API rejects requests without a valid `X-Access-Code` header.

When the access code is not set or empty, the application is fully open with no access gate — this is the default behavior.

### Configure via appsettings

In `marginalia-service/src/Api/appsettings.json` (or `appsettings.Development.json`):

```json
{
  "AccessControl": {
    "AccessCode": "your-access-code-here"
  }
}
```

### Configure via environment variable

```bash
export ACCESS_CODE="your-access-code-here"
```

Or on Windows:

```powershell
$env:ACCESS_CODE = "your-access-code-here"
```

### Configure via user secrets

```bash
cd marginalia-service/src/Api
dotnet user-secrets set AccessControl:AccessCode "your-access-code-here"
```

### Configure for production deployment

When deploying to Azure via GitHub Actions, add an `ACCESS_CODE` secret to your GitHub repository (or the `prod` environment). If the secret is set, the CD pipeline passes it through to the Azure Container App as an environment variable. If the secret is not set, the application runs without access code protection.

1. Go to your GitHub repository **Settings** > **Secrets and variables** > **Actions**.
1. Click **New repository secret** (or add to the `prod` environment).
1. Set **Name** to `ACCESS_CODE` and **Value** to your desired access code.

The secret flows through the workflow chain: `continuous-delivery.yml` → `deploy-production.yml` → `provision-infrastructure.yml` → Bicep `accessCode` parameter → Container App `ACCESS_CODE` environment variable.

### How it works

1. The frontend checks `GET /api/config/access-status` on startup to determine if an access code is required.
1. If required, a modal dialog prompts for the access code before allowing access.
1. The access code is stored in the browser's session storage (cleared when the tab closes).
1. All API requests include the access code in the `X-Access-Code` header.
1. Health endpoints (`/health`, `/alive`) and the access status endpoint are not protected.

> **Note:** This is a lightweight access gate for single-user deployments, not a high-security authentication mechanism. For production multi-user deployments, use Entra ID authentication.

## Run tests

### Backend

```bash
cd marginalia-service
dotnet test Marginalia.slnx
```

### Frontend

```bash
cd marginalia-app
pnpm test
```

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `aspire` command not found | Install with `dotnet tool install --global Microsoft.Aspire.Cli --prerelease` |
| Azure provisioning fails with quota error | Ensure your subscription has `gpt-5.3-chat` GlobalStandard quota in `swedencentral`. Try a different region by setting `Azure__Location` in launchSettings. |
| `pnpm: command not found` | Run `corepack enable && corepack prepare pnpm@latest --activate` |
| Frontend shows connection errors | Ensure the API is running — check the Aspire Dashboard for service health. The frontend waits for the API via `WaitFor`. |
| Port conflicts | Another process is using the default ports. Stop conflicting processes or update `launchSettings.json` in the AppHost and API projects. |
| `Azure:SubscriptionId` not set | Set user secrets as described in step 2. Aspire needs these to provision AI Foundry resources. |

## Next steps

- **Deploy to Azure** — see [Deploy to Azure with Azure Developer CLI](QUICKSTART-AZURE.md) (coming soon).
- **Architecture** — read the [PRD](../PRD.md) for product requirements and design context.
