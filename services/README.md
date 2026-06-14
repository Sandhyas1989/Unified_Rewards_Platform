# Unified Rewards — Microservices (Phase 0 foundation)

Microservices evolution of the modular monolith, per the Requirement_Document (7 backend services
behind an API gateway). Built to run **with zero local installs** — only the already-present .NET SDK.

## What's here
- `gateway/UnifiedRewards.Gateway` — **YARP** reverse proxy (local stand-in for **Azure API Management**), port **5080**.
- `employee-profile/UnifiedRewards.EmployeeProfile` — first extracted service (reference template), port **5101**, own SQLite DB (`database-per-service`).
- `employee-profile/UnifiedRewards.EmployeeProfile/Dockerfile`, `employee-profile/k8s/*.yaml` — production artifacts (built/applied in the **cloud**: Codespaces / Azure Cloud Shell / CI — *not* on the dev laptop).

## Run locally (no Docker, no installs)
Open two terminals from the repo root:

```
dotnet run --project services/employee-profile/UnifiedRewards.EmployeeProfile   # http://localhost:5101
dotnet run --project services/gateway/UnifiedRewards.Gateway                    # http://localhost:5080
```

Then:
```
curl http://localhost:5080/health
curl http://localhost:5080/api/employees                 # routed through the gateway to the service
curl "http://localhost:5080/api/employees?page=1&pageSize=1"
curl -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" http://localhost:5080/api/employees
```

Each new service is added the same way: a new `dotnet` project on its own port + a route entry in the
gateway's `appsettings.json` `ReverseProxy` section.

## Local vs cloud (matches the migration plan)
| Concern | Local (zero-install) | Production (Azure) |
|---|---|---|
| Run services | `dotnet run` processes | Containers on **AKS** |
| Gateway | YARP | **API Management** |
| Messaging | in-process / HTTP / SQLite outbox | **Azure Service Bus** |
| Data | SQLite per service | Azure SQL / Cosmos per service |
| Identity | dev JWT | **Microsoft Entra ID** |
| Build images / deploy | (not needed locally) | GitHub Actions / Codespaces / Cloud Shell |

## Services & ports
| Service | Port | Route (via gateway) |
|---|---|---|
| Gateway (YARP) | 5080 | — |
| Employee Profile (EF+SQLite) | 5101 | /api/employees |
| Benefits Catalogue | 5102 | /api/benefit-plans |
| Compensation Rules | 5103 | /api/compensation |
| Reimbursement Workflow | 5104 | /api/claims |
| Document & Receipt Processing | 5105 | /api/documents |
| Payroll Integration | 5106 | /api/settlements |
| Reporting & Compliance | 5107 | /api/reports |

Run each with: `dotnet run --project <path> --no-launch-profile` (the `--no-launch-profile` flag is
required so the scaffolded launchSettings random port doesn't override the fixed dev port).

## Status
All 7 services + gateway built and verified end-to-end (routing, pagination, tenant isolation) — zero installs.
Employee Profile demonstrates the EF/SQLite database-per-service pattern; the other six use seeded in-memory
data in this skeleton and would each own a database the same way. Next: deepen each service with the real
business logic ported from the monolith; add the Service Bus event backbone (config-gated, as in the
monolith's P0 work); wire the micro-frontends to call the gateway.
