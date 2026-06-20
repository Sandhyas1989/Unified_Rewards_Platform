param location string
param namePrefix string

var acrName = replace('${namePrefix}acr', '-', '')

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: take(acrName, 50)
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: true }
}

output loginServer string = acr.properties.loginServer
output adminUsername string = acr.listCredentials().username
output adminPassword string = acr.listCredentials().passwords[0].value
