using System.Net;
using DotNetWebSearchApi.Search;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed partial class SearxngApiIntegrationTests
{
    [Theory]
    [InlineData("/search")]
    [InlineData("/api/search")]
    public async Task Search_AcceptsQueryAtMaximumLength(string endpoint)
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        string query = new('x', 512);

        using HttpResponseMessage response = await client.GetAsync(
            endpoint + "?q=" + Uri.EscapeDataString(query));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/search")]
    [InlineData("/api/search")]
    public async Task Search_RejectsQueryLongerThanMaximumLength(string endpoint)
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        string query = new('x', 513);

        using HttpResponseMessage response = await client.GetAsync(
            endpoint + "?q=" + Uri.EscapeDataString(query));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/search")]
    [InlineData("/api/search")]
    public async Task Search_RejectsMoreThanTenCategories(string endpoint)
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        string categories = string.Join(",", Enumerable.Range(0, 11).Select(index => "category" + index));

        using HttpResponseMessage response = await client.GetAsync(
            endpoint + "?q=test&categories=" + Uri.EscapeDataString(categories));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/search")]
    [InlineData("/api/search")]
    public async Task Search_RejectsCategoryLongerThanThirtyTwoCharacters(string endpoint)
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        string category = new('x', 33);

        using HttpResponseMessage response = await client.GetAsync(
            endpoint + "?q=test&categories=" + Uri.EscapeDataString(category));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
