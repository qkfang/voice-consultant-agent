using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceConsultant.FunctionApp.Models;

namespace VoiceConsultant.FunctionApp.Services;

/// <summary>
/// Sends a call transcript to the Microsoft Foundry prompt agent and parses back
/// the hardship/issue detection feedback.
/// </summary>
public class FoundryAgentService
{
    private readonly PersistentAgentsClient _client;
    private readonly FoundryOptions _options;
    private readonly ILogger<FoundryAgentService> _logger;

    public FoundryAgentService(IOptions<FoundryOptions> options, ILogger<FoundryAgentService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new PersistentAgentsClient(_options.ProjectEndpoint, new DefaultAzureCredential());
    }

    public async Task<InsightDocument> AnalyzeAsync(ConversationDocument conversation, CancellationToken cancellationToken = default)
    {
        var thread = await _client.Threads.CreateThreadAsync(cancellationToken: cancellationToken);

        var prompt = $"Review the following call centre transcript. Identify any signs of customer hardship, " +
                     $"list any issues raised by the customer, and suggest actions for the consultant. " +
                     $"Respond in JSON with fields: hardshipDetected (boolean), issues (array of strings), " +
                     $"suggestions (array of strings), summary (string).\n\nTranscript:\n{conversation.Transcript}";

        await _client.Messages.CreateMessageAsync(thread.Value.Id, MessageRole.User, prompt, cancellationToken: cancellationToken);

        var run = await _client.Runs.CreateRunAsync(thread.Value.Id, _options.AgentId, cancellationToken: cancellationToken);
        var runValue = run.Value;

        while (runValue.Status == RunStatus.Queued || runValue.Status == RunStatus.InProgress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            runValue = (await _client.Runs.GetRunAsync(thread.Value.Id, runValue.Id, cancellationToken)).Value;
        }

        if (runValue.Status != RunStatus.Completed)
        {
            _logger.LogWarning("Foundry agent run for call {CallId} ended with status {Status}", conversation.CallId, runValue.Status);
            return new InsightDocument
            {
                CallId = conversation.CallId,
                ConversationId = conversation.Id,
                Summary = $"Agent run did not complete successfully (status: {runValue.Status})."
            };
        }

        var messages = _client.Messages.GetMessagesAsync(thread.Value.Id, order: ListSortOrder.Descending, cancellationToken: cancellationToken);
        string responseText = string.Empty;
        await foreach (var message in messages)
        {
            if (message.Role != MessageRole.Agent)
            {
                continue;
            }

            responseText = string.Concat(message.ContentItems.OfType<MessageTextContent>().Select(c => c.Text));
            break;
        }

        return AgentResponseParser.Parse(responseText, conversation);
    }
}
