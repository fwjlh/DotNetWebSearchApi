using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetWebSearchApi.Search;

public sealed class JsonApiSearchProvider : ISearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly JsonApiSearchDefinition _definition;

    public JsonApiSearchProvider(HttpClient httpClient, JsonApiSearchDefinition definition)
    {
        _httpClient = httpClient;
        _definition = definition;
    }

    public string Name => _definition.Name;

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        Uri requestUri = BuildRequestUri(query);
        using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: cancellationToken);

        return ReadResults(document.RootElement);
    }

    private Uri BuildRequestUri(SearchQuery query)
    {
        Dictionary<string, string> parameters = new(_definition.FixedParameters, StringComparer.Ordinal);
        parameters[_definition.QueryParameter] = query.Text;
        parameters[_definition.LimitParameter] = Math.Clamp(query.Limit, 1, 50).ToString();

        string queryString = string.Join("&", parameters.Select(parameter =>
            Uri.EscapeDataString(parameter.Key) + "=" + Uri.EscapeDataString(parameter.Value)));
        string separator = _definition.Endpoint.Contains('?') ? "&" : "?";
        return new Uri(_definition.Endpoint + separator + queryString);
    }

    private IReadOnlyList<SearchResult> ReadResults(JsonElement root)
    {
        if (!TryReadPath(root, _definition.ResultsPath, out JsonElement resultsElement)
            || resultsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<SearchResult>();
        }

        List<SearchResult> results = new();
        foreach (JsonElement item in resultsElement.EnumerateArray())
        {
            if (!TryReadResult(item, results.Count, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
        }

        return results;
    }

    private bool TryReadResult(JsonElement item, int position, out SearchResult? result)
    {
        result = null;
        if (!TryReadText(item, _definition.TitlePath, out string title)
            || !TryReadText(item, _definition.UrlPath, out string url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? resultUri) && !string.IsNullOrWhiteSpace(_definition.UrlPrefix))
        {
            resultUri = new Uri(new Uri(_definition.UrlPrefix), url.TrimStart('/'));
        }

        if (resultUri is null || (resultUri.Scheme != Uri.UriSchemeHttp && resultUri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        TryReadText(item, _definition.SnippetPath, out string snippet);
        result = new SearchResult(
            title,
            resultUri.AbsoluteUri,
            CleanText(snippet),
            Name,
            1.0 / (position + 1));
        return true;
    }

    private static bool TryReadText(JsonElement root, string path, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        JsonElement current = root;
        foreach (string segment in path.Replace("[", ".").Replace("]", string.Empty).Split('.'))
        {
            if (int.TryParse(segment, out int index))
            {
                if (current.ValueKind != JsonValueKind.Array || index < 0 || index >= current.GetArrayLength())
                {
                    return false;
                }

                current = current[index];
                continue;
            }

            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return false;
            }
        }

        if (current.ValueKind == JsonValueKind.String)
        {
            value = current.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        if (current.ValueKind == JsonValueKind.Number || current.ValueKind == JsonValueKind.True
            || current.ValueKind == JsonValueKind.False)
        {
            value = current.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static string CleanText(string value)
    {
        return WebUtility.HtmlDecode(Regex.Replace(value, "<[^>]+>", string.Empty)).Trim();
    }
}

public sealed record JsonApiSearchDefinition
{
    public required string Name { get; init; }
    public required string Endpoint { get; init; }
    public required string QueryParameter { get; init; }
    public required string LimitParameter { get; init; }
    public required string ResultsPath { get; init; }
    public required string TitlePath { get; init; }
    public required string UrlPath { get; init; }
    public string SnippetPath { get; init; } = string.Empty;
    public string UrlPrefix { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> FixedParameters { get; init; } =
        new Dictionary<string, string>();
}
