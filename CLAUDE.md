# CLAUDE.md — Project Instructions (Unified Rewards Platform)

> Common conventions, commands, and rules for this repo. Read this first; follow it always.
> Step-by-step runbooks (deploy, demo, tests) live under `docs/` for "specific-time" use — this file is the always-on guidance.

---

## 1. What this is

**Unified Rewards Platform (URP)** — an enterprise HR-rewards system: employees submit reimbursement claims & enrol in benefits; managers approve; finance runs payroll/settlements; HR manages plans; everything is captured in an immutable audit trail.

- **7 .NET 8 microservices** + a **YARP API gateway**, behind **React** micro-front-ends for 4 roles.
- Runs **fully on Azure** (Container Apps), and **fully offline on a laptop** (SQLite) from the *same codebase*.

| Service (`services/<dir>`) | Owns |
|---|---|
| `employee-profile` | Identity, users, login/JWT, promotions |
| `reimbursement-workflow` | Expense claims lifecycle |
| `document-processing` | Receipts / OCR |
| `benefits-catalogue` | Benefit plans & enrolments |
| `compensation-rules` | Pay/compensation rules engine |
| `payroll-integration` | Payroll & settlements |
| `reporting-compliance` | Audit trail, cross-service reports, Excel export |
| `gateway` | Single entry point (YARP) — routing + auth offload |

---

## 2. Repository layout

```
services/            7 microservices + gateway + shared + tests   (services/UnifiedRewards.Services.sln)
frontend/            shell + portals/{employee,manager,hr,finance} + shared   (5 apps; deployed consolidated)
functions/           Azure Functions (isolated worker): Blob:OCR, Timer:Reports, Queue:Payroll
infra/               Bicep IaC — main-rg.bicep (sandbox, RG-scoped), prod/ (AKS topology)
k8s/                 Kubernetes manifests for the production AKS deployment
docs/                HLD/LLD, diagrams, deployment + demo + test guides
```

---

## 3. Build, run & test (local)

The .NET 8 SDK is **user-local** in `~/.dotnet`. Always export it first:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
"$HOME/.dotnet/dotnet" build services/UnifiedRewards.Services.sln      # build all backend
"$HOME/.dotnet/dotnet" test  services/UnifiedRewards.Services.sln      # run tests
"$HOME/.dotnet/dotnet" build functions/UrpFunctions/UrpFunctions.csproj
```

- `global.json` pins SDK **8.0.421** (`rollForward: latestFeature`). Keep it; it guards reproducible builds.
- Run a service locally: `cd services/<dir>/<Project> && "$HOME/.dotnet/dotnet" run` (uses SQLite, no cloud needed).
- Frontend: `cd frontend/shell && npm install && npm start` (or `npm run build`).

---

## 4. Architecture conventions — MUST follow

1. **Database-per-service.** Each service owns its own database; **no cross-database joins or foreign keys.** Relate data across services only by (a) storing the other entity's **id** as a plain value, (b) **API calls**, or (c) **Service Bus events**. Never query another service's tables.
2. **Inter-service comms = event-driven.** Publish domain events to **Service Bus** (transactional-outbox pattern); don't chain synchronous calls between services. Synchronous traffic goes through the gateway.
3. **Per-user reads are scoped by JWT identity, never by a client-supplied id.** Use the `sub` claim (`CurrentUserId`), not a query-param `employeeId`/`userId`. (See `/api/payslips/me`, `/api/claims/me`.)
4. **Conditional data providers — one codebase, two environments.** Select the provider from configuration, defaulting to SQLite:
   ```csharp
   var sql = builder.Configuration.GetConnectionString("Sql");
   var cosmos = builder.Configuration.GetConnectionString("Cosmos");
   if (!string.IsNullOrWhiteSpace(cosmos)) o.UseCosmos(cosmos, "urp");   // benefits only
   else if (!string.IsNullOrWhiteSpace(sql)) o.UseSqlServer(sql);
   else o.UseSqlite($"Data Source={dbPath}");                            // local fallback
   ```
5. **Cosmos query rules** (the EF Cosmos provider is limited — keep queries translatable):
   - No bare `.Any()` / `.Count()` aggregates → use `.AsEnumerable().Any()` or fetch + check client-side.
   - No offset paging (`Skip/Take`) or enum-cast predicates server-side → fetch by partition key (`TenantId`), then filter/sort/page in memory (collections are small & per-tenant).
   - No global query filter using nullable `_tenantId.HasValue` on Cosmos — controllers filter by `TenantId` explicitly.
6. **DB init is idempotent and non-fatal.** Relational: `IRelationalDatabaseCreator` (`Exists`/`Create`/`HasTables`/`CreateTables`). Cosmos: `EnsureCreated()`. Wrap init/seed in try/catch so a transient store issue never crashes startup.
7. **Inter-service URLs must be `https://`.** Container Apps redirect `http→https` (301) and the redirect **drops the Authorization header** → 401. Configure `Services__*` with the `https` FQDN.
8. **Multi-tenant:** every entity carries `TenantId`; all queries filter by the JWT `tenant_id` claim.

