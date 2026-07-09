using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VoiceConsultant.Web.Models;
using VoiceConsultant.Web.Services;

namespace VoiceConsultant.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CosmosReaderService _cosmosReaderService;

    public IndexModel(CosmosReaderService cosmosReaderService)
    {
        _cosmosReaderService = cosmosReaderService;
    }

    public List<CallSummary> Calls { get; private set; } = new();

    public string? ErrorMessage { get; private set; }

    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            Calls = await _cosmosReaderService.GetRecentCallsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load calls from Cosmos DB: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostRetriggerAsync(string id, string callId)
    {
        try
        {
            await _cosmosReaderService.RetriggerConversationAsync(id, callId);
            StatusMessage = $"Retriggered processing for call '{callId}'.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to retrigger call '{callId}': {ex.Message}";
        }

        await OnGetAsync();
        return Page();
    }
}
