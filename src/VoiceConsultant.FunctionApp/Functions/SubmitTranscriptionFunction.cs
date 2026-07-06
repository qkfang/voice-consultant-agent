using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using VoiceConsultant.FunctionApp.Models;
using VoiceConsultant.FunctionApp.Services;

namespace VoiceConsultant.FunctionApp.Functions;

/// <summary>
/// HTTP endpoint that lets a caller submit a call transcript directly, bypassing the
/// change feed, and get the agent's feedback back in the response.
/// </summary>
public class SubmitTranscriptionFunction
{
    private readonly CosmosService _cosmosService;
    private readonly ConversationInsightService _insightService;
    private readonly ILogger<SubmitTranscriptionFunction> _logger;

    public SubmitTranscriptionFunction(
        CosmosService cosmosService,
        ConversationInsightService insightService,
        ILogger<SubmitTranscriptionFunction> logger)
    {
        _cosmosService = cosmosService;
        _insightService = insightService;
        _logger = logger;
    }

    [Function("SubmitTranscription")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "transcriptions")] HttpRequestData req)
    {
        SubmitTranscriptionRequest? payload;
        try
        {
            payload = await JsonSerializer.DeserializeAsync<SubmitTranscriptionRequest>(req.Body);
        }
        catch (JsonException)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is not valid JSON.");
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.CallId) || string.IsNullOrWhiteSpace(payload.Transcript))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "callId and transcript are required.");
        }

        var conversation = new ConversationDocument
        {
            CallId = payload.CallId,
            ConsultantId = payload.ConsultantId,
            CustomerId = payload.CustomerId,
            Transcript = payload.Transcript,
            Source = "api"
        };

        await _cosmosService.SaveConversationAsync(conversation);

        try
        {
            var insight = await _insightService.ProcessAsync(conversation);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(insight);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyse transcript for call {CallId}", conversation.CallId);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to analyse the transcript.");
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}

public class SubmitTranscriptionRequest
{
    public string CallId { get; set; } = string.Empty;
    public string? ConsultantId { get; set; }
    public string? CustomerId { get; set; }
    public string Transcript { get; set; } = string.Empty;
}
