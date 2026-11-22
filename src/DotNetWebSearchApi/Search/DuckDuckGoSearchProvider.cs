using System.Net;

namespace DotNetWebSearchApi.Search;

public sealed class DuckDuckGoSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://html.duckduckgo.com/html/";
    private const int MaximumQueryLength = 499;
    private readonly HttpClient _httpClient;

    public DuckDuckGoSearchProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "duckduckgo";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Text.Length > MaximumQueryLength)
        {
            return Array.Empty<SearchResult>();
        }

        using FormUrlEncodedContent content = BuildFormContent(query.Text);
        using HttpResponseMessage response = await _httpClient.PostAsync(
            SearchEndpoint,
            content,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.SeeOther)
        {
            return Array.Empty<SearchResult>();
        }

        string html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (DuckDuckGoSearchResultParser.IsCaptchaPage(html))
        {
            throw new HttpRequestException("DuckDuckGo returned a CAPTCHA page.");
        }

        response.EnsureSuccessStatusCode();
        return DuckDuckGoSearchResultParser.Parse(html);
    }

    private static FormUrlEncodedContent BuildFormContent(string query)
    {
        KeyValuePair<string, string>[] fields =
        {
            new("q", query),
            new("b", string.Empty),
            new("kl", "wt-wt")
        };

        return new FormUrlEncodedContent(fields);
    }
}
