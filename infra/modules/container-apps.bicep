param location string
param namePrefix string
param acrLoginServer string
param acrUsername string
@secure()
param acrPassword string
param imageTag string = 'latest'

param serviceBusConnectionString string
param blobConnectionString string

// Azure Files storage account — used for per-service SQLite persistence across restarts/scale events.
param storageAccountName string
@secure()
param storageAccountKey string

// Log Analytics for Container Apps environment
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${namePrefix}-logs'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource env 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: '${namePrefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Register one Azure File Share per service with the Container Apps environment.
// Each service's SQLite .db file is stored on its own share so data survives restarts.
resource envStorageEP 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  parent: env
  name: 'employee-profile-data'
  properties: { azureFile: { accountName: storageAccountName, accountKey: storageAccountKey, shareName: 'employee-profile-data', accessMode: 'ReadWrite' } }
}
resource envStorageBC 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  parent: env
  name: 'benefits-catalogue-data'
  properties: { azureFile: { accountName: storageAccountName, accountKey: storageAccountKey, shareName: 'benefits-catalogue-data', accessMode: 'ReadWrite' } }
}
resource envStorageCR 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  parent: env
  name: 'compensation-rules-data'
  properties: { azureFile: { accountName: storageAccountName, accountKey: storageAccountKey, shareName: 'compensation-rules-data', accessMode: 'ReadWrite' } }
}
resource envStorageRW 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  parent: env
  name: 'reimbursement-workflow-data'
  properties: { azureFile: { accountName: storageAccountName, accountKey: storageAccountKey, shareName: 'reimbursement-workflow-data', accessMode: 'ReadWrite' } }
}
resource envStorageDP 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  parent: env
  name: 'document-processing-data'
  properties: { azureFile: { accountName: storageAccountName, accountKey: storageAccountKey, shareName: 'document-processing-data', accessMode: 'ReadWrite' } }
}
resource envStoragePI 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  parent: env
  name: 'payroll-integration-data'
  properties: { azureFile: { accountName: storageAccountName, accountKey: storageAccountKey, shareName: 'payroll-integration-data', accessMode: 'ReadWrite' } }
}
resource envStorageRC 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  parent: env
  name: 'reporting-compliance-data'
  properties: { azureFile: { accountName: storageAccountName, accountKey: storageAccountKey, shareName: 'reporting-compliance-data', accessMode: 'ReadWrite' } }
}

// Standard liveness + readiness probes for every container (hits /health on port 80).
var healthProbes = [
  {
    type: 'Liveness'
    httpGet: { path: '/health', port: 80, scheme: 'HTTP' }
    initialDelaySeconds: 10
    periodSeconds: 30
    failureThreshold: 3
  }
  {
    type: 'Readiness'
    httpGet: { path: '/health', port: 80, scheme: 'HTTP' }
    initialDelaySeconds: 5
    periodSeconds: 10
    failureThreshold: 3
  }
]

// Env vars shared by every service that publishes/subscribes to Azure Service Bus.
var messagingEnv = [
  { name: 'Messaging__Provider',                     value: 'ServiceBus' }
  { name: 'Messaging__ServiceBus__ConnectionString', value: serviceBusConnectionString }
  { name: 'Messaging__ServiceBus__Topic',            value: 'urp-events' }
]

// ── Employee Profile ────────────────────────────────────────────────────────
resource employeeProfile 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${namePrefix}-employee-profile'
  location: location
  dependsOn: [envStorageEP]
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: false, targetPort: 80 }
      registries: [{ server: acrLoginServer, username: acrUsername, passwordSecretRef: 'acr-pwd' }]
      secrets: [{ name: 'acr-pwd', value: acrPassword }]
    }
    template: {
      containers: [{
        name: 'employee-profile'
        image: '${acrLoginServer}/employee-profile:${imageTag}'
        env: concat(messagingEnv, [
          { name: 'ASPNETCORE_URLS', value: 'http://+:80' }
          { name: 'DB_DIR',          value: '/app/data' }
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Azure' }
        ])
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: healthProbes
        volumeMounts: [{ volumeName: 'data', mountPath: '/app/data' }]
      }]
      volumes: [{ name: 'data', storageType: 'EmptyDir' }]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}

