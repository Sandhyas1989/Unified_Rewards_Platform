# Unified Rewards Platform — Deployment & Testing Guide

This guide covers two scenarios:
- **Part A** — Running and testing the application on your local machine
- **Part B** — Deploying the application to Microsoft Azure

---

## Part A — Local Testing

### Prerequisites

Install the following tools before you begin:

| Tool | Version | Download |
|---|---|---|
| .NET 8 SDK | 8.x | https://dotnet.microsoft.com/download/dotnet/8 |
| Node.js | 18 or 20 LTS | https://nodejs.org |
| Git | Any recent | https://git-scm.com |

Verify your installations:

```bash
dotnet --version       # should print 8.x.x
node --version         # should print v18.x or v20.x
npm --version          # should print 9.x or 10.x
```

> **macOS note — if `dotnet` resolves to an old version (2.x, 3.x, or 6.x)**
>
> The .NET 8 SDK installs to `~/.dotnet` by default on macOS, but your shell may still
> point to an older system SDK at `/usr/local/share/dotnet`. Fix it by adding .NET 8 to
> your PATH (run once, then restart your terminal):
> ```bash
> echo 'export DOTNET_ROOT=~/.dotnet' >> ~/.zshrc
> echo 'export PATH=$PATH:~/.dotnet' >> ~/.zshrc
> source ~/.zshrc
> dotnet --version   # should now print 8.x.x
> ```

---

### Option 1 — Run with `dotnet run` (recommended for development)

This starts each service as a plain process on your machine. No Docker required.

#### Step 1 — Open 8 terminal windows (or use a terminal multiplexer like tmux)

Each service runs in its own terminal. Run each command from the **repo root**.

**Terminal 1 — Employee Profile service (authentication + employees)**
```bash
cd services/employee-profile/UnifiedRewards.EmployeeProfile
dotnet run
# Listening on: http://localhost:5101
```

**Terminal 2 — Benefits Catalogue**
```bash
cd services/benefits-catalogue/UnifiedRewards.BenefitsCatalogue
dotnet run
# Listening on: http://localhost:5102
```

**Terminal 3 — Compensation Rules**
```bash
cd services/compensation-rules/UnifiedRewards.CompensationRules
dotnet run
# Listening on: http://localhost:5103
```

**Terminal 4 — Reimbursement Workflow**
```bash
cd services/reimbursement-workflow/UnifiedRewards.ReimbursementWorkflow
dotnet run
# Listening on: http://localhost:5104
```

**Terminal 5 — Document Processing**
```bash
cd services/document-processing/UnifiedRewards.DocumentProcessing
dotnet run
# Listening on: http://localhost:5105
```

**Terminal 6 — Payroll Integration**
```bash
cd services/payroll-integration/UnifiedRewards.PayrollIntegration
dotnet run
# Listening on: http://localhost:5106
```

**Terminal 7 — Reporting & Compliance**
```bash
cd services/reporting-compliance/UnifiedRewards.ReportingCompliance
dotnet run
# Listening on: http://localhost:5107
```

**Terminal 8 — API Gateway (YARP)**
```bash
cd services/gateway/UnifiedRewards.Gateway
dotnet run
# Listening on: http://localhost:5080  ← this is the single entry point
```

> **Tip — macOS/Linux one-liner** (runs all 8 services in the background):
> ```bash
> for svc in employee-profile/UnifiedRewards.EmployeeProfile \
>             benefits-catalogue/UnifiedRewards.BenefitsCatalogue \
>             compensation-rules/UnifiedRewards.CompensationRules \
>             reimbursement-workflow/UnifiedRewards.ReimbursementWorkflow \
>             document-processing/UnifiedRewards.DocumentProcessing \
>             payroll-integration/UnifiedRewards.PayrollIntegration \
>             reporting-compliance/UnifiedRewards.ReportingCompliance \
>             gateway/UnifiedRewards.Gateway; do
>   (cd "services/$svc" && dotnet run &)
> done
> wait
> ```

#### Step 2 — Start the frontend

Open a **9th terminal** from the `frontend/` directory:

```bash
cd frontend
npm install          # first time only — installs all workspace dependencies
npm start            # starts shell + all 4 portals concurrently
```

This launches:

| App | URL | Who uses it |
|---|---|---|
| Shell (login/layout) | http://localhost:3000 | Everyone |
| Employee Portal | http://localhost:3001 | Employees |
| Manager Portal | http://localhost:3002 | Managers |
| HR Portal | http://localhost:3003 | HR Admins |
| Finance Portal | http://localhost:3004 | Finance team |

Open http://localhost:3000 in your browser.

---

#### Step 3 — Create your first user and log in

