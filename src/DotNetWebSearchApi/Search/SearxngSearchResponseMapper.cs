namespace DotNetWebSearchApi.Search;

public static class SearxngSearchResponseMapper
{
    public static SearxngSearchResponse Map(string query, SearchExecutionResult execution)
    {
        SearxngSearchResult[] results = execution.Results.Select(MapResult).ToArray();
        return new SearxngSearchResponse(
            query, results.Length, results, Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<object>(), Array.Empty<string>(), execution.UnresponsiveProviders,
            execution.UnsupportedProviders, execution.CoolingDownProviders)
        {
            ProviderOutcomes = execution.ProviderOutcomes
        };
    }

    private static string ResolveTemplate(SearchResult result)
    {
        if (string.Equals(result.Category, "images", StringComparison.OrdinalIgnoreCase)
            && IsAbsoluteHttpUrl(result.ImageMetadata?.ImageUrl))
        {
            return "images.html";
        }

        if (string.Equals(result.Category, "videos", StringComparison.OrdinalIgnoreCase)
            && IsAbsoluteHttpUrl(result.VideoMetadata?.EmbedUrl))
        {
            return "videos.html";
        }

        return "default.html";
    }

    private static bool IsAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string? ResolveAuthor(SearchResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.VideoMetadata?.Author))
        {
            return result.VideoMetadata.Author;
        }

        return string.IsNullOrWhiteSpace(result.MusicMetadata?.Artist)
            ? null
            : result.MusicMetadata.Artist;
    }

    private static SearxngSearchResult MapResult(SearchResult result)
    {
        Uri uri = new(result.Url);
        string[] engines = result.Engines.Count > 0 ? result.Engines.ToArray() : new[] { result.Provider };
        string[] parsedUrl =
        {
            uri.Scheme, uri.Authority, uri.AbsolutePath, string.Empty,
            uri.Query.TrimStart('?'), uri.Fragment.TrimStart('#')
        };
        string template = ResolveTemplate(result);
        return new SearxngSearchResult(
            result.Url, result.Title, result.Snippet, result.Provider, engines,
            result.Positions, result.Score, result.Category, template, parsedUrl, result.PublishedDate)
        {
            Thumbnail = result.ThumbnailUrl,
            ImageUrl = result.ImageMetadata?.ImageUrl,
            ImageThumbnailUrl = result.ImageMetadata?.ThumbnailUrl,
            ImageResolution = result.ImageMetadata?.Resolution,
            ImageFormat = result.ImageMetadata?.Format,
            ImageSource = result.ImageMetadata?.Source,
            ImageFileSize = result.ImageMetadata?.FileSize,
            ImageFormats = result.ImageMetadata?.Formats ?? Array.Empty<ImageFormatReference>(),
            ImageMetadata = result.ImageMetadata,
            VideoEmbedUrl = result.VideoMetadata?.EmbedUrl,
            LengthSeconds = result.VideoMetadata?.DurationSeconds ?? result.MusicMetadata?.DurationSeconds,
            Author = ResolveAuthor(result),
            VideoViews = result.VideoMetadata?.Views,
            VideoMetadata = result.VideoMetadata,
            AudioUrl = result.MusicMetadata?.AudioUrl,
            MusicMetadata = result.MusicMetadata
        };
    }
}
