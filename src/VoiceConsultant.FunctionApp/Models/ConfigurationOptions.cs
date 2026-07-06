namespace VoiceConsultant.FunctionApp.Models;

public class CosmosOptions
{
    public string AccountEndpoint { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ConversationsContainerName { get; set; } = "conversations";
    public string InsightsContainerName { get; set; } = "insights";
}

public class FoundryOptions
{
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
}
