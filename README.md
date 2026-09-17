# Portfolio Analytics Platform

A full-stack investment portfolio tracker: an ASP.NET Core API with a FIFO cost-basis
engine, a scheduled job that pulls daily prices from Alpha Vantage, and a React
dashboard with charts and CSV/Excel export.

## Stack

- **API:** ASP.NET Core 10, EF Core + Npgsql, ASP.NET Core Identity + JWT auth, Quartz.NET
- **Database:** PostgreSQL
- **Frontend:** React + TypeScript (Vite), TanStack Query, Recharts
- **Market data:** [Alpha Vantage](https://www.alphavantage.co/) (free tier)
- **Containers:** Docker, docker-compose for local dev
- **CI/CD:** GitHub Actions → GHCR → Azure App Service for Containers

## Repository layout

```
src/
  Portfolio.Api/             REST API host, controllers, auth, DI wiring
  Portfolio.Application/     DTOs, validators, service interfaces
  Portfolio.Domain/          Entities, enums, pure domain logic (FIFO engine, snapshot calculator)
  Portfolio.Infrastructure/  EF Core, Identity, Alpha Vantage client, Quartz jobs
tests/
  Portfolio.UnitTests/           xUnit — Domain/Application logic, mocked HTTP
  Portfolio.IntegrationTests/    xUnit + Testcontainers — full API against real Postgres
frontend/                    React app
```

## Running locally with Docker

1. Copy `.env.example` to `.env` and fill in a real `ALPHA_VANTAGE_API_KEY`
   (free at <https://www.alphavantage.co/support/#api-key>).
2. `docker compose up --build`
3. API: <http://localhost:8080/swagger> (Development only) · Frontend: <http://localhost:8081>

## Running locally without Docker

```bash
docker compose up -d postgres        # just the database
dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api
dotnet run --project src/Portfolio.Api

cd frontend
npm install
npm run dev
```

The API reads `src/Portfolio.Api/appsettings.Development.json` for local config, including
a placeholder Alpha Vantage key — replace it with a real one to see live price data.

## Tests

```bash
dotnet test tests/Portfolio.UnitTests
dotnet test tests/Portfolio.IntegrationTests   # needs a running Docker daemon (Testcontainers)

cd frontend && npm run lint && npm run test && npm run build
```

## CI/CD

- **CI** (`.github/workflows/ci.yml`): runs on every push/PR to `main` — backend build +
  unit + integration tests, frontend lint/test/build, and a Docker build validation pass.
- **CD** (`.github/workflows/cd.yml`): runs on push to `main` — builds and pushes both
  images to GHCR, then deploys to Azure App Service for Containers via OIDC (no stored
  Azure secret). This needs one-time manual setup below.

## Deploying to Azure (one-time setup)

This deliberately isn't automated with Bicep/ARM yet — for a project built solo and
incrementally, getting a manual deploy working first is the more honest v1 than IaC that's
never been exercised. The commands below are what `cd.yml` assumes exists.

Prerequisites: an Azure subscription and the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli), logged in (`az login`).

### 1. Resource group and Postgres

```bash
RG=portfolio-analytics-rg
LOCATION=eastus

az group create --name $RG --location $LOCATION

az postgres flexible-server create \
  --resource-group $RG \
  --name portfolio-analytics-db \
  --location $LOCATION \
  --admin-user portfolioadmin \
  --admin-password "<choose-a-strong-password>" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16 \
  --public-access 0.0.0.0-255.255.255.255   # tighten this to App Service's outbound IPs afterward

az postgres flexible-server db create \
  --resource-group $RG \
  --server-name portfolio-analytics-db \
  --database-name portfolio
```

### 2. App Service plan and the two web apps

```bash
az appservice plan create \
  --resource-group $RG \
  --name portfolio-analytics-plan \
  --is-linux \
  --sku B1

az webapp create \
  --resource-group $RG \
  --plan portfolio-analytics-plan \
  --name portfolio-analytics-api \
  --deployment-container-image-name mcr.microsoft.com/appsvc/staticsite:latest   # placeholder until first CD run

az webapp create \
  --resource-group $RG \
  --plan portfolio-analytics-plan \
  --name portfolio-analytics-web \
  --deployment-container-image-name mcr.microsoft.com/appsvc/staticsite:latest
```

Names must be globally unique — adjust `portfolio-analytics-*` if taken. Note the two
hostnames (`https://portfolio-analytics-api.azurewebsites.net`, `...-web...`) — you'll need
them below.

### 3. App settings for the API

```bash
az webapp config appsettings set \
  --resource-group $RG \
  --name portfolio-analytics-api \
  --settings \
    ConnectionStrings__Default="Host=portfolio-analytics-db.postgres.database.azure.com;Port=5432;Database=portfolio;Username=portfolioadmin;Password=<the-password-above>;SSL Mode=Require;Trust Server Certificate=true" \
    Jwt__Issuer="PortfolioAnalytics" \
    Jwt__Audience="PortfolioAnalytics" \
    Jwt__SigningKey="<generate-a-real-random-secret>" \
    AlphaVantage__ApiKey="<your-alpha-vantage-key>" \
    Cors__AllowedOrigins__0="https://portfolio-analytics-web.azurewebsites.net" \
    ASPNETCORE_ENVIRONMENT="Production" \
    WEBSITES_PORT="8080"
```

Generate a real signing key rather than reusing the dev one committed in this repo, e.g.
`openssl rand -base64 48`.

### 4. Azure AD app registration for GitHub OIDC

```bash
APP_ID=$(az ad app create --display-name "portfolio-analytics-github-actions" --query appId -o tsv)
az ad sp create --id $APP_ID

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
az role assignment create \
  --assignee $APP_ID \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG

az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-main-branch",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<your-github-org-or-user>/<your-repo>:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### 5. GitHub repository configuration

Under **Settings → Secrets and variables → Actions**:

| Type     | Name                     | Value                                                    |
|----------|--------------------------|-----------------------------------------------------------|
| Secret   | `AZURE_CLIENT_ID`        | `$APP_ID` from step 4                                     |
| Secret   | `AZURE_TENANT_ID`        | `az account show --query tenantId -o tsv`                 |
| Secret   | `AZURE_SUBSCRIPTION_ID`  | `$SUBSCRIPTION_ID` from step 4                             |
| Variable | `AZURE_API_APP_NAME`     | `portfolio-analytics-api`                                  |
| Variable | `AZURE_WEB_APP_NAME`     | `portfolio-analytics-web`                                  |
| Variable | `API_PUBLIC_URL`         | `https://portfolio-analytics-api.azurewebsites.net`        |

### 6. Make the GHCR images pullable

The first `cd.yml` run pushes `ghcr.io/<org>/<repo>-api` and `-web` as **private** packages
by default, and App Service can't pull a private GHCR image without credentials. Either:

- Simplest: after the first CD run, go to the package's GitHub page → Package settings →
  change visibility to **public**, or
- More locked-down: `az webapp config container set --docker-registry-server-url https://ghcr.io --docker-registry-server-user <gh-username> --docker-registry-server-password <a GitHub PAT with read:packages>` on both web apps.

### 7. Ship it

Push to `main`. `cd.yml` builds and pushes both images, then redeploys both Web Apps to
the new image tags. Watch the Actions tab, then hit the web app's URL.

**Known gap:** migrations still run automatically on API container startup (same as
local/CI), which is fine for this project's single-instance scale but would need a
proper migration-as-a-separate-step approach before running multiple API replicas.
