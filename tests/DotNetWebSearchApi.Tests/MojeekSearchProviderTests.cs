using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class MojeekSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_SolvesChallengeAndParsesSearchResults()
    {
        const string challenge = "{\"algorithm\":\"PBKDF2/SHA-256\",\"parameters\":{\"algorithm\":\"PBKDF2/SHA-256\",\"nonce\":\"01020304\",\"salt\":\"05060708\",\"keyPrefix\":\"\",\"cost\":1,\"keyLength\":16}}";
        const string html = "<ul class=\"results-standard\"><li><a class=\"ob\" href=\"https://example.org/mojeek\">Open</a><h2><a>Mojeek result</a></h2><p class=\"s\">A search snippet</p></li></ul>";
        CookieContainer cookies = new();
        List<string> paths = new();
        string? searchCookie = null;
        using HttpClient client = new(new StubHttpMessageHandler(async request =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            if (request.RequestUri.AbsolutePath == "/captcha/challenge")
            {
                return JsonResponse(challenge);
            }

            if (request.RequestUri.AbsolutePath == "/captcha/verify")
            {
                string body = await request.Content!.ReadAsStringAsync();
                Assert.Contains("altcha", body);
                HttpResponseMessage verified = new(HttpStatusCode.OK);
                verified.Headers.TryAddWithoutValidation("Set-Cookie", "chllg=verified; Path=/; Max-Age=600");
                return verified;
            }

            searchCookie = request.Headers.GetValues("Cookie").Single();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };
        }));
        MojeekSearchProvider provider = new(client, cookies);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("Mojeek search", 10),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal("Mojeek result", result.Title);
        Assert.Equal("https://example.org/mojeek", result.Url);
        Assert.Contains("chllg=verified", searchCookie);
        Assert.Equal(new[] { "/captcha/challenge", "/captcha/verify", "/search" }, paths);
    }

    [Fact]
    public async Task SearchAsync_RejectsChallengeWhenProofCannotBeSolved()
    {
        const string challenge = "{\"parameters\":{\"algorithm\":\"PBKDF2/SHA-256\",\"nonce\":\"01020304\",\"salt\":\"05060708\",\"keyPrefix\":\"ffffffffffffffffffffffffffffffff\",\"cost\":1,\"keyLength\":16}}";
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            JsonResponse(challenge)));
        MojeekSearchProvider provider = new(client, new CookieContainer());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.SearchAsync(new SearchQuery("Mojeek", 5), CancellationToken.None));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return responseFactory(request);
        }
    }
}
