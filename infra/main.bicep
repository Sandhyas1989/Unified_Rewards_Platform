// Unified Rewards Platform — Azure deployment
// Deploy:  az deployment sub create -l eastus -f infra/main.bicep -p infra/parameters/prod.json
// Prereqs: Azure CLI logged in, target subscription selected.

targetScope = 'subscription'

@description('Short prefix for all resource names (3-12 lowercase alphanumeric chars).')
@minLength(3)
@maxLength(12)
param namePrefix string = 'urp'

@description('Azure region for all resources.')
param location string = 'eastus'

@description('Docker image tag to deploy.')
param imageTag string = 'latest'

// ── Resource Group ──────────────────────────────────────────────────────────
resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: '${namePrefix}-rg'
  location: location
}

// ── Azure Service Bus ───────────────────────────────────────────────────────
module serviceBus 'modules/service-bus.bicep' = {
  name: 'serviceBus'
  scope: rg
  params: { location: location, namePrefix: namePrefix }
}

// ── Azure Blob Storage (receipt documents) ──────────────────────────────────
module storage 'modules/storage.bicep' = {
  name: 'storage'
  scope: rg
  params: { location: location, namePrefix: namePrefix }
}

// ── Azure Container Registry ────────────────────────────────────────────────
module acr 'modules/container-registry.bicep' = {
  name: 'containerRegistry'
  scope: rg
  params: { location: location, namePrefix: namePrefix }
}

// ── Key Vault ───────────────────────────────────────────────────────────────
module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault'
  scope: rg
  params: { location: location, namePrefix: namePrefix }
}

// ── Container Apps (all 7 services + gateway) ───────────────────────────────
module containerApps 'modules/container-apps.bicep' = {
  name: 'containerApps'
  scope: rg
  params: {
    location: location
    namePrefix: namePrefix
    imageTag: imageTag
    acrLoginServer: acr.outputs.loginServer
    acrUsername: acr.outputs.adminUsername
    acrPassword: acr.outputs.adminPassword
    serviceBusConnectionString: serviceBus.outputs.connectionString
    blobConnectionString: storage.outputs.blobConnectionString
    storageAccountName: storage.outputs.storageAccountName
    storageAccountKey: storage.outputs.storageAccountKey
  }
}

// ── Outputs ─────────────────────────────────────────────────────────────────
output resourceGroup string = rg.name
output gatewayUrl string = 'https://${containerApps.outputs.gatewayFqdn}'
output acrLoginServer string = acr.outputs.loginServer
output keyVaultName string = keyVault.outputs.keyVaultName

// Build & push images before deploying:
//   docker compose build
//   az acr login --name <acrLoginServer>
//   docker tag employee-profile <acrLoginServer>/employee-profile:latest
//   ... (repeat for each service)
//   az acr push <acrLoginServer>/employee-profile:latest
//   ... then run this bicep deployment.
