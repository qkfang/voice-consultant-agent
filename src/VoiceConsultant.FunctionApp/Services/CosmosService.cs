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
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
        {
            credentialOptions.TenantId = _options.TenantId;
        }

        // Locally there is no IMDS endpoint, so skip Managed Identity to avoid an
        // unrecoverable auth failure that would otherwise stop the credential chain.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
        {
            credentialOptions.ExcludeManagedIdentityCredential = true;
        }

        var clientOptions = new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions()
        };

        _client = new Lazy<CosmosClient>(() => new CosmosClient(_options.AccountEndpoint, new DefaultAzureCredential(credentialOptions), clientOptions));
    }

    private Container ConversationsContainer =>
        _client.Value.GetContainer(_options.DatabaseName, _options.ConversationsContainerName);

    private Container InsightsContainer =>
        _client.Value.GetContainer(_options.DatabaseName, _options.InsightsContainerName);

    public Task<ItemResponse<ConversationDocument>> SaveConversationAsync(ConversationDocument conversation, CancellationToken cancellationToken = default) =>
        ConversationsContainer.UpsertItemAsync(conversation, new PartitionKey(conversation.CallId), cancellationToken: cancellationToken);

    public Task<ItemResponse<InsightDocument>> SaveInsightAsync(InsightDocument insight, CancellationToken cancellationToken = default) =>
        InsightsContainer.UpsertItemAsync(insight, new PartitionKey(insight.CallId), cancellationToken: cancellationToken);

    public async Task<ConversationDocument?> GetLatestConversationAsync(string callId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.callId = @callId ORDER BY c.createdAt DESC")
            .WithParameter("@callId", callId);

        using var iterator = ConversationsContainer.GetItemQueryIterator<ConversationDocument>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(callId), MaxItemCount = 1 });

        if (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            return page.FirstOrDefault();
        }

        return null;
    }
}
