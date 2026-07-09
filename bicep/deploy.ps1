

az group create --name "rg-voicecon" --location 'australiaeast'

az deployment group create --name "deploy-voicecon" --resource-group "rg-voicecon" --template-file './main.bicep' --parameters './main.bicepparam'

# Store the MCP webhook system key in a Foundry "Custom keys" project connection so the agent can
# authenticate to the MCP endpoint without passing sensitive headers inline (run after the function app code is deployed)
$mcpKey = az functionapp keys list --resource-group "rg-voicecon" --name "voicecon-func" --query "systemKeys.mcp_extension" -o tsv
if ($mcpKey) {
    $subscriptionId = az account show --query 'id' -o tsv
    $connectionName = 'voicecon-mcp'
    $accountId = "/subscriptions/$subscriptionId/resourceGroups/rg-voicecon/providers/Microsoft.CognitiveServices/accounts/voicecon-ais"
    $connectionBody = @{
        properties = @{
            authType    = 'CustomKeys'
            category    = 'CustomKeys'
            target      = 'https://voicecon-func.azurewebsites.net/runtime/webhooks/mcp'
            isSharedToAll = $false
            credentials = @{ keys = @{ 'x-functions-key' = $mcpKey } }
        }
    } | ConvertTo-Json -Depth 6

    az rest --method put `
        --url "https://management.azure.com$accountId/projects/voicecon-proj/connections/$connectionName?api-version=2025-04-01-preview" `
        --body $connectionBody -o none

    az functionapp config appsettings set --resource-group "rg-voicecon" --name "voicecon-func" --settings "Foundry__McpConnectionId=$connectionName" -o none
}

# sp-demo-01
$spObjectId = 'a6efe236-83c5-472b-a068-65006e369ad7'  
$subscriptionId = az account show --query 'id' -o tsv
az role assignment create --assignee-object-id $spObjectId --assignee-principal-type ServicePrincipal --role 'Contributor' --scope "/subscriptions/$subscriptionId/resourceGroups/rg-voicecon"
az role assignment create --assignee-object-id $spObjectId --assignee-principal-type ServicePrincipal --role 'User Access Administrator' --scope "/subscriptions/$subscriptionId/resourceGroups/rg-voicecon"

