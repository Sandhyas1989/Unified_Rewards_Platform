param location string
param namePrefix string

var saName = replace('${namePrefix}receipts', '-', '')

resource sa 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: take(saName, 24)
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: { minimumTlsVersion: 'TLS1_2', allowBlobPublicAccess: false }

  resource blobSvc 'blobServices' = {
    name: 'default'
    resource receipts 'containers' = {
      name: 'receipts'
      properties: { publicAccess: 'None' }
    }
  }

  // Azure File Shares — one per service for SQLite persistence across ACA restarts/scale events.
  resource fileSvc 'fileServices' = {
    name: 'default'
    resource epShare   'shares' = { name: 'employee-profile-data' }
    resource bcShare   'shares' = { name: 'benefits-catalogue-data' }
    resource crShare   'shares' = { name: 'compensation-rules-data' }
    resource rwShare   'shares' = { name: 'reimbursement-workflow-data' }
    resource dpShare   'shares' = { name: 'document-processing-data' }
    resource piShare   'shares' = { name: 'payroll-integration-data' }
    resource rcShare   'shares' = { name: 'reporting-compliance-data' }
  }
}

output blobConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${sa.name};AccountKey=${sa.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
output storageAccountName string = sa.name
output storageAccountKey string = sa.listKeys().keys[0].value
