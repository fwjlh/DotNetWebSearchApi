namespace DotNetWebSearchApi.Search;

public sealed record EngineLocaleTraits(
    IReadOnlyDictionary<string, string> Languages,
    IReadOnlyDictionary<string, string> Regions,
    string? AllLocale,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? Custom = null);

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

    public static string? GetCustom(string providerName, string traitName, string key, string? defaultValue = null)
    {
        if (!Traits.TryGetValue(providerName, out EngineLocaleTraits? traits)
            || traits.Custom is null
            || !traits.Custom.TryGetValue(traitName, out IReadOnlyDictionary<string, string>? values))
        {
            return defaultValue;
        }

        return values.TryGetValue(key, out string? value) ? value : defaultValue;
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

        Dictionary<string, string> wikipediaLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "en", ["fr"] = "fr", ["de"] = "de", ["es"] = "es",
            ["ja"] = "ja", ["ko"] = "ko", ["zh"] = "zh", ["ar"] = "ar",
            ["ru"] = "ru", ["pt"] = "pt", ["it"] = "it", ["nl"] = "nl",
            ["sv"] = "sv", ["pl"] = "pl", ["cs"] = "cs", ["da"] = "da",
            ["fi"] = "fi", ["vi"] = "vi", ["id"] = "id", ["he"] = "he"
        };
        Dictionary<string, string> wikipediaRegions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] = "zh", ["zh-TW"] = "zh", ["zh-HK"] = "zh",
            ["zh-MO"] = "zh", ["zh-MY"] = "zh", ["zh-SG"] = "zh"
        };
        Dictionary<string, string> wikipediaHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "en.wikipedia.org", ["fr"] = "fr.wikipedia.org",
            ["de"] = "de.wikipedia.org", ["es"] = "es.wikipedia.org",
            ["ja"] = "ja.wikipedia.org", ["ko"] = "ko.wikipedia.org",
            ["zh"] = "zh.wikipedia.org", ["ar"] = "ar.wikipedia.org",
            ["ru"] = "ru.wikipedia.org", ["pt"] = "pt.wikipedia.org",
            ["it"] = "it.wikipedia.org", ["nl"] = "nl.wikipedia.org",
            ["sv"] = "sv.wikipedia.org", ["pl"] = "pl.wikipedia.org",
            ["cs"] = "cs.wikipedia.org", ["da"] = "da.wikipedia.org",
            ["fi"] = "fi.wikipedia.org", ["vi"] = "vi.wikipedia.org",
            ["id"] = "id.wikipedia.org", ["he"] = "he.wikipedia.org"
        };

        Dictionary<string, string> googleLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "lang_en", ["fr"] = "lang_fr", ["de"] = "lang_de",
            ["es"] = "lang_es", ["ja"] = "lang_ja", ["ko"] = "lang_ko",
            ["zh"] = "lang_zh-CN", ["zh-TW"] = "lang_zh-TW",
            ["zh-HK"] = "lang_zh-TW", ["ru"] = "lang_ru",
            ["pt"] = "lang_pt-PT", ["pt-BR"] = "lang_pt-BR",
            ["it"] = "lang_it", ["nl"] = "lang_nl", ["ar"] = "lang_ar"
        };
        Dictionary<string, string> googleRegions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en-US"] = "US", ["en-GB"] = "GB", ["en-AU"] = "AU",
            ["en-CA"] = "CA", ["fr-FR"] = "FR", ["fr-CA"] = "CA",
            ["de-DE"] = "DE", ["es-ES"] = "ES", ["es-MX"] = "MX",
            ["ja-JP"] = "JP", ["ko-KR"] = "KR", ["zh-CN"] = "CN",
            ["zh-TW"] = "TW", ["zh-HK"] = "HK", ["ru-RU"] = "RU",
            ["pt-BR"] = "BR", ["it-IT"] = "IT", ["nl-NL"] = "NL"
        };

        Dictionary<string, string> qwantRegions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en-GB"] = "en_GB", ["en-US"] = "en_US", ["en-AU"] = "en_AU",
            ["en-CA"] = "en_CA", ["en-IE"] = "en_IE", ["en-NZ"] = "en_NZ",
            ["fr-FR"] = "fr_FR", ["fr-CA"] = "fr_CA", ["fr-BE"] = "fr_BE",
            ["fr-CH"] = "fr_CH", ["de-DE"] = "de_DE", ["de-AT"] = "de_AT",
            ["de-CH"] = "de_CH", ["es-ES"] = "es_ES", ["es-MX"] = "es_MX",
            ["it-IT"] = "it_IT", ["nl-NL"] = "nl_NL", ["nl-BE"] = "nl_BE",
            ["pt-PT"] = "pt_PT", ["pt-BR"] = "pt_PT"
        };

        Dictionary<string, string> yahooLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ar"] = "ar", ["bg"] = "bg", ["cs"] = "cs", ["da"] = "da",
            ["de"] = "de", ["el"] = "el", ["en"] = "en", ["es"] = "es",
            ["et"] = "et", ["fi"] = "fi", ["fr"] = "fr", ["he"] = "he",
            ["hr"] = "hr", ["hu"] = "hu", ["it"] = "it", ["ja"] = "ja",
            ["ko"] = "ko", ["lt"] = "lt", ["lv"] = "lv", ["nl"] = "nl",
            ["no"] = "no", ["pl"] = "pl", ["pt"] = "pt", ["ro"] = "ro",
            ["ru"] = "ru", ["sk"] = "sk", ["sl"] = "sl", ["sv"] = "sv",
            ["th"] = "th", ["tr"] = "tr", ["zh"] = "zh_chs"
        };
        Dictionary<string, string> yahooRegions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en-GB"] = "GB", ["en-US"] = "US", ["en-CA"] = "CA",
            ["en-AU"] = "AU", ["en-NZ"] = "NZ", ["en-IE"] = "IE",
            ["fr-FR"] = "FR", ["de-DE"] = "DE", ["es-ES"] = "ES",
            ["pt-BR"] = "BR", ["it-IT"] = "IT", ["nl-NL"] = "NL",
            ["sv-SE"] = "SE", ["no-NO"] = "NO", ["da-DK"] = "DK",
            ["fi-FI"] = "FI", ["tr-TR"] = "TR", ["pl-PL"] = "PL",
            ["ro-RO"] = "RO", ["th-TH"] = "TH", ["id-ID"] = "ID",
            ["vi-VN"] = "VN", ["zh-CN"] = "CN", ["zh-TW"] = "TW",
            ["ja-JP"] = "JP", ["ko-KR"] = "KR"
        };
        Dictionary<string, string> yahooDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CO"] = "co.search.yahoo.com", ["TH"] = "th.search.yahoo.com",
            ["VE"] = "ve.search.yahoo.com", ["CL"] = "cl.search.yahoo.com",
            ["PE"] = "pe.search.yahoo.com", ["CA"] = "ca.search.yahoo.com",
            ["DE"] = "de.search.yahoo.com", ["FR"] = "fr.search.yahoo.com",
            ["GB"] = "uk.search.yahoo.com", ["UK"] = "uk.search.yahoo.com",
            ["BR"] = "br.search.yahoo.com", ["IN"] = "in.search.yahoo.com",
            ["ES"] = "espanol.search.yahoo.com", ["PH"] = "ph.search.yahoo.com",
            ["AR"] = "ar.search.yahoo.com", ["MX"] = "mx.search.yahoo.com",
            ["SG"] = "sg.search.yahoo.com", ["AU"] = "au.search.yahoo.com",
            ["NZ"] = "nz.search.yahoo.com", ["IE"] = "ie.search.yahoo.com",
            ["ZA"] = "za.search.yahoo.com", ["US"] = "search.yahoo.com",
            ["IT"] = "it.search.yahoo.com", ["NL"] = "nl.search.yahoo.com",
            ["BE"] = "be.search.yahoo.com", ["CH"] = "ch.search.yahoo.com",
            ["AT"] = "at.search.yahoo.com", ["SE"] = "se.search.yahoo.com",
            ["NO"] = "no.search.yahoo.com", ["DK"] = "dk.search.yahoo.com",
            ["FI"] = "fi.search.yahoo.com", ["GR"] = "gr.search.yahoo.com",
            ["TR"] = "tr.search.yahoo.com", ["MY"] = "malaysia.search.yahoo.com",
            ["ID"] = "id.search.yahoo.com", ["VN"] = "vn.search.yahoo.com",
            ["PL"] = "pl.search.yahoo.com", ["RO"] = "ro.search.yahoo.com"
        };

        return new Dictionary<string, EngineLocaleTraits>(StringComparer.OrdinalIgnoreCase)
        {
            ["yahoo"] = new EngineLocaleTraits(
                yahooLanguages,
                yahooRegions,
                null,
                new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["region2domain"] = yahooDomains
                }),
            ["qwant"] = new EngineLocaleTraits(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                qwantRegions,
                "en_US"),
            ["google"] = new EngineLocaleTraits(googleLanguages, googleRegions, "ZZ"),
            ["bing"] = new EngineLocaleTraits(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                bingRegions,
                "clear"),
            ["duckduckgo"] = new EngineLocaleTraits(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                duckDuckGoRegions,
                "wt-wt"),
            ["wikipedia"] = new EngineLocaleTraits(
                wikipediaLanguages,
                wikipediaRegions,
                "en",
                new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["wiki_netloc"] = wikipediaHosts
                })
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
