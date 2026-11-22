# Python-to-C# Porting Audit

Date: 2026-11-22

## Executive summary

The project currently contains 67 provider adapters and a working .NET 10 aggregation/API layer. This is meaningful provider coverage, but it is **not yet a faithful port of the SearXNG engine framework**. We have ported selected provider request/response behaviour and built our own smaller execution model around it.

The best next move is to pause provider-count expansion and port the reusable framework patterns first. That should make later provider additions faster, more consistent and more faithful.

The audit compares the current C# source with the upstream SearXNG Python implementation, principally:

- [Engine processor](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/search/processors/abstract.py)
- [Search query model](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/search/models.py)
- [Result container and ranking](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/results.py)
- [Engine traits](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/enginelib/traits.py)
- [Network layer](https://github.com/searxng/searxng/tree/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/network)
- [Bing engine](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/bing.py)
- [Brave engine](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/brave.py)
- [DuckDuckGo engine](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/duckduckgo.py)
- [Wikipedia engine](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/wikipedia.py)

This is an architectural comparison, not a line-by-line port certification. SearXNG changes over time; future port work should pin an upstream commit and record the exact source revision for each engine.

## What we already have

- A common `ISearchProvider` contract and parallel `SearchAggregator`.
- Per-provider timeouts, cancellation propagation, failure isolation and basic diagnostics.
- URL validation, deduplication and result limiting.
- A declarative JSON API adapter with endpoint, query/limit parameters, JSON paths, URL prefixes, headers, API-key configuration, POST bodies and minimum request intervals.
- A separate RSS/Atom adapter and dedicated HTML/XML implementations where a generic JSON mapping is insufficient.
- Provider fixture tests and GitHub Actions CI.
- A SearXNG-shaped `/search` response mapper and provider-selection parameter.
- Search request context and capability pre-flight, with unsupported providers reported separately.
- Typed provider outcomes and a process-local Closed/Open/HalfOpen health registry with cooldown, backoff, one recovery probe and caller-cancellation release.
- Initial shared HTTP transport used by Bing and Qwant: bounded response reads, per-host concurrency, response-header capture and Retry-After extraction.

These are good foundations. The JSON definition approach is especially useful for providers whose request and response shapes are genuinely simple.

## Main framework patterns: completed foundations and remaining gaps

### 1. A complete search request context

Upstream `SearchQuery` and `RequestParams` carry more than the search text. The engine receives the query, locale, safe-search level, page number, time range, category and engine-specific continuation data.

Our `SearchQuery` now carries text, result limit, optional provider filter, categories, language, safe-search, page and time range. The original constructor remains compatible. Both `/api/search` and `/search` parse these values and validate their ranges. Provider-specific continuation state and region are not yet represented.

**Remaining:** add provider-specific continuation/session state only where an upstream protocol requires it. Do not mistake parsed request context for provider support: request builders still need to consume the values before capabilities can advertise them.

### 2. Engine capabilities and pre-flight eligibility

SearXNG checks engine capabilities before calling an engine. It skips unsupported page numbers, pages beyond an engine's maximum, and time ranges the engine cannot honour. It also has engine configuration for categories, paging, maximum page and weight.

An `EngineCapabilities` record and optional `ISearchProviderCapabilities` interface now describe categories, paging/max page, time-range, safe-search, locale and weight. `SearchAggregator` performs pre-flight eligibility and returns unsupported providers separately. Legacy adapters receive a conservative general/first-page/default-locale profile. The API accepts valid request context and the aggregator decides which providers can participate.

**Remaining:** add provider declarations only after each C# request builder genuinely honours the feature and fixture tests prove it. No provider should inherit SearXNG's Python capability flags automatically.

### 3. Shared transport and request construction

SearXNG separates an engine's request-building function from its response parser, then sends the request through a common network layer. That common layer owns consistent transport behaviour and engine-specific request parameters can adjust headers, cookies, proxy/impersonation, redirects and request body.

The main provider paths now use `SearchHttpTransport`: HTML adapters (Bing, DuckDuckGo, Google, Naver, Qwant, Seznam, Yahoo, Mojeek), Brave and Yandex API adapters, Wikipedia and arXiv JSON/XML adapters, plus the generic JSON and RSS/Atom providers. The transport uses `ResponseHeadersRead`, enforces a 4 MiB response limit by default, caps concurrency per host, preserves response headers and captures Retry-After. It maps common HTTP statuses to typed provider failures, applies fallback User-Agent/Accept headers only when a provider has not supplied them, and emits privacy-safe method/host/status/size/duration diagnostics without URL queries or secrets. Provider minimum intervals are handled by the shared transport's provider-keyed rate gate; the JSON definition supplies its configured interval. Common provider-specific header profiles are still configured in `Program.cs`; cookies and redirect handling remain provider-specific.

**Port:** continue the small pipeline:
1. provider builds a request (method, URI, provider-specific headers/body and session requirements);
2. shared `SearchHttpTransport` executes it and returns bounded content plus status/header metadata;
3. provider-specific challenge/status logic and parser convert the response into typed outcomes/results.

Provider-specific cookie stores are now isolated for Mojeek and Yahoo; Seznam retains its own typed-client handler-managed cookie jar. These sessions are still process-scoped, not per-caller or per-tenant. The transport still needs a deliberate policy for timeout ownership, decompression, redirects, per-caller cookie/session isolation, proxy support, richer request-header profiles and safe retries. Do not retry CAPTCHA, access-denied or other permanent failures. Do not move all cookie and redirect behaviour into a single handler where it would break provider-specific flows.

### 4. Failure classification and provider suspension

SearXNG tracks consecutive errors and can suspend an engine for a configured period. CAPTCHA, access-denied, network failures and ordinary HTTP errors are not all treated alike. An engine that is suspended is skipped rather than repeatedly contacted on every query.

The first typed outcome layer now distinguishes timeout, rate limit, transient HTTP, invalid response, authentication, access denied, challenge, configuration and unknown failures. Provider-specific adapters can throw `ProviderFailureException` with an optional retry duration. Caller cancellation is propagated rather than classified as provider failure; empty success is distinct from failure.

**Remaining:** no persistent health state, consecutive-failure policy, suspension window, cooldown/backoff or half-open probe exists yet. The generic classifier cannot recover `Retry-After` from a bare `HttpRequestException`; shared transport must preserve response headers and produce a typed failure.

### 5. Real result merging and ranking

SearXNG's `ResultContainer` merges duplicate results from multiple engines and retains the engines and positions that contributed them. Its score calculation considers engine weight, number of contributing engines and each engine's result position. It also handles answers, infoboxes, suggestions, corrections and other result types.

Our aggregator now assigns one-based positions from each provider's returned order and preserves the contributing engine names plus per-engine positions when duplicate URLs merge. It calculates the SearXNG-style weighted position score: multiply contributing engine weights by the number of positions, then sum weight divided by each position. Adapter score is retained internally to choose the primary display record. The SearXNG response mapper emits recorded positions instead of fabricating them by inverting a score. Current providers all use the default weight 1.0; no provider-specific weights or high/low priority modes are configured. Rich metadata merge and answer/infobox/suggestion models remain unimplemented.

**Remaining:** declare provider-specific engine weights from evidence/configuration, implement high/low priority modes if they are part of the compatibility target, and merge published date/category/template/rich metadata. Keep deterministic tie-breaks and tests where the same URL appears in two or more providers.

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

The current `/search` endpoint is a useful compatibility starting point, but it is not a complete SearXNG API implementation. It now parses category, page, language, safe-search and time-range context and reports `unsupported_engines` separately from `unresponsive_engines`. Providers without matching capabilities are skipped. The result shape still omits real suggestions, answers, corrections and infoboxes; `number_of_results` is the number returned by this API, not an upstream total. Result limit is still fixed on this compatibility route.

**Remaining:** define an explicit compatibility target and version it. Implement provider request fields only when the underlying adapter can honour them. Return honest unsupported/empty semantics rather than silently pretending a feature is supported.

## Recommended implementation order

| Priority | Shared component | Why it comes first |
|---|---|---|
| P0 | Provider capability declarations | Advertises only options the current C# request builders genuinely honour |
| P0 | Persistent/provider-specific health policy | Extends the initial process-local cooldown and half-open registry with persistence and per-engine policy |
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
2. Declare capabilities only where provider request builders and fixture tests substantiate them.
3. Add provider health/suspension, cooldown/backoff and half-open recovery using the typed outcomes.
4. Introduce shared HTTP transport and session policies while preserving provider-specific challenge flows.
5. Implement proper result merging/scoring and compare it with SearXNG's algorithm.
6. Add locale/traits for Bing, Brave, DuckDuckGo and Wikipedia.
7. Revisit the existing major-engine adapters against the pinned upstream rationale corpus.
8. Resume provider additions once the common patterns are in place.

## Challenge classification

The C# outcome model separates operational failure kind from challenge subtype. It distinguishes browser verification, proof-of-work, interactive CAPTCHA, interactive verification and login requirements, while keeping rate limiting and generic access denial separate. Current detectors map Google/DuckDuckGo/Yandex to interactive CAPTCHA, Qwant/Seznam to browser verification, and Mojeek Altcha to bounded proof-of-work. The public `/api/search` and SearXNG-shaped `/search` responses expose typed provider outcomes; diagnostics retain the last challenge subtype. See [challenge classification notes](PORTING/CHALLENGE_CLASSIFICATION.md). Classification is not bypass: AI may explain a challenge, but must not automatically solve CAPTCHA or human verification.

## Important distinction

The objective should be **behavioural compatibility with selected SearXNG engines and architecture**, not a mechanical translation of every Python file. SearXNG is a mature search metasearch application with settings, categories, network/session behaviour, result types, ranking and engine lifecycle. Our project is currently a .NET search-provider API with a growing set of adapters. Porting the shared contracts first is the shortest route to making the latter behave more like the former.

