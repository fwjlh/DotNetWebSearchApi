using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetWebSearchApi.Search;

public sealed class JsonApiSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private readonly HttpClient _httpClient;
    private readonly JsonApiSearchDefinition _definition;
    private readonly SearchHttpTransport _transport;

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

    public EngineCapabilities Capabilities => new(
        _definition.Categories,
        false,
        1,
        false,
        false,
        false);

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = BuildRequest(query);
        TimeSpan? minimumInterval = _definition.MinimumIntervalMilliseconds > 0
            ? TimeSpan.FromMilliseconds(_definition.MinimumIntervalMilliseconds)
            : null;
        SearchHttpResponse response = await _transport.SendAsync(
            _httpClient,
            request,
            cancellationToken,
            Name,
            minimumInterval);
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
        ImageResultMetadata? imageMetadata = ReadImageMetadata(item);
        VideoResultMetadata? videoMetadata = ReadVideoMetadata(item, resultUri);
        result = new SearchResult(
            title,
            resultUri.AbsoluteUri,
            AppendAttribution(CleanText(snippet)),
            Name,
            1.0 / (position + 1))
        {
            Category = ResolveResultCategory(),
            ThumbnailUrl = ResolveThumbnailUrl(imageMetadata, videoMetadata),
            PublishedDate = videoMetadata?.PublishedDate,
            ImageMetadata = imageMetadata,
            VideoMetadata = videoMetadata
        };
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

    private static string? ResolveThumbnailUrl(
        ImageResultMetadata? imageMetadata,
        VideoResultMetadata? videoMetadata)
    {
        if (!string.IsNullOrWhiteSpace(imageMetadata?.ThumbnailUrl))
        {
            return imageMetadata.ThumbnailUrl;
        }

        return string.IsNullOrWhiteSpace(videoMetadata?.ThumbnailUrl)
            ? null
            : videoMetadata.ThumbnailUrl;
    }

    private ImageResultMetadata? ReadImageMetadata(JsonElement item)
    {
        JsonApiImageMetadataPaths? paths = _definition.ImageMetadata;
        if (paths is null)
        {
            return null;
        }

        string imageUrl = ReadSafeUrl(item, paths.ImageUrl);
        string thumbnailUrl = ReadSafeUrl(item, paths.ThumbnailUrl);
        string width = ReadOptionalText(item, paths.Width);
        string height = ReadOptionalText(item, paths.Height);
        string licenseUrl = ReadSafeUrl(item, paths.LicenseUrl);
        string creatorUrl = ReadSafeUrl(item, paths.CreatorUrl);
        string license = ReadOptionalText(item, paths.License);
        string creator = ReadOptionalText(item, paths.Creator);
        string source = ReadOptionalText(item, paths.Source);
        string format = ReadOptionalText(item, paths.Format);
        string fileSizeText = ReadOptionalText(item, paths.FileSizeBytes);
        long? fileSize = long.TryParse(
            fileSizeText,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out long parsedFileSize) && parsedFileSize >= 0 ? parsedFileSize : null;

        if (imageUrl.Length == 0 && thumbnailUrl.Length == 0 && license.Length == 0
            && creator.Length == 0 && source.Length == 0 && format.Length == 0
            && licenseUrl.Length == 0 && creatorUrl.Length == 0 && fileSize is null)
        {
            return null;
        }

        string resolution = width.Length > 0 && height.Length > 0
            ? width + " x " + height
            : string.Empty;
        return new ImageResultMetadata
        {
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            Resolution = resolution,
            Format = format,
            Source = source,
            FileSizeBytes = fileSize,
            FileSize = FormatFileSize(fileSize),
            License = license,
            LicenseUrl = licenseUrl,
            Creator = creator,
            CreatorUrl = creatorUrl,
            Provider = ReadOptionalText(item, paths.Provider),
            LicenseVersion = ReadOptionalText(item, paths.LicenseVersion),
            Attribution = ReadOptionalText(item, paths.Attribution),
            MediaCategory = ReadOptionalText(item, paths.MediaCategory)
        };
    }

    private VideoResultMetadata? ReadVideoMetadata(JsonElement item, Uri resultUri)
    {
        JsonApiVideoMetadataPaths? paths = _definition.VideoMetadata;
        if (paths is null)
        {
            return null;
        }

        string embedUrl = ReadSafeEmbedUrl(item, paths, resultUri);
        string thumbnailUrl = ReadSafeUrl(item, paths.ThumbnailUrl);
        string durationText = ReadOptionalText(item, paths.DurationSeconds);
        double? duration = double.TryParse(
            durationText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double parsedDuration) && parsedDuration >= 0 ? parsedDuration : null;
        string author = ReadOptionalText(item, paths.Author);
        string views = ReadOptionalText(item, paths.Views);
        DateTimeOffset? publishedDate = ReadOptionalDate(item, paths.PublishedDate);

        if (embedUrl.Length == 0 && thumbnailUrl.Length == 0 && duration is null
            && author.Length == 0 && views.Length == 0 && publishedDate is null)
        {
            return null;
        }

        return new VideoResultMetadata
        {
            EmbedUrl = embedUrl,
            ThumbnailUrl = thumbnailUrl,
            DurationSeconds = duration,
            PublishedDate = publishedDate,
            Author = author,
            Views = views
        };
    }

    private static DateTimeOffset? ReadOptionalDate(JsonElement item, string path)
    {
        string value = ReadOptionalText(item, path);
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixSeconds))
        {
            if (unixSeconds < DateTimeOffset.MinValue.ToUnixTimeSeconds()
                || unixSeconds > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset parsedDate) ? parsedDate : null;
    }

    private static string FormatFileSize(long? fileSize)
    {
        if (fileSize is null || fileSize.Value < 0)
        {
            return string.Empty;
        }

        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = fileSize.Value;
        int unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return value.ToString("0.##", CultureInfo.InvariantCulture) + " " + units[unitIndex];
    }

    private static string ReadSafeEmbedUrl(
        JsonElement item,
        JsonApiVideoMetadataPaths paths,
        Uri resultUri)
    {
        string value = ReadOptionalText(item, paths.EmbedUrl);
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absoluteUri))
        {
            bool sameHost = absoluteUri.Host.Equals(resultUri.Host, StringComparison.OrdinalIgnoreCase);
            bool allowedHost = paths.AllowedEmbedHosts.Contains(absoluteUri.Host);
            return IsHttpUrl(absoluteUri) && (sameHost || allowedHost)
                ? absoluteUri.AbsoluteUri
                : string.Empty;
        }

        if (!value.StartsWith("/", StringComparison.Ordinal)
            || !Uri.TryCreate(resultUri, value, out Uri? relativeUri)
            || !IsHttpUrl(relativeUri)
            || !relativeUri.Host.Equals(resultUri.Host, StringComparison.OrdinalIgnoreCase)
            || relativeUri.Port != resultUri.Port)
        {
            return string.Empty;
        }

        return relativeUri.AbsoluteUri;
    }

    private static string ReadSafeUrl(JsonElement item, string path, Uri? baseUri = null)
    {
        string value = ReadOptionalText(item, path);
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absoluteUri))
        {
            return IsHttpUrl(absoluteUri) ? absoluteUri.AbsoluteUri : string.Empty;
        }

        if (baseUri is null || !value.StartsWith("/", StringComparison.Ordinal)
            || !Uri.TryCreate(baseUri, value, out Uri? relativeUri)
            || !IsHttpUrl(relativeUri)
            || !relativeUri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase)
            || relativeUri.Port != baseUri.Port)
        {
            return string.Empty;
        }

        return relativeUri.AbsoluteUri;
    }

    private static bool IsHttpUrl(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static string ReadOptionalText(JsonElement item, string path)
    {
        return TryReadText(item, path, out string value) ? value : string.Empty;
    }

    private string ResolveResultCategory()
    {
        return _definition.Categories.FirstOrDefault() ?? "general";
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
    public IReadOnlySet<string> Categories { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" };
    public JsonApiImageMetadataPaths? ImageMetadata { get; init; }
    public JsonApiVideoMetadataPaths? VideoMetadata { get; init; }
}

public sealed record JsonApiImageMetadataPaths
{
    public string ImageUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string Width { get; init; } = string.Empty;
    public string Height { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string FileSizeBytes { get; init; } = string.Empty;
    public string License { get; init; } = string.Empty;
    public string LicenseUrl { get; init; } = string.Empty;
    public string Creator { get; init; } = string.Empty;
    public string CreatorUrl { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string LicenseVersion { get; init; } = string.Empty;
    public string Attribution { get; init; } = string.Empty;
    public string MediaCategory { get; init; } = string.Empty;
}


public sealed record JsonApiVideoMetadataPaths
{
    public string EmbedUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string DurationSeconds { get; init; } = string.Empty;
    public string PublishedDate { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Views { get; init; } = string.Empty;
    public IReadOnlySet<string> AllowedEmbedHosts { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
