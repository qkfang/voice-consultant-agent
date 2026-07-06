using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using VoiceConsultant.FunctionApp.Models;
using VoiceConsultant.FunctionApp.Services;

namespace VoiceConsultant.FunctionApp.Mcp;

/// <summary>
/// MCP tools that expose conversation and insight storage to the Foundry agent.
/// </summary>
[McpServerToolType]
public sealed class VoiceConsultantMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly CosmosService _cosmosService;

    public VoiceConsultantMcpTools(CosmosService cosmosService)
    {
        _cosmosService = cosmosService;
    }

    [McpServerTool(Name = "get_conversation_transcript"), Description("Read the most recent call transcript for a given callId. Returns JSON with callId, conversationId and transcript.")]
    public async Task<string> GetConversationTranscript(
        [Description("The call identifier to look up.")] string callId)
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

    [McpServerTool(Name = "store_insight"), Description("Persist an insight generated for a conversation to the insights store. Returns JSON with the stored insight id.")]
    public async Task<string> StoreInsight(
        [Description("The call identifier the insight belongs to.")] string callId,
        [Description("The source conversation identifier.")] string conversationId,
        [Description("Whether the customer shows signs of hardship.")] bool hardshipDetected,
        [Description("Issues raised by the customer.")] string[] issues,
        [Description("Suggested next actions for the consultant.")] string[] suggestions,
        [Description("Short summary of the call.")] string summary)
    {
        if (string.IsNullOrWhiteSpace(callId))
        {
            return "Error: callId is required.";
        }

        var insight = new InsightDocument
        {
            CallId = callId,
            ConversationId = conversationId ?? string.Empty,
            HardshipDetected = hardshipDetected,
            Issues = issues?.ToList() ?? new List<string>(),
            Suggestions = suggestions?.ToList() ?? new List<string>(),
            Summary = summary ?? string.Empty
        };

        await _cosmosService.SaveInsightAsync(insight);

        return JsonSerializer.Serialize(new { id = insight.Id, callId = insight.CallId }, JsonOptions);
    }
}
