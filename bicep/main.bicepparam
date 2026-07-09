using 'main.bicep'

param projectAbbr = 'voicecon'
param projectName = 'voice_consultant'
param location = 'australiaeast'

param principals = [
  {
    id: '4b74544b-02c6-4e4f-b936-732c9c3fff65'
    principalType: 'User'
  }
]

param fabricAdminMembers = [
  'danielfang@MngEnvMCAP951655.onmicrosoft.com'
  'fabric@MngEnvMCAP951655.onmicrosoft.com'
]

// Set to true to deploy the Fabric capacity. Skipped by default.
param deployFabric = false

param foundryAgentId = 'voicecon-insight'

param fabricWorkspaceId = '6d9003b1-ca61-42a8-8b95-2962e3d9a085'

param fabricLakehouseId = '46dfde66-933b-47c0-9433-5b92f9d497ca'

// Object id of the service principal used by the GitHub Actions deployment (AZURE_CREDENTIALS)
param deploymentPrincipalId = 'a6efe236-83c5-472b-a068-65006e369ad7'
