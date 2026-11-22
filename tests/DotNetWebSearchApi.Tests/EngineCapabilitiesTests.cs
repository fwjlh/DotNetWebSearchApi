using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class EngineCapabilitiesTests
{
    [Fact]
    public void Supports_RejectsTimeRangesOutsideProviderSubset()
    {
        EngineCapabilities capabilities = new(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" },
            false,
            1,
            true,
            false,
            false)
        {
            SupportedTimeRanges = new HashSet<SearchTimeRange>
            {
                SearchTimeRange.Day,
                SearchTimeRange.Week,
                SearchTimeRange.Month
            }
        };

        Assert.True(capabilities.Supports(new SearchQuery("test", 10) { TimeRange = SearchTimeRange.Day }));
        Assert.False(capabilities.Supports(new SearchQuery("test", 10) { TimeRange = SearchTimeRange.Year }));
    }
}
