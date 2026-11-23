using System.Globalization;
using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed partial class JsonApiSearchProvider
{
    private static string? ResolveThumbnailUrl(
        ImageResultMetadata? imageMetadata,
        VideoResultMetadata? videoMetadata,
        MusicResultMetadata? musicMetadata)
    {
        if (!string.IsNullOrWhiteSpace(imageMetadata?.ThumbnailUrl))
        {
            return imageMetadata.ThumbnailUrl;
        }

        if (!string.IsNullOrWhiteSpace(videoMetadata?.ThumbnailUrl))
        {
            return videoMetadata.ThumbnailUrl;
        }

        return string.IsNullOrWhiteSpace(musicMetadata?.ThumbnailUrl)
            ? null
            : musicMetadata.ThumbnailUrl;
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

    private MusicResultMetadata? ReadMusicMetadata(JsonElement item)
    {
        JsonApiMusicMetadataPaths? paths = _definition.MusicMetadata;
        if (paths is null)
        {
            return null;
        }

        string audioUrl = ReadSafeUrl(item, paths.AudioUrl);
        string thumbnailUrl = ReadSafeUrl(item, paths.ThumbnailUrl);
        string artist = ReadOptionalText(item, paths.Artist);
        string album = ReadOptionalText(item, paths.Album);
        string durationText = ReadOptionalText(item, paths.DurationSeconds);
        string bitrateText = ReadOptionalText(item, paths.Bitrate);
        double? duration = double.TryParse(
            durationText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double parsedDuration) && parsedDuration >= 0 ? parsedDuration : null;
        int? bitrate = int.TryParse(
            bitrateText,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int parsedBitrate) && parsedBitrate >= 0 ? parsedBitrate : null;
        string country = ReadOptionalText(item, paths.Country);
        string tags = ReadOptionalText(item, paths.Tags);
        string codec = ReadOptionalText(item, paths.Codec);

        if (audioUrl.Length == 0 && thumbnailUrl.Length == 0 && artist.Length == 0
            && album.Length == 0 && duration is null && country.Length == 0
            && tags.Length == 0 && codec.Length == 0 && bitrate is null)
        {
            return null;
        }

        return new MusicResultMetadata
        {
            AudioUrl = audioUrl,
            ThumbnailUrl = thumbnailUrl,
            Artist = artist,
            Album = album,
            DurationSeconds = duration,
            Country = country,
            Tags = tags,
            Codec = codec,
            Bitrate = bitrate
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
            bool sameOrigin = absoluteUri.Host.Equals(resultUri.Host, StringComparison.OrdinalIgnoreCase)
                && absoluteUri.Scheme.Equals(resultUri.Scheme, StringComparison.OrdinalIgnoreCase)
                && absoluteUri.Port == resultUri.Port;
            bool allowedHost = paths.AllowedEmbedHosts.Contains(absoluteUri.Host);
            return IsHttpsUrl(absoluteUri) && (sameOrigin || allowedHost)
                ? absoluteUri.AbsoluteUri
                : string.Empty;
        }

        if (!value.StartsWith("/", StringComparison.Ordinal)
            || !Uri.TryCreate(resultUri, value, out Uri? relativeUri)
            || !IsHttpsUrl(relativeUri)
            || !relativeUri.Host.Equals(resultUri.Host, StringComparison.OrdinalIgnoreCase)
            || relativeUri.Port != resultUri.Port)
        {
            return string.Empty;
        }

        return relativeUri.AbsoluteUri;
    }

    private static string ReadSafeUrl(JsonElement item, string path)
    {
        string value = ReadOptionalText(item, path);
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && IsHttpUrl(uri)
            ? uri.AbsoluteUri
            : string.Empty;
    }

    private static bool IsHttpsUrl(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool IsHttpUrl(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static string ReadOptionalText(JsonElement item, string path)
    {
        return TryReadText(item, path, out string value) ? value : string.Empty;
    }

    private string ResolveResultCategory(SearchQuery query)
    {
        if (_definition.Categories.Contains("general")
            && (query.Categories is null || query.Categories.Contains("general")))
        {
            return "general";
        }

        string? requestedCategory = query.Categories?
            .Where(_definition.Categories.Contains)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        return requestedCategory
            ?? _definition.Categories.OrderBy(category => category, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
            ?? "general";
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


public sealed record JsonApiMusicMetadataPaths
{
    public string AudioUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public string DurationSeconds { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string Codec { get; init; } = string.Empty;
    public string Bitrate { get; init; } = string.Empty;
}
