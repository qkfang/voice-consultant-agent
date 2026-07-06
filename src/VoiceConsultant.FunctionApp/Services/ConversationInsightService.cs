using Microsoft.Extensions.Logging;
using VoiceConsultant.FunctionApp.Models;

namespace VoiceConsultant.FunctionApp.Services;

/// <summary>
/// Coordinates calling the Foundry agent for a conversation and persisting the resulting insight.
/// </summary>
public class ConversationInsightService
{
    private readonly FoundryAgentService _foundryAgentService;
    private readonly CosmosService _cosmosService;
    private readonly ILogger<ConversationInsightService> _logger;

    public ConversationInsightService(
        FoundryAgentService foundryAgentService,
        CosmosService cosmosService,
        ILogger<ConversationInsightService> logger)
    {
        _foundryAgentService = foundryAgentService;
        _cosmosService = cosmosService;
        _logger = logger;
    }

    public async Task<InsightDocument> ProcessAsync(ConversationDocument conversation, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analysing call {CallId} with the Foundry agent", conversation.CallId);

        var insight = await _foundryAgentService.AnalyzeAsync(conversation, cancellationToken);
        await _cosmosService.SaveInsightAsync(insight, cancellationToken);

        _logger.LogInformation("Stored insight for call {CallId}, hardshipDetected={HardshipDetected}", conversation.CallId, insight.HardshipDetected);
        return insight;
    }
}
