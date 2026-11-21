using DotNetWebSearchApi.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetWebSearchApi.Tests;

public sealed class SearchAggregatorTests
{
    [Fact]
    public async Task SearchAsync_ReturnsResultsFromAllProviders()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("alpha", Result("Alpha", "https://example.com/a")),
            Provider("beta", Result("Beta", "https://example.com/b")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, result => result.Title == "Alpha");
        Assert.Contains(results, result => result.Title == "Beta");
    }

    [Fact]
    public async Task SearchAsync_DeduplicatesUrlsAndKeepsHighestScore()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("first", Result("Lower", "https://example.com/item#part", 0.2)),
            Provider("second", Result("Higher", "https://example.com/item/", 0.9)));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal("Higher", result.Title);
    }

    [Fact]
    public async Task SearchAsync_SortsByScoreAndUsesTitleAsTieBreaker()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("one",
                Result("Zulu", "https://example.com/z", 0.5),
                Result("Alpha", "https://example.com/a", 0.5),
                Result("Top", "https://example.com/top", 0.8)));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal(new[] { "Top", "Alpha", "Zulu" }, results.Select(result => result.Title));
    }

    [Fact]
    public async Task SearchAsync_RespectsRequestedLimit()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("one",
                Result("One", "https://example.com/1"),
                Result("Two", "https://example.com/2"),
                Result("Three", "https://example.com/3")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 2),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchAsync_IgnoresProviderFailureAndKeepsOtherResults()
    {
        SearchAggregator aggregator = CreateAggregator(
            new TestProvider("broken", (_, _) => throw new InvalidOperationException("failure")),
            Provider("working", Result("Available", "https://example.com/ok")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal("Available", Assert.Single(results).Title);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyForBlankQuery()
    {
        SearchAggregator aggregator = CreateAggregator(Provider("one", Result("One", "https://example.com/1")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery(" ", 10),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_RejectsNonHttpUrls()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("one", Result("File", "file:///etc/passwd")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Empty(results);
    }

    private static SearchAggregator CreateAggregator(params ISearchProvider[] providers)
    {
        return new SearchAggregator(providers, NullLogger<SearchAggregator>.Instance);
    }

    private static TestProvider Provider(string name, params SearchResult[] results)
    {
        return new TestProvider(name, (_, _) => Task.FromResult<IReadOnlyList<SearchResult>>(results));
    }

    private static SearchResult Result(string title, string url, double score = 0.5)
    {
        return new SearchResult(title, url, title, "test", score);
    }

    private sealed class TestProvider(
        string name,
        Func<SearchQuery, CancellationToken, Task<IReadOnlyList<SearchResult>>> search) : ISearchProvider
    {
        public string Name => name;

        public TimeSpan Timeout => TimeSpan.FromSeconds(2);

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            SearchQuery query,
            CancellationToken cancellationToken)
        {
            return search(query, cancellationToken);
        }
    }
}
