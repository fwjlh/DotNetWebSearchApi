# Locale and region traits: Python EngineTraits to C#

Date: 2026-11-22

## Upstream Python shape

SearXNG's `EngineTraits` is a per-engine mapping with:
- `languages`: SearXNG language tag → upstream engine language value.
- `regions`: SearXNG locale/region tag → upstream market/region value.
- `all_locale`: the engine's explicit value for SearXNG's "all/default language".
- `custom`: provider-specific data, such as Wikipedia language-to-hostname mappings.

The methods `get_language` and `get_region` delegate to SearXNG's locale best-match rules. Traits are fetched from upstream provider pages where possible, persisted to `engine_traits.json`, and reused if a refresh fails. This gives broad provider-specific coverage, but it also means trait acquisition and persistence are a separate subsystem from an individual search request.

Pinned source: [searx/enginelib/traits.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/enginelib/traits.py).

## C# translation decision

The C# service now has:
- `SearchQuery.Language` as the incoming BCP-47-style locale.
- `EngineLocaleTraits` for language/region maps and an all-locale fallback.
- `EngineLocaleRegistry` as a versioned in-code registry with deterministic lookup.
- Provider-specific request builders that consume the resolved value.
- `EngineCapabilities.SupportsLocale` so a provider is only selected for a non-default locale when it declares locale support.

The first implementation is intentionally static. It does not scrape provider preference pages at runtime, download traits on every startup or persist dynamic maps. This avoids adding an unreliable external dependency to the search path. The trade-off is that the initial locale map is a curated subset, not SearXNG's full dynamically fetched catalogue.

## Bing: first request builder using the registry

**Python source:** [searx/engines/bing.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/bing.py)

Python's Bing engine fetches the region selector from `/account/general`, derives market codes from official languages and maps a locale such as `en-GB` to `setlang=en&cc=gb`. It deliberately omits `cc` for US, China and Russia. It also contains a special correction for `zh-HK`, which maps to the market `en-HK`. Its request maps safe-search levels 0/1/2 to `off`/`moderate`/`strict`.

C# now:
- resolves the locale through `EngineLocaleRegistry.GetRegion("bing", language)`;
- emits `setlang` and `cc` using the upstream request shape;
- preserves the `zh-HK → en-HK` exception;
- maps safe-search 0/1/2 to the same three Bing values;
- declares categories `general` and `web`, locale and safe-search support;
- does not declare pagination or time-range support, matching the upstream module's documented JavaScript dependency for those features.

The initial static map covers common locales including en-GB, en-US, fr-FR, fr-CA, de-DE, es-ES, ja-JP, zh-CN, zh-TW and zh-HK. Unknown or language-only locales resolve to Bing's `clear` all-market setting rather than inventing a market code.

C# source:
- [EngineLocaleRegistry.cs](../../src/DotNetWebSearchApi/Search/EngineLocaleRegistry.cs)
- [BingSearchProvider.cs](../../src/DotNetWebSearchApi/Search/BingSearchProvider.cs)
- [BingSearchProviderTests.cs](../../tests/DotNetWebSearchApi.Tests/BingSearchProviderTests.cs)
- [EngineLocaleRegistryTests.cs](../../tests/DotNetWebSearchApi.Tests/EngineLocaleRegistryTests.cs)

## Why this is not yet a full EngineTraits port

- The registry currently contains Bing region mappings only; the language-map and custom-trait slots are present for future providers but are not populated.
- The curated list is deliberately smaller than the dynamic upstream map. Unknown locales fall back to the provider's all-locale value; they are not silently mapped to a guessed country.
- DuckDuckGo has region and language traits plus a special language-region mapping; Wikipedia maps language codes to hostnames and language-converter variants. Those need separate data and tests rather than being forced into Bing's market-code logic.
- No automatic trait refresh, JSON persistence, conflict resolution or stale-data fallback has been implemented yet.

## Next porting sequence

1. Add DuckDuckGo region/language maps and carry `kl` / `df` request state only when the provider implementation supports the relevant fields.
2. Add Wikipedia language-to-host mappings and LanguageConverter variants; preserve its different API/result semantics.
3. Add Google locale/region traits and map `hl`, `lr` and `cr` without claiming support until the request builder consumes them.
4. Add a versioned external JSON source only if maintaining static maps becomes costly. Do not make live trait fetching part of a user search.

## Verification limits

Fixture tests verify known Bing mappings, safe-search request construction and conservative fallback. They do not prove current live market behaviour. No live upstream requests were made.