// ── Benefits Catalogue ──────────────────────────────────────────────────────
resource benefitsCatalogue 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${namePrefix}-benefits-catalogue'
  location: location
  dependsOn: [envStorageBC]
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: false, targetPort: 80 }
      registries: [{ server: acrLoginServer, username: acrUsername, passwordSecretRef: 'acr-pwd' }]
      secrets: [{ name: 'acr-pwd', value: acrPassword }]
    }
    template: {
      containers: [{
        name: 'benefits-catalogue'
        image: '${acrLoginServer}/benefits-catalogue:${imageTag}'
        env: [
          { name: 'ASPNETCORE_URLS', value: 'http://+:80' }
          { name: 'DB_DIR',          value: '/app/data' }
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Azure' }
        ]
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: healthProbes
        volumeMounts: [{ volumeName: 'data', mountPath: '/app/data' }]
      }]
      volumes: [{ name: 'data', storageType: 'EmptyDir' }]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}

// ── Compensation Rules ──────────────────────────────────────────────────────
resource compensationRules 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${namePrefix}-compensation-rules'
  location: location
  dependsOn: [envStorageCR]
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: false, targetPort: 80 }
      registries: [{ server: acrLoginServer, username: acrUsername, passwordSecretRef: 'acr-pwd' }]
      secrets: [{ name: 'acr-pwd', value: acrPassword }]
    }
    template: {
      containers: [{
        name: 'compensation-rules'
        image: '${acrLoginServer}/compensation-rules:${imageTag}'
        env: [
          { name: 'ASPNETCORE_URLS', value: 'http://+:80' }
          { name: 'DB_DIR',          value: '/app/data' }
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Azure' }
        ]
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: healthProbes
        volumeMounts: [{ volumeName: 'data', mountPath: '/app/data' }]
      }]
      volumes: [{ name: 'data', storageType: 'EmptyDir' }]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}

// ── Reimbursement Workflow ──────────────────────────────────────────────────
resource reimbursementWorkflow 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${namePrefix}-reimbursement-workflow'
  location: location
  dependsOn: [envStorageRW]
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: false, targetPort: 80 }
      registries: [{ server: acrLoginServer, username: acrUsername, passwordSecretRef: 'acr-pwd' }]
      secrets: [{ name: 'acr-pwd', value: acrPassword }]
    }
    template: {
      containers: [{
        name: 'reimbursement-workflow'
        image: '${acrLoginServer}/reimbursement-workflow:${imageTag}'
        env: concat(messagingEnv, [
          { name: 'ASPNETCORE_URLS',                        value: 'http://+:80' }
          { name: 'DB_DIR',                                 value: '/app/data' }
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Azure' }
          { name: 'Messaging__ServiceBus__Subscription',    value: 'reimbursement-workflow' }
        ])
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: healthProbes
        volumeMounts: [{ volumeName: 'data', mountPath: '/app/data' }]
      }]
      volumes: [{ name: 'data', storageType: 'EmptyDir' }]
      scale: { minReplicas: 2, maxReplicas: 10 }
    }
  }
}

// ── Document Processing ─────────────────────────────────────────────────────
resource documentProcessing 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${namePrefix}-document-processing'
  location: location
  dependsOn: [envStorageDP]
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: false, targetPort: 80 }
      registries: [{ server: acrLoginServer, username: acrUsername, passwordSecretRef: 'acr-pwd' }]
      secrets: [{ name: 'acr-pwd', value: acrPassword }]
    }
    template: {
      containers: [{
        name: 'document-processing'
        image: '${acrLoginServer}/document-processing:${imageTag}'
        env: concat(messagingEnv, [
          { name: 'ASPNETCORE_URLS',                     value: 'http://+:80' }
          { name: 'DB_DIR',                              value: '/app/data' }
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Azure' }
          { name: 'Storage__Provider',                   value: 'AzureBlob' }
          { name: 'Storage__AzureBlob__ConnectionString', value: blobConnectionString }
          { name: 'Storage__AzureBlob__Container',       value: 'receipts' }
        ])
        resources: { cpu: json('0.5'), memory: '1Gi' }
        probes: healthProbes
        volumeMounts: [{ volumeName: 'data', mountPath: '/app/data' }]
      }]
      volumes: [{ name: 'data', storageType: 'EmptyDir' }]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}

