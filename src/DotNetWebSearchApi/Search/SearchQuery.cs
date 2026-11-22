namespace DotNetWebSearchApi.Search;

public sealed record SearchQuery(string Text, int Limit, IReadOnlySet<string>? ProviderNames = null);
