using System.Net;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed partial class SearxngApiIntegrationTests
{
    public async Task Search_ParsesAllRequestContextFields()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(
            "/search?q=test&pageno=2&categories=general&language=fr-FR&safesearch=1&time_range=week");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("alpha", document.RootElement.GetProperty("unsupported_engines")[0].GetString());
    }

    [Fact]
    public async Task Search_MapsRequestContextIntoProviderQuery()
    {
        CapturingProvider provider = new();
        using WebApplicationFactory<Program> factory = CreateFactory(provider);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/search?q=test&pageno=2&categories=general,news&language=fr-FR&safesearch=2&time_range=week");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(provider.LastQuery);
        Assert.Equal(2, provider.LastQuery.Page);
        Assert.Equal(new[] { "general", "news" }, provider.LastQuery.Categories!.OrderBy(value => value));
        Assert.Equal("fr-FR", provider.LastQuery.Language);
        Assert.Equal(2, provider.LastQuery.SafeSearch);
        Assert.Equal(SearchTimeRange.Week, provider.LastQuery.TimeRange);
    }

    [Fact]
    public async Task Search_RejectsInvalidRequestContextValues()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?q=test&safesearch=3");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApiSearch_AcceptsRequestContextAndReportsUnsupportedProviders()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(
            "/api/search?q=test&limit=5&pageno=2&language=fr-FR");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("alpha", document.RootElement.GetProperty("unsupportedProviders")[0].GetString());
        Assert.Empty(document.RootElement.GetProperty("results").EnumerateArray());
    }

    [Fact]
}
