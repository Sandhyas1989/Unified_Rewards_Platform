// Foundation: VNet + subnets, Log Analytics + App Insights, Container Registry, Key Vault.
param namePrefix string
param location string

var acrName = replace('${namePrefix}acr', '-', '')

// ── Virtual Network ─────────────────────────────────────────────────────────
resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: '${namePrefix}-vnet'
  location: location
  properties: {
    addressSpace: { addressPrefixes: ['10.0.0.0/16'] }
    subnets: [
      { name: 'snet-aks',  properties: { addressPrefix: '10.0.0.0/20' } }
      { name: 'snet-apim', properties: { addressPrefix: '10.0.16.0/24' } }
      { name: 'snet-data', properties: { addressPrefix: '10.0.17.0/24' } }
    ]
  }
}

// ── Log Analytics + Application Insights ────────────────────────────────────
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${namePrefix}-logs'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-appi'
  location: location
  kind: 'web'
  properties: { Application_Type: 'web', WorkspaceResourceId: logAnalytics.id }
}

// ── Azure Container Registry ────────────────────────────────────────────────
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: take(acrName, 50)
  location: location
  sku: { name: 'Standard' } // use Premium for geo-replication + private endpoint
  properties: { adminUserEnabled: false }
}

// ── Key Vault (RBAC) ────────────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${namePrefix}-kv'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

output aksSubnetId string = '${vnet.id}/subnets/snet-aks'
output dataSubnetId string = '${vnet.id}/subnets/snet-data'
output logAnalyticsId string = logAnalytics.id
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer
output keyVaultName string = keyVault.name
