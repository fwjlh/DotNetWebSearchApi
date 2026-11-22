using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using HtmlAgilityPack;

namespace DotNetWebSearchApi.Search;

public sealed class MojeekSearchProvider : ISearchProvider
{
    private static readonly Uri BaseUri = new("https://www.mojeek.com/");
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookies;
    private readonly SearchHttpTransport _transport;
    private readonly SemaphoreSlim _challengeGate = new(1, 1);

    public MojeekSearchProvider(
        HttpClient httpClient,
        CookieContainer cookies,
        SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _cookies = cookies;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "mojeek";

    public TimeSpan Timeout => TimeSpan.FromSeconds(20);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureChallengeCookieAsync(cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Get, BuildSearchUri(query.Text));
        string cookieHeader = _cookies.GetCookieHeader(BaseUri);
        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        string html = response.Content;
        if (html.Contains("/captcha/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Mojeek returned a CAPTCHA page after challenge verification.");
        }

        response.EnsureSuccessStatusCode(Name);
        return ParseResults(html, query.Limit);
    }

    private async Task EnsureChallengeCookieAsync(CancellationToken cancellationToken)
    {
        if (HasChallengeCookie())
        {
            return;
        }

        await _challengeGate.WaitAsync(cancellationToken);
        try
        {
            if (HasChallengeCookie())
            {
                return;
            }

            await SolveChallengeAsync(cancellationToken);
        }
        finally
        {
            _challengeGate.Release();
        }
    }

    private async Task SolveChallengeAsync(CancellationToken cancellationToken)
    {
        using HttpRequestMessage challengeRequest = new(HttpMethod.Get, new Uri(BaseUri, "captcha/challenge"));
        SearchHttpResponse challengeResponse = await _transport.SendAsync(
            _httpClient,
            challengeRequest,
            cancellationToken);
        challengeResponse.EnsureSuccessStatusCode(Name);
        using JsonDocument challengeDocument = JsonDocument.Parse(challengeResponse.Content);

        JsonElement challenge = challengeDocument.RootElement.Clone();
        JsonElement parameters = challenge.GetProperty("parameters");
        AltchaSolution? solution = SolveAltcha(parameters);
        if (solution is null)
        {
            throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Mojeek Altcha proof-of-work exceeded the permitted search limit.");
        }

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            challenge,
            solution = new
            {
                counter = solution.Counter,
                derivedKey = solution.DerivedKey,
                time = 100
            }
        });
        using MultipartFormDataContent content = new();
        content.Add(new StringContent(Convert.ToBase64String(payload)), "altcha");
        using HttpRequestMessage verifyRequest = new(HttpMethod.Post, new Uri(BaseUri, "captcha/verify"))
        {
            Content = content
        };
        SearchHttpResponse verifyResponse = await _transport.SendAsync(
            _httpClient,
            verifyRequest,
            cancellationToken);
        verifyResponse.EnsureSuccessStatusCode(Name);
        StoreResponseCookies(verifyResponse);
        if (!HasChallengeCookie())
        {
            throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Mojeek did not issue a challenge cookie after proof verification.");
        }
    }

    private bool HasChallengeCookie()
    {
        return _cookies.GetCookies(BaseUri).Cast<Cookie>()
            .Any(cookie => cookie.Name == "chllg" && !cookie.Expired);
    }

    private void StoreResponseCookies(SearchHttpResponse response)
    {
        if (!response.TryGetHeader("Set-Cookie", out IReadOnlyList<string>? values))
        {
            return;
        }

        foreach (string value in values)
        {
            _cookies.SetCookies(BaseUri, value);
        }
    }

    private static AltchaSolution? SolveAltcha(JsonElement parameters)
    {
        byte[] nonce = Convert.FromHexString(parameters.GetProperty("nonce").GetString() ?? string.Empty);
        byte[] salt = Convert.FromHexString(parameters.GetProperty("salt").GetString() ?? string.Empty);
        byte[] prefix = Convert.FromHexString(parameters.GetProperty("keyPrefix").GetString() ?? string.Empty);
        int cost = parameters.GetProperty("cost").GetInt32();
        int keyLength = parameters.GetProperty("keyLength").GetInt32();
        HashAlgorithmName algorithm = GetHashAlgorithm(parameters.GetProperty("algorithm").GetString());

        for (int counter = 0; counter < 1000; counter++)
        {
            byte[] secret = new byte[nonce.Length + sizeof(uint)];
            nonce.CopyTo(secret, 0);
            BinaryPrimitives.WriteUInt32BigEndian(secret.AsSpan(nonce.Length), (uint)counter);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(secret, salt, cost, algorithm, keyLength);
            if (key.AsSpan().StartsWith(prefix))
            {
                return new AltchaSolution(Convert.ToHexString(key).ToLowerInvariant(), counter);
            }
        }

        return null;
    }

    private static HashAlgorithmName GetHashAlgorithm(string? algorithm)
    {
        string name = algorithm?.Split('/').Last().Replace("-", string.Empty).ToLowerInvariant() ?? string.Empty;
        return name switch
        {
            "sha1" => HashAlgorithmName.SHA1,
            "sha256" => HashAlgorithmName.SHA256,
            "sha384" => HashAlgorithmName.SHA384,
            "sha512" => HashAlgorithmName.SHA512,
            _ => throw new NotSupportedException("Mojeek supplied an unsupported Altcha hash algorithm.")
        };
    }

    private static Uri BuildSearchUri(string query)
    {
        string encoded = Uri.EscapeDataString(query);
        return new Uri(BaseUri, $"search?q={encoded}&safe=1");
    }

    private static IReadOnlyList<SearchResult> ParseResults(string html, int limit)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);
        HtmlNodeCollection? nodes = document.DocumentNode.SelectNodes(
            "//ul[contains(concat(' ',normalize-space(@class),' '),' results-standard ')]"
            + "/li/a[contains(concat(' ',normalize-space(@class),' '),' ob ')]");
        if (nodes is null)
        {
            return Array.Empty<SearchResult>();
        }

        List<SearchResult> results = new();
        foreach (HtmlNode node in nodes)
        {
            if (!TryReadResult(node, results.Count, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
            if (results.Count >= Math.Clamp(limit, 1, 50))
            {
                break;
            }
        }

        return results;
    }

    private static bool TryReadResult(HtmlNode node, int position, out SearchResult? result)
    {
        result = null;
        string link = node.GetAttributeValue("href", string.Empty);
        string title = CleanText(node.SelectSingleNode("../h2/a")?.InnerText ?? node.InnerText);
        string snippet = CleanText(node.SelectSingleNode("..//p[@class='s']")?.InnerText ?? string.Empty);
        if (!Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        result = new SearchResult(title, uri.AbsoluteUri, snippet, "mojeek", 1.0 / (position + 1));
        return true;
    }

    private static string CleanText(string value)
    {
        return WebUtility.HtmlDecode(HtmlEntity.DeEntitize(value)).Trim();
    }

    private sealed record AltchaSolution(string DerivedKey, int Counter);
}
