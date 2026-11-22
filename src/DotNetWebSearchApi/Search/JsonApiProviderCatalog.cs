using Microsoft.Extensions.DependencyInjection;

namespace DotNetWebSearchApi.Search;

public static partial class JsonApiProviderCatalog
{
    private static IReadOnlyList<JsonApiSearchDefinition> BaseDefinitions { get; } = new[]
    {
        new JsonApiSearchDefinition
        {
            Name = "crossref",
            Endpoint = "https://api.crossref.org/works",
            QueryParameter = "query",
            LimitParameter = "rows",
            ResultsPath = "message.items",
            TitlePath = "title.0",
            UrlPath = "URL",
            SnippetPath = "abstract"
        },
        new JsonApiSearchDefinition
        {
            Name = "openalex",
            Endpoint = "https://api.openalex.org/works",
            QueryParameter = "search",
            LimitParameter = "per-page",
            ResultsPath = "results",
            TitlePath = "title",
            UrlPath = "id"
        },
        new JsonApiSearchDefinition
        {
            Name = "hackernews",
            Endpoint = "https://hn.algolia.com/api/v1/search",
            QueryParameter = "query",
            LimitParameter = "hitsPerPage",
            ResultsPath = "hits",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "story_text"
        },
        new JsonApiSearchDefinition
        {
            Name = "stackoverflow",
            Endpoint = "https://api.stackexchange.com/2.3/search/advanced",
            QueryParameter = "q",
            LimitParameter = "pagesize",
            ResultsPath = "items",
            TitlePath = "title",
            UrlPath = "link",
            SnippetPath = "body",
            FixedParameters = new Dictionary<string, string> { ["site"] = "stackoverflow" }
        },
        new JsonApiSearchDefinition
        {
            Name = "npm",
            Endpoint = "https://registry.npmjs.org/-/v1/search",
            QueryParameter = "text",
            LimitParameter = "size",
            ResultsPath = "objects",
            TitlePath = "package.name",
            UrlPath = "package.links.npm",
            SnippetPath = "package.description"
        },
        new JsonApiSearchDefinition
        {
            Name = "cratesio",
            Endpoint = "https://crates.io/api/v1/crates",
            QueryParameter = "q",
            LimitParameter = "per_page",
            ResultsPath = "crates",
            TitlePath = "name",
            UrlPath = "id",
            SnippetPath = "description",
            UrlPrefix = "https://crates.io/crates/"
        },
        new JsonApiSearchDefinition
        {
            Name = "nuget",
            Endpoint = "https://azuresearch-usnc.nuget.org/query",
            QueryParameter = "q",
            LimitParameter = "$top",
            ResultsPath = "data",
            TitlePath = "id",
            UrlPath = "@id",
            SnippetPath = "description"
        },
        new JsonApiSearchDefinition
        {
            Name = "openlibrary",
            Endpoint = "https://openlibrary.org/search.json",
            QueryParameter = "q",
            LimitParameter = "limit",
            ResultsPath = "docs",
            TitlePath = "title",
            UrlPath = "key",
            UrlPrefix = "https://openlibrary.org"
        },
        new JsonApiSearchDefinition
        {
            Name = "europepmc",
            Endpoint = "https://www.ebi.ac.uk/europepmc/webservices/rest/search",
            QueryParameter = "query",
            LimitParameter = "pageSize",
            ResultsPath = "resultList.result",
            TitlePath = "title",
            UrlPath = "id",
            SnippetPath = "authorString",
            UrlPrefix = "https://europepmc.org/article/MED/"
        },
        new JsonApiSearchDefinition
        {
            Name = "semanticscholar",
            Endpoint = "https://api.semanticscholar.org/graph/v1/paper/search",
            QueryParameter = "query",
            LimitParameter = "limit",
            ResultsPath = "data",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "abstract",
            FixedParameters = new Dictionary<string, string> { ["fields"] = "title,url,abstract" }
        },
        new JsonApiSearchDefinition
        {
            Name = "github",
            Endpoint = "https://api.github.com/search/repositories",
            QueryParameter = "q",
            LimitParameter = "per_page",
            ResultsPath = "items",
            TitlePath = "full_name",
            UrlPath = "html_url",
            SnippetPath = "description"
        },
        new JsonApiSearchDefinition
        {
            Name = "dockerhub",
            Endpoint = "https://hub.docker.com/v2/search/repositories/",
            QueryParameter = "query",
            LimitParameter = "page_size",
            ResultsPath = "results",
            TitlePath = "repo_name",
            UrlPath = "repo_name",
            SnippetPath = "short_description",
            UrlPrefix = "https://hub.docker.com/r/"
        },
        new JsonApiSearchDefinition
        {
            Name = "googlebooks",
            Endpoint = "https://www.googleapis.com/books/v1/volumes",
            QueryParameter = "q",
            LimitParameter = "maxResults",
            ResultsPath = "items",
            TitlePath = "volumeInfo.title",
            UrlPath = "volumeInfo.infoLink",
            SnippetPath = "volumeInfo.description"
        },
        new JsonApiSearchDefinition
        {
            Name = "internetarchive",
            Endpoint = "https://archive.org/advancedsearch.php",
            QueryParameter = "q",
            LimitParameter = "rows",
            ResultsPath = "response.docs",
            TitlePath = "title",
            UrlPath = "identifier",
            SnippetPath = "description",
            UrlPrefix = "https://archive.org/details/",
            FixedParameters = new Dictionary<string, string>
            {
                ["fl[]"] = "identifier,title,description",
                ["output"] = "json"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "rubygems",
            Endpoint = "https://rubygems.org/api/v1/search.json",
            QueryParameter = "query",
            LimitParameter = "limit",
            ResultsPath = "$",
            TitlePath = "name",
            UrlPath = "project_uri",
            SnippetPath = "info"
        },
        new JsonApiSearchDefinition
        {
            Name = "huggingface",
            Endpoint = "https://huggingface.co/api/models",
            QueryParameter = "search",
            LimitParameter = "limit",
            ResultsPath = "$",
            TitlePath = "id",
            UrlPath = "id",
            SnippetPath = "pipeline_tag",
            UrlPrefix = "https://huggingface.co/"
        },
        new JsonApiSearchDefinition
        {
            Name = "superuser",
            Endpoint = "https://api.stackexchange.com/2.3/search/advanced",
            QueryParameter = "q",
            LimitParameter = "pagesize",
            ResultsPath = "items",
            TitlePath = "title",
            UrlPath = "link",
            SnippetPath = "body",
            FixedParameters = new Dictionary<string, string> { ["site"] = "superuser" }
        },
        new JsonApiSearchDefinition
        {
            Name = "serverfault",
            Endpoint = "https://api.stackexchange.com/2.3/search/advanced",
            QueryParameter = "q",
            LimitParameter = "pagesize",
            ResultsPath = "items",
            TitlePath = "title",
            UrlPath = "link",
            SnippetPath = "body",
            FixedParameters = new Dictionary<string, string> { ["site"] = "serverfault" }
        },
        new JsonApiSearchDefinition
        {
            Name = "askubuntu",
            Endpoint = "https://api.stackexchange.com/2.3/search/advanced",
            QueryParameter = "q",
            LimitParameter = "pagesize",
            ResultsPath = "items",
            TitlePath = "title",
            UrlPath = "link",
            SnippetPath = "body",
            FixedParameters = new Dictionary<string, string> { ["site"] = "askubuntu" }
        },
        new JsonApiSearchDefinition
        {
            Name = "gitlab",
            Endpoint = "https://gitlab.com/api/v4/projects",
            QueryParameter = "search",
            LimitParameter = "per_page",
            ResultsPath = "$",
            TitlePath = "name_with_namespace",
            UrlPath = "web_url",
            SnippetPath = "description"
        },
        new JsonApiSearchDefinition
        {
            Name = "gitea",
            Endpoint = "https://gitea.com/api/v1/repos/search",
            QueryParameter = "q",
            LimitParameter = "limit",
            ResultsPath = "data",
            TitlePath = "full_name",
            UrlPath = "html_url",
            SnippetPath = "description"
        },
        new JsonApiSearchDefinition
        {
            Name = "mavencentral",
            Endpoint = "https://search.maven.org/solrsearch/select",
            QueryParameter = "q",
            LimitParameter = "rows",
            ResultsPath = "response.docs",
            TitlePath = "id",
            UrlPath = "id",
            SnippetPath = "latestVersion",
            UrlPrefix = "https://central.sonatype.com/artifact/",
            FixedParameters = new Dictionary<string, string> { ["wt"] = "json" }
        },
        new JsonApiSearchDefinition
        {
            Name = "packagist",
            Endpoint = "https://packagist.org/search.json",
            QueryParameter = "q",
            LimitParameter = "per_page",
            ResultsPath = "results",
            TitlePath = "name",
            UrlPath = "url",
            SnippetPath = "description"
        },
        new JsonApiSearchDefinition
        {
            Name = "pubdev",
            Endpoint = "https://pub.dev/api/search",
            QueryParameter = "q",
            LimitParameter = "limit",
            ResultsPath = "packages",
            TitlePath = "package",
            UrlPath = "package",
            UrlPrefix = "https://pub.dev/packages/"
        },
        new JsonApiSearchDefinition
        {
            Name = "bilibili",
            Endpoint = "https://api.bilibili.com/x/web-interface/search/type",
            QueryParameter = "keyword",
            LimitParameter = "page_size",
            ResultsPath = "data.result",
            TitlePath = "title",
            UrlPath = "arcurl",
            SnippetPath = "description",
            FixedParameters = new Dictionary<string, string> { ["search_type"] = "video" }
        },
        new JsonApiSearchDefinition
        {
            Name = "dailymotion",
            Endpoint = "https://api.dailymotion.com/videos",
            QueryParameter = "search",
            LimitParameter = "limit",
            ResultsPath = "list",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "description",
            FixedParameters = new Dictionary<string, string> { ["fields"] = "id,title,description,url" }
        },
        new JsonApiSearchDefinition
        {
            Name = "deezer",
            Endpoint = "https://api.deezer.com/search",
            QueryParameter = "q",
            LimitParameter = "limit",
            ResultsPath = "data",
            TitlePath = "title",
            UrlPath = "link",
            SnippetPath = "artist.name"
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
            Endpoint = "https://musicbrainz.org/ws/2/artist/",
            QueryParameter = "query",
            LimitParameter = "limit",
            ResultsPath = "artists",
            TitlePath = "name",
            UrlPath = "id",
            SnippetPath = "disambiguation",
            UrlPrefix = "https://musicbrainz.org/artist/",
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
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "peertube",
            Endpoint = "https://peertube.cpy.re/api/v1/search/videos",
            QueryParameter = "search",
            LimitParameter = "count",
            ResultsPath = "data",
            TitlePath = "name",
            UrlPath = "url",
            SnippetPath = "description",
            FixedParameters = new Dictionary<string, string> { ["searchTarget"] = "local" }
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
                ["srprop"] = "snippet"
            }
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
            Endpoint = "https://api.openverse.org/v1/images/",
            QueryParameter = "q",
            LimitParameter = "page_size",
            ResultsPath = "results",
            TitlePath = "title",
            UrlPath = "foreign_landing_url",
            SnippetPath = "attribution"
        }
        ,
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

    public static IReadOnlyList<JsonApiSearchDefinition> Definitions =>
        BaseDefinitions.Concat(AdditionalDefinitions).ToArray();

    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        foreach (JsonApiSearchDefinition definition in Definitions)
        {
            if (TryConfigureDefinition(definition, configuration, out JsonApiSearchDefinition configured))
            {
                RegisterProvider(services, configured);
            }
        }
    }

    private static bool TryConfigureDefinition(
        JsonApiSearchDefinition definition,
        IConfiguration configuration,
        out JsonApiSearchDefinition configured)
    {
        configured = definition;
        if (string.IsNullOrWhiteSpace(definition.ApiKeyConfigurationKey))
        {
            return true;
        }

        string? apiKey = configuration[definition.ApiKeyConfigurationKey];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        Dictionary<string, string> headers = definition.Headers.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        headers[definition.ApiKeyHeader] = definition.ApiKeyPrefix + apiKey;
        configured = definition with { Headers = headers };
        return true;
    }

    private static void RegisterProvider(IServiceCollection services, JsonApiSearchDefinition definition)
    {
        services.AddHttpClient(definition.Name, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            foreach (KeyValuePair<string, string> header in definition.Headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        });
        services.AddSingleton<ISearchProvider>(serviceProvider =>
            new JsonApiSearchProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(definition.Name),
                definition,
                serviceProvider.GetRequiredService<SearchHttpTransport>()));
    }
}
