using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceConsultant.FunctionApp.Models;

namespace VoiceConsultant.FunctionApp.Services;

public class FabricLakehouseService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] TokenScopes = ["https://onelake.azure.net/.default"];

    private readonly FabricOptions _options;
    private readonly HttpClient _httpClient;
    private readonly TokenCredential _credential;
    private readonly ILogger<FabricLakehouseService> _logger;

    public FabricLakehouseService(
        HttpClient httpClient,
        IOptions<FabricOptions> options,
        ILogger<FabricLakehouseService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;

        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
        {
            credentialOptions.TenantId = _options.TenantId;
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
        {
            credentialOptions.ExcludeManagedIdentityCredential = true;
        }

        _credential = new DefaultAzureCredential(credentialOptions);
    }

    public async Task SaveAgentOutputAsync(string transcriptionId, string output, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("Fabric lakehouse is not configured. Skipping lakehouse persistence for transcription {TranscriptionId}", transcriptionId);
            return;
        }

        var payload = JsonSerializer.Serialize(
            new FabricLakehouseAgentOutput
            {
                TranscriptionId = transcriptionId,
                Output = output
            },
            JsonOptions);

        var content = Encoding.UTF8.GetBytes(payload);
        var fileUri = BuildFileUri(transcriptionId);
        var accessToken = await _credential.GetTokenAsync(new TokenRequestContext(TokenScopes), cancellationToken);

        await SendAsync(
            HttpMethod.Put,
            $"{fileUri}?resource=file&overwrite=true",
            accessToken.Token,
            cancellationToken: cancellationToken);

        await SendAsync(
            HttpMethod.Patch,
            $"{fileUri}?action=append&position=0",
            accessToken.Token,
            new ByteArrayContent(content),
            cancellationToken);

        await SendAsync(
            HttpMethod.Patch,
            $"{fileUri}?action=flush&position={content.Length}",
            accessToken.Token,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Stored final agent output in Fabric lakehouse for transcription {TranscriptionId}", transcriptionId);
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_options.WorkspaceId) &&
        !string.IsNullOrWhiteSpace(_options.LakehouseId);

    private string BuildFileUri(string transcriptionId)
    {
        var baseUri = _options.OneLakeUri.TrimEnd('/');
        var workspace = Uri.EscapeDataString(_options.WorkspaceId);
        var lakehouse = Uri.EscapeDataString(_options.LakehouseId);
        var fileName = $"agent-output-{Uri.EscapeDataString(transcriptionId)}.json";

        return $"{baseUri}/{workspace}/{lakehouse}/Files/{fileName}";
    }

    private async Task SendAsync(
        HttpMethod method,
        string requestUri,
        string accessToken,
        HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("x-ms-version", "2023-11-03");
        request.Content = content;

        if (request.Content is not null)
        {
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed class FabricLakehouseAgentOutput
    {
        public string TranscriptionId { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
    }
}
