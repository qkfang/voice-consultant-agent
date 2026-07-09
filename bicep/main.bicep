targetScope = 'resourceGroup'

@description('Azure location')
param location string = resourceGroup().location

@description('Project abbreviation for resources')
@minLength(1)
param projectAbbr string

@description('Project abbreviation for resources')
@minLength(1)
param projectName string

@description('SKU for App Service plan')
@allowed([
  'F1'
  'B1'
  'S1'
])
param appServiceSku string = 'S1'

@description('Additional principals to grant Cosmos DB data access')
param principals array = []

@description('UPN/email addresses of Fabric capacity administrators')
param fabricAdminMembers array = []

@description('Deploy the Fabric capacity. Skipped by default.')
param deployFabric bool = false

@description('Azure AI Foundry agent id used by the function app to analyse call conversations')
param foundryAgentId string = ''

@description('Fabric workspace id used by the function app to store final agent output')
param fabricWorkspaceId string = ''

@description('Fabric lakehouse id used by the function app to store final agent output')
param fabricLakehouseId string = ''

@description('Object id of the CI/deployment principal that uploads the function package')
param deploymentPrincipalId string = ''

var logAnalyticsName = '${projectAbbr}-law'
var appInsightsName = '${projectAbbr}-appi'
var functionsStorageAccountName = toLower('${projectAbbr}fasa')

var appServicePlanName = '${projectAbbr}-plan'
var webAppName = '${projectAbbr}-web'

var functionAppPlanName = '${projectAbbr}-func-plan'
var functionAppName = '${projectAbbr}-func'

var cosmosAccountName = toLower('${projectAbbr}-cosmos')

var aiProjectName = '${projectAbbr}-proj'
var aiServicesName = '${projectAbbr}-ais'
var fabricCapacityName = '${projectAbbr}fabric'


module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    logAnalyticsName: logAnalyticsName
    appInsightsName: appInsightsName
  }
}

module cosmos './modules/cosmosdb.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    accountName: cosmosAccountName
  }
}

module foundry './modules/foundry.bicep' = {
  name: 'foundry'
  params: {
    location: location
    foundryServicesName: aiServicesName
    foundryProjectName: aiProjectName
    mcpConnectionName: 'voicecon-mcp'
    mcpConnectionTarget: 'https://${functionAppName}.azurewebsites.net/runtime/webhooks/mcp'
  }
}

module fabric './modules/fabric.bicep' = if (deployFabric) {
  name: 'fabric'
  params: {
    location: location
    capacityName: fabricCapacityName
    skuName: 'F2'
    adminMembers: fabricAdminMembers
  }
}


module appService './modules/appservice.bicep' = {
  name: 'appservice'
  params: {
    location: location
    webAppName: webAppName
    appServicePlanName: appServicePlanName
    appServiceSku: appServiceSku
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    cosmosAccountEndpoint: cosmos.outputs.documentEndpoint
    cosmosDatabaseName: cosmos.outputs.databaseName
    cosmosConversationsContainerName: cosmos.outputs.conversationsContainerName
    cosmosInsightsContainerName: cosmos.outputs.insightsContainerName
  }
}

module functionApp './modules/functionapp.bicep' = {
  name: 'functionapp'
  params: {
    location: location
    functionAppName: functionAppName
    appServicePlanName: functionAppPlanName
    storageAccountName: functionsStorageAccountName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    cosmosAccountEndpoint: cosmos.outputs.documentEndpoint
    cosmosDatabaseName: cosmos.outputs.databaseName
    cosmosConversationsContainerName: cosmos.outputs.conversationsContainerName
    cosmosInsightsContainerName: cosmos.outputs.insightsContainerName
    cosmosLeasesContainerName: cosmos.outputs.leasesContainerName
    foundryProjectEndpoint: foundry.outputs.aiProjectEndpoint
    foundryAgentId: foundryAgentId
    foundryMcpConnectionId: foundry.outputs.mcpConnectionName
    foundryModelDeploymentName: foundry.outputs.modelDeploymentName
    fabricWorkspaceId: fabricWorkspaceId
    fabricLakehouseId: fabricLakehouseId
    deploymentPrincipalId: deploymentPrincipalId
  }
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
  name: cosmosAccountName
  dependsOn: [
    cosmos
  ]
}

var cosmosDataContributorRoleId = '00000000-0000-0000-0000-000000000002'

// Function App needs read/write access to conversations, insights and leases containers
resource functionAppCosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
  parent: cosmosAccount
  name: guid(cosmosAccount.id, functionAppName, cosmosDataContributorRoleId)
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'
    principalId: functionApp.outputs.principalId
    scope: cosmosAccount.id
  }
}

// Web App reads conversations/insights and writes new conversations from the UI
resource webAppCosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
  parent: cosmosAccount
  name: guid(cosmosAccount.id, webAppName, cosmosDataContributorRoleId)
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'
    principalId: appService.outputs.principalId
    scope: cosmosAccount.id
  }
}

resource principalCosmosDataContributorAssignments 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = [for principal in principals: {
  parent: cosmosAccount
  name: guid(cosmosAccount.id, principal.id, cosmosDataContributorRoleId)
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'
    principalId: principal.id
    scope: cosmosAccount.id
  }
}]

var readerRoleId = 'acdd72a7-3385-48ef-bd42-f606fba81ae7'

// Principals need control-plane read access to open the Cosmos DB Data Explorer in the portal
resource principalCosmosReaderAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principal in principals: {
  scope: cosmosAccount
  name: guid(cosmosAccount.id, principal.id, readerRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', readerRoleId)
    principalId: principal.id
    principalType: principal.principalType
  }
}]

resource foundryAccount 'Microsoft.CognitiveServices/accounts@2025-10-01-preview' existing = {
  name: aiServicesName
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' existing = {
  parent: foundryAccount
  name: aiProjectName
}

// Foundry User role grants the data-plane agents/* actions (create/read/run) the function app needs
var foundryUserRoleId = '53ca6127-db72-4b80-b1b0-d745d6d5456d'

// Function App needs to create and call agents in the Azure AI Foundry project
resource functionAppFoundryRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: foundryProject
  name: guid(foundryProject.id, functionAppName, foundryUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', foundryUserRoleId)
    principalId: functionApp.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}
