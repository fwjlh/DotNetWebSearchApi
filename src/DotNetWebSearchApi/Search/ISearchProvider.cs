namespace DotNetWebSearchApi.Search;

public interface ISearchProvider
{
    string Name { get; }

    TimeSpan Timeout { get; }

    Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken);
}
