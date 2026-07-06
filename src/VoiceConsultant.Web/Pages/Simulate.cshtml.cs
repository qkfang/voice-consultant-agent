using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VoiceConsultant.Web.Models;
using VoiceConsultant.Web.Services;

namespace VoiceConsultant.Web.Pages;

public class SimulateModel : PageModel
{
    private readonly CosmosReaderService _cosmosReaderService;

    public SimulateModel(CosmosReaderService cosmosReaderService)
    {
        _cosmosReaderService = cosmosReaderService;
    }

    [BindProperty]
    public string CallId { get; set; } = $"call-{Guid.NewGuid().ToString()[..8]}";

    [BindProperty]
    public string? ConsultantId { get; set; } = "consultant-01";

    [BindProperty]
    public string? CustomerId { get; set; } = "customer-01";

    [BindProperty]
    public string Transcript { get; set; } = string.Empty;

    public string? StatusMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(CallId) || string.IsNullOrWhiteSpace(Transcript))
        {
            ErrorMessage = "Call Id and transcript are required.";
            return;
        }

        var conversation = new ConversationDocument
        {
            CallId = CallId.Trim(),
            ConsultantId = ConsultantId,
            CustomerId = CustomerId,
            Transcript = Transcript.Trim(),
            Source = "simulated"
        };

        try
        {
            await _cosmosReaderService.SaveConversationAsync(conversation);
            StatusMessage = $"Saved conversation for call '{conversation.CallId}'. The Function App change feed will process it shortly.";
            CallId = $"call-{Guid.NewGuid().ToString()[..8]}";
            Transcript = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to save conversation to Cosmos DB: {ex.Message}";
        }
    }
}
