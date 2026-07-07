namespace VoiceConsultant.FunctionApp.Models;

public class CosmosOptions
{
    public string AccountEndpoint { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ConversationsContainerName { get; set; } = "conversations";
    public string InsightsContainerName { get; set; } = "insights";
}

public class FoundryOptions
{
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string ModelDeploymentName { get; set; } = string.Empty;
    public string McpServerUri { get; set; } = string.Empty;
}

public class FabricOptions
{
    public string TenantId { get; set; } = string.Empty;
    public string OneLakeUri { get; set; } = "https://onelake.dfs.fabric.microsoft.com";
    /// <summary>Fabric workspace identifier used in the OneLake path.</summary>
    public string WorkspaceId { get; set; } = string.Empty;
    /// <summary>Fabric lakehouse identifier used in the OneLake path.</summary>
    public string LakehouseId { get; set; } = string.Empty;
}
