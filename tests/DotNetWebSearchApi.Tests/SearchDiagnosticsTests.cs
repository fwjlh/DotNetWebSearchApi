using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class SearchDiagnosticsTests
{
    [Fact]
    public void GetSnapshot_ReturnsNotRunForNewProviders()
    {
        SearchDiagnostics diagnostics = new();
        ProviderDiagnosticsSnapshot snapshot = Assert.Single(diagnostics.GetSnapshot(new[] { "alpha" }));
        Assert.Equal("not-run", snapshot.Status);
        Assert.Equal(0, snapshot.SuccessfulSearches);
        Assert.Equal(0, snapshot.FailedSearches);
        Assert.Null(snapshot.LastChecked);
    }

    [Fact]
    public void RecordSuccess_TracksDurationAndResultCount()
    {
        SearchDiagnostics diagnostics = new();
        diagnostics.RecordSuccess("alpha", TimeSpan.FromMilliseconds(24), 3);
        ProviderDiagnosticsSnapshot snapshot = Assert.Single(diagnostics.GetSnapshot(new[] { "alpha" }));
        Assert.Equal("healthy", snapshot.Status);
        Assert.Equal(1, snapshot.SuccessfulSearches);
        Assert.Equal(0, snapshot.FailedSearches);
        Assert.Equal(24, snapshot.LastDurationMilliseconds);
        Assert.Equal(3, snapshot.LastResultCount);
        Assert.NotNull(snapshot.LastChecked);
    }

    [Fact]
    public void RecordFailure_TracksErrorTypeAndFailureCount()
    {
        SearchDiagnostics diagnostics = new();
        diagnostics.RecordFailure("alpha", TimeSpan.FromMilliseconds(80), "HttpRequestException");
        ProviderDiagnosticsSnapshot snapshot = Assert.Single(diagnostics.GetSnapshot(new[] { "alpha" }));
        Assert.Equal("unresponsive", snapshot.Status);
        Assert.Equal(0, snapshot.SuccessfulSearches);
        Assert.Equal(1, snapshot.FailedSearches);
        Assert.Equal("HttpRequestException", snapshot.LastErrorType);
    }

    [Fact]
    public void RecordSuccess_AfterFailureRestoresHealthyStatus()
    {
        SearchDiagnostics diagnostics = new();
        diagnostics.RecordFailure("alpha", TimeSpan.FromMilliseconds(80), "HttpRequestException");
        diagnostics.RecordSuccess("alpha", TimeSpan.FromMilliseconds(20), 2);
        ProviderDiagnosticsSnapshot snapshot = Assert.Single(diagnostics.GetSnapshot(new[] { "alpha" }));
        Assert.Equal("healthy", snapshot.Status);
        Assert.Equal(1, snapshot.SuccessfulSearches);
        Assert.Equal(1, snapshot.FailedSearches);
        Assert.Null(snapshot.LastErrorType);
    }
}
