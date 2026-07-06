using System.Text.Json.Serialization;

namespace VoiceConsultant.FunctionApp.Models;

/// <summary>
/// A single call conversation transcript stored in the "conversations" container.
/// </summary>
public class ConversationDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Partition key. Identifies the voice call this transcript belongs to.</summary>
    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    [JsonPropertyName("consultantId")]
    public string? ConsultantId { get; set; }

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    /// <summary>Full or partial call transcript text.</summary>
    [JsonPropertyName("transcript")]
    public string Transcript { get; set; } = string.Empty;

    /// <summary>Where the transcript came from, e.g. "changefeed" or "api".</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "api";

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
