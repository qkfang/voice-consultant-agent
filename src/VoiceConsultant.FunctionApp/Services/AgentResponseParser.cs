using System.Text.Json;
using VoiceConsultant.FunctionApp.Models;

namespace VoiceConsultant.FunctionApp.Services;

/// <summary>
/// Parses the Foundry agent's JSON response into an <see cref="InsightDocument"/>,
/// falling back to a plain-text summary if the agent didn't return valid JSON.
/// </summary>
public static class AgentResponseParser
{
    public static InsightDocument Parse(string responseText, ConversationDocument conversation)
    {
        var insight = new InsightDocument
        {
            CallId = conversation.CallId,
            ConversationId = conversation.Id
        };

        var json = ExtractJson(responseText);
        if (json is not null)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("hardshipDetected", out var hardshipElement))
                {
                    insight.HardshipDetected = hardshipElement.ValueKind == JsonValueKind.True;
                }

                if (root.TryGetProperty("issues", out var issuesElement) && issuesElement.ValueKind == JsonValueKind.Array)
                {
                    insight.Issues = issuesElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
                }

                if (root.TryGetProperty("suggestions", out var suggestionsElement) && suggestionsElement.ValueKind == JsonValueKind.Array)
                {
                    insight.Suggestions = suggestionsElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
                }

                if (root.TryGetProperty("summary", out var summaryElement))
                {
                    insight.Summary = summaryElement.GetString() ?? string.Empty;
                }

                return insight;
            }
            catch (JsonException)
            {
                // Fall through to plain-text handling below.
            }
        }

        insight.Summary = responseText;
        return insight;
    }

    private static string? ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }
}
