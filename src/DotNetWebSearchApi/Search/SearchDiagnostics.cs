using System.Collections.Concurrent;

namespace DotNetWebSearchApi.Search;

public sealed class SearchDiagnostics
{
    private readonly ConcurrentDictionary<string, ProviderDiagnosticsSnapshot> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    public void RecordSuccess(string providerName, TimeSpan duration, int resultCount)
    {
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        _providers.AddOrUpdate(
            providerName,
            _ => new ProviderDiagnosticsSnapshot(providerName, "healthy", 1, 0, duration.TotalMilliseconds, resultCount, checkedAt, null),
            (_, current) => current with
            {
                Status = "healthy",
                SuccessfulSearches = current.SuccessfulSearches + 1,
                LastDurationMilliseconds = duration.TotalMilliseconds,
                LastResultCount = resultCount,
                LastChecked = checkedAt,
                LastErrorType = null
            });
    }

    public void RecordFailure(string providerName, TimeSpan duration, string errorType)
    {
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        _providers.AddOrUpdate(
            providerName,
            _ => new ProviderDiagnosticsSnapshot(providerName, "unresponsive", 0, 1, duration.TotalMilliseconds, 0, checkedAt, errorType),
            (_, current) => current with
            {
                Status = "unresponsive",
                FailedSearches = current.FailedSearches + 1,
                LastDurationMilliseconds = duration.TotalMilliseconds,
                LastResultCount = 0,
                LastChecked = checkedAt,
                LastErrorType = errorType
            });
    }

    public IReadOnlyList<ProviderDiagnosticsSnapshot> GetSnapshot(IEnumerable<string> providerNames)
    {
        return providerNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(GetProviderSnapshot)
            .ToArray();
    }

    private ProviderDiagnosticsSnapshot GetProviderSnapshot(string providerName)
    {
        return _providers.TryGetValue(providerName, out ProviderDiagnosticsSnapshot? snapshot)
            ? snapshot
            : new ProviderDiagnosticsSnapshot(providerName, "not-run", 0, 0, 0, 0, null, null);
    }
}

public sealed record ProviderDiagnosticsSnapshot(
    string Name,
    string Status,
    long SuccessfulSearches,
    long FailedSearches,
    double LastDurationMilliseconds,
    int LastResultCount,
    DateTimeOffset? LastChecked,
    string? LastErrorType);
