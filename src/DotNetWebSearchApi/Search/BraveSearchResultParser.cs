using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public static class BraveSearchResultParser
{
    public static IReadOnlyList<SearchResult> Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("web", out JsonElement web)
            || web.ValueKind != JsonValueKind.Object
            || !web.TryGetProperty("results", out JsonElement resultsElement)
            || resultsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<SearchResult>();
        }

        List<SearchResult> results = new();
        foreach (JsonElement item in resultsElement.EnumerateArray())
        {
            if (!TryParseResult(item, results.Count, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
        }

        return results;
    }

    private static bool TryParseResult(JsonElement item, int position, out SearchResult? result)
    {
        result = null;
        if (item.ValueKind != JsonValueKind.Object
            || !TryReadString(item, "title", out string title)
            || !TryReadString(item, "url", out string url)
            || !IsHttpUrl(url))
        {
            return false;
        }

        string snippet = TryReadString(item, "description", out string description)
            ? description
            : string.Empty;

        result = new SearchResult(title, url, snippet, "brave", 1.0 / (position + 1));
        return true;
    }

    private static bool TryReadString(JsonElement item, string propertyName, out string value)
    {
        value = string.Empty;
        if (!item.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
