using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
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

        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(foundryOptions.TenantId))
        {
            credentialOptions.TenantId = foundryOptions.TenantId;
        }

        // Locally there is no IMDS endpoint, so skip Managed Identity to avoid an
        // unrecoverable auth failure that would otherwise stop the credential chain.
        // WEBSITE_SITE_NAME is reliably set on the Azure Functions/App Service host.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
        {
            credentialOptions.ExcludeManagedIdentityCredential = true;
        }

        var projectClient = new AIProjectClient(new Uri(foundryOptions.ProjectEndpoint), new DefaultAzureCredential(credentialOptions));

        var tools = new List<ResponseTool>();
        if (!string.IsNullOrWhiteSpace(foundryOptions.McpServerUri))
        {
            var mcpTool = ResponseTool.CreateMcpTool(
                serverLabel: "voicecon-mcp",
                serverUri: new Uri($"{foundryOptions.McpServerUri.TrimEnd('/')}/runtime/webhooks/mcp"),
                toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

            // Sensitive headers are not accepted inline; the MCP webhook credential must be
            // supplied through a Foundry project connection referenced by its connection id.
            if (!string.IsNullOrWhiteSpace(foundryOptions.McpConnectionId))
            {
                mcpTool.ProjectConnectionId = foundryOptions.McpConnectionId;
            }

            tools.Add(mcpTool);
        }

        _agent = new ConversationInsightAgent(
            projectClient,
            foundryOptions.ModelDeploymentName,
            tools,
            loggerFactory.CreateLogger<ConversationInsightAgent>());
    }

    public async Task<FoundryAnalysisResult> AnalyzeAsync(ConversationDocument conversation, CancellationToken cancellationToken = default)
    {
        var responseText = await _agent.RunAsync(conversation.Transcript);

        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger.LogWarning("Foundry agent returned no output for call {CallId}", conversation.CallId);
        }

        return new FoundryAnalysisResult(
            AgentResponseParser.Parse(responseText, conversation),
            responseText);
    }
}

public sealed record FoundryAnalysisResult(InsightDocument Insight, string ResponseText);
