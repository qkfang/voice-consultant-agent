@description('Azure location')
param location string

@description('Cosmos DB account name')
param accountName string

@description('Cosmos DB database name')
param databaseName string = 'voiceconsultant'

@description('Container name for raw call conversation history')
param conversationsContainerName string = 'conversations'

@description('Container name for agent-generated feedback/insights')
param insightsContainerName string = 'insights'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: accountName
  location: location
  kind: 'GlobalDocumentDB'
  tags: {
    SecurityControl: 'Ignore'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: []
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
    networkAclBypass: 'AzureServices'
    isVirtualNetworkFilterEnabled: false
    ipRules: []
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource conversationsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  parent: database
  name: conversationsContainerName
  properties: {
    resource: {
      id: conversationsContainerName
      partitionKey: {
        paths: ['/callId']
        kind: 'Hash'
      }
    }
  }
}

resource insightsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  parent: database
  name: insightsContainerName
  properties: {
    resource: {
      id: insightsContainerName
      partitionKey: {
        paths: ['/callId']
        kind: 'Hash'
      }
    }
  }
}

// Leases container used by the Function App's Cosmos DB change feed trigger
resource leasesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  parent: database
  name: 'leases'
  properties: {
    resource: {
      id: 'leases'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
    }
  }
}

output accountName string = cosmosAccount.name
output accountId string = cosmosAccount.id
output documentEndpoint string = cosmosAccount.properties.documentEndpoint
output databaseName string = database.name
output conversationsContainerName string = conversationsContainer.name
output insightsContainerName string = insightsContainer.name
output leasesContainerName string = leasesContainer.name
