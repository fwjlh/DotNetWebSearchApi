using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class EngineLocaleRegistryTests
{
    [Theory]
    [InlineData("en-GB", "en-GB")]
    [InlineData("fr-FR", "fr-FR")]
    [InlineData("zh-HK", "en-HK")]
    public void GetRegion_ResolvesKnownBingMarket(string locale, string expected)
    {
        Assert.Equal(expected, EngineLocaleRegistry.GetRegion("bing", locale));
    }

    [Theory]
    [InlineData("auto", "clear")]
    [InlineData("all", "clear")]
    [InlineData("fr", "clear")]
    [InlineData("xx-ZZ", "clear")]
    public void GetRegion_UsesBingAllLocaleForUnknownMarket(string locale, string expected)
    {
        Assert.Equal(expected, EngineLocaleRegistry.GetRegion("bing", locale));
    }

    [Theory]
    [InlineData("en-GB", "uk-en")]
    [InlineData("zh-HK", "hk-tzh")]
    [InlineData("zh-TW", "tw-tzh")]
    [InlineData("ja-JP", "jp-jp")]
    public void GetRegion_ResolvesKnownDuckDuckGoRegion(string locale, string expected)
    {
        Assert.Equal(expected, EngineLocaleRegistry.GetRegion("duckduckgo", locale));
    }

    [Fact]
    public void GetRegion_UsesDuckDuckGoAllRegionForUnknownLocale()
    {
        Assert.Equal("wt-wt", EngineLocaleRegistry.GetRegion("duckduckgo", "xx-ZZ"));
    }

    [Fact]
    public void GetRegion_ReturnsCallerFallbackForUnknownProvider()
    {
        Assert.Equal("en-US", EngineLocaleRegistry.GetRegion("unknown", "fr-FR", "en-US"));
    }

    [Fact]
    public void GetLanguage_ReturnsCallerFallbackWhenNoProviderMapExists()
    {
        Assert.Equal("en", EngineLocaleRegistry.GetLanguage("bing", "fr-FR", "en"));
    }
}