The database starts empty. Use the Swagger UI to create a user, or use `curl`.

**Option A — Swagger UI**

Navigate to http://localhost:5101/swagger — this is the Employee Profile service.
Use `POST /api/auth/register` with:
```json
{
  "email": "admin@urp.com",
  "password": "Password1!",
  "firstName": "Admin",
  "lastName": "User",
  "role": "HrAdmin"
}
```

**Option B — curl**
```bash
# Register
curl -s -X POST http://localhost:5080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@urp.com","password":"Password1!","firstName":"Admin","lastName":"User","role":"HrAdmin"}' \
  | python3 -m json.tool

# Login — copy the "token" from the response
curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@urp.com","password":"Password1!"}' \
  | python3 -m json.tool
```

Set the token in a variable for subsequent calls:
```bash
TOKEN="paste-your-token-here"
```

**Suggested seed data (run in order):**
```bash
# 1. Register an employee
curl -s -X POST http://localhost:5080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@urp.com","password":"Password1!","firstName":"Alice","lastName":"Smith","role":"Employee"}'

# 2. Register a manager
curl -s -X POST http://localhost:5080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"bob@urp.com","password":"Password1!","firstName":"Bob","lastName":"Jones","role":"Manager"}'

# 3. Register a finance user
curl -s -X POST http://localhost:5080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"carol@urp.com","password":"Password1!","firstName":"Carol","lastName":"Lee","role":"Finance"}'

# 4. Create a benefit plan (login as HrAdmin first, get token)
ADMIN_TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@urp.com","password":"Password1!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

curl -s -X POST http://localhost:5080/api/benefit-plans \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Medical Cover","description":"Annual medical coverage","coverageAmount":50000,"monthlyCost":500,"currencyCode":"INR"}'
```

---

### End-to-End Test Flow

This walks through a complete reimbursement claim from submission to settlement.

```bash
# Step 1: Alice logs in and submits a claim
ALICE_TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@urp.com","password":"Password1!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

CLAIM_ID=$(curl -s -X POST http://localhost:5080/api/claims \
  -H "Authorization: Bearer $ALICE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":1,"amount":2500,"currencyCode":"INR","description":"Doctor visit - fever"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")

echo "Claim ID: $CLAIM_ID"

# Step 2: Bob (Manager) approves it
BOB_TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"bob@urp.com","password":"Password1!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

curl -s -X POST "http://localhost:5080/api/claims/$CLAIM_ID/approve" \
  -H "Authorization: Bearer $BOB_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notes":"Approved — valid medical receipt"}'

# Step 3: Carol (Finance) triggers settlement
CAROL_TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"carol@urp.com","password":"Password1!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

curl -s -X POST "http://localhost:5080/api/claims/$CLAIM_ID/settle" \
  -H "Authorization: Bearer $CAROL_TOKEN"

# Step 4: Check the claim status (should be Settled after ~2 seconds)
sleep 3
curl -s "http://localhost:5080/api/claims/$CLAIM_ID" \
  -H "Authorization: Bearer $BOB_TOKEN" | python3 -m json.tool
```

---

### Option 2 — Run with Docker Compose

This builds and starts everything in containers. Requires Docker Desktop.

```bash
# From the repo root:
docker compose up --build

# The gateway is available at http://localhost:5080
# All other services are internal (not exposed on the host)
```

To stop:
```bash
docker compose down        # stops containers, keeps data volumes
docker compose down -v     # stops and deletes all data (fresh start)
```

> **Note:** The first build takes 5–10 minutes because it compiles all 8 .NET projects. Subsequent builds are cached and much faster.

---

### Running the Automated Tests

Integration tests live under `services/tests/`. They use in-memory SQLite and a local test bus — no running services needed.

```bash
# Run all backend tests
find services/tests -name "*.csproj" | xargs -I{} dotnet test {} --nologo -v q

# Run tests for a single service
dotnet test services/tests/UnifiedRewards.ReimbursementWorkflow.IntegrationTests/ --nologo
```

Expected output: all tests pass. If a test fails, it will print the specific assertion that failed.

---

### Swagger API Explorer

Every service exposes a Swagger UI when running locally:

| Service | Swagger URL |
|---|---|
| Employee Profile | http://localhost:5101/swagger |
| Benefits Catalogue | http://localhost:5102/swagger |
| Compensation Rules | http://localhost:5103/swagger |
| Reimbursement Workflow | http://localhost:5104/swagger |
| Document Processing | http://localhost:5105/swagger |
| Payroll Integration | http://localhost:5106/swagger |
| Reporting & Compliance | http://localhost:5107/swagger |
| Gateway (all routes) | http://localhost:5080 (no Swagger — use individual services) |

