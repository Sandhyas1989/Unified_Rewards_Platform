// Unified Rewards Platform — PRODUCTION infrastructure (subscription-scoped).
//
// Deploys: VNet + Log Analytics/App Insights + ACR + Key Vault (foundation), AKS cluster,
//          Service Bus + Event Hub (messaging), and the data tier (Azure SQL, Cosmos, Redis,
//          Storage). APIM, Front Door, Functions, Communication Services and the Entra app
//          registration are provisioned outside this template (see the Portal guide / steps doc).
//
//   az deployment sub create -l eastus2 -f infra/prod/main.bicep -p infra/prod/parameters/prod.json
//
// NOTE: the app runs on AKS with its current code (SQLite). Azure SQL/Cosmos/Redis are provisioned
// here but require the code-change checklist before the app uses them.

targetScope = 'subscription'

@description('Lowercase prefix for resource names (3-12 chars).')
@minLength(3)
@maxLength(12)
param namePrefix string = 'urpprod'

@description('Primary region.')
param location string = 'eastus2'

@description('Secondary region for geo-replication / multi-region.')
param secondaryLocation string = 'westus2'

@description('SQL admin login (prefer Entra-only auth in real prod).')
param sqlAdminLogin string = 'urpadmin'

@secure()
@description('SQL admin password. Pass at deploy time; do not commit.')
param sqlAdminPassword string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: '${namePrefix}-rg'
  location: location
}

module foundation 'modules/foundation.bicep' = {
  name: 'foundation'
  scope: rg
  params: { namePrefix: namePrefix, location: location }
}

module aks 'modules/aks.bicep' = {
  name: 'aks'
  scope: rg
  params: {
    namePrefix: namePrefix
    location: location
    aksSubnetId: foundation.outputs.aksSubnetId
    acrName: foundation.outputs.acrName
    logAnalyticsId: foundation.outputs.logAnalyticsId
  }
}

module messaging 'modules/messaging.bicep' = {
  name: 'messaging'
  scope: rg
  params: { namePrefix: namePrefix, location: location }
}

module data 'modules/data.bicep' = {
  name: 'data'
  scope: rg
  params: {
    namePrefix: namePrefix
    location: location
    secondaryLocation: secondaryLocation
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
  }
}

output resourceGroup string = rg.name
output acrLoginServer string = foundation.outputs.acrLoginServer
output acrName string = foundation.outputs.acrName
output aksName string = aks.outputs.aksName
output keyVaultName string = foundation.outputs.keyVaultName
output appInsightsConnectionString string = foundation.outputs.appInsightsConnectionString
output serviceBusNamespace string = messaging.outputs.serviceBusNamespace
output storageAccountName string = data.outputs.storageAccountName
