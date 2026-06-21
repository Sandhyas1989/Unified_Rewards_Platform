# Unified Rewards Platform — Azure **Production** Deployment Guide

Step-by-step, click-by-click deployment of the **production architecture shown in the Deployment
Architecture diagram / HLD** (AKS · APIM · Azure SQL · Cosmos DB · Redis · Front Door · Entra ID ·
Event Hub · Functions · Communication Services · Key Vault · private VNet · multi-region).

> This is the **target production** architecture — **not** the same as the Container Apps demo you
> already deployed. Each phase below is tagged so you know what's new vs. familiar:
>
> | Tag | Meaning |
> |---|---|
> | ✅ **DONE IN DEMO** | You created this in the Container Apps deployment — same Portal steps. Reuse the resource if you're on the **same subscription**, or repeat the steps on the production subscription. |
> | ♻️ **PARTIAL** | Partly covered by the demo — needs extending for production. |
> | 🆕 **NEW** | Production-only — not in the demo. |

---

## ⚠️ Read this first — three things that are different from the demo

1. **You need a real, paid Azure subscription — the Microsoft Learn sandbox will NOT work.** Production
   uses AKS, APIM, Front Door, Entra app registrations, multi-region, and a private VNet — all of which
   the sandbox blocks. You also need to be **Owner** of the subscription and an **admin of an Entra
   tenant**.
