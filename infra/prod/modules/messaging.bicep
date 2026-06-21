// Service Bus (claim lifecycle events) + Event Hub (audit stream).
param namePrefix string
param location string

resource sb 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${namePrefix}-bus'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }

  resource topic 'topics' = {
    name: 'urp-events'
    resource subRW 'subscriptions' = { name: 'reimbursement-workflow' }
    resource subPI 'subscriptions' = { name: 'payroll-integration' }
    resource subRC 'subscriptions' = { name: 'reporting-compliance' }
  }
}

resource eh 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: '${namePrefix}-events-hub'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
  resource hub 'eventhubs' = {
    name: 'audit-stream'
    properties: { partitionCount: 4, messageRetentionInDays: 7 }
  }
}

output serviceBusNamespace string = sb.name
output eventHubNamespace string = eh.name
