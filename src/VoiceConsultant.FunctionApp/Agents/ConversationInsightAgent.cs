using Azure.AI.Projects;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace VoiceConsultant.FunctionApp.Agents;

/// <summary>
/// Reviews a call centre transcript and returns hardship/issue detection feedback.
/// </summary>
public sealed class ConversationInsightAgent : BaseAgent
{
    private const string AgentInstructions = """
        You are a call centre quality analyst. You receive a call transcript in the user message.
        Review the transcript and, when MCP tools are available, you may call them to look up the
        conversation or persist the generated insight.
        Respond with a single JSON object using exactly these fields (no markdown fences, no extra keys):
        {
          "callId": "...",
          "guid": "...",
          "timestamp": "...",
          "suggestionDetected": true|false,
          "suggestions": [
            { "topic": "...", "suggestions": ["...", "..."] }
          ],
          "summary": "..."
        }
        - callId: the call identifier taken from the transcript.
        - guid: a unique identifier you generate for this insight.
        - timestamp: the UTC time the insight was generated, in ISO 8601 format.
        - suggestionDetected: whether the analysis produced any actionable suggestions.
        - suggestions: grouped suggestions, each with a topic and its list of suggestions for the consultant.
        - summary: an overall summary of the call.
        """;

    public ConversationInsightAgent(
        AIProjectClient aiProjectClient,
        string deploymentName,
        IList<ResponseTool>? tools = null,
        ILogger<ConversationInsightAgent>? logger = null)
        : base(aiProjectClient, "voicecon-insight", deploymentName, AgentInstructions, tools, logger)
    {
    }
}
