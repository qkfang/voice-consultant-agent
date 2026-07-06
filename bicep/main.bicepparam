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
