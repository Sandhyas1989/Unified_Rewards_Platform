// Unified Rewards Platform — deploy into an EXISTING resource group (resource-group scope).
//
// Use this when you CANNOT create resource groups at subscription scope — e.g. a Microsoft Learn
// / lab "sandbox" subscription that pre-creates one resource group for you and restricts the rest.
//
// Deploy:
//   az deployment group create \
//     --resource-group <your-existing-rg> \
//     --template-file infra/main-rg.bicep \
//     --parameters namePrefix=urp imageTag=latest \
//     --name urp-deployment
//
// For a normal subscription where you CAN create the resource group, use main.bicep instead
// (it is subscription-scoped and creates "<namePrefix>-rg" for you).

@description('Short prefix for all resource names (3-12 lowercase alphanumeric chars).')
@minLength(3)
@maxLength(12)
param namePrefix string = 'urp'

@description('Azure region for all resources. Defaults to the resource group\'s region.')
param location string = resourceGroup().location

@description('Docker image tag to deploy.')
param imageTag string = 'latest'

// ── Azure Service Bus ───────────────────────────────────────────────────────
module serviceBus 'modules/service-bus.bicep' = {
  name: 'serviceBus'
  params: { location: location, namePrefix: namePrefix }
}

// ── Azure Blob Storage (receipt documents) ──────────────────────────────────
module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: { location: location, namePrefix: namePrefix }
}

// ── Azure Container Registry ────────────────────────────────────────────────
module acr 'modules/container-registry.bicep' = {
  name: 'containerRegistry'
  params: { location: location, namePrefix: namePrefix }
}

// ── Key Vault ───────────────────────────────────────────────────────────────
module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault'
  params: { location: location, namePrefix: namePrefix }
}

// ── Container Apps (all 7 services + gateway) ───────────────────────────────
module containerApps 'modules/container-apps.bicep' = {
  name: 'containerApps'
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
output resourceGroup string = resourceGroup().name
output gatewayUrl string = 'https://${containerApps.outputs.gatewayFqdn}'
output acrLoginServer string = acr.outputs.loginServer
output keyVaultName string = keyVault.outputs.keyVaultName