---

## 5. Auth & demo accounts

- **Self-issued RS256 JWT** (baked RSA keypair in `appsettings.json` under `Jwt`; **not** Entra in this build). The **same public key must be in every service** — a one-char mismatch = 401 everywhere.
- Roles enum: **Employee=0, Manager=1, HrAdmin=2, Finance=3**; endpoints use `[Authorize(Roles="…")]`.
- **No `/register` endpoint.** Four seeded demo users, all password **`Password123!`**:

  `employee@urp.local` · `manager@urp.local` · `hr@urp.local` · `finance@urp.local`

---

## 6. Data tier (which store each service uses)

| Store | Used by |
|---|---|
| **Azure SQL** (`urptest20161`, one DB per service) | the 6 relational services |
| **Cosmos DB** (`urpcosmos7074`, db `urp`) | **benefits-catalogue only** |
| **Blob Storage** (`urpreceipts`) | receipt/document files |
| **Redis** (`urpredis7074`) | **provisioned, NOT wired** (no caching code yet) |
| SQLite (local) | everything, when no connection string is set |

---

## 7. Azure deployment (sandbox)

Resource group: **`rg-azuser7074_mml.local-eJJJX`** · region centralindia. AKS is blocked in the Learn sandbox, so compute runs on **Azure Container Apps** (same images deploy to AKS in prod — see `infra/prod/`, `k8s/`).

**Standard deploy loop** (from repo root, in Cloud Shell):
```bash
RG="rg-azuser7074_mml.local-eJJJX"
az acr build -r urpacr -t <service>:<tag> -f services/<service>/Dockerfile .
az containerapp update -g "$RG" -n urp-<service> --image urpacr.azurecr.io/<service>:<tag> [--set-env-vars KEY=val ...]
```

Key resources: ACR `urpacr` · env `urp-env` · 8 apps `urp-<service>` + `urp-gateway` · SQL `urptest20161` · Cosmos `urpcosmos7074` · Service Bus `urp-bus` · Event Hub `audit-stream` · ACS `urp-acs`+`urp-email` · Functions `urp-func7074` · App Insights + Log Analytics `urp-logs` · Key Vault `urp-kv` · Static Web App `urp-frontend`.

**Deploy-time env conventions:**
- `ASPNETCORE_ENVIRONMENT=Azure` on every service (skips the placeholder-Entra `Production.json`).
- SQLite path on `EmptyDir`, never Azure Files (avoids "database is locked").
- Connection strings as env vars: `ConnectionStrings__Sql`, `ConnectionStrings__Cosmos`, `ConnectionStrings__EventHub`, `ConnectionStrings__Acs`; observability via `APPLICATIONINSIGHTS_CONNECTION_STRING`; inter-service via `Services__<Name>` (https FQDN).

---

## 8. Azure Cloud Shell gotchas

- **`echo "RG=$RG"` first whenever a command errors weirdly.** A fresh/ephemeral Cloud Shell wipes shell variables — an empty `$RG` produces misleading `AuthorizationFailed`/`ResourceNotFound`/`-n ''` errors.
- **Cloud Shell has only .NET 9**, but `global.json` pins 8.0.421 → builds fail. Move it aside for the build: `mv global.json /tmp/gj.bak ; dotnet publish … ; mv /tmp/gj.bak global.json` (the .NET 9 SDK builds the `net8.0` projects fine).
- **`func` and `zip` may be missing.** Deploy Functions via zip instead: `dotnet publish` → `python3 -c "import shutil; shutil.make_archive('/tmp/app','zip','.')"` → `az functionapp deployment source config-zip`.
- Multi-line `az` commands with `\` continuations often break on paste — use single-line commands.
- Cloud Shell is storage-less (ephemeral): `git clone` the repo and re-set `RG` each new session.

---

## 9. Coding conventions

- Match the style of the surrounding file (naming, comment density, idiom).
- Backward-compatible by default: new cloud integrations are **no-ops unless their connection string is configured** (preserves local/dev behavior).
- New service? Follow the conditional-provider + idempotent-init + JWT/tenant patterns above.
- Compile-check before declaring done: `"$HOME/.dotnet/dotnet" build …` (or `test`).

---

## 10. Where things live

- **Test cases:** `docs/UI_Test_Cases_and_E2E_Flows.md` (E2E flows + per-module positive/negative cases).
- **Deployment runbooks:** `docs/Azure_Production_Deployment_Guide.md`, `docs/Azure_Production_IaC_Deployment_Steps.md`, `docs/Deployment_and_Testing_Guide.md`.
- **Review demo script:** `docs/Review_Demo_Presentation_Guide.md`.
- **Architecture:** `docs/Diagrams/`, HLD/LLD under `docs/`.
