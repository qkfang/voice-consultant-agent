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
          "hardshipDetected": true|false,
          "issues": ["..."],
          "suggestions": ["..."],
          "summary": "..."
        }
        - hardshipDetected: whether the customer shows any sign of financial or personal hardship.
        - issues: concrete problems the customer raised.
        - suggestions: recommended next actions for the consultant.
        - summary: a short overview of the call.
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
