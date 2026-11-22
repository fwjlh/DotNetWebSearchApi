using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class JsonApiSearchProviderTests
{
    [Theory]
    [MemberData(nameof(ProviderCases))]
    public async Task SearchAsync_UsesProviderEndpointAndMapsItsJson(
        JsonApiSearchDefinition definition,
        string fixture,
        string expectedTitle,
        string expectedUrl)
    {
        Uri? requestUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fixture, Encoding.UTF8, "application/json")
            };
        }));
        JsonApiSearchProvider provider = new(client, definition);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("C# & search", 12),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal(definition.Name, result.Provider);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(expectedUrl, result.Url);
        Assert.Contains("C%23%20%26%20search", requestUri!.Query);
        Assert.Contains(definition.LimitParameter + "=", Uri.UnescapeDataString(requestUri.Query));
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyForUnexpectedJsonShape()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"unexpected\":true}")
            }));
        JsonApiSearchProvider provider = new(client, JsonApiProviderCatalog.Definitions[0]);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("missing", 5),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_RejectsHttpErrors()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        JsonApiSearchProvider provider = new(client, JsonApiProviderCatalog.Definitions[0]);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.SearchAsync(new SearchQuery("rate limited", 5), CancellationToken.None));
    }

    public static IEnumerable<object[]> ProviderCases()
    {
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "crossref"),
            "{\"message\":{\"items\":[{\"title\":[\"Research paper\"],\"URL\":\"https://doi.org/10.1000/example\",\"abstract\":\"<p>Research abstract</p>\"}]}}",
            "Research paper",
            "https://doi.org/10.1000/example"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "openalex"),
            "{\"results\":[{\"title\":\"Open scholarly work\",\"id\":\"https://openalex.org/W123\"}]}",
            "Open scholarly work",
            "https://openalex.org/W123"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "hackernews"),
            "{\"hits\":[{\"title\":\"HN story\",\"url\":\"https://example.com/hn\",\"story_text\":\"A story\"}]}",
            "HN story",
            "https://example.com/hn"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "stackoverflow"),
            "{\"items\":[{\"title\":\"C# answer\",\"link\":\"https://stackoverflow.com/questions/1\",\"body\":\"<p>Example</p>\"}]}",
            "C# answer",
            "https://stackoverflow.com/questions/1"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "npm"),
            "{\"objects\":[{\"package\":{\"name\":\"example-package\",\"links\":{\"npm\":\"https://www.npmjs.com/package/example-package\"},\"description\":\"A package\"}}]}",
            "example-package",
            "https://www.npmjs.com/package/example-package"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "cratesio"),
            "{\"crates\":[{\"name\":\"example-crate\",\"id\":\"example-crate\",\"description\":\"A crate\"}]}",
            "example-crate",
            "https://crates.io/crates/example-crate"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "nuget"),
            "{\"data\":[{\"id\":\"Example.Package\",\"@id\":\"https://www.nuget.org/packages/Example.Package\",\"description\":\"A package\"}]}",
            "Example.Package",
            "https://www.nuget.org/packages/Example.Package"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "openlibrary"),
            "{\"docs\":[{\"title\":\"Example book\",\"key\":\"/works/OL123W\"}]}",
            "Example book",
            "https://openlibrary.org/works/OL123W"
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
