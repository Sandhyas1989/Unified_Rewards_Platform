# Production IaC Deployment — Step by Step (Bicep + AKS + Pipeline)

This deploys the platform onto the **production compute architecture (AKS)** using the IaC in this repo.
It runs your **current code** (SQLite/self-JWT/YARP) on AKS, and provisions the production data/messaging
infra (Service Bus, Storage, plus Azure SQL/Cosmos/Redis ready for wiring).

## What's in this package
| Path | What |
|---|---|
| `infra/prod/main.bicep` + `modules/` | VNet · Log Analytics/App Insights · ACR · Key Vault · AKS · Service Bus · Event Hub · Azure SQL · Cosmos · Redis · Storage |
| `infra/prod/parameters/prod.json` | Parameter values (prefix, regions) |
| `k8s/00,10,20-*.yaml` | Namespace + secret, the 7 backend services, gateway + autoscaler + ingress |
| `.github/workflows/prod-deploy.yml` | CI/CD: infra → build images → deploy to AKS |

> **Not included (need extra config / code):** APIM, Front Door, Azure Functions, Communication
> Services, the Entra app registration (Portal — done in Phase 2), and *wiring the app to Azure
> SQL/Cosmos/Redis/Entra* (see [Code changes](#code-changes-to-use-the-managed-services)). The AKS
> gateway (YARP) stands in for APIM until you add it.

---

## 0. Prerequisites — 🆕 (vs. the demo which used Cloud Shell only)
- A **real, paid Azure subscription** you're **Owner** of (not the Learn sandbox).
- Tools: **Azure CLI** (`az`), **kubectl**, **Helm**, and Bicep (`az bicep install`). *(Cloud Shell has
  all of these — you can run everything there, same as the demo.)*
- `az login` then `az account set --subscription "<your-sub>"`.

---

## 1. Validate the templates locally — 🆕 (do this before deploying)
```bash
az bicep build --file infra/prod/main.bicep        # compiles -> no errors = valid
kubectl apply --dry-run=client -f k8s/00-namespace-and-secrets.yaml
kubectl apply --dry-run=client -f k8s/10-backend-services.yaml
kubectl apply --dry-run=client -f k8s/20-gateway-and-ingress.yaml
```

---

## Option A — Deploy by hand (CLI, step by step)

### 2. Provision all infrastructure — 🆕
```bash
az deployment sub create \
  --location eastus2 \
  --template-file infra/prod/main.bicep \
  --parameters infra/prod/parameters/prod.json \
  --parameters sqlAdminPassword='<choose-a-strong-password>' \
  --name urp-prod
```
Creates RG **urpprod-rg** with VNet, ACR (**urpprodacr**), AKS (**urpprod-aks**), Key Vault, Service
Bus (**urpprod-bus**), Event Hub, Azure SQL, Cosmos, Redis, Storage (**urpprodstg**). ~15–20 min.

### 3. Build & push the 8 images to ACR — ✅ SAME AS DEMO (`az acr build`)
```bash
for s in employee-profile benefits-catalogue compensation-rules reimbursement-workflow \
         document-processing payroll-integration reporting-compliance gateway; do
  az acr build --registry urpprodacr --image $s:latest --file services/$s/Dockerfile .
done
```

### 4. Connect kubectl to the cluster — 🆕
```bash
az aks get-credentials --resource-group urpprod-rg --name urpprod-aks --overwrite-existing
kubectl get nodes        # should list the AKS nodes
```

### 5. Install an ingress controller — 🆕
```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace
```

### 6. Namespace + connection-string secret — 🆕
```bash
kubectl apply -f k8s/00-namespace-and-secrets.yaml   # creates the 'urp' namespace

SB=$(az servicebus namespace authorization-rule keys list -g urpprod-rg \
      --namespace-name urpprod-bus -n RootManageSharedAccessKey \
      --query primaryConnectionString -o tsv)
BLOB=$(az storage account show-connection-string -g urpprod-rg -n urpprodstg -o tsv)

kubectl -n urp create secret generic urp-secrets \
  --from-literal=ServiceBusConnection="$SB" \
  --from-literal=BlobConnection="$BLOB" \
  --dry-run=client -o yaml | kubectl apply -f -
```

### 7. Deploy the workloads — 🆕
```bash
kubectl apply -f k8s/10-backend-services.yaml
kubectl apply -f k8s/20-gateway-and-ingress.yaml
kubectl -n urp get pods -w          # wait until all are Running/Ready
```

### 8. Get the public IP & verify — 🆕
```bash
kubectl -n ingress-nginx get service ingress-nginx-controller   # copy EXTERNAL-IP
curl http://<EXTERNAL-IP>/health                                # -> {"status":"healthy","service":"gateway"}
# Log in with a seeded account (same as the demo):
curl -s -X POST "http://<EXTERNAL-IP>/api/auth/login" -H "Content-Type: application/json" \
  -d '{"email":"hr@urp.local","password":"Password123!"}'       # -> returns a token
```
> Seeded accounts (password **`Password123!`**): `employee@urp.local`, `manager@urp.local`,
> `hr@urp.local`, `finance@urp.local` — same as the Container Apps demo.

---

## Option B — Deploy with the GitHub Actions pipeline

### Set up once
1. Create an app registration / service principal for CI and give it **Owner** (or Contributor +
   User Access Administrator) on the subscription. Add a **federated credential** for your repo
   (OIDC), or use a client secret.
2. In the repo → **Settings → Secrets and variables → Actions**, add:
   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and `AZURE_SQL_PASSWORD`.

### Run it
GitHub → **Actions** → **URP Production Deploy (AKS)** → **Run workflow** (pick an image tag). It runs
**infra → build (8 images) → deploy to AKS** automatically. Then do **Step 5 (ingress)** and **Step 8
(verify)** above (the pipeline assumes an ingress controller exists).

---

## Code changes to use the managed services
The AKS deploy above runs the app **as-is** (SQLite, self-JWT, Service Bus, Blob). To light up the rest
of the production architecture, make these changes, then redeploy:

| Service | Change |
|---|---|
| **Azure SQL** | EF `UseSqlite`→`UseSqlServer`; set conn string in `urp-secrets`; remove the `emptyDir` and `replicas:1` cap → add an HPA (2–10) |
| **Cosmos DB** | Benefits/Compensation → Cosmos SDK / EF Cosmos provider |
| **Redis** | `IMemoryCache`→`IDistributedCache` (StackExchange.Redis) |
| **Entra ID** | `Jwt:Authority`→Entra (the `appsettings.Production.json` stubs); SPA uses MSAL/PKCE |
| **APIM** | put APIM in front of the AKS ingress; retire YARP |
| **Front Door** | add Front Door over the SWA + APIM origins (WAF/CDN) |
| **Event Hub / Functions / ACS** | publish audit to Event Hub; move OCR/reports/payroll to Functions; email via Communication Services |
| **Key Vault** | `AddAzureKeyVault()` + Workload Identity so pods read secrets via MSI (no k8s Secret) |

---

## Teardown
```bash
az group delete --name urpprod-rg --yes --no-wait
```
Deletes everything (AKS, data, messaging, registry). Delete the Entra app registrations separately.

> ⚠️ **Validation note:** these templates were authored but **not deploy-tested** in this environment
> (no Azure access here). Run **Step 1** first — `az bicep build` and `kubectl --dry-run` will catch any
> syntax issue before you spend money. Ping me with any error and I'll fix it.
