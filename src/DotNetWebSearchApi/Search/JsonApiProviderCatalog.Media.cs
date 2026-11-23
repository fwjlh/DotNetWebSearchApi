using System.Collections.Generic;

namespace DotNetWebSearchApi.Search;

public static partial class JsonApiProviderCatalog
{
    private static IReadOnlyList<JsonApiSearchDefinition> MediaDefinitions { get; } = new[]
    {
        new JsonApiSearchDefinition
        {
            Name = "bilibili",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "videos" },
            Endpoint = "https://api.bilibili.com/x/web-interface/search/type",
            QueryParameter = "keyword",
            LimitParameter = string.Empty,
            ResultsPath = "data.result",
            TitlePath = "title",
            UrlPath = "arcurl",
            SnippetPath = "description",
            FixedParameters = new Dictionary<string, string> { ["search_type"] = "video" },
            Headers = new Dictionary<string, string>
            {
                ["Referer"] = "https://search.bilibili.com"
            },
            VideoMetadata = new JsonApiVideoMetadataPaths
            {
                ThumbnailUrl = "pic",
                Author = "author",
                Views = "play",
                PublishedDate = "pubdate"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "dailymotion",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "videos" },
            Endpoint = "https://api.dailymotion.com/videos",
            QueryParameter = "search",
            LimitParameter = "limit",
            ResultsPath = "list",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "description",
            FixedParameters = new Dictionary<string, string>
            {
                ["fields"] = "id,title,description,url,embed_url,thumbnail_360_url,duration,uploaded_time"
            },
            VideoMetadata = new JsonApiVideoMetadataPaths
            {
                EmbedUrl = "embed_url",
                ThumbnailUrl = "thumbnail_360_url",
                DurationSeconds = "duration",
                PublishedDate = "uploaded_time",
                AllowedEmbedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "geo.dailymotion.com"
                }
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "deezer",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "music" },
            Endpoint = "https://api.deezer.com/search",
            QueryParameter = "q",
            LimitParameter = "limit",
            ResultsPath = "data",
            TitlePath = "title",
            UrlPath = "link",
            SnippetPath = "artist.name",
            MusicMetadata = new JsonApiMusicMetadataPaths
            {
                AudioUrl = "preview",
                ThumbnailUrl = "album.cover_medium",
                Artist = "artist.name",
                Album = "album.title",
                DurationSeconds = "duration"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "steam",
            Endpoint = "https://store.steampowered.com/api/storesearch/",
            QueryParameter = "term",
            LimitParameter = "count",
            ResultsPath = "items",
            TitlePath = "name",
            UrlPath = "id",
            UrlPrefix = "https://store.steampowered.com/app/",
            FixedParameters = new Dictionary<string, string> { ["l"] = "english", ["cc"] = "gb" }
        },
        new JsonApiSearchDefinition
        {
            Name = "musicbrainz",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "music" },
            Endpoint = "https://musicbrainz.org/ws/2/artist/",
            QueryParameter = "query",
            LimitParameter = "limit",
            ResultsPath = "artists",
            TitlePath = "name",
            UrlPath = "id",
            SnippetPath = "disambiguation",
            UrlPrefix = "https://musicbrainz.org/artist/",
            MusicMetadata = new JsonApiMusicMetadataPaths
            {
                Artist = "name",
                Country = "country"
            },
            FixedParameters = new Dictionary<string, string> { ["fmt"] = "json" }
        },
        new JsonApiSearchDefinition
        {
            Name = "wikidata",
            Endpoint = "https://www.wikidata.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            UrlPrefix = "https://www.wikidata.org/wiki/",
            FixedParameters = new Dictionary<string, string>
            {
                ["action"] = "query",
                ["list"] = "search",
                ["format"] = "json"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "wikipedia_de",
            Endpoint = "https://de.wikipedia.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            UrlPrefix = "https://de.wikipedia.org/wiki/",
            FixedParameters = new Dictionary<string, string>
            {
                ["action"] = "query",
                ["list"] = "search",
                ["format"] = "json"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "wiby",
            Endpoint = "https://wiby.me/json/",
            QueryParameter = "q",
            LimitParameter = string.Empty,
            ResultsPath = "$",
            TitlePath = "Title",
            UrlPath = "URL",
            SnippetPath = "Snippet",
            Attribution = "Search via https://wiby.me"
        },
        new JsonApiSearchDefinition
        {
            Name = "tvmaze",
            Endpoint = "https://api.tvmaze.com/search/shows",
            QueryParameter = "q",
            LimitParameter = string.Empty,
            ResultsPath = "$",
            TitlePath = "show.name",
            UrlPath = "show.url",
            SnippetPath = "show.summary"
        },
        new JsonApiSearchDefinition
        {
            Name = "radiobrowser",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "music" },
            Endpoint = "https://de1.api.radio-browser.info/json/stations/search",
            QueryParameter = "name",
            LimitParameter = "limit",
            ResultsPath = "$",
            TitlePath = "name",
            UrlPath = "url",
            SnippetPath = "tags",
            FixedParameters = new Dictionary<string, string>
            {
                ["order"] = "votes",
                ["reverse"] = "true",
                ["hidebroken"] = "true"
            },
            MusicMetadata = new JsonApiMusicMetadataPaths
            {
                AudioUrl = "url_resolved",
                ThumbnailUrl = "favicon",
                Country = "country",
                Tags = "tags",
                Codec = "codec",
                Bitrate = "bitrate"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "peertube",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "videos" },
            Endpoint = "https://peertube.cpy.re/api/v1/search/videos",
            QueryParameter = "search",
            LimitParameter = "count",
            ResultsPath = "data",
            TitlePath = "name",
            UrlPath = "url",
            SnippetPath = "description",
            FixedParameters = new Dictionary<string, string> { ["searchTarget"] = "local" },
            VideoMetadata = new JsonApiVideoMetadataPaths
            {
                EmbedUrl = "embedPath",
                ThumbnailUrl = "thumbnails.0.fileUrl",
                DurationSeconds = "duration",
                PublishedDate = "publishedAt",
                Author = "account.displayName",
                Views = "views"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "wikimedia_commons",
            Endpoint = "https://commons.wikimedia.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            SnippetPath = "snippet",
            UrlPrefix = "https://commons.wikimedia.org/wiki/",
            FixedParameters = new Dictionary<string, string>
            {
                ["action"] = "query",
                ["list"] = "search",
                ["format"] = "json",
                ["srnamespace"] = "6",
                ["srprop"] = "snippet"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "wikinews",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "news" },
            Endpoint = "https://en.wikinews.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            SnippetPath = "snippet",
            UrlPrefix = "https://en.wikinews.org/wiki/",
            FixedParameters = new Dictionary<string, string>
            {
                ["action"] = "query",
                ["list"] = "search",
                ["format"] = "json",
                ["srprop"] = "snippet|timestamp"
            },
            PublishedDatePath = "timestamp"
        },
        new JsonApiSearchDefinition
        {
            Name = "wikipedia_es",
            Endpoint = "https://es.wikipedia.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            UrlPrefix = "https://es.wikipedia.org/wiki/",
            FixedParameters = new Dictionary<string, string>
            {
                ["action"] = "query",
                ["list"] = "search",
                ["format"] = "json"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "openverse",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "images" },
            Endpoint = "https://api.openverse.org/v1/images/",
            QueryParameter = "q",
            LimitParameter = "page_size",
            ResultsPath = "results",
            TitlePath = "title",
            UrlPath = "foreign_landing_url",
            SnippetPath = "attribution",
            ImageMetadata = new JsonApiImageMetadataPaths
            {
                ImageUrl = "url",
                ThumbnailUrl = "thumbnail",
                Width = "width",
                Height = "height",
                Format = "filetype",
                Source = "source",
                FileSizeBytes = "filesize",
                License = "license",
                LicenseUrl = "license_url",
                Creator = "creator",
                CreatorUrl = "creator_url",
                Provider = "provider",
                LicenseVersion = "license_version",
                Attribution = "attribution",
                MediaCategory = "category"
            }
        },
    };

    private static IReadOnlyList<JsonApiSearchDefinition> TailDefinitions { get; } = new[]
    {
        new JsonApiSearchDefinition
        {
            Name = "mwmbl",
            Endpoint = "https://api.mwmbl.org/api/v2/search/",
            QueryParameter = "q",
            LimitParameter = string.Empty,
            ResultsPath = "results",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "content",
            MinimumIntervalMilliseconds = 1000
        },
        new JsonApiSearchDefinition
        {
            Name = "marginalia",
            Endpoint = "https://api2.marginalia-search.com/search",
            QueryParameter = "query",
            LimitParameter = "count",
            ResultsPath = "results",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "description",
            Headers = new Dictionary<string, string> { ["API-Key"] = "public" }
        },
        new JsonApiSearchDefinition
        {
            Name = "yep",
            Endpoint = "https://api.yep.com/search",
            QueryParameter = "query",
            LimitParameter = "limit",
            ResultsPath = "1.results",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "snippet",
            FixedParameters = new Dictionary<string, string> { ["safeSearch"] = "moderate" }
        },
        new JsonApiSearchDefinition
        {
            Name = "exa",
            Endpoint = "https://api.exa.ai/search",
            QueryParameter = string.Empty,
            LimitParameter = string.Empty,
            ResultsPath = "results",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "highlights.0",
            RequestMethod = "POST",
            BodyQueryParameter = "query",
            BodyLimitParameter = "numResults",
            BodyFixedParameters = new Dictionary<string, object>
            {
                ["type"] = "auto",
                ["contents"] = new Dictionary<string, object> { ["highlights"] = true }
            },
            ApiKeyConfigurationKey = "EXA_API_KEY",
            ApiKeyHeader = "x-api-key"
        },
        new JsonApiSearchDefinition
        {
            Name = "wiktionary",
            Endpoint = "https://en.wiktionary.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            SnippetPath = "snippet",
            UrlPrefix = "https://en.wiktionary.org/wiki/",
            FixedParameters = new Dictionary<string, string>
            {
                ["action"] = "query",
                ["list"] = "search",
                ["format"] = "json",
                ["srprop"] = "snippet"
            }
        }
    };
}
