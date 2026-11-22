using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetWebSearchApi.Search;

public sealed class JsonApiSearchProvider : ISearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly JsonApiSearchDefinition _definition;
    private readonly SearchHttpTransport _transport;
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private DateTimeOffset _nextRequestUtc = DateTimeOffset.MinValue;

    public JsonApiSearchProvider(
        HttpClient httpClient,
        JsonApiSearchDefinition definition,
        SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _definition = definition;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => _definition.Name;

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        await WaitForRequestSlotAsync(cancellationToken);
        using HttpRequestMessage request = BuildRequest(query);
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        using JsonDocument document = JsonDocument.Parse(response.Content);

        return ReadResults(document.RootElement);
    }

    private HttpRequestMessage BuildRequest(SearchQuery query)
    {
        if (string.Equals(_definition.RequestMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, object> body = _definition.BodyFixedParameters.ToDictionary(
                parameter => parameter.Key,
                parameter => parameter.Value,
                StringComparer.Ordinal);
            body[_definition.BodyQueryParameter] = query.Text;
            if (!string.IsNullOrWhiteSpace(_definition.BodyLimitParameter))
            {
                body[_definition.BodyLimitParameter] = Math.Clamp(query.Limit, 1, 50);
            }

            return new HttpRequestMessage(HttpMethod.Post, _definition.Endpoint)
            {
                Content = JsonContent.Create(body)
            };
        }

        return new HttpRequestMessage(HttpMethod.Get, BuildRequestUri(query));
    }

    private async Task WaitForRequestSlotAsync(CancellationToken cancellationToken)
    {
        if (_definition.MinimumIntervalMilliseconds <= 0)
        {
            return;
        }

        await _requestGate.WaitAsync(cancellationToken);
        try
        {
            TimeSpan delay = _nextRequestUtc - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            _nextRequestUtc = DateTimeOffset.UtcNow.AddMilliseconds(_definition.MinimumIntervalMilliseconds);
        }
        finally
        {
            _requestGate.Release();
        }
    }

    private Uri BuildRequestUri(SearchQuery query)
    {
        Dictionary<string, string> parameters = _definition.FixedParameters.ToDictionary(
            parameter => parameter.Key,
            parameter => parameter.Value,
            StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(_definition.QueryParameter))
        {
            parameters[_definition.QueryParameter] = query.Text;
        }
        if (!string.IsNullOrWhiteSpace(_definition.LimitParameter))
        {
            parameters[_definition.LimitParameter] = Math.Clamp(query.Limit, 1, 50).ToString();
        }

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

        Uri? resultUri = null;
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            resultUri = absoluteUri;
        }
        else if (!string.IsNullOrWhiteSpace(_definition.UrlPrefix))
        {
            string relativePath = Uri.EscapeDataString(url.TrimStart('/'))
                .Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
            resultUri = _definition.UrlPrefix.Contains("?", StringComparison.Ordinal)
                ? new Uri(_definition.UrlPrefix + relativePath)
                : new Uri(new Uri(_definition.UrlPrefix), relativePath);
        }

        if (resultUri is null || (resultUri.Scheme != Uri.UriSchemeHttp && resultUri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        TryReadText(item, _definition.SnippetPath, out string snippet);
        result = new SearchResult(
            title,
            resultUri.AbsoluteUri,
            AppendAttribution(CleanText(snippet)),
            Name,
            1.0 / (position + 1));
        return true;
    }

    private static bool TryReadPath(JsonElement root, string path, out JsonElement value)
    {
        value = root;
        if (path == "$" || string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        foreach (string segment in path.Replace("[", ".").Replace("]", string.Empty).Split('.'))
        {
            if (int.TryParse(segment, out int index))
            {
                if (value.ValueKind != JsonValueKind.Array || index < 0 || index >= value.GetArrayLength())
                {
                    return false;
                }

                value = value[index];
                continue;
            }

            if (value.ValueKind != JsonValueKind.Object
                || !value.TryGetProperty(segment, out JsonElement child))
            {
                return false;
            }

            value = child;
        }

        return true;
    }

    private static bool TryReadText(JsonElement root, string path, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !TryReadPath(root, path, out JsonElement element))
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        if (element.ValueKind == JsonValueKind.Number || element.ValueKind == JsonValueKind.True
            || element.ValueKind == JsonValueKind.False)
        {
            value = element.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private string AppendAttribution(string snippet)
    {
        if (string.IsNullOrWhiteSpace(_definition.Attribution))
        {
            return snippet;
        }

        return string.IsNullOrWhiteSpace(snippet)
            ? _definition.Attribution
            : snippet + " " + _definition.Attribution;
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
    public string Attribution { get; init; } = string.Empty;
    public string RequestMethod { get; init; } = "GET";
    public string BodyQueryParameter { get; init; } = string.Empty;
    public string BodyLimitParameter { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> BodyFixedParameters { get; init; } =
        new Dictionary<string, object>();
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();
    public string ApiKeyConfigurationKey { get; init; } = string.Empty;
    public string ApiKeyHeader { get; init; } = string.Empty;
    public string ApiKeyPrefix { get; init; } = string.Empty;
    public int MinimumIntervalMilliseconds { get; init; }
    public IReadOnlyDictionary<string, string> FixedParameters { get; init; } =
        new Dictionary<string, string>();
}
