using System.Collections.Generic;

namespace DotNetWebSearchApi.Search;

public static partial class JsonApiProviderCatalog
{
    private static IReadOnlyList<JsonApiSearchDefinition> AdditionalDefinitions { get; } = new[]
    {
        new JsonApiSearchDefinition
        {
            Name = "datacite",
            Endpoint = "https://api.datacite.org/dois",
            QueryParameter = "query",
            LimitParameter = "page[size]",
            ResultsPath = "data",
            TitlePath = "attributes.titles.0.title",
            UrlPath = "attributes.url",
            SnippetPath = "attributes.descriptions.0.description",
            FixedParameters = new Dictionary<string, string>
            {
                ["sort"] = "relevance",
                ["disable-facets"] = "true"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "zenodo",
            Endpoint = "https://zenodo.org/api/records",
            QueryParameter = "q",
            LimitParameter = "size",
            ResultsPath = "hits.hits",
            TitlePath = "metadata.title",
            UrlPath = "links.html",
            SnippetPath = "metadata.description",
            FixedParameters = new Dictionary<string, string> { ["sort"] = "bestmatch" }
        },
        new JsonApiSearchDefinition
        {
            Name = "openaire",
            Endpoint = "https://api.openaire.eu/graph/v4/research-products",
            QueryParameter = "search",
            LimitParameter = "page_size",
            ResultsPath = "results",
            TitlePath = "mainTitle",
            UrlPath = "id",
            SnippetPath = "descriptions.0",
            UrlPrefix = "https://explore.openaire.eu/search/publication?pid=",
            FixedParameters = new Dictionary<string, string>
            {
                ["select"] = "id,mainTitle,descriptions"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "plos",
            Endpoint = "https://api.plos.org/search",
            QueryParameter = "q",
            LimitParameter = "rows",
            ResultsPath = "response.docs",
            TitlePath = "title_display",
            UrlPath = "id",
            SnippetPath = "abstract",
            UrlPrefix = "https://doi.org/",
            MinimumIntervalMilliseconds = 6000,
            FixedParameters = new Dictionary<string, string>
            {
                ["wt"] = "json",
                ["fl"] = "id,title_display,abstract"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "wikisource",
            Endpoint = "https://en.wikisource.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            SnippetPath = "snippet",
            UrlPrefix = "https://en.wikisource.org/wiki/",
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
            Name = "wikibooks",
            Endpoint = "https://en.wikibooks.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            SnippetPath = "snippet",
            UrlPrefix = "https://en.wikibooks.org/wiki/",
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
            Name = "wikiversity",
            Endpoint = "https://en.wikiversity.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            SnippetPath = "snippet",
            UrlPrefix = "https://en.wikiversity.org/wiki/",
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
            Name = "wikivoyage",
            Endpoint = "https://en.wikivoyage.org/w/api.php",
            QueryParameter = "srsearch",
            LimitParameter = "srlimit",
            ResultsPath = "query.search",
            TitlePath = "title",
            UrlPath = "title",
            SnippetPath = "snippet",
            UrlPrefix = "https://en.wikivoyage.org/wiki/",
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
