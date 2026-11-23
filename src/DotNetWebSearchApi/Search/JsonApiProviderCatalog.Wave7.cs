using System.Collections.Generic;

namespace DotNetWebSearchApi.Search;

public static partial class JsonApiProviderCatalog
{
    private static IReadOnlyList<JsonApiSearchDefinition> AdditionalDefinitions { get; } = new[]
    {
        new JsonApiSearchDefinition
        {
            Name = "datacite",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
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
            },
            PaperMetadata = new JsonApiPaperMetadataPaths
            {
                Content = "attributes.descriptions.0.description",
                Authors = "attributes.creators",
                AuthorName = "name",
                Publisher = "attributes.publisher",
                Type = "attributes.types.resourceTypeGeneral",
                PublicationYear = "attributes.publicationYear",
                Doi = "attributes.doi",
                HtmlUrl = "attributes.url"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "zenodo",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
            Endpoint = "https://zenodo.org/api/records",
            QueryParameter = "q",
            LimitParameter = "size",
            ResultsPath = "hits.hits",
            TitlePath = "metadata.title",
            UrlPath = "links.html",
            SnippetPath = "metadata.description",
            FixedParameters = new Dictionary<string, string> { ["sort"] = "bestmatch" },
            PaperMetadata = new JsonApiPaperMetadataPaths
            {
                PublishedDate = "metadata.publication_date",
                Content = "metadata.description",
                Authors = "metadata.creators",
                AuthorName = "name",
                Publisher = "metadata.publisher",
                Journal = "metadata.journal.title",
                Volume = "metadata.journal.volume",
                Number = "metadata.journal.issue",
                Pages = "metadata.journal.pages",
                Doi = "metadata.doi",
                Type = "metadata.resource_type.title",
                HtmlUrl = "links.html"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "openaire",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
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
                ["select"] = "id,mainTitle,descriptions,authors,publicationDate,publisher,journal,pids,webLinks"
            },
            PaperMetadata = new JsonApiPaperMetadataPaths
            {
                PublishedDate = "publicationDate",
                Content = "descriptions.0",
                Authors = "authors",
                AuthorName = "fullName",
                Publisher = "publisher",
                Journal = "journal.title",
                Type = "type",
                HtmlUrl = "webLinks.0.url"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "plos",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
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
                ["fl"] = "id,title_display,abstract,author_display,publication_date,journal,volume,issue,page,eissn,article_type"
            },
            PaperMetadata = new JsonApiPaperMetadataPaths
            {
                PublishedDate = "publication_date",
                Content = "abstract",
                Authors = "author_display",
                Journal = "journal",
                Volume = "volume",
                Number = "issue",
                Pages = "page",
                Issn = "eissn",
                Doi = "id",
                Type = "article_type"
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
