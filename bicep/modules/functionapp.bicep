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

@description('Azure AI Foundry model deployment name used by the conversation insight agent')
param foundryModelDeploymentName string

@description('Fabric workspace id for writing final agent output to the lakehouse')
param fabricWorkspaceId string = ''

@description('Fabric lakehouse id for writing final agent output')
param fabricLakehouseId string = ''

@description('Object id of the CI/deployment principal that uploads the function package')
param deploymentPrincipalId string = ''

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: {
    SecurityControl: 'Ignore'
  }
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
          name: 'Cosmos__TenantId'
          value: tenant().tenantId
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
          name: 'Foundry__TenantId'
          value: tenant().tenantId
        }
        {
          name: 'Foundry__AgentId'
          value: foundryAgentId
        }
        {
          name: 'Foundry__ModelDeploymentName'
          value: foundryModelDeploymentName
        }
        {
          name: 'Foundry__McpServerUri'
          value: 'https://${functionAppName}.azurewebsites.net'
        }
        {
          name: 'Fabric__TenantId'
          value: tenant().tenantId
        }
        {
          name: 'Fabric__OneLakeUri'
          value: 'https://onelake.dfs.fabric.microsoft.com'
        }
        {
          name: 'Fabric__WorkspaceId'
          value: fabricWorkspaceId
        }
        {
          name: 'Fabric__LakehouseId'
          value: fabricLakehouseId
        }
        {
          name: 'Fabric__LandTranscription'
          value: 'true'
        }
      ]
    }
  }
}

var storageBlobDataOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

// Function App identity needs blob data access for identity-based AzureWebJobsStorage and run-from-package
resource functionAppStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount
  name: guid(storageAccount.id, functionApp.id, storageBlobDataOwnerRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataOwnerRoleId)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// CI/deployment principal needs blob data access to upload the package during deployment
resource deploymentStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(deploymentPrincipalId)) {
  scope: storageAccount
  name: guid(storageAccount.id, deploymentPrincipalId, storageBlobDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: deploymentPrincipalId
  }
}

output functionAppName string = functionApp.name
output principalId string = functionApp.identity.principalId
output storageAccountName string = storageAccount.name
