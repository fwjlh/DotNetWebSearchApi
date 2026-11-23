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
        if (result.PaperMetadata is not null)
        {
            return "paper.html";
        }

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

        if (!string.IsNullOrWhiteSpace(result.MusicMetadata?.Artist))
        {
            return result.MusicMetadata.Artist;
        }

        return string.IsNullOrWhiteSpace(result.PaperMetadata?.Author)
            ? null
            : result.PaperMetadata.Author;
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
            result.Url, result.Title, result.PaperMetadata?.Content ?? result.Snippet, result.Provider, engines,
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
            MusicMetadata = result.MusicMetadata,
            PaperDateOfPublication = result.PaperMetadata?.DateOfPublication ?? result.PublishedDate,
            PaperComments = result.PaperMetadata?.Comments,
            PaperTags = result.PaperMetadata?.Tags,
            PaperType = result.PaperMetadata?.Type,
            PaperPublicationYear = result.PaperMetadata?.PublicationYear,
            PaperAuthors = result.PaperMetadata?.Authors,
            PaperEditor = result.PaperMetadata?.Editor,
            PaperPublisher = result.PaperMetadata?.Publisher,
            PaperJournal = result.PaperMetadata?.Journal,
            PaperVolume = result.PaperMetadata?.Volume,
            PaperPages = result.PaperMetadata?.Pages,
            PaperNumber = result.PaperMetadata?.Number,
            PaperDoi = result.PaperMetadata?.Doi,
            PaperIssn = result.PaperMetadata?.Issn,
            PaperIsbn = result.PaperMetadata?.Isbn,
            PdfUrl = result.PaperMetadata?.PdfUrl,
            HtmlUrl = result.PaperMetadata?.HtmlUrl,
            PaperMetadata = result.PaperMetadata
        };
    }
}
