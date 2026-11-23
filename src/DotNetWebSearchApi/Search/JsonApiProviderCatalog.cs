using Microsoft.Extensions.DependencyInjection;

namespace DotNetWebSearchApi.Search;

public static partial class JsonApiProviderCatalog
{
    private static IReadOnlyList<JsonApiSearchDefinition> BaseDefinitions { get; } = new[]
    {
        new JsonApiSearchDefinition
        {
            Name = "crossref",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
            Endpoint = "https://api.crossref.org/works",
            QueryParameter = "query",
            LimitParameter = "rows",
            ResultsPath = "message.items",
            TitlePath = "title.0",
            UrlPath = "URL",
            SnippetPath = "abstract",
            PaperMetadata = new JsonApiPaperMetadataPaths
            {
                Content = "abstract",
                Authors = "author",
                AuthorGiven = "given",
                AuthorFamily = "family",
                Publisher = "publisher",
                Journal = "container-title.0",
                Volume = "volume",
                Pages = "page",
                Number = "issue",
                Doi = "DOI",
                Issn = "ISSN",
                Isbn = "ISBN",
                Type = "type",
                HtmlUrl = "URL"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "openalex",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
            Endpoint = "https://api.openalex.org/works",
            QueryParameter = "search",
            LimitParameter = "per-page",
            ResultsPath = "results",
            TitlePath = "title",
            UrlPath = "id",
            PaperMetadata = new JsonApiPaperMetadataPaths
            {
                PublishedDate = "publication_date",
                Authors = "authorships",
                AuthorName = "author.display_name",
                Publisher = "primary_location.source.host_organization_name",
                Journal = "primary_location.source.display_name",
                Volume = "biblio.volume",
                Number = "biblio.issue",
                Pages = "biblio.first_page",
                Doi = "doi",
                PdfUrl = "best_oa_location.pdf_url",
                HtmlUrl = "primary_location.landing_page_url",
                Type = "type"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "hackernews",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "news" },
            Endpoint = "https://hn.algolia.com/api/v1/search",
            QueryParameter = "query",
            LimitParameter = "hitsPerPage",
            ResultsPath = "hits",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "story_text",
            PublishedDatePath = "created_at"
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
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
            Endpoint = "https://www.ebi.ac.uk/europepmc/webservices/rest/search",
            QueryParameter = "query",
            LimitParameter = "pageSize",
            ResultsPath = "resultList.result",
            TitlePath = "title",
            UrlPath = "id",
            SnippetPath = "authorString",
            UrlPrefix = "https://europepmc.org/article/MED/",
            FixedParameters = new Dictionary<string, string> { ["format"] = "json", ["resultType"] = "core" },
            PaperMetadata = new JsonApiPaperMetadataPaths
            {
                PublishedDate = "firstPublicationDate",
                Author = "authorString",
                Journal = "journalTitle",
                Volume = "journalVolume",
                Pages = "pageInfo",
                Doi = "doi",
                Type = "pubType",
                HtmlUrl = "fullTextUrlList.fullTextUrl.0.url"
            }
        },
        new JsonApiSearchDefinition
        {
            Name = "semanticscholar",
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
            Endpoint = "https://api.semanticscholar.org/graph/v1/paper/search",
            QueryParameter = "query",
            LimitParameter = "limit",
            ResultsPath = "data",
            TitlePath = "title",
            UrlPath = "url",
            SnippetPath = "abstract",
            FixedParameters = new Dictionary<string, string>
            {
                ["fields"] = "title,url,abstract,authors,publicationDate,venue,externalIds,openAccessPdf,publicationTypes"
            },
            PaperMetadata = new JsonApiPaperMetadataPaths
            {
                PublishedDate = "publicationDate",
                Content = "abstract",
                Authors = "authors",
                AuthorName = "name",
                Journal = "venue",
                Doi = "externalIds.DOI",
                PdfUrl = "openAccessPdf.url",
                Type = "publicationTypes.0",
                HtmlUrl = "url"
            }
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

    };

    public static IReadOnlyList<JsonApiSearchDefinition> Definitions =>
        BaseDefinitions.Concat(MediaDefinitions).Concat(TailDefinitions).Concat(AdditionalDefinitions).ToArray();

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
