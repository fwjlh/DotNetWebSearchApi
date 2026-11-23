using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed partial class SearxngApiIntegrationTests
{
    [Fact]
    public async Task Search_RequiresConfiguredBearerApiKey()
    {
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?>
            {
                ["SEARCH_API_KEYS"] = "0123456789abcdef0123456789abcdef"
            });
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/search?q=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.First().Scheme);
    }

    [Fact]
    public async Task Search_AcceptsConfiguredBearerApiKey()
    {
        const string apiKey = "0123456789abcdef0123456789abcdef";
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?> { ["SEARCH_API_KEYS"] = apiKey });
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using HttpResponseMessage response = await client.GetAsync("/api/search?q=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_RejectsConfiguredBearerApiKeyWhenIncorrect()
    {
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?>
            {
                ["SEARCH_API_KEYS"] = "0123456789abcdef0123456789abcdef"
            });
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

        using HttpResponseMessage response = await client.GetAsync("/api/search?q=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_RateLimitReturnsTooManyRequestsAfterSixtyRequests()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        HttpStatusCode finalStatus = HttpStatusCode.OK;

        for (int index = 0; index < 61; index++)
        {
            using HttpResponseMessage response = await client.GetAsync("/api/search?q=test");
            finalStatus = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, finalStatus);
    }
}
