param location string
param namePrefix string
param tenantId string = subscription().tenantId
// Object ID of the managed identity (or service principal) that should read secrets.
param readerPrincipalId string = ''

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${namePrefix}-kv'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// Grant the Container Apps managed identity read access to secrets.
resource secretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(readerPrincipalId)) {
  name: guid(kv.id, readerPrincipalId, 'KeyVaultSecretsUser')
  scope: kv
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: readerPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output keyVaultName string = kv.name
output keyVaultUri string = kv.properties.vaultUri
