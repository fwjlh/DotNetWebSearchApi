using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public static class QwantSearchResultParser
{
    public static IReadOnlyList<SearchResult> Parse(JsonElement root)
    {
        if (!TryReadString(root, "status", out string status) || !status.Equals("success", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Qwant returned an unsuccessful API response.");
        if (!TryGetObject(root, "data", out JsonElement data)
            || !TryGetObject(data, "result", out JsonElement result)
            || !TryGetObject(result, "items", out JsonElement items)
            || !TryGetArray(items, "mainline", out JsonElement mainline))
            return Array.Empty<SearchResult>();

        List<SearchResult> results = new();
        foreach (JsonElement row in mainline.EnumerateArray())
        {
            if (!TryReadString(row, "type", out string type)
                || !type.Equals("web", StringComparison.OrdinalIgnoreCase)
                || !TryGetArray(row, "items", out JsonElement rowItems))
                continue;
            AddResults(rowItems, results);
        }
        return results;
    }

    private static void AddResults(JsonElement items, List<SearchResult> results)
    {
        foreach (JsonElement item in items.EnumerateArray())
        {
            if (!TryReadString(item, "title", out string title)
                || !TryReadString(item, "url", out string url)
                || !IsHttpUrl(url))
                continue;
            string snippet = TryReadString(item, "desc", out string description) ? description : string.Empty;
            results.Add(new SearchResult(title, url, snippet, "qwant", 1.0 / (results.Count + 1)));
        }
    }

    private static bool TryGetObject(JsonElement parent, string propertyName, out JsonElement value)
    {
        value = default;
        return parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(propertyName, out value)
            && value.ValueKind == JsonValueKind.Object;
    }

    private static bool TryGetArray(JsonElement parent, string propertyName, out JsonElement value)
    {
        value = default;
        return parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(propertyName, out value)
            && value.ValueKind == JsonValueKind.Array;
    }

    private static bool TryReadString(JsonElement item, string propertyName, out string value)
    {
        value = string.Empty;
        if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
            return false;
        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
