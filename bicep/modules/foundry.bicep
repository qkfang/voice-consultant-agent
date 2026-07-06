@description('Azure location')
param location string

@description('AI Services account name (serves as Foundry hub)')
param foundryServicesName string

@description('AI Foundry project name')
param foundryProjectName string


// Azure AI Services account with project management enabled
resource foundrySvc 'Microsoft.CognitiveServices/accounts@2025-10-01-preview' = {
  name: foundryServicesName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  properties: {
    allowProjectManagement: true
    customSubDomainName: foundryServicesName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Azure AI Foundry Project
resource aiProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: foundrySvc
  name: foundryProjectName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

// gpt-5.4 model deployment
resource gpt54Deployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundrySvc
  name: 'gpt-5.4'
  sku: {
    name: 'GlobalStandard'
    capacity: 900
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-5.4'
      version: '2026-03-05'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

output aiProjectEndpoint string = aiProject.properties.endpoints['AI Foundry API']
output aiServicesEndpoint string = foundrySvc.properties.endpoint
output modelDeploymentName string = gpt54Deployment.name
output aiHubPrincipalId string = foundrySvc.identity.principalId
