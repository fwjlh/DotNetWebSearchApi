using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed partial class SearxngApiIntegrationTests
{

    [Fact]
    public void NonDevelopmentHost_RefusesToStartWithoutApiKey()
    {
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?>
            {
                ["SEARCH_API_KEYS"] = null,
                ["ASPNETCORE_ENVIRONMENT"] = "Staging",
                ["DOTNET_ENVIRONMENT"] = "Staging"
            });
        using WebApplicationFactory<Program> factory = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => factory.CreateClient());

        Assert.Contains("SEARCH_API_KEYS", exception.ToString(), StringComparison.Ordinal);
    }


    [Fact]
    public async Task PostSearch_RejectsOversizedBodyBeforeApiKeyAuthentication()
    {
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?>
            {
                ["SEARCH_API_KEYS"] = "0123456789abcdef0123456789abcdef"
            });
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using ByteArrayContent content = new(new byte[(16 * 1024) + 1]);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/x-www-form-urlencoded");

        using HttpResponseMessage response = await client.PostAsync("/search", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Theory]
    [InlineData("/search/?q=test")]
    [InlineData("/api/search/?q=test")]
    public async Task Search_TrailingSlashDoesNotBypassBearerAuthentication(string requestPath)
    {
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?>
            {
                ["SEARCH_API_KEYS"] = "0123456789abcdef0123456789abcdef"
            });
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(requestPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostSearch_TrailingSlashStillEnforcesBodyLimitBeforeAuthentication()
    {
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?>
            {
                ["SEARCH_API_KEYS"] = "0123456789abcdef0123456789abcdef"
            });
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using ByteArrayContent content = new(new byte[(16 * 1024) + 1]);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/x-www-form-urlencoded");

        using HttpResponseMessage response = await client.PostAsync("/search/", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

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
    public async Task RootSearch_RequiresConfiguredBearerApiKey()
    {
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?>
            {
                ["SEARCH_API_KEYS"] = "0123456789abcdef0123456789abcdef"
            });
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/?q=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    public async Task UnauthorizedRequests_DoNotConsumeAuthenticatedSearchQuota()
    {
        const string apiKey = "0123456789abcdef0123456789abcdef";
        using EnvironmentVariableScope environment = EnvironmentVariableScope.Apply(
            new Dictionary<string, string?> { ["SEARCH_API_KEYS"] = apiKey });
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();

        for (int index = 0; index < 10; index++)
        {
            using HttpResponseMessage response = await client.GetAsync("/api/search?q=test");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using HttpResponseMessage blockedResponse = await client.GetAsync("/api/search?q=test");
        Assert.Equal(HttpStatusCode.TooManyRequests, blockedResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using HttpResponseMessage authorizedResponse = await client.GetAsync("/api/search?q=test");

        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
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
