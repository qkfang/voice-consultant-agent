@description('Azure location')
param location string

@description('Web App name')
param webAppName string

@description('App Service plan name')
param appServicePlanName string

@description('SKU for App Service plan')
param appServiceSku string

@description('App Insights connection string')
param appInsightsConnectionString string

@description('Cosmos DB account endpoint')
param cosmosAccountEndpoint string

@description('Cosmos DB database name')
param cosmosDatabaseName string

@description('Cosmos DB conversations container name')
param cosmosConversationsContainerName string

@description('Cosmos DB insights container name')
param cosmosInsightsContainerName string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServiceSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appCommandLine: 'dotnet VoiceConsultant.Web.dll'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'false'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'Cosmos__AccountEndpoint'
          value: cosmosAccountEndpoint
        }
        {
          name: 'Cosmos__DatabaseName'
          value: cosmosDatabaseName
        }
        {
          name: 'Cosmos__ConversationsContainerName'
          value: cosmosConversationsContainerName
        }
        {
          name: 'Cosmos__InsightsContainerName'
          value: cosmosInsightsContainerName
        }
      ]
    }
    httpsOnly: true
  }
}

output webAppName string = webApp.name
output principalId string = webApp.identity.principalId
