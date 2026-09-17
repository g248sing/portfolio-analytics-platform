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
- **CI/CD:** GitHub Actions → Render (API + frontend) + Neon (Postgres), free tier

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
- **CD** (`.github/workflows/cd.yml`): runs on push to `main` — re-runs the full test suite,
  and only if that passes, calls two Render deploy hooks (plain webhook URLs, no stored
  cloud credential) to trigger the actual deploys. This needs the one-time manual setup below.

## Deploying to Render + Neon (one-time setup, free)

Render has no native Postgres-as-a-service tier worth using here, so the database lives on
Neon (serverless Postgres, generous free tier) while Render hosts the API (as the existing
Docker image) and the frontend (as a static site, no container needed for that half).
`render.yaml` in the repo root is a Render "Blueprint" — it describes both services so you
provision them in one step instead of clicking through the dashboard twice.

Free-tier caveats worth knowing going in: Render's free web services spin down after 15
minutes of no traffic (the next request wakes it back up, taking 30-60s), and Neon's free
compute similarly suspends after a few minutes of inactivity. Fine for a portfolio/demo
project; not something you'd want for anything latency-sensitive. Also: platform free-tier
terms change, and so does the exact `render.yaml` field syntax (`runtime: docker` below is
current as of when this was written but Render has renamed these fields before) — this
config was written from documentation, not verified against a real Render account, so if
the Blueprint step rejects a field name, check
[Render's Blueprint spec](https://render.com/docs/blueprint-spec) for the current name and
let me know so I can fix it here too.

### 1. Create the Neon database

1. Sign up at [neon.tech](https://neon.tech) (no credit card required for the free tier) and create a project.
2. In the project dashboard, copy the connection string it gives you — it looks like
   `postgresql://<user>:<password>@<endpoint>.neon.tech/<dbname>?sslmode=require`.
3. Convert it to the ADO.NET format the API expects (same values, different syntax):
   ```
   Host=<endpoint>.neon.tech;Port=5432;Database=<dbname>;Username=<user>;Password=<password>;Ssl Mode=Require;Trust Server Certificate=true
   ```
   Use the **pooled** connection string Neon shows (not "direct") — it's the one meant for a normal running app.

### 2. Deploy the Blueprint on Render

1. Sign up at [render.com](https://render.com) and connect your GitHub account.
2. New → Blueprint → pick this repo. Render reads `render.yaml` and proposes two services:
   `portfolio-analytics-api` (Docker web service) and `portfolio-analytics-web` (static site).
3. Before the first deploy, it'll prompt for the env vars marked `sync: false` in
   `render.yaml`. Fill in:
   - **API service** — `ConnectionStrings__Default` (from step 1), `Jwt__SigningKey`
     (generate one: `openssl rand -base64 48`, don't reuse the dev key committed in this
     repo), `AlphaVantage__ApiKey`, and `Cors__AllowedOrigins__0` (the frontend service's
     URL — Render shows you the assigned `https://portfolio-analytics-web.onrender.com`-style
     URL before you finish, or you can add this one after both services exist)
   - **Frontend service** — `VITE_API_BASE_URL` set to the API service's URL + `/api`
     (e.g. `https://portfolio-analytics-api.onrender.com/api`) — this is baked in at build
     time, so if you ever change the API's URL you need to redeploy the frontend, not just
     the API
4. Both services in `render.yaml` are set `autoDeploy: false` — Render won't redeploy on
   every push by itself. Deploys are meant to be triggered by `cd.yml` after tests pass
   (step 4 below), not directly by Render watching the branch.

### 3. Grab the deploy hook URLs

For each service in the Render dashboard: Settings → Deploy Hook → copy the URL. These are
bearer-token-in-the-URL webhooks — treat them as secrets.

### 4. GitHub repository configuration

Under **Settings → Secrets and variables → Actions → Secrets**, add:

| Name                     | Value                                    |
|--------------------------|-------------------------------------------|
| `RENDER_DEPLOY_HOOK_API` | the API service's deploy hook URL          |
| `RENDER_DEPLOY_HOOK_WEB` | the frontend service's deploy hook URL     |

### 5. Ship it

Push to `main`. `cd.yml` runs the full test suite, and only on success, hits both deploy
hooks — Render then pulls the latest commit and rebuilds each service itself (it builds the
Docker image for the API and runs the static site build for the frontend; GitHub Actions
never builds or pushes any image here, unlike a registry-based deploy).

**Known gaps:** migrations still run automatically on API startup (fine at this scale, not
fine with multiple replicas — moot here since Render's free tier is single-instance anyway);
and cold starts mean the first request after idle time will be slow on both the API and the
database — don't be alarmed if a demo link takes a few seconds to wake up.
