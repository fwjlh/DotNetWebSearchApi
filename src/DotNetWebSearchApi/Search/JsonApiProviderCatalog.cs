using Microsoft.Extensions.DependencyInjection;

namespace DotNetWebSearchApi.Search;

public static class JsonApiProviderCatalog
{
    public static IReadOnlyList<JsonApiSearchDefinition> Definitions { get; } = new[]
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
        }
    };

    public static void Register(IServiceCollection services)
    {
        foreach (JsonApiSearchDefinition definition in Definitions)
        {
            RegisterProvider(services, definition);
        }
    }

    private static void RegisterProvider(IServiceCollection services, JsonApiSearchDefinition definition)
    {
        services.AddHttpClient(definition.Name, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        services.AddSingleton<ISearchProvider>(serviceProvider =>
            new JsonApiSearchProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(definition.Name),
                definition));
    }
}
