# Python-to-C# Porting Audit

Date: 2026-11-22

## Executive summary

The project currently contains 67 provider adapters and a working .NET 10 aggregation/API layer. This is meaningful provider coverage, but it is **not yet a faithful port of the SearXNG engine framework**. We have ported selected provider request/response behaviour and built our own smaller execution model around it.

The best next move is to pause provider-count expansion and port the reusable framework patterns first. That should make later provider additions faster, more consistent and more faithful.

The audit compares the current C# source with the upstream SearXNG Python implementation, principally:

- [Engine processor](https://github.com/searxng/searxng/blob/master/searx/search/processors/abstract.py)
- [Search query model](https://github.com/searxng/searxng/blob/master/searx/search/models.py)
- [Result container and ranking](https://github.com/searxng/searxng/blob/master/searx/results.py)
- [Engine traits](https://github.com/searxng/searxng/blob/master/searx/enginelib/traits.py)
- [Network layer](https://github.com/searxng/searxng/tree/master/searx/network)
- [Bing engine](https://github.com/searxng/searxng/blob/master/searx/engines/bing.py)
- [Brave engine](https://github.com/searxng/searxng/blob/master/searx/engines/brave.py)
- [DuckDuckGo engine](https://github.com/searxng/searxng/blob/master/searx/engines/duckduckgo.py)
- [Wikipedia engine](https://github.com/searxng/searxng/blob/master/searx/engines/wikipedia.py)

This is an architectural comparison, not a line-by-line port certification. SearXNG changes over time; future port work should pin an upstream commit and record the exact source revision for each engine.

## What we already have

- A common `ISearchProvider` contract and parallel `SearchAggregator`.
- Per-provider timeouts, cancellation propagation, failure isolation and basic diagnostics.
- URL validation, deduplication and result limiting.
- A declarative JSON API adapter with endpoint, query/limit parameters, JSON paths, URL prefixes, headers, API-key configuration, POST bodies and minimum request intervals.
- A separate RSS/Atom adapter and dedicated HTML/XML implementations where a generic JSON mapping is insufficient.
- Provider fixture tests and GitHub Actions CI.
- A SearXNG-shaped `/search` response mapper and provider-selection parameter.

These are good foundations. The JSON definition approach is especially useful for providers whose request and response shapes are genuinely simple.

## Main framework patterns not yet ported

### 1. A complete search request context

Upstream `SearchQuery` and `RequestParams` carry more than the search text. The engine receives the query, locale, safe-search level, page number, time range, category and engine-specific continuation data.

Our `SearchQuery` currently carries only text, result limit and an optional provider-name filter. This means provider code cannot consistently implement locale, region, page number, safe search, date range or continuation tokens.

**Port:** introduce an immutable `SearchRequestContext` (or extend `SearchQuery`) with query, locale, category set, safe-search, page, time range, requested providers, result limit and provider-specific state. Keep defaults compatible with today's API.

### 2. Engine capabilities and pre-flight eligibility

SearXNG checks engine capabilities before calling an engine. It skips unsupported page numbers, pages beyond an engine's maximum, and time ranges the engine cannot honour. It also has engine configuration for categories, paging, maximum page and weight.

Our providers mostly accept the same small query object and decide ad hoc what to do. The public SearXNG-compatible endpoint currently rejects non-general categories, non-auto language, non-default safe search and time range rather than negotiating supported subsets.

**Port:** add an `EngineDefinition`/capabilities model: categories, paging, max page, time-range support, safe-search support, locale support, weight, enabled state, API-key requirement and rate-limit policy. A shared dispatcher should decide whether each provider can participate before making a request.

### 3. Shared transport and request construction

SearXNG separates an engine's request-building function from its response parser, then sends the request through a common network layer. That common layer owns consistent transport behaviour and engine-specific request parameters can adjust headers, cookies, proxy/impersonation, redirects and request body.

Our C# providers currently build and send requests inside each `SearchAsync`. Common user-agent and header configuration is repeated in `Program.cs`; cookies and redirect handling are configured differently for individual providers. The JSON adapter has its own throttling mechanism, while other adapters have no shared rate policy.

**Port:** use a small pipeline:
1. provider builds a typed `SearchRequest` (method, URI, headers, cookies, body, redirect policy);
2. shared `SearchHttpTransport` executes it;
3. provider-specific parser converts the response into typed results.

The transport should centralise timeout, cancellation, decompression, redirect policy, cookies, proxy support, response-size limits, retry policy for safe transient failures, Retry-After handling, request diagnostics and per-host/per-provider concurrency limits. Do not retry CAPTCHA, access-denied or other permanent failures.

### 4. Failure classification and provider suspension

SearXNG tracks consecutive errors and can suspend an engine for a configured period. CAPTCHA, access-denied, network failures and ordinary HTTP errors are not all treated alike. An engine that is suspended is skipped rather than repeatedly contacted on every query.

Our aggregator catches failures and reports an unresponsive provider, but has no persistent health state, consecutive-failure policy, suspension window or reason classification. Every new search can hit the same failing endpoint again.

**Port:** add typed failure categories (timeout, rate-limited, CAPTCHA/challenge, access denied, transient HTTP, invalid response, configuration/authentication). Add a provider health state with configurable cooldown/backoff and a half-open probe. Preserve cancellation as caller cancellation, not provider failure.

### 5. Real result merging and ranking

SearXNG's `ResultContainer` merges duplicate results from multiple engines and retains the engines and positions that contributed them. Its score calculation considers engine weight, number of contributing engines and each engine's result position. It also handles answers, infoboxes, suggestions, corrections and other result types.

Our aggregator deduplicates by canonical URL and keeps the single result with the highest score. Current scores are largely generated as `1 / (position + 1)`; there is no engine weight, multi-engine contribution, combined positions or metadata merge. The compatibility mapper then fabricates a position from that score and fills answers, corrections, infoboxes and suggestions with empty arrays.

**Port:** introduce a mutable internal aggregation record (with immutable public output) that accumulates canonical URL, title/content, source engines, per-engine positions, engine weights, published date, category, template and optional rich-result data. Implement a documented scoring function and deterministic tie-breaks. Add tests where the same URL appears in two or more providers.

### 6. Engine traits: locale, language and region mappings

SearXNG has an `EngineTraits` model and persisted trait maps. Providers translate SearXNG locales into each upstream service's language and region codes. The Bing, Brave, DuckDuckGo and Wikipedia engines all use these mappings rather than relying on a single global language header.

Our implementation hard-codes a few language headers and request defaults. Locale is not part of the search query, so the user cannot request a locale and providers cannot consistently map it.

**Port:** add a provider-specific traits registry with language and region mappings, a fallback policy and tests. Start with static, versioned JSON data; add automatic trait discovery only where it is stable and useful. Avoid making every search depend on a live traits download.

### 7. Provider definitions are more than URL + JSON paths

The generic JSON definition already covers several useful dimensions, but real SearXNG engines demonstrate further reusable patterns:
- request parameters derived from locale, region, safe search and time range;
- cookie values and state carried between pages;
- multi-step token acquisition (DuckDuckGo's VQD is an example);
- multiple categories/parsers sharing one upstream engine (Brave search/news/images/videos; Bing variants);
- redirect decoding and response-specific error handling;
- pagination and continuation tokens;
- published dates, thumbnails and rich result fields.

**Port:** evolve the definition model in layers. Keep the simple JSON/RSS definition path for simple sources. Add named request strategies, parser strategies and state handlers for complex sources. Do not try to express every provider in one giant collection of nullable properties, and do not force HTML or challenge-based providers into the JSON adapter.

### 8. SearXNG API compatibility is currently partial

The current `/search` endpoint is a useful compatibility starting point, but it is not a complete SearXNG API implementation. It returns a subset of the response shape, hardcodes the general category and result limit, and does not implement locale, safe-search levels, time range, page number, suggestions, answers, corrections or infoboxes. `number_of_results` is the number returned by this API, not an upstream total.

**Port:** define an explicit compatibility target and version it. Implement request fields only when the underlying engine capability model can honour them. Return honest unsupported/empty semantics rather than silently pretending a feature is supported.

## Recommended implementation order

| Priority | Shared component | Why it comes first |
|---|---|---|
| P0 | Search request context + engine capabilities | Removes ad-hoc query handling and unlocks locale, safe search, paging and time range |
| P0 | Provider execution/health state | Stops repeatedly hitting broken or blocked providers |
| P1 | Shared HTTP transport and request model | Removes repeated headers/cookie/timeout/rate-limit plumbing |
| P1 | Result container, source positions and scoring | Makes multi-provider aggregation meaningfully SearXNG-like |
| P1 | Engine traits registry | Allows the same provider to serve different regions/languages correctly |
| P2 | Request/parser/state strategies | Makes more complex providers declarative without over-generalising |
| P2 | API compatibility conformance tests | Measures actual compatibility rather than only response shape |

## How this should accelerate future ports

For each upstream Python engine, classify its implementation into these reusable pieces:

1. **Configuration:** categories, paging, max page, weight, safe search, time range, locale and credentials.
2. **Request strategy:** GET/POST, query parameters, headers, cookies, locale mapping, tokens and continuation state.
3. **Transport requirements:** timeout, redirects, proxy/impersonation, rate limit, retries and challenge behaviour.
4. **Response strategy:** JSON, HTML/XPath-like DOM, RSS/Atom, XML, embedded JSON or multi-step parser.
5. **Normalisation:** title, URL, content, published date, thumbnail, category, template and rich-result type.
6. **Failure semantics:** CAPTCHA, rate limit, access denied, unsupported query, transient failure and permanent configuration error.
7. **Tests:** captured fixtures, request assertions, malformed response cases, paging/state tests and live smoke tests where permitted.

Each provider should then be implemented by composing existing strategies, adding a new shared strategy only when the upstream source demonstrates a genuinely new pattern.

## Suggested next work

1. Keep provider count at 67 while the shared framework is improved.
2. Add the request-context and capability model without breaking the existing `/api/search` contract.
3. Add tests for capability gating and provider eligibility.
4. Add provider health/suspension and shared transport policies.
5. Implement proper result merging/scoring and compare it with SearXNG's algorithm.
6. Add locale/traits for Bing, Brave, DuckDuckGo and Wikipedia.
7. Revisit the existing major-engine adapters and correct them against pinned upstream source revisions.
8. Resume provider additions once the common patterns are in place.

## Important distinction

The objective should be **behavioural compatibility with selected SearXNG engines and architecture**, not a mechanical translation of every Python file. SearXNG is a mature search metasearch application with settings, categories, network/session behaviour, result types, ranking and engine lifecycle. Our project is currently a .NET search-provider API with a growing set of adapters. Porting the shared contracts first is the shortest route to making the latter behave more like the former.

