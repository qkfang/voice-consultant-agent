namespace VoiceConsultant.Web.Models;

public class CosmosOptions
{
    public string AccountEndpoint { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ConversationsContainerName { get; set; } = "conversations";
    public string InsightsContainerName { get; set; } = "insights";
}