In Swagger, click **Authorize**, paste your JWT token (from the login call) with prefix `Bearer `, then try any endpoint.

---

---

## Part B — Deploying to Microsoft Azure (beginner-friendly, click-by-click)

> **New to Azure? Read this first.**
> This guide assumes you have **never used Azure before** — every step says exactly what to click
> and what to type. You'll do almost everything from your web browser using two things:
>
> 1. **The Azure Portal** (`https://portal.azure.com`) — the visual, point-and-click website for
>    creating and managing Azure resources.
> 2. **Azure Cloud Shell** — a ready-to-use command line that lives *inside* the Portal (one click
>    to open). We use it only for the couple of steps that have no point-and-click equivalent:
>    building the app images and creating all the services at once. You do **not** need to install
>    anything on your own computer — not even Docker.
>
> First-time setup takes about **60–90 minutes**. Running cost is roughly **$35–50/month**, and you
> can delete everything in one click when you're done (see [Turning everything off](#turning-everything-off-delete)).

### What we'll build

By the end you'll have all of the following running in Azure, inside a single resource group:

```
Azure Subscription
└── Resource Group: urp-rg  (or your existing one)
    ├── Azure Container Registry (urpacr)          ← stores your app images
    ├── Azure Service Bus (urp-bus)                 ← event bus between services
    ├── Azure Storage Account (urpreceipts)
    │   ├── Blob container: receipts               ← uploaded receipt files
    │   └── File Shares (7×)                       ← SQLite persistence per service
    ├── Azure Key Vault (urp-kv)                   ← secrets (future use)
    ├── Log Analytics Workspace (urp-logs)          ← container logs
    └── Container Apps Environment (urp-env)
        ├── urp-employee-profile   (internal)
        ├── urp-benefits-catalogue (internal)
        ├── urp-compensation-rules (internal)
        ├── urp-reimbursement-workflow (internal)
        ├── urp-document-processing    (internal)
        ├── urp-payroll-integration    (internal)
        ├── urp-reporting-compliance   (internal)
        └── urp-gateway                (PUBLIC ← your entry point)
```

---

### Before you begin — what you need

You only need three things:

| What | How to get it |
|---|---|
| A computer with a web browser | You already have this — Chrome, Edge, Firefox, or Safari all work. |
| A Microsoft account | A personal Outlook/Hotmail/Live email works, or a work account. |
| An Azure subscription | Free to create, includes free credit — see below. |

