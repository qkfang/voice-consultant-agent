using System.Text.Json.Serialization;

namespace VoiceConsultant.Web.Models;

public class InsightDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("suggestionDetected")]
    public bool SuggestionDetected { get; set; }

    [JsonPropertyName("suggestions")]
    public List<SuggestionItem> Suggestions { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}

public class SuggestionItem
{
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();
}
