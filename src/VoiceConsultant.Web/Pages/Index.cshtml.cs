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
}
