namespace DotNetWebSearchApi.Search;

public sealed record EngineLocaleTraits(
    IReadOnlyDictionary<string, string> Languages,
    IReadOnlyDictionary<string, string> Regions,
    string? AllLocale);

public static class EngineLocaleRegistry
{
    private static readonly IReadOnlyDictionary<string, EngineLocaleTraits> Traits = CreateTraits();

    public static string? GetLanguage(string providerName, string locale, string? defaultValue = null)
    {
        if (!Traits.TryGetValue(providerName, out EngineLocaleTraits? traits))
        {
            return defaultValue;
        }

        return Resolve(locale, traits.Languages, null, defaultValue, true);
    }

    public static string? GetRegion(string providerName, string locale, string? defaultValue = null)
    {
        if (!Traits.TryGetValue(providerName, out EngineLocaleTraits? traits))
        {
            return defaultValue;
        }

        return Resolve(locale, traits.Regions, traits.AllLocale, defaultValue, false);
    }

    private static IReadOnlyDictionary<string, EngineLocaleTraits> CreateTraits()
    {
        Dictionary<string, string> bingRegions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en-US"] = "en-US",
            ["en-GB"] = "en-GB",
            ["en-CA"] = "en-CA",
            ["en-AU"] = "en-AU",
            ["fr-FR"] = "fr-FR",
            ["fr-CA"] = "fr-CA",
            ["de-DE"] = "de-DE",
            ["es-ES"] = "es-ES",
            ["es-MX"] = "es-MX",
            ["it-IT"] = "it-IT",
            ["pt-BR"] = "pt-BR",
            ["ja-JP"] = "ja-JP",
            ["ko-KR"] = "ko-KR",
            ["zh-CN"] = "zh-CN",
            ["zh-TW"] = "zh-TW",
            ["zh-HK"] = "en-HK",
            ["ru-RU"] = "ru-RU",
            ["nl-NL"] = "nl-NL",
            ["sv-SE"] = "sv-SE",
            ["pl-PL"] = "pl-PL",
            ["cs-CZ"] = "cs-CZ",
            ["da-DK"] = "da-DK",
            ["fi-FI"] = "fi-FI",
            ["nb-NO"] = "nb-NO",
            ["tr-TR"] = "tr-TR"
        };

        Dictionary<string, string> duckDuckGoRegions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en-US"] = "us-en",
            ["en-GB"] = "uk-en",
            ["en-AU"] = "au-en",
            ["en-CA"] = "ca-en",
            ["fr-FR"] = "fr-fr",
            ["de-DE"] = "de-de",
            ["es-ES"] = "es-es",
            ["ja-JP"] = "jp-jp",
            ["ko-KR"] = "kr-kr",
            ["zh-TW"] = "tw-tzh",
            ["zh-HK"] = "hk-tzh",
            ["id-ID"] = "id-en",
            ["nb-NO"] = "no-no",
            ["ar-SA"] = "xa-ar",
            ["sl-SI"] = "sl-sl",
            ["th-TH"] = "th-en",
            ["vi-VN"] = "vn-en"
        };

        return new Dictionary<string, EngineLocaleTraits>(StringComparer.OrdinalIgnoreCase)
        {
            ["bing"] = new EngineLocaleTraits(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                bingRegions,
                "clear"),
            ["duckduckgo"] = new EngineLocaleTraits(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                duckDuckGoRegions,
                "wt-wt")
        };
    }

    private static string? Resolve(
        string locale,
        IReadOnlyDictionary<string, string> mappings,
        string? allLocale,
        string? defaultValue,
        bool allowLanguageFallback)
    {
        if (string.IsNullOrWhiteSpace(locale)
            || locale.Equals("auto", StringComparison.OrdinalIgnoreCase)
            || locale.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return allLocale ?? defaultValue;
        }

        if (mappings.TryGetValue(locale, out string? exact))
        {
            return exact;
        }

        if (allowLanguageFallback)
        {
            string language = locale.Split('-', 2)[0];
            if (mappings.TryGetValue(language, out string? languageValue))
            {
                return languageValue;
            }
        }

        return defaultValue ?? allLocale;
    }
}
