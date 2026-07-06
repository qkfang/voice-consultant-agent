using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using VoiceConsultant.FunctionApp.Models;

namespace VoiceConsultant.FunctionApp.Services;

/// <summary>
/// Thin wrapper around the Cosmos DB SDK used to read/write conversations and insights.
/// </summary>
public class CosmosService
{
    private readonly CosmosOptions _options;
    private readonly Lazy<CosmosClient> _client;

    public CosmosService(IOptions<CosmosOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<CosmosClient>(() => new CosmosClient(_options.AccountEndpoint, new DefaultAzureCredential()));
    }

    private Container ConversationsContainer =>
        _client.Value.GetContainer(_options.DatabaseName, _options.ConversationsContainerName);

    private Container InsightsContainer =>
        _client.Value.GetContainer(_options.DatabaseName, _options.InsightsContainerName);

    public Task<ItemResponse<ConversationDocument>> SaveConversationAsync(ConversationDocument conversation, CancellationToken cancellationToken = default) =>
        ConversationsContainer.UpsertItemAsync(conversation, new PartitionKey(conversation.CallId), cancellationToken: cancellationToken);

    public Task<ItemResponse<InsightDocument>> SaveInsightAsync(InsightDocument insight, CancellationToken cancellationToken = default) =>
        InsightsContainer.UpsertItemAsync(insight, new PartitionKey(insight.CallId), cancellationToken: cancellationToken);
}
