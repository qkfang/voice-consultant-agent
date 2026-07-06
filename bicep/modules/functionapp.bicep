@description('Azure location')
param location string

@description('Function App name')
param functionAppName string

@description('App Service plan name (Consumption/Elastic Premium Linux plan)')
param appServicePlanName string

@description('Storage account name required by the Functions runtime')
param storageAccountName string

@description('App Insights connection string')
param appInsightsConnectionString string

@description('Cosmos DB account endpoint')
param cosmosAccountEndpoint string

@description('Cosmos DB database name')
param cosmosDatabaseName string

@description('Cosmos DB conversations container name (change feed source)')
param cosmosConversationsContainerName string

@description('Cosmos DB insights container name (agent output target)')
param cosmosInsightsContainerName string

@description('Cosmos DB leases container name (change feed trigger)')
param cosmosLeasesContainerName string

@description('Azure AI Foundry project endpoint')
param foundryProjectEndpoint string

@description('Azure AI Foundry agent id used to analyse conversations')
param foundryAgentId string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccount.name
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'Cosmos__accountEndpoint'
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
        {
          name: 'Cosmos__LeasesContainerName'
          value: cosmosLeasesContainerName
        }
        {
          name: 'Foundry__ProjectEndpoint'
          value: foundryProjectEndpoint
        }
        {
          name: 'Foundry__AgentId'
          value: foundryAgentId
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output principalId string = functionApp.identity.principalId
output storageAccountName string = storageAccount.name