**Do I need to install the Azure CLI or Docker?** No. Every command in this guide runs inside
**Azure Cloud Shell** (in the browser), which already has all the tools pre-installed. *(If you'd
rather use your own computer's terminal, install the
[Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and run the same commands
locally — the project code is already on your machine, so you can skip the "get the code into
Cloud Shell" step.)*

#### Create your free Azure account (skip if you already have one)

1. Go to **https://azure.microsoft.com/free**.
2. Click the green **Start free** button.
3. Sign in with your Microsoft account, or click **Create one!** to make a new one.
4. Fill in the verification form. Azure asks for a **phone number and a credit/debit card** to
   confirm you're a real person — but a free account is **not charged** unless you choose to
   upgrade. You get US$200 of credit for 30 days plus a set of always-free services.
5. Verify your phone (you'll get a code by text or call), accept the terms, and click **Sign up**.
6. When the "Welcome to Azure" page appears, click **Go to the Azure portal**.

### Get to know the Azure Portal (60-second tour)

Open **https://portal.azure.com** and sign in — you'll land on the **Home** page. Find these
things; you'll use them in almost every step:

- **Search bar** — the wide box across the top middle (*"Search resources, services, and docs"*).
  This is the fastest way to get anywhere. **Whenever this guide says "search for X", click this
  bar, type X, and click the matching result.**
- **Portal menu** — the **☰** (hamburger) icon at the far top-left. Click it to show a side menu
  with **+ Create a resource**, **Home**, **Resource groups**, **Subscriptions**, **All services**.
- **Cloud Shell** — the **`>_`** icon in the top-right toolbar (we open it in Step 5).
- **Notifications** — the **🔔** bell (top-right). Azure tells you here when something finishes
  being created.
- Most things open as a side panel ("blade"); close one with the **X** in its top-right corner.
  Click **Home** any time to start fresh.

---

### Step 1 — Sign in and check your subscription

1. Go to **https://portal.azure.com** and sign in.
2. In the **search bar**, type **Subscriptions** and click **Subscriptions** in the results.
3. You should see at least one subscription (e.g. *Azure subscription 1* or *Free Trial*). **Note
   its name** — you'll need it if you have more than one.
4. If the list is empty, finish creating your free account (above) first.

Staying signed in to the Portal keeps you authenticated everywhere, including Cloud Shell — so
there's no separate login command to run.

---

### Step 2 — Turn on the Azure services we use (register providers)

**What is this, and why?** A **resource provider** is the part of Azure that knows how to create
one kind of resource (Container Apps, Storage, and so on). Azure won't let you create a resource
until its provider is **Registered** on your subscription. New subscriptions have most providers
registered already, but a few (like Container Apps) often aren't — so we'll switch on all six we
need. You only do this **once per subscription**.

> You must be the **Owner** or **Contributor** of the subscription to register providers. On a
> personal/free subscription that's automatically you, so you're fine.

#### 2a — Open the Resource providers list

1. Go to **https://portal.azure.com** (sign in if asked).
2. Click the **search bar** at the very top of the page and type **`Subscriptions`**.
3. In the dropdown, under the **Services** heading, click **Subscriptions**.
4. You'll see a table of your subscriptions. **Click the subscription name** (for example
   *Azure subscription 1* or *Free Trial*) to open it.
5. The subscription page opens with a menu down the **left-hand side**. Scroll that menu down to
   the **Settings** group and click **Resource providers**.
6. A large table appears with two columns — **Provider** and **Status** — and a search box above it
   labelled **"Filter by name..."**. This is where you'll work.

#### 2b — Register the first provider (full walk-through for `Microsoft.App`)

We'll do **Microsoft.App** step by step; the other five are identical.

1. Click the **"Filter by name..."** box and type **`Microsoft.App`**.
2. The list shrinks to matching rows. ⚠️ **Several names start the same way** (e.g.
   *Microsoft.AppConfiguration*, *Microsoft.AppPlatform*). Find the row whose **Provider** column
   reads **exactly `Microsoft.App`** — with nothing after it.
3. **Click that row once** so it becomes highlighted/selected.
4. Look at the **toolbar along the top** of the table and click the **Register** button.
   - If **Register** is greyed out and the **Status** already says **Registered**, this one is
     already done — just move on.
5. The row's **Status** changes to **Registering**, and after a short wait to **Registered**. Click
   the **Refresh** button on the toolbar to update what you see.

That's one done. ✅

#### 2c — Repeat for the other five

Do the exact same thing for each row below — clear the filter box, type the name, click the
**exact** matching row, then click **Register**:

| Type this in the filter box | Select the row named exactly | What it's for |
|---|---|---|
| `Microsoft.OperationalInsights` | **Microsoft.OperationalInsights** | Log Analytics (container logs) |
| `Microsoft.ServiceBus` | **Microsoft.ServiceBus** | the event bus between services |
| `Microsoft.Storage` | **Microsoft.Storage** | file & blob storage |
| `Microsoft.KeyVault` | **Microsoft.KeyVault** | secrets |
| `Microsoft.ContainerRegistry` | **Microsoft.ContainerRegistry** | your image registry |

> ⚠️ **Match the name exactly.** Typing `Microsoft.Storage` also shows *Microsoft.StorageSync* and
> *Microsoft.StorageCache*; `Microsoft.ContainerRegistry` sits right next to
> *Microsoft.ContainerInstance* and *Microsoft.ContainerService*. Always pick the one with **no
> extra words on the end**.

> You **don't** have to wait for one to finish before starting the next — click **Register** on all
> six, one after another, then come back and **Refresh**. Each takes about **1–3 minutes**.

#### 2d — Check that all six say "Registered"

In the **"Filter by name..."** box, type **`Microsoft.`** and scan the **Status** column (or filter
each name again one at a time). These six should all read **Registered**:

- Microsoft.App
- Microsoft.OperationalInsights
- Microsoft.ServiceBus
- Microsoft.Storage
- Microsoft.KeyVault
- Microsoft.ContainerRegistry

If any still says *Registering*, wait a minute and click **Refresh**. Once all six are
**Registered**, you're ready for the next step.

> **Prefer the command line?** You can register all six at once later, inside Cloud Shell (Step 5),
> instead of clicking through the list:
> ```bash
> for p in Microsoft.App Microsoft.OperationalInsights Microsoft.ServiceBus \
>          Microsoft.Storage Microsoft.KeyVault Microsoft.ContainerRegistry; do
>   az provider register --namespace $p
> done
>
> # Check progress (empty output means they're all done):
> az provider list --query "[?registrationState=='Registering'].namespace" -o table
> ```

---

### Step 3 — Get a Resource Group (the folder that holds everything)

A **resource group** is just a named box that holds all your resources.

> **⚡ On a Microsoft Learn / lab sandbox?** You already have a resource group (for example
> `rg-azuser7074_mml.local-eJJJX`) and you **can't create another one** — so **skip 3a** and use
> **3b** below. Sandbox resource-group names change each time you start a new sandbox, so always
> copy the current one from the Portal.

#### 3a — Create a new resource group (personal Azure account)

1. Search for **Resource groups** and click it.
2. Click **+ Create** (top-left).
3. On the **Basics** tab:
   - **Subscription:** leave it on your subscription.
   - **Resource group:** type **`urp-rg`**.
   - **Region:** open the dropdown and choose **(US) East US**.
4. Click **Review + create** (bottom-left), then **Create** on the next screen.
5. When the green notification appears, click **Go to resource group**.

#### 3b — Use your existing resource group (Microsoft Learn / lab)

Nothing to create — just note the **exact name** your sandbox gave you:

1. Search for **Resource groups** and click it.
2. Copy the name shown (for example **`rg-azuser7074_mml.local-eJJJX`**).

> For the rest of the guide, **wherever you see `urp-rg`, use your own resource group's name
> instead.** In the Portal you'll pick it from a dropdown; in Cloud Shell the commands use a `$RG`
> variable, so you type the name just once (Step 7).

---

### Step 4 — Create the Container Registry (storage for your app images)

An **Azure Container Registry (ACR)** privately stores the images that make up the app. We create
it now so we have somewhere to push images before deploying.

1. Search for **Container registries** and click it.
2. Click **+ Create**.
3. On the **Basics** tab:
   - **Subscription:** your subscription.
   - **Resource group:** select **your resource group** (from Step 3 — `urp-rg`, or your sandbox's) from the dropdown.
   - **Registry name:** type **`urpacr`**. This must be **globally unique** across all of Azure.
     If you see *"already taken"*, add a few digits (e.g. `urpacr12345`) and **remember the name
     you chose** — use it everywhere this guide says `urpacr`.
   - **Location:** **East US**.
   - **Pricing plan:** open the dropdown and choose **Basic** (cheapest; fine for this).
4. Click **Review + create**, then **Create**. Wait ~1 minute for *"Your deployment is complete"*,
   then click **Go to resource**.
5. **Turn on the admin login** (the deploy step needs it): in the registry's left menu, under
   **Settings**, click **Access keys**, then switch the **Admin user** toggle to **Enabled**.

> The one-command deploy in Step 7 *also* declares this same registry, so creating it now just
> gives us a place to push images first. Re-declaring it with identical settings does nothing
> harmful.

---

### Step 5 — Open Cloud Shell and load the project code

#### 5a — Open Cloud Shell (first time)

1. In the Portal's **top-right toolbar**, click the **`>_`** (Cloud Shell) icon. A terminal panel
   opens along the bottom of the screen.
2. When asked **Bash or PowerShell**, click **Bash**.
3. It asks about storage for your files. Two cases:
   - **If you can create storage** (most personal accounts): click the blue **Create storage**
     button and wait ~30 seconds.
   - **If "Create storage" is blocked or errors** — common on **Microsoft Learn / lab sandboxes**,
     where you don't have rights to make a storage account — choose **No storage account required**
     (an *ephemeral* session). This works for everything in this guide.
4. When you see a prompt like `user@Azure:~$`, Cloud Shell is ready.

> **If you chose "No storage account required":** your Cloud Shell home folder isn't saved — files
> you clone/upload and shell variables like `$RG` are **wiped when the session closes or after
> ~20 minutes idle**. That's fine: do the build + deploy **in one sitting**, and if the session
> resets just re-run the setup (re-`cd` into the project, re-set `RG="..."`). The Azure resources
> you deploy are **not** affected — they live in your subscription regardless of Cloud Shell storage.

#### 5b — Get the code into Cloud Shell

**If the project is on GitHub** (use your repo's URL):
```bash
git clone https://github.com/<your-account>/Unified_Rewards_Platform.git
cd Unified_Rewards_Platform
```

**If the project is only on your computer**, upload it as a zip instead:
1. On your computer, zip the project folder (right-click `Unified_Rewards_Platform` →
   *Compress* / *Send to → Zipped folder*).
2. In the Cloud Shell toolbar, click the **Upload/Download files** icon (a page with an arrow) →
   **Upload** → pick your zip. Wait for the upload notification.
3. In Cloud Shell:
   ```bash
   unzip Unified_Rewards_Platform.zip
   cd Unified_Rewards_Platform
   ```

Confirm you're in the project root — this should list `infra`, `services`, `frontend`:
```bash
ls
```

---

### Step 6 — Build the 8 app images (no Docker needed)

**`az acr build`** uploads each service's code to your registry and **builds the image in the
cloud**, so you don't need Docker installed anywhere. Run this from the project root — it builds
all 8 images:

```bash
for s in employee-profile benefits-catalogue compensation-rules reimbursement-workflow \
         document-processing payroll-integration reporting-compliance gateway; do
  echo "=== building $s ==="
  az acr build --registry urpacr --image $s:latest --file services/$s/Dockerfile .
done
```

*(If you named your registry something other than `urpacr`, change it here.)* Each image takes
**2–5 minutes**; all 8 together is roughly **20–35 minutes**. Build logs stream by — that's normal.

**Verify the images arrived, in the Portal:**
1. Search **Container registries** → click **urpacr**.
2. In the left menu, under **Services**, click **Repositories**.
3. You should see all **8**: `employee-profile`, `benefits-catalogue`, `compensation-rules`,
   `reimbursement-workflow`, `document-processing`, `payroll-integration`,
   `reporting-compliance`, `gateway`.

---

### Step 7 — Create all the Azure resources with one command

Everything else — the event bus, storage, Key Vault, logging, the Container Apps environment, and
all 8 services wired together — is described in **Bicep** templates under `infra/`. One command
builds it all, in **Cloud Shell**, from the project root.

First tell the commands which resource group to use — paste **one** of these (you'll reuse `$RG`
for the rest of the guide):
```bash
# Personal account — the group you made in Step 3a:
RG=urp-rg

#  — OR —  Microsoft Learn / lab sandbox — use YOUR sandbox's exact name:
RG="rg-azuser7074_mml.local-eJJJX"
```

Now deploy with the option that matches your resource group:

**Option A — let the deploy create the group (personal account).** Runs at subscription scope and
creates `urp-rg` itself:
```bash
az deployment sub create \
  --location eastus \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.json \
  --name urp-deployment
```

**Option B — deploy into your existing group (Microsoft Learn / lab sandbox).** Sandboxes block
creating resource groups, so this deploys *into* the one you already have, using the
resource-group-scoped template `infra/main-rg.bicep`:
```bash
az deployment group create \
  --resource-group "$RG" \
  --template-file infra/main-rg.bicep \
  --parameters namePrefix=urp imageTag=latest \
  --name urp-deployment
```

> `namePrefix=urp` is what makes the resources come out as `urpacr`, `urp-bus`, `urp-gateway`, etc.
> — matching every name in this guide. Only change it if a name clash forces you to (see the box
> below), and then use your prefix everywhere this guide shows `urp...`.

This runs for **10–20 minutes**. Lines scroll as each resource is created, and a block of JSON
prints at the end.

**Watch it happen in the Portal (optional but reassuring):**
1. Open your resource group (search its name).
2. In its left menu, under **Settings**, click **Deployments**.
3. Click the **urp-deployment** row — you'll see each resource flip from *Creating* (clock icon) to
   a green check in real time. Click **Refresh** to update.

A final *"provisioningState: Succeeded"* means you're deployed. If it errors, see
[Troubleshooting](#troubleshooting).

> **⚠️ Microsoft Learn / lab sandbox limits.** Sandboxes cap which services, regions, and names you
> can use, and this is a large 8-service stack. If Option B fails:
> - **"name already taken"** (ACR or storage) — those names are global and the sandbox subscription
>   is shared. Re-run with a unique prefix, e.g. `--parameters namePrefix=urp7074`, and use that
>   prefix wherever this guide shows `urp...` (also name your Step 4 registry `urp7074acr` to match).
> - **"disallowed by policy"** or a region error — that service/region isn't allowed in your
>   sandbox. If the stack won't fit, a free **personal** Azure account is the reliable way to finish.

---

### Step 8 — Find your gateway URL and test the backend

The **gateway** is the one public web address for the entire backend.

1. Search for **Container apps** and click it — you'll see all 8 apps in **your resource group**.
2. Click **urp-gateway**.
3. On its **Overview** page, copy the **Application Url** (top-right area; click the copy icon
   next to it). It looks like `https://urp-gateway.<random-words>.eastus.azurecontainerapps.io`.

**Test #1 — in your browser:** paste that URL into a new tab and add **`/health`** to the end. You
should see `{"status":"healthy","service":"gateway"}`.

**Test #2 — create your first user.** Back in **Cloud Shell**, paste this (swap in your URL):
```bash
GATEWAY_URL="https://urp-gateway.<...>.azurecontainerapps.io"

curl -s -X POST "$GATEWAY_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@urp.com","password":"Password1!","firstName":"Admin","lastName":"User","role":"HrAdmin"}'
```
A JSON reply containing a user `id` and a `token` means the whole backend works.

> **Check every service:** in **Container apps**, each app's **Overview** shows a **Running
> status** — all 8 should read **Running**. If one is stuck on *Failed* or *Activating*, open it →
> **Monitoring → Log stream** (see [Viewing logs](#viewing-logs)).

---

### Step 9 — Deploy the website (the four portals)

The backend is fully usable through its API now. The browser UI is a bundle of static files we'll
host on **Azure Static Web Apps** (free tier).

> The gateway API you just deployed is the working core of the platform. The website is a
> convenience layer — if you only need to demo the backend, you can skip this step and use the API
> directly through the gateway URL.

#### 9a — Point the website at your gateway

In **Cloud Shell**, open the front-end's runtime config file in the built-in editor:
```bash
code frontend/shell/public/config.js
```
Change the `apiBase` value to **your gateway URL** followed by **`/api`**:
```javascript
window.__URP_CONFIG__ = {
  apiBase: 'https://urp-gateway.<...>.azurecontainerapps.io/api'
};
```
Save with **Ctrl+S** (Cmd+S on Mac), then close the editor with **Ctrl+Q**.

#### 9b — Build the website
```bash
cd frontend
npm install      # first time only — takes a few minutes
npm run build
cd ..
```
The files to publish end up in **`frontend/shell/dist`**.

#### 9c — Create the Static Web App (in the Portal)

1. Search for **Static Web Apps** and click it.
2. Click **+ Create**.
3. On the **Basics** tab:
   - **Subscription:** your subscription.
   - **Resource group:** **your resource group** (the one in `$RG`).
   - **Name:** **`urp-frontend`**.
   - **Plan type:** **Free**.
   - **Region:** pick the nearest, e.g. **East US 2**.
   - **Deployment details → Source:** choose **Other** (this avoids being forced to connect a
     GitHub repo — we upload from Cloud Shell instead).
4. Click **Review + create**, then **Create**, then **Go to resource**.

#### 9d — Upload the built site from Cloud Shell
```bash
# Install the deploy tool (first time only)
npm install -g @azure/static-web-apps-cli

# Fetch the upload token automatically
SWA_TOKEN=$(az staticwebapp secrets list --name urp-frontend \
  --resource-group "$RG" --query "properties.apiKey" -o tsv)

# Upload the built files
swa deploy frontend/shell/dist --deployment-token "$SWA_TOKEN" --env production
```
Then get your website address:
```bash
az staticwebapp show --name urp-frontend --resource-group "$RG" \
  --query "defaultHostname" -o tsv
```
Your site is `https://<that-hostname>` (for example `https://urp-frontend.azurestaticapps.net`).

---

### Step 10 — Let the website talk to the backend (update CORS)

Browsers block a website from calling an API at a different address unless the API explicitly
allows it (**CORS**). Tell the gateway to allow your new website's address.

**Option A — in the Portal (click-by-click):**
1. Search **Container apps** → click **urp-gateway**.
2. In the left menu, under **Application**, click **Containers**.
3. Click **Edit and deploy** (top toolbar).
4. Tick the **gateway** container's checkbox, then click **Edit**.
5. Open the **Environment variables** tab, click **+ Add**, and enter:
   - **Name:** `Cors__AllowedOrigins__0`
   - **Source:** **Manual entry**
   - **Value:** your website URL, e.g. `https://urp-frontend.azurestaticapps.net` (no trailing slash)
6. Click **Save**, then **Create** (bottom) to roll out a new revision.

**Option B — one line in Cloud Shell** (swap in your URL):
```bash
az containerapp update --name urp-gateway --resource-group "$RG" \
  --set-env-vars "Cors__AllowedOrigins__0=https://urp-frontend.azurestaticapps.net"
```
Either way, the gateway restarts with the new setting after ~30 seconds.

---

### Step 11 — Log in and use the platform

1. Open your website address (`https://urp-frontend.azurestaticapps.net`) in a browser.
2. Log in with the admin user you created in Step 8 — **`admin@urp.com`** / **`Password1!`**.
3. You're in 🎉. To add more users (employee, manager, finance) and run a full
   claim → approve → settle flow, reuse the API calls from **Part A** ("Create your first user and
   log in" and "End-to-End Test Flow") — just replace `http://localhost:5080` with your **gateway
   URL**.

---

### Viewing logs

When something misbehaves, check the logs first.

**In the Portal (easiest):**
1. Search **Container apps** and click the app (e.g. **urp-gateway**).
2. In the left menu, under **Monitoring**, click **Log stream**.
3. Live logs appear — reproduce the problem and watch the lines arrive.

**In Cloud Shell** (swap in any app name):
```bash
az containerapp logs show --name urp-gateway --resource-group "$RG" --follow
```

---

### Deploying updates later

Changed some backend code? Rebuild that one image and point its app at the new tag. Example for
`reimbursement-workflow`, run from the project root in **Cloud Shell**:
```bash
# 1. Rebuild just that image with a new tag (e.g. v2)
az acr build --registry urpacr --image reimbursement-workflow:v2 \
  --file services/reimbursement-workflow/Dockerfile .

# 2. Point its Container App at the new image
az containerapp update --name urp-reimbursement-workflow --resource-group "$RG" \
  --image urpacr.azurecr.io/reimbursement-workflow:v2
```
Or in the Portal: **Container apps → urp-reimbursement-workflow → Containers → Edit and deploy →**
(tick the container) **→ Edit →** change the image tag **→ Save → Create**.

---

### Watching your costs

1. In the **search bar**, type **Cost Management** and click **Cost Management + Billing**.
2. Click **Cost analysis** in the left menu to see your running total, grouped by resource.
3. **Set a safety alert:** click **Budgets → + Add**, set a small monthly amount (e.g. $50) and add
   your email — Azure warns you before you reach it.

Rough monthly cost for this demo-sized deployment:

| Resource | Approximate cost |
|---|---|
| Container Apps (8 apps, min replicas) | ~$15–30 / month |
| Azure Service Bus (Standard) | ~$10 / month |
| Azure Storage (LRS) | < $1 / month |
| Azure Container Registry (Basic) | ~$5 / month |
| Log Analytics | ~$5 / month |
| Key Vault | < $1 / month |
| **Total** | **~$35–50 / month** |

> **Cut idle cost to near-zero:** in **Container apps → (each app) → Scaling**, set **Min
> replicas** to **0**. Apps then sleep when unused (you pay almost nothing) and wake on the next
> request after a ~10-second cold start.

---

### Turning everything off (delete)

> **Microsoft Learn / lab sandbox?** Don't delete the resource group — the sandbox owns it and
> usually blocks deleting it. Just **stop / close the sandbox** (or let it expire) and everything
> goes away automatically. To free things up sooner, delete individual resources inside the group.

For a **personal account**, the simplest way to stop **all** charges is to delete the resource
group — it removes every resource you created in this guide at once.

**In the Portal (click-by-click):**
1. Open your resource group (search its name).
2. Click **Delete resource group** in the top toolbar.
3. Confirm by **typing the resource group's name** in the box, then click **Delete**. Everything is
   removed in a few minutes.

**In Cloud Shell** (set `RG` first if you opened a fresh session — see Step 7):
```bash
az group delete --name "$RG" --yes --no-wait
```

> ⚠️ This permanently deletes every resource **and all data** — there is no undo.

---

### Troubleshooting

**A Container App shows "Failed" or keeps restarting**
- Portal: open the app → **Revisions** (under Application) for health, and **Monitoring → Log
  stream** for the error message.
- Cloud Shell:
  ```bash
  az containerapp revision list -n urp-employee-profile -g "$RG" --output table
  az containerapp logs show -n urp-employee-profile -g "$RG" --tail 50
  ```
- Most common cause: an image wasn't built before Step 7. Confirm **ACR → Repositories** lists all
  8 images, then re-run Step 7.

**The Step 7 deployment failed**
- Open **your resource group → Deployments → urp-deployment** and click the red operation to read the exact
  error. A frequent one is a name clash (the registry or storage name isn't globally unique) — pick
  a different `namePrefix` and re-run.
- *"... not registered"* means a provider is missing — finish Step 2 and re-run.

**Login returns 401 Unauthorized**
- All services share one signing key (baked into the images), so a token from one is accepted by
  all. A 401 almost always means the `Authorization: Bearer <token>` header is missing or the token
  expired — log in again to get a fresh token.

**The website shows "blocked by CORS policy"**
- The gateway doesn't know the website's address yet. Do **Step 10** with your exact Static Web App
  URL (no trailing slash).

**The website loads but every action fails / it calls `localhost:5080`**
- `config.js` still points at localhost. Redo **Step 9a–9b** (set `apiBase` to your gateway URL +
  `/api`, rebuild, redeploy).

**"Service Bus connection error" in the logs**
- Search **Service Bus**, open **urp-bus**, and confirm **Status: Active** on its Overview. The
  connection string is wired in automatically by Step 7.

**`az acr build` or `npm run build` can't find files**
- You're not in the project root. Run `ls` — you should see `infra`, `services`, `frontend`. The
  Dockerfiles reference `services/shared/`, so builds must run from the root with `.` as the build
  context (exactly as shown).
