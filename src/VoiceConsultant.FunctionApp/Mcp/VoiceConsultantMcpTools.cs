using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using VoiceConsultant.FunctionApp.Models;
using VoiceConsultant.FunctionApp.Services;

namespace VoiceConsultant.FunctionApp.Mcp;

/// <summary>
/// MCP tools that expose conversation and insight storage to the Foundry agent.
/// Served over the Azure Functions MCP extension at /runtime/webhooks/mcp.
/// </summary>
public sealed class VoiceConsultantMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly CosmosService _cosmosService;

    public VoiceConsultantMcpTools(CosmosService cosmosService)
    {
        _cosmosService = cosmosService;
    }

    [Function(nameof(GetConversationTranscript))]
    public async Task<string> GetConversationTranscript(
        [McpToolTrigger("get_conversation_transcript", "Read the most recent call transcript for a given callId. Returns JSON with callId, conversationId and transcript.")]
            ToolInvocationContext context,
        [McpToolProperty("callId", "The call identifier to look up.", isRequired: true)]
            string callId)
    {
        if (string.IsNullOrWhiteSpace(callId))
        {
            return "Error: callId is required.";
        }

        var conversation = await _cosmosService.GetLatestConversationAsync(callId);
        if (conversation is null)
        {
            return JsonSerializer.Serialize(new { callId, conversationId = string.Empty, transcript = string.Empty, error = "No conversation found." }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            callId = conversation.CallId,
            conversationId = conversation.Id,
            transcript = conversation.Transcript
        }, JsonOptions);
    }

    [Function(nameof(StoreInsight))]
    public async Task<string> StoreInsight(
        [McpToolTrigger("store_insight", "Persist an insight generated for a conversation to the insights store. Returns JSON with the stored insight id.")]
            ToolInvocationContext context,
        [McpToolProperty("callId", "The call identifier the insight belongs to.", isRequired: true)]
            string callId,
        [McpToolProperty("conversationId", "The source conversation identifier.")]
            string conversationId,
        [McpToolProperty("suggestionDetected", "Whether the analysis produced actionable suggestions.")]
            bool suggestionDetected,
        [McpToolProperty("suggestions", "JSON array of grouped suggestions, each item has a topic and a suggestions string array.")]
            string suggestions,
        [McpToolProperty("summary", "Short summary of the call.")]
            string summary)
    {
        if (string.IsNullOrWhiteSpace(callId))
        {
            return "Error: callId is required.";
        }

        var suggestionItems = new List<SuggestionItem>();
        if (!string.IsNullOrWhiteSpace(suggestions))
        {
            try
            {
                suggestionItems = JsonSerializer.Deserialize<List<SuggestionItem>>(suggestions) ?? new List<SuggestionItem>();
            }
            catch (JsonException)
            {
                // Ignore malformed suggestions and store an empty list.
            }
        }

        var insight = new InsightDocument
        {
            CallId = callId,
            ConversationId = conversationId ?? string.Empty,
            SuggestionDetected = suggestionDetected,
            Suggestions = suggestionItems,
            Summary = summary ?? string.Empty
        };

        await _cosmosService.SaveInsightAsync(insight);

        return JsonSerializer.Serialize(new { id = insight.Id, callId = insight.CallId }, JsonOptions);
    }
}