// ── Payroll Integration ─────────────────────────────────────────────────────
resource payrollIntegration 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${namePrefix}-payroll-integration'
  location: location
  dependsOn: [envStoragePI]
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: false, targetPort: 80 }
      registries: [{ server: acrLoginServer, username: acrUsername, passwordSecretRef: 'acr-pwd' }]
      secrets: [{ name: 'acr-pwd', value: acrPassword }]
    }
    template: {
      containers: [{
        name: 'payroll-integration'
        image: '${acrLoginServer}/payroll-integration:${imageTag}'
        env: concat(messagingEnv, [
          { name: 'ASPNETCORE_URLS',                     value: 'http://+:80' }
          { name: 'DB_DIR',                              value: '/app/data' }
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Azure' }
          { name: 'Messaging__ServiceBus__Subscription', value: 'payroll-integration' }
        ])
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: healthProbes
        volumeMounts: [{ volumeName: 'data', mountPath: '/app/data' }]
      }]
      volumes: [{ name: 'data', storageType: 'EmptyDir' }]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}

// ── Reporting & Compliance ──────────────────────────────────────────────────
resource reportingCompliance 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${namePrefix}-reporting-compliance'
  location: location
  dependsOn: [envStorageRC]
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: false, targetPort: 80 }
      registries: [{ server: acrLoginServer, username: acrUsername, passwordSecretRef: 'acr-pwd' }]
      secrets: [{ name: 'acr-pwd', value: acrPassword }]
    }
    template: {
      containers: [{
        name: 'reporting-compliance'
        image: '${acrLoginServer}/reporting-compliance:${imageTag}'
        env: concat(messagingEnv, [
          { name: 'ASPNETCORE_URLS',                     value: 'http://+:80' }
          { name: 'DB_DIR',                              value: '/app/data' }
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Azure' }
          { name: 'Messaging__ServiceBus__Subscription', value: 'reporting-compliance' }
          { name: 'Services__Reimbursement',             value: 'https://${reimbursementWorkflow.properties.configuration.ingress.fqdn}' }
          { name: 'Services__Payroll',                   value: 'https://${payrollIntegration.properties.configuration.ingress.fqdn}' }
        ])
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: healthProbes
        volumeMounts: [{ volumeName: 'data', mountPath: '/app/data' }]
      }]
      volumes: [{ name: 'data', storageType: 'EmptyDir' }]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}

// ── API Gateway (YARP) — the only public-facing endpoint ───────────────────
resource gateway 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${namePrefix}-gateway'
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: true, targetPort: 80 }
      registries: [{ server: acrLoginServer, username: acrUsername, passwordSecretRef: 'acr-pwd' }]
      secrets: [{ name: 'acr-pwd', value: acrPassword }]
    }
    template: {
      containers: [{
        name: 'gateway'
        image: '${acrLoginServer}/gateway:${imageTag}'
        env: [
          { name: 'ASPNETCORE_URLS', value: 'http://+:80' }
          { name: 'ReverseProxy__Clusters__employee-profile__Destinations__primary__Address',       value: 'https://${employeeProfile.properties.configuration.ingress.fqdn}' }
          { name: 'ReverseProxy__Clusters__benefits-catalogue__Destinations__primary__Address',     value: 'https://${benefitsCatalogue.properties.configuration.ingress.fqdn}' }
          { name: 'ReverseProxy__Clusters__compensation-rules__Destinations__primary__Address',     value: 'https://${compensationRules.properties.configuration.ingress.fqdn}' }
          { name: 'ReverseProxy__Clusters__reimbursement-workflow__Destinations__primary__Address', value: 'https://${reimbursementWorkflow.properties.configuration.ingress.fqdn}' }
          { name: 'ReverseProxy__Clusters__document-processing__Destinations__primary__Address',    value: 'https://${documentProcessing.properties.configuration.ingress.fqdn}' }
          { name: 'ReverseProxy__Clusters__payroll-integration__Destinations__primary__Address',    value: 'https://${payrollIntegration.properties.configuration.ingress.fqdn}' }
          { name: 'ReverseProxy__Clusters__reporting-compliance__Destinations__primary__Address',   value: 'https://${reportingCompliance.properties.configuration.ingress.fqdn}' }
          // After deploying the frontend (Static Web App), update CORS by running:
          // az containerapp update -n ${namePrefix}-gateway -g ${namePrefix}-rg \
          //   --set-env-vars "Cors__AllowedOrigins__0=https://<your-swa-url>"
        ]
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: healthProbes
      }]
      scale: { minReplicas: 2, maxReplicas: 10 }
    }
  }
}

output gatewayFqdn string = gateway.properties.configuration.ingress.fqdn
