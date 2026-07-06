

az group create --name "rg-{projectName}" --location 'australiaeast'

az deployment group create --name "deploy-{projectName}" --resource-group "rg-{projectName}" --template-file './main.bicep' --parameters './main.bicepparam'

# sp-demo-01
$spObjectId = 'a6efe236-83c5-472b-a068-65006e369ad7'  
$subscriptionId = az account show --query 'id' -o tsv
az role assignment create --assignee-object-id $spObjectId --assignee-principal-type ServicePrincipal --role 'Contributor' --scope "/subscriptions/$subscriptionId/resourceGroups/rg-{projectName}"
az role assignment create --assignee-object-id $spObjectId --assignee-principal-type ServicePrincipal --role 'User Access Administrator' --scope "/subscriptions/$subscriptionId/resourceGroups/rg-{projectName}"

