using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using VoiceConsultant.FunctionApp.Agents;
using VoiceConsultant.FunctionApp.Models;

namespace VoiceConsultant.FunctionApp.Services;

/// <summary>
/// Sends a call transcript to the Foundry conversation insight agent and parses the response.
/// </summary>
public class FoundryAgentService
{
    private readonly ConversationInsightAgent _agent;
    private readonly ILogger<FoundryAgentService> _logger;

    public FoundryAgentService(
        IOptions<FoundryOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<FoundryAgentService> logger)
    {
        var foundryOptions = options.Value;
        _logger = logger;

        var projectClient = new AIProjectClient(new Uri(foundryOptions.ProjectEndpoint), new DefaultAzureCredential());

        var tools = new List<ResponseTool>();
        if (!string.IsNullOrWhiteSpace(foundryOptions.McpServerUri))
        {
            tools.Add(ResponseTool.CreateMcpTool(
                serverLabel: "voicecon-mcp",
                serverUri: new Uri($"{foundryOptions.McpServerUri.TrimEnd('/')}/mcp"),
                toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval)));
        }

        _agent = new ConversationInsightAgent(
            projectClient,
            foundryOptions.ModelDeploymentName,
            tools,
            loggerFactory.CreateLogger<ConversationInsightAgent>());
    }

    public async Task<InsightDocument> AnalyzeAsync(ConversationDocument conversation, CancellationToken cancellationToken = default)
    {
        var responseText = await _agent.RunAsync(conversation.Transcript);

        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger.LogWarning("Foundry agent returned no output for call {CallId}", conversation.CallId);
        }

        return AgentResponseParser.Parse(responseText, conversation);
    }
}
