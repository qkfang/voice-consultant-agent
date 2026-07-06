namespace VoiceConsultant.Web.Models;

/// <summary>
/// Combines a conversation and its (optional) agent insight for display purposes.
/// </summary>
public class CallSummary
{
    public ConversationDocument Conversation { get; set; } = new();
    public InsightDocument? Insight { get; set; }
}