2. **Code changes are required to actually USE these services.** The current app uses SQLite, a
   self-issued JWT, a YARP gateway, and one merged SPA. To run on Azure SQL/Cosmos/Redis/Entra/APIM you
   must change the app — each place is flagged inline with **[CODE CHANGE]**. This guide provisions the
   **infrastructure**; wiring the app to it is separate work (summarised in the [Code-change checklist](#code-change-checklist)).
3. **For real production, use IaC + CI/CD, not the Portal.** The HLD itself mandates **Bicep + GitHub
   Actions**. ~15 services clicked by hand is not reproducible or auditable. The Portal steps below are
   for understanding and a first build-out; production should be templated. *(Say the word and I'll
   generate the Bicep modules.)*

**Regions used in this guide:** primary **East US 2**, secondary **West US 2** (per the diagram's
multi-region note). Use the same two regions everywhere.

---

## 0. At-a-glance: what you already did vs. what's new

| # | Component (per diagram) | Status | Phase |
|---|---|---|---|
| 1 | Resource Group | ✅ DONE IN DEMO | 1 |
| 2 | Virtual Network + subnets + private DNS | 🆕 NEW | 1 |
| 3 | Entra ID app registration (SSO/MFA, JWT) | 🆕 NEW | 2 |
| 4 | Key Vault | ✅ DONE IN DEMO | 3 |
| 5 | Azure SQL Database (+ geo-replica) | 🆕 NEW | 4 |
| 6 | Cosmos DB (multi-region) | 🆕 NEW | 4 |
| 7 | Azure Cache for Redis (Premium) | 🆕 NEW | 4 |
| 8 | Storage Account + Blob (receipts) | ✅ DONE IN DEMO | 4 |
| 9 | Service Bus (events) | ✅ DONE IN DEMO | 5 |
| 10 | Event Hub (audit stream) | 🆕 NEW | 5 |
| 11 | Container Registry + service images | ✅ DONE IN DEMO | 6 |
| 12 | AKS cluster + 7 service workloads | 🆕 NEW | 7 |
| 13 | API Management (APIM) | 🆕 NEW | 8 |
| 14 | Azure Functions (OCR / timer / payroll) | 🆕 NEW | 9 |
| 15 | Communication Services (email) | 🆕 NEW | 10 |
| 16 | Static Web Apps (frontend) | ♻️ PARTIAL | 11 |
| 17 | Front Door (WAF/CDN) | 🆕 NEW | 11 |
| 18 | App Insights / Azure Monitor | ♻️ PARTIAL (Log Analytics done) | 12 |

---

## Phase 1 — Resource Group & private network

### 1a. Resource group — ✅ DONE IN DEMO
Same as the demo: search **Resource groups** → **+ Create** → name **`urp-prod-rg`** → region
**East US 2** → **Review + create** → **Create**. *(In the demo you named it `urp-rg`; use a clean
prod name here.)*

### 1b. Virtual Network + subnets — 🆕 NEW
The whole production estate sits in a private VNet (the demo's Container Apps used a managed network, so
this is new).
1. Search **Virtual networks** → **+ Create**.
2. **Basics:** Resource group **urp-prod-rg**, Name **`urp-vnet`**, Region **East US 2**.
3. **IP Addresses:** keep `10.0.0.0/16`. Add three subnets (click **+ Add subnet** for each):
   - **`snet-aks`** `10.0.0.0/20` (AKS nodes/pods)
   - **`snet-apim`** `10.0.16.0/24` (APIM VNet integration)
   - **`snet-data`** `10.0.17.0/24` (private endpoints for SQL/Cosmos/Redis/Blob/Key Vault)
4. **Review + create** → **Create**.
5. **Private DNS:** later, when you add Private Endpoints (Phases 3–4), tick **"Integrate with private
   DNS zone"** on each — the Portal creates the `privatelink.*` zones automatically.

---

## Phase 2 — Identity: Entra ID (Azure AD) — 🆕 NEW

> The demo used a **self-issued RSA JWT**. Production uses **Entra ID** for SSO/MFA and JWT validation —
> this is entirely new. **[CODE CHANGE]** every service's `appsettings` must switch to
> `Jwt:Authority = https://login.microsoftonline.com/<tenant>/v2.0` (the `appsettings.Production.json`
> stubs already in the repo are placeholders for exactly this).

1. Search **Microsoft Entra ID** → left menu **App registrations** → **+ New registration**.
2. **API app:** Name **`urp-api`** → Supported accounts: **Single tenant** → **Register**.
3. On **urp-api** → **Expose an API** → **Add a scope** → accept the App ID URI (`api://<id>`) → scope
   name **`access_as_user`** → admin consent → **Add scope**.
4. **App roles** (left menu) → **+ Create app role** for each: **Employee, Manager, HrAdmin, Finance**
   (Allowed member types: Users/Groups; value = role name).
5. **SPA app:** App registrations → **+ New registration** → Name **`urp-spa`** → **Single-page
   application** redirect URI = your Front Door URL (add later) → **Register**. Under **API permissions**
   → **+ Add** → **My APIs** → **urp-api** → `access_as_user` → **Grant admin consent**.
6. **MFA:** Entra ID → **Security** → **Conditional Access** → **+ New policy** → target the URP apps →
   Grant: **Require MFA** → **On**.
7. **Note down:** Tenant ID, `urp-api` Application ID URI, `urp-spa` Client ID. You'll put these in
   Key Vault / APIM / the SPA config.

---

## Phase 3 — Key Vault — ✅ DONE IN DEMO

Same steps as the demo (search **Key vaults** → **+ Create** → RG **urp-prod-rg** → name
**`urp-prod-kv`** → **Review + create**). For production, additionally:
1. **Networking** tab during create → **Private endpoint** → in **snet-data**, integrate with private
   DNS. (Demo used public access.)
2. After create → **Access configuration** → **Azure RBAC**. You'll grant each AKS workload's **Managed
   Identity** the **Key Vault Secrets User** role in Phase 7.
3. You'll store here (later phases): JWT/Entra settings, SQL/Cosmos/Redis/Service Bus/Storage connection
   strings. **[CODE CHANGE]** services read secrets from Key Vault via Managed Identity (the demo baked
   them into images).

---

## Phase 4 — Data tier

### 4a. Azure SQL Database — 🆕 NEW  (holds Users, Claims, Payroll, Reporting)
> **[CODE CHANGE]** EF Core provider `UseSqlite(...)` → `UseSqlServer(...)` with an MSI connection string.
1. Search **SQL databases** → **+ Create**.
2. **Basics:** RG **urp-prod-rg** → DB name **`urp-sql`** → **Create new** server **`urp-sqlsrv`**
   (East US 2) → Authentication: **Use Microsoft Entra-only** (MSI, no passwords).
3. **Compute + storage:** **General Purpose · Serverless** (or Business Critical for zone redundancy) →
   tick **Zone redundant** if offered.
4. **Networking:** **Private endpoint** in **snet-data** (+ private DNS). Disable public access.
5. **Security:** enable **Transparent Data Encryption** (on by default) and **Microsoft Defender for SQL**.
6. **Review + create** → **Create**.
7. **Geo-replica (DR):** open **urp-sqlsrv** → **Replicas** → **+ Create replica** → secondary server in
   **West US 2** → optionally add to a **Failover group**.

### 4b. Cosmos DB — 🆕 NEW  (holds Benefits, Compensation rules)
> **[CODE CHANGE]** Benefits/Compensation services switch their EF/data layer to the Cosmos SDK (or EF
> Core Cosmos provider).
1. Search **Azure Cosmos DB** → **+ Create** → **Azure Cosmos DB for NoSQL**.
2. **Basics:** RG **urp-prod-rg** → Account **`urp-cosmos`** → Region **East US 2** → Capacity mode
   **Provisioned** (or Serverless).
3. **Global Distribution:** turn **Geo-Redundancy ON** and **Multi-region Writes ON** → add **West US 2**.
4. **Networking:** **Private endpoint** in **snet-data**. **Review + create** → **Create**.
5. **Data Explorer** → **New Database** `urp` → containers `benefitPlans`, `compensationRules`.

### 4c. Azure Cache for Redis — 🆕 NEW  (NRules / cache-aside, TTL 15/30/60)
> **[CODE CHANGE]** swap `IMemoryCache` for `IDistributedCache` (StackExchange.Redis).
1. Search **Azure Cache for Redis** → **+ Create**.
2. **Basics:** RG **urp-prod-rg** → DNS name **`urp-redis`** → Region **East US 2** → Cache type
   **Premium P1** (zone-redundant).
3. **Networking:** **Private endpoint** in **snet-data**. **Advanced:** enable zones.
4. **Review + create** → **Create**.

### 4d. Storage Account + Blob (receipts) — ✅ DONE IN DEMO
Same as the demo (search **Storage accounts** → **+ Create** → name **`urpprodstg`** → **Review +
create**), but for production:
1. **Redundancy:** **GRS** (geo-redundant) instead of LRS.
2. **Networking:** **Private endpoint** in **snet-data**.
3. Create blob container **`receipts`** (Containers → **+ Container**). *(In the demo this held receipts
   too — same purpose.)*

---

## Phase 5 — Messaging

### 5a. Service Bus — ✅ DONE IN DEMO  (claim lifecycle events)
Same as the demo (search **Service Bus** → **+ Create** → namespace **`urp-prod-bus`** → **Standard**).
For production: tick **Premium** for VNet integration + a **private endpoint** in **snet-data**. Recreate
the topic **`urp-events`** and the per-service subscriptions exactly as the demo did.

### 5b. Event Hub — 🆕 NEW  (immutable audit stream)
> **[CODE CHANGE]** the Reporting/Audit service publishes audit events to Event Hub (demo wrote audit to
> SQLite).
1. Search **Event Hubs** → **+ Create** → namespace **`urp-events-hub`** → **Standard** → East US 2.
2. After create → **+ Event Hub** → name **`audit-stream`** → partitions 4 → retention 7 days.
3. **Networking:** private endpoint in **snet-data**.

---

## Phase 6 — Container Registry & images — ✅ DONE IN DEMO

Same as the demo:
1. Search **Container registries** → **+ Create** → name **`urpprodacr`** → SKU **Standard/Premium**
   (Premium for geo-replication + private endpoint).
2. Build & push the 7 service images exactly as the demo (`az acr build --registry urpprodacr --image
   <svc>:latest --file services/<svc>/Dockerfile .`). The same images run on AKS — **no rebuild needed**
   if you're on the same registry.
3. For production: **Networking → Private endpoint** in **snet-data**, and **Geo-replications → + Add**
   **West US 2**.

---

## Phase 7 — AKS cluster + the 7 services — 🆕 NEW

> The demo ran the services on **Container Apps**. Production runs them on **AKS**. The cluster is created
> in the Portal; the **workloads are deployed with `kubectl`/Helm** (there is no pure-Portal way to deploy
> custom pods). **[CODE CHANGE / NEW ARTIFACT]** the repo has Bicep for Container Apps but **no Kubernetes
> manifests yet** — you'll need Deployment/Service/Ingress/HPA YAML per service (I can generate these).

### 7a. Create the cluster (Portal)
1. Search **Kubernetes services** → **+ Create** → **Create a Kubernetes cluster**.
2. **Basics:** RG **urp-prod-rg** → Cluster name **`urp-aks`** → Region **East US 2** → **Production
   Standard** preset.
3. **Node pools:** system pool (2–3 nodes) + a user pool; enable **autoscaling** (matches HPA 2–10).
4. **Networking:** **Azure CNI** → select **urp-vnet** / **snet-aks**. Enable **private cluster** if
   required.
5. **Integrations:** attach **urpprodacr** (so AKS can pull images) and enable **Azure Monitor /
   Container Insights** (→ Log Analytics from the demo, or a new workspace).
6. **Review + create** → **Create** (~10 min).

### 7b. Connect and deploy workloads (command line)
```bash
az aks get-credentials --resource-group urp-prod-rg --name urp-aks
kubectl create namespace urp
# Apply per-service manifests (Deployment + Service + HPA 2–10 + probes on /health):
kubectl apply -n urp -f k8s/        # <-- these manifests need to be created (see note above)
```
- Use **Workload Identity** so each pod gets a **Managed Identity** → grant it **Key Vault Secrets User**
  (Phase 3) and DB/Cosmos/Redis/Service Bus data roles (no connection strings in config).
- Install an ingress controller (or use **APIM → AKS** via internal load balancer) for Phase 8.

---

## Phase 8 — API Management (APIM) — 🆕 NEW

> Replaces the demo's **YARP gateway**. **[CODE CHANGE]** remove/retire the YARP gateway service; the SPA
> calls APIM instead.
1. Search **API Management services** → **+ Create**.
2. **Basics:** RG **urp-prod-rg** → Region **East US 2** → Resource name **`urp-apim`** → Org/admin email
   → Pricing tier **Developer** (test) or **Premium** (prod, for VNet + multi-region).
3. **Review + create** → **Create** (⏳ 30–60 min — APIM is slow to provision).
4. **VNet:** **Network** → integrate with **urp-vnet / snet-apim** (Internal or External).
5. **Import each service API:** **APIs** → **+ Add API** → **HTTP** (or OpenAPI if you expose Swagger) →
   set the backend to the service's AKS internal URL → repeat for the 7 services under `/api/v1/...`.
6. **JWT validation policy:** **APIs** → **All APIs** → **Inbound processing** → add
   `validate-jwt` pointing at the Entra **OpenID config**
   (`https://login.microsoftonline.com/<tenant>/v2.0/.well-known/openid-configuration`).
7. **Rate limit + CORS policies:** add `rate-limit-by-key` (100/min per subscription key) and a `cors`
   policy allowing the Front Door origin.

---

## Phase 9 — Azure Functions — 🆕 NEW  (Blob:OCR · Timer:Reports · Queue:Payroll)

> The demo ran OCR/reporting/payroll inside the services. Production offloads them to Functions.
> **[CODE CHANGE / NEW ARTIFACT]** Function projects need to be created (Blob trigger, Timer trigger,
> Queue trigger).
1. Search **Function App** → **+ Create** → RG **urp-prod-rg** → name **`urp-func`** → Runtime **.NET 8
   isolated** → Region **East US 2** → Plan **Premium** (for VNet).
2. **Networking:** VNet integration with **urp-vnet**.
3. Deploy three functions: **Blob trigger** (OCR on `receipts`), **Timer trigger** (scheduled reports),
   **Queue/Service Bus trigger** (payroll batch).

---

## Phase 10 — Communication Services (email) — 🆕 NEW

> The demo had no email. Production sends claim/notification emails.
1. Search **Communication Services** → **+ Create** → name **`urp-acs`** → Data location.
2. **Email** → **+ Connect email domain** (Azure-managed subdomain to start, or your custom domain).
3. Copy the **connection string** → store in Key Vault. **[CODE CHANGE]** the notification handler uses
   the ACS Email SDK.

---

## Phase 11 — Frontend: Static Web Apps + Front Door

### 11a. Static Web App(s) — ♻️ PARTIAL
You deployed **one consolidated SPA** to a Static Web App in the demo. The diagram shows **5 independent
SWAs** (shell + 4 portals). Two options:
- **Keep the consolidated SPA** (simpler; what you built) — deploy it as one SWA (same steps as the demo,
  Step 9). **[Recommended.]**
- **Split into 5 SWAs** (matches the diagram exactly) — create 5 Static Web Apps and rewire Module
  Federation remote URLs. **[CODE CHANGE — reverts the consolidation we did.]**

### 11b. Azure Front Door — 🆕 NEW  (WAF · CDN · TLS · geo-routing)
1. Search **Front Door and CDN profiles** → **+ Create** → **Azure Front Door (Standard/Premium)** →
   **Quick create**.
2. **Endpoint:** name **`urp-fd`**. **Origin:** add your **Static Web App** as one origin and **APIM** as
   a second origin (route `/api/*` → APIM, everything else → SWA).
3. **WAF:** Premium → attach a **WAF policy** (Managed rules + rate limiting).
4. **Review + create** → **Create**. Update the **urp-spa** Entra redirect URI and APIM CORS to the
   Front Door URL.

---

## Phase 12 — Observability — ♻️ PARTIAL

You created **Log Analytics** in the demo (Container Apps logs). For production:
1. Search **Application Insights** → **+ Create** → name **`urp-appi`** → connect to the existing
   **Log Analytics workspace**. 🆕
2. Wire AKS (Container Insights, Phase 7), APIM (diagnostics), Functions, and the services to **urp-appi**.
   **[CODE CHANGE]** add the App Insights connection string + Serilog sink.
3. **Alerts:** create metric alerts (5xx rate, pod restarts, SQL DTU, Service Bus dead-letters).

---

## Code-change checklist

Provisioning the infra above is only half the job. To make the app actually use it:

| Area | Demo (now) | Production target | Change |
|---|---|---|---|
| Relational data | SQLite | Azure SQL | EF `UseSqlite`→`UseSqlServer` + MSI conn |
| NoSQL data | SQLite | Cosmos DB | Benefits/Compensation → Cosmos SDK/EF Cosmos |
| Cache | IMemoryCache | Redis | `IDistributedCache` (StackExchange.Redis) |
| Auth | self-issued RSA JWT | Entra ID | `Jwt:Authority`→Entra; SPA uses MSAL/PKCE |
| Gateway | YARP container | APIM | retire YARP; SPA → APIM |
| Secrets | baked in images | Key Vault + MSI | `AddAzureKeyVault()` + Managed Identity |
| Audit | SQLite | Event Hub | publish audit events to Event Hub |
| OCR/reports/payroll | in-service | Azure Functions | extract to Function triggers |
| Email | none | Communication Services | ACS Email SDK |
| Compute | Container Apps | AKS | add K8s manifests/Helm (Deployment/Service/HPA/Ingress) |
| Frontend | 1 consolidated SPA | 5 SWAs (optional) | split + rewire Module Federation (only if matching the diagram exactly) |

---

## Cost & teardown

- This is a **substantially more expensive** estate than the demo (AKS + APIM Premium + SQL + Cosmos
  multi-region + Redis Premium + Front Door easily runs **hundreds to thousands of USD/month**). Set a
  **Budget** alert (Cost Management) before you start.
- **Teardown:** delete the resource group **urp-prod-rg** (same as the demo's teardown) to remove
  everything. Entra app registrations live in the tenant — delete `urp-api` / `urp-spa` separately under
  **Entra ID → App registrations**.

---

## Recommended next step

Because this is ~15 services with private networking, the reliable way to stand it up (and the HLD's
mandate) is **Bicep modules + a GitHub Actions pipeline**, with the Portal steps above as the reference.
**I can generate the Bicep + pipeline for this production architecture if you want to go that route.**
