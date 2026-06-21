// Data tier: Azure SQL (relational), Cosmos DB (NoSQL, multi-region), Redis (cache), Storage (blob).
// NOTE: Azure SQL / Cosmos / Redis are provisioned for the target architecture but the app uses
// SQLite today — wire them per the code-change checklist before they carry real data. Storage (blob)
// and Service Bus ARE used by the current images (document-processing + messaging).
param namePrefix string
param location string
param secondaryLocation string
param sqlAdminLogin string
@secure()
param sqlAdminPassword string

var saName = take(replace('${namePrefix}stg', '-', ''), 24)

// ── Azure SQL ───────────────────────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${namePrefix}-sqlsrv'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
  resource db 'databases' = {
    name: 'urp-sql'
    location: location
    sku: { name: 'GP_S_Gen5_1', tier: 'GeneralPurpose' } // serverless General Purpose
  }
}

// ── Cosmos DB (multi-region write) ──────────────────────────────────────────
resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: '${namePrefix}-cosmos'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableMultipleWriteLocations: true
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    locations: [
      { locationName: location, failoverPriority: 0, isZoneRedundant: false }
      { locationName: secondaryLocation, failoverPriority: 1, isZoneRedundant: false }
    ]
  }
  resource db 'sqlDatabases' = {
    name: 'urp'
    properties: { resource: { id: 'urp' } }
    resource cPlans 'containers' = {
      name: 'benefitPlans'
      properties: { resource: { id: 'benefitPlans', partitionKey: { paths: ['/tenantId'], kind: 'Hash' } } }
    }
    resource cRules 'containers' = {
      name: 'compensationRules'
      properties: { resource: { id: 'compensationRules', partitionKey: { paths: ['/tenantId'], kind: 'Hash' } } }
    }
  }
}

// ── Redis (Premium) ─────────────────────────────────────────────────────────
resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: '${namePrefix}-redis'
  location: location
  properties: {
    sku: { name: 'Premium', family: 'P', capacity: 1 }
    minimumTlsVersion: '1.2'
  }
}

// ── Storage + Blob (receipts) ───────────────────────────────────────────────
resource sa 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: saName
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_GRS' }
  properties: { minimumTlsVersion: 'TLS1_2', allowBlobPublicAccess: false }
  resource blob 'blobServices' = {
    name: 'default'
    resource receipts 'containers' = { name: 'receipts' }
  }
}

output sqlServerName string = sqlServer.name
output cosmosName string = cosmos.name
output redisName string = redis.name
output storageAccountName string = sa.name
