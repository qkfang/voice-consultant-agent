using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using VoiceConsultant.Web.Models;

namespace VoiceConsultant.Web.Services;

/// <summary>
/// Reads conversations and their agent-generated insights from Cosmos DB for display in the UI.
/// </summary>
public class CosmosReaderService
{
    private readonly CosmosOptions _options;
    private readonly Lazy<CosmosClient> _client;

    public CosmosReaderService(IOptions<CosmosOptions> options)
    {
        _options = options.Value;
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
        {
            credentialOptions.TenantId = _options.TenantId;
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

    /// <summary>
    /// Writes a conversation document to the conversations container. This insert is what the
    /// Function App change feed trigger listens for.
    /// </summary>
    public async Task SaveConversationAsync(ConversationDocument conversation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversation.Id))
        {
            conversation.Id = Guid.NewGuid().ToString();
        }
        if (conversation.CreatedAt == default)
        {
            conversation.CreatedAt = DateTimeOffset.UtcNow;
        }

        await ConversationsContainer.CreateItemAsync(conversation, new PartitionKey(conversation.CallId), cancellationToken: cancellationToken);
    }

    public async Task<List<CallSummary>> GetRecentCallsAsync(int maxItems = 50, CancellationToken cancellationToken = default)
    {
        var conversations = new List<ConversationDocument>();
        var query = ConversationsContainer.GetItemQueryIterator<ConversationDocument>(
            new QueryDefinition("SELECT * FROM c ORDER BY c.createdAt DESC"),
            requestOptions: new QueryRequestOptions { MaxItemCount = maxItems });

        while (query.HasMoreResults && conversations.Count < maxItems)
        {
            var page = await query.ReadNextAsync(cancellationToken);
            conversations.AddRange(page);
        }

        var summaries = new List<CallSummary>();
        foreach (var conversation in conversations)
        {
            summaries.Add(new CallSummary
            {
                Conversation = conversation,
                Insight = await GetInsightForConversationAsync(conversation, cancellationToken)
            });
        }

        return summaries;
    }

    private async Task<InsightDocument?> GetInsightForConversationAsync(ConversationDocument conversation, CancellationToken cancellationToken)
    {
        var query = InsightsContainer.GetItemQueryIterator<InsightDocument>(
            new QueryDefinition("SELECT * FROM c WHERE c.conversationId = @conversationId ORDER BY c.generatedAt DESC")
                .WithParameter("@conversationId", conversation.Id),
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(conversation.CallId), MaxItemCount = 1 });

        if (query.HasMoreResults)
        {
            var page = await query.ReadNextAsync(cancellationToken);
            return page.FirstOrDefault();
        }

        return null;
    }
}
