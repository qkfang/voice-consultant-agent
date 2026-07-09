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

    /// <summary>Writes the structured AI insight to Files/insights/{fileKey}.json.</summary>
    public async Task SaveInsightAsync(string fileKey, InsightDocument insight, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("Fabric lakehouse is not configured. Skipping insight upload for {FileKey}", fileKey);
            return;
        }

        await UploadFileAsync("insights", $"{fileKey}.json", JsonSerializer.Serialize(insight, JsonOptions), cancellationToken);
        _logger.LogInformation("Stored insight in Fabric lakehouse: insights/{FileKey}.json", fileKey);
    }

    /// <summary>Writes the raw conversation transcript to Files/conversations/{fileKey}.json when LandTranscription is enabled.</summary>
    public async Task SaveConversationAsync(string fileKey, ConversationDocument conversation, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured() || !_options.LandTranscription)
        {
            return;
        }

        await UploadFileAsync("conversations", $"{fileKey}.json", JsonSerializer.Serialize(conversation, JsonOptions), cancellationToken);
        _logger.LogInformation("Stored conversation in Fabric lakehouse: conversations/{FileKey}.json", fileKey);
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_options.WorkspaceId) &&
        !string.IsNullOrWhiteSpace(_options.LakehouseId);

    private async Task UploadFileAsync(string folder, string fileName, string content, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var fileUri = BuildFileUri(folder, fileName);
        var tokenContext = string.IsNullOrWhiteSpace(_options.TenantId)
            ? new TokenRequestContext(TokenScopes)
            : new TokenRequestContext(TokenScopes, tenantId: _options.TenantId);
        var accessToken = await _credential.GetTokenAsync(tokenContext, cancellationToken);

        await SendAsync(HttpMethod.Put, $"{fileUri}?resource=file&overwrite=true", accessToken.Token, cancellationToken: cancellationToken);
        await SendAsync(HttpMethod.Patch, $"{fileUri}?action=append&position=0", accessToken.Token, new ByteArrayContent(bytes), cancellationToken);
        await SendAsync(HttpMethod.Patch, $"{fileUri}?action=flush&position={bytes.Length}", accessToken.Token, cancellationToken: cancellationToken);
    }

    private string BuildFileUri(string folder, string fileName)
    {
        var baseUri = _options.OneLakeUri.TrimEnd('/');
        var workspace = Uri.EscapeDataString(_options.WorkspaceId);
        var lakehouse = Uri.EscapeDataString(_options.LakehouseId);

        return $"{baseUri}/{workspace}/{lakehouse}/Files/{folder}/{Uri.EscapeDataString(fileName)}";
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
}
