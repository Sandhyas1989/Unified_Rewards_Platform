param location string
param namePrefix string

resource ns 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${namePrefix}-bus'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
  properties: { minimumTlsVersion: '1.2' }

  resource topic 'topics' = {
    name: 'urp-events'
    properties: { defaultMessageTimeToLive: 'P7D', enablePartitioning: false }

    // One subscription per consuming service; each filters all messages (no SQL filter needed —
    // handlers inspect EventType themselves for idempotent multi-event routing).
    resource subReimbursement 'subscriptions' = {
      name: 'reimbursement-workflow'
      properties: { lockDuration: 'PT1M', maxDeliveryCount: 3 }
    }
    resource subPayroll 'subscriptions' = {
      name: 'payroll-integration'
      properties: { lockDuration: 'PT1M', maxDeliveryCount: 3 }
    }
    resource subReporting 'subscriptions' = {
      name: 'reporting-compliance'
      properties: { lockDuration: 'PT1M', maxDeliveryCount: 3 }
    }
  }
}

output connectionString string = listKeys(
  '${ns.id}/AuthorizationRules/RootManageSharedAccessKey',
  ns.apiVersion
).primaryConnectionString
