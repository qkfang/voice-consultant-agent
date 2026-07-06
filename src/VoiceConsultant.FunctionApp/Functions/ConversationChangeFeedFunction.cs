using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VoiceConsultant.FunctionApp.Models;
using VoiceConsultant.FunctionApp.Services;

namespace VoiceConsultant.FunctionApp.Functions;

/// <summary>
/// Triggered by the Cosmos DB change feed whenever a new (or updated) conversation
/// document is written to the conversations container.
/// </summary>
public class ConversationChangeFeedFunction
{
    private readonly ConversationInsightService _insightService;
    private readonly ILogger<ConversationChangeFeedFunction> _logger;

    public ConversationChangeFeedFunction(ConversationInsightService insightService, ILogger<ConversationChangeFeedFunction> logger)
    {
        _insightService = insightService;
        _logger = logger;
    }

    [Function("ConversationChangeFeedFunction")]
    public async Task Run(
        [CosmosDBTrigger(
            databaseName: "%Cosmos:DatabaseName%",
            containerName: "%Cosmos:ConversationsContainerName%",
            Connection = "Cosmos",
            LeaseContainerName = "%Cosmos:LeasesContainerName%",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<ConversationDocument> conversations)
    {
        foreach (var conversation in conversations)
        {
            try
            {
                await _insightService.ProcessAsync(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process conversation {CallId} from change feed", conversation.CallId);
            }
        }
    }
}
