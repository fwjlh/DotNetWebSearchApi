# Python-to-C# Porting Audit

Date: 2026-11-23

## Executive summary

The repository contains **67 provider adapters**, and provider expansion is frozen while the shared architecture is consolidated. This is meaningful provider coverage, but it is **not a complete or behaviourally equivalent port of the SearXNG Python framework**.

The current .NET 10 service now has a substantial shared foundation: typed request context and capability pre-flight, shared bounded HTTP transport, typed provider outcomes and challenge subtypes, process-local provider health/cooldown, locale traits for the major engines, result provenance and weighted ranking, and a SearXNG-shaped JSON endpoint.

The remaining work is primarily about fidelity, richer result types, session isolation, operational hardening and systematic provider-by-provider audit. Fixture tests prove request construction and parsing against captured examples; they do not prove live upstream acceptance.

## Source baseline and scope

The provider rationale is compared against SearXNG revision [c6ee3e9fd0235004a694e63720efc869bb1e3594](https://github.com/searxng/searxng/commit/c6ee3e9fd0235004a694e63720efc869bb1e3594), unless an entry explicitly says otherwise.

Shared framework references:
- [Engine processor](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/search/processors/abstract.py)
- [Search query model](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/search/models.py)
- [Result container and ranking](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/results.py)
- [Engine traits](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/enginelib/traits.py)
- [Network layer](https://github.com/searxng/searxng/tree/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/network)

Per-provider translation decisions are recorded in [PYTHON_TO_CSHARP_PORTING_NOTES.md](PYTHON_TO_CSHARP_PORTING_NOTES.md). Focused challenge and locale design notes are in [docs/PORTING](PORTING/).

## Implemented shared foundations

- A common `ISearchProvider` contract and parallel `SearchAggregator`.
- Per-provider timeouts, caller cancellation propagation and failure isolation.
- `SearchQuery` request context: text, result limit, optional provider selection, categories, language, safe-search, page and time range.
- `EngineCapabilities` and optional `ISearchProviderCapabilities`; unsupported providers are skipped before execution and reported separately from failures.
- `SearchHttpTransport` used by the major HTML/API providers, Wikipedia, arXiv, generic JSON and RSS/Atom. It uses bounded response reads (4 MiB default), per-host concurrency, response-header capture, Retry-After extraction, fallback User-Agent/Accept headers and privacy-safe transport diagnostics.
- Provider-keyed minimum-interval gates in the shared transport.
- Typed provider outcomes for success, failure and skipped/cooling-down states. Failure kinds include timeout, rate limit, transient HTTP, invalid response, authentication, access denied, challenge, configuration and unknown. Caller cancellation is not converted into provider failure.
- Challenge subtypes for temporary block, browser verification, proof-of-work, interactive CAPTCHA, interactive verification, login-required and unknown. Current provider detectors include Google/DuckDuckGo/Yandex CAPTCHA, Qwant DataDome, Seznam verification and Mojeek Altcha.
- `ProviderHealthRegistry` with process-local Closed/Open/HalfOpen states, failure thresholds, failure-kind cooldowns, capped exponential backoff, Retry-After handling and a single half-open recovery probe. Cooling-down providers are skipped and reported separately.
- `EngineLocaleRegistry` with curated static locale/region mappings for Bing, DuckDuckGo, Google, Qwant, Yahoo and Wikipedia.
- Result deduplication, one-based per-provider positions, contributing engine names, per-engine positions and SearXNG-style weighted-position scoring. Engine weights are configurable through capabilities; current providers use the default weight unless explicitly changed.
- `SearchResult` retains provider category and optional publication date; generic JSON adapters populate their declared category, RSS/Atom adapters declare `news` and parse RSS `pubDate` / Atom `published` or `updated`, and the SearXNG mapper preserves category and publication date. Optional `ThumbnailUrl` is currently populated for Naver general-web results. Openverse populates typed `ImageResultMetadata` with direct image/thumbnail URLs, dimensions, format, source, byte size, licence, creator and attribution; image-related URLs are restricted to absolute HTTP(S). Wikimedia Commons is intentionally general until its file-page results are enriched with direct image metadata. Dailymotion populates typed `VideoResultMetadata` with a safe embed URL when returned, thumbnail, duration and upload date; thumbnail URLs are restricted to absolute HTTP(S); embed URLs require HTTPS and either the result origin or an explicitly allowed provider host.
- GET/POST SearXNG-shaped `/search`, GET `/api/search`, provider listing and diagnostics endpoints.
- Provider fixture tests and GitHub Actions build/test workflow.

## Current architecture assessment

### 1. Request context and capability pre-flight

Both `/api/search` and SearXNG-shaped `/search` parse and validate categories, page, language, safe-search and time range. A capable provider receives those values through `SearchQuery`; an incapable provider is skipped and listed under unsupported providers rather than being labelled unresponsive.

Explicit capability declarations exist for the main providers whose request builders now consume relevant fields, including Bing, DuckDuckGo, Google, Kagi, Naver, Qwant, Wikipedia, Yahoo and Yandex. Providers without a declaration receive a conservative general/first-page/default-locale profile. That fallback is intentional: do not copy Python capability flags into C# until the C# request builder actually honours the feature.

The end-to-end request-context integration test proves that categories, page, language, safe-search and time range reach a provider that declares support. The Yandex Cloud test proves that page, locale, safe-search and time-range requests are rejected by its capability profile.

**Remaining:** provider-specific continuation tokens and richer region/session context are not yet part of the common request model. Continue auditing undeclared providers and add capabilities only with request-builder and fixture evidence.

### 2. Shared transport, sessions and rate control

Providers build their own method, URI, headers and body; `SearchHttpTransport` owns common send/read behaviour and returns bounded content plus status/header metadata. Provider-specific response logic still decides how to interpret challenges and parse results.

The transport currently enforces the response-size bound, per-host concurrency, provider-keyed minimum intervals and Retry-After capture. It adds fallback User-Agent and Accept headers only when neither the request nor configured HttpClient supplies them. Diagnostics record method, host, status, byte count, duration and exception type without logging URL query strings, response bodies, cookies or tokens.

Cookie/session state is not yet a universal per-user abstraction. Mojeek and Yahoo have separate keyed CookieContainers; Seznam uses its typed client's handler-managed cookie jar. These are process-scoped provider sessions, not isolated per caller or tenant. Handler lifetime and cross-request session semantics therefore remain operational concerns.

**Remaining:** deliberate per-caller/per-tenant session isolation, configurable redirect/decompression/proxy policy, timeout ownership and safe retry policy. Do not centralise provider-specific multi-step flows in a way that breaks their semantics. Never retry CAPTCHA, access-denied or permanent configuration failures.

### 3. Provider failure, challenge and health lifecycle

Provider exceptions are classified into typed outcomes. Challenge subtype is separate from the general failure kind, so a browser-verification page is not confused with a rate limit or a generic access denial. Mojeek's bounded Altcha proof-of-work remains a provider-specific protocol implementation; this is not an automated CAPTCHA solver.

The health registry uses a global default policy and process-local memory. It distinguishes Closed, Open and HalfOpen, opens after configured failure conditions, applies failure-kind cooldowns and allows one recovery probe. Retry-After is capped by the configured maximum. Caller cancellation releases a half-open probe and is not recorded as provider failure; a regression test asserts that provider health remains Closed with zero consecutive failures after caller cancellation.

**Remaining:** per-provider policy configuration, persistence/shared state for multiple API instances, and operationally tuned cooldown values based on observed upstream limits. The current policy resets when the process restarts.

### 4. Locale and region traits

`EngineLocaleRegistry` now has static mappings for Bing, DuckDuckGo, Google, Qwant, Yahoo and Wikipedia. The mappings are consumed by their corresponding request builders:
- Bing maps locale to `setlang`/`cc` and safe-search to off/moderate/strict.
- DuckDuckGo maps locale to `kl` and time range to `df`, including the related cookie values.
- Google maps locale, country, page, time range and safe-search.
- Qwant maps locale and its supported page/safe-search fields.
- Yahoo maps language/region and regional search domains, and handles its YBV session cookie.
- Wikipedia maps locale to a wiki host and sends Accept-Language for language variants such as zh-TW.

This is a curated static subset, not a full replacement for SearXNG's dynamically fetched and persisted EngineTraits. Unknown locales use provider-defined fallback behaviour; they are not assigned guessed country codes.

**Remaining:** broader language/region coverage, aliases and dynamic trait refresh/persistence. Extend only where the upstream source and provider request behaviour justify it.

### 5. Result model, aggregation and ranking

The aggregator normalises HTTP(S) destinations, deduplicates results, retains contributing engines and their one-based positions, and calculates a weighted-position score. `EngineCapabilities.Weight` is the provider weight input; current weights remain mostly at the default 1.0. The SearXNG mapper emits actual engine positions rather than reconstructing them from score.

The result model now retains category and optional publication date, and generic JSON/RSS adapters populate those fields where their endpoint semantics are clear. RSS 2.0 `pubDate` and Atom `published` / `updated` flow through to the SearXNG `publishedDate` field. Naver general-web parsing also supplies optional thumbnail metadata. A typed image metadata record models image source, thumbnail, resolution, format, source attribution, file size, licence, creator and alternate formats. Openverse populates the supported fields from its documented response, and image-related URLs are restricted to absolute HTTP(S). Typed `PaperResultMetadata` now models the pinned SearXNG Paper fields. arXiv Atom, Crossref, OpenAlex, Europe PMC and Semantic Scholar populate the available publication date, authors, abstract, journal/publisher, DOI and full-text link fields; the mapper selects `paper.html` only for typed paper results. DOI URLs are normalized, and PDF/HTML URLs are restricted to absolute HTTP(S). OpenAlex inverted-index abstracts are not yet reconstructed. Generic JSON definitions now support optional publication-date paths; Wikinews requests and maps its MediaWiki timestamp to the common result date and SearXNG `publishedDate`, leaving missing/invalid dates unset. Hacker News now declares both `general` and `news`; the generic parser chooses the requested vertical and preserves Algolia `created_at`. A typed music metadata record now carries audio stream/preview URL, thumbnail, artist, album, duration, country, tags, codec and bitrate. Deezer and Radio Browser populate their documented fields, and the SearXNG mapper emits `audio_src` plus shared `length`/`author` fields; music results keep the default template. MusicBrainz remains a metadata-only result. A typed video metadata record now carries embed URL, thumbnail, duration, author and view count. Dailymotion maps its safe embed URL, thumbnail, duration and upload date; Bilibili maps thumbnail, author, views and upload timestamp; PeerTube maps its same-origin embed path, current thumbnail collection, duration, author, views and publication date. The SearXNG mapper selects image/video templates and emits the corresponding fields. Dailymotion's functional embed URL requires a Player ID context that is not yet configured, so live embed behaviour remains unverified. Bilibili's legacy endpoint may require Referer/WBI handling; PeerTube uses a fixed public instance. Alternate image formats and richer paper/file result types remain outstanding. The SearXNG response currently returns empty answers, corrections, infoboxes and suggestions because those features are not yet implemented.

**Remaining:** introduce a richer typed result model before broadening image/news/video verticals. Preserve provider-specific metadata rather than flattening it into title/URL/snippet. Define merge rules for metadata when multiple providers return the same URL.

### 6. Provider adapters and translation fidelity

The provider catalogue contains 67 adapters and remains frozen. The corpus records major engine differences and C#-only adapter families. Important examples:
- Brave uses the documented JSON API in C#, rather than reproducing SearXNG's HTML/embedded-SvelteKit scraper; this introduces API-key/quota constraints and is explicitly labelled a protocol substitution.
- Yandex C# uses the authenticated Yandex Cloud Search API, rather than Python's public HTML endpoint. It now declares general/web and first-page-only capability and pre-flight rejects unsupported locale, time-range and safe-search requests.
- Wikipedia C# uses MediaWiki Action API multi-result search, rather than Python's REST page-summary/infobox workflow.
- Generic JSON definitions and RSS/Atom adapters are C# shared abstractions, not one-to-one Python base-class ports.
- Naver general-web now preserves non-circular thumbnails; images/news/videos verticals remain unimplemented.
- Mojeek's Altcha protocol is provider-specific and bounded; it must not be generalized into CAPTCHA bypass.

Every provider entry should distinguish preserved, adapted, omitted, known divergence and unverified behaviour. Fixture tests do not establish live upstream acceptance. See the [porting rationale corpus](PYTHON_TO_CSHARP_PORTING_NOTES.md).

### 7. SearXNG API compatibility

The `/search` endpoint supports GET and form-encoded POST, JSON format selection, engine selection, page, categories, language, safe-search and time range. Unknown engine names and invalid request values are rejected. Unsupported providers and cooling-down providers are reported separately from unresponsive providers.

The current compatibility route still uses a fixed result limit of 10. `number_of_results` reflects results returned by this service, not an upstream total. Answers, corrections, infoboxes and suggestions are currently empty. The API is therefore a useful compatibility subset, not a full SearXNG API implementation.

**Remaining:** define and version the compatibility target, decide how the SearXNG route should accept result limits, and add conformance tests for unsupported categories, engine selection, typed outcomes, paging and result metadata.

## Operational gaps before public exposure

- API authentication is not implemented.
- Per-client quotas and API-level rate limiting are not implemented.
- Request/query/body size limits need a deliberate public contract.
- Outbound request destination policy and provider endpoint configuration need review before allowing user-managed providers.
- Provider health and cookie/session state are process-local and not tenant-isolated.
- Live upstream smoke tests have not been run as part of this work; fixture success must not be described as live availability.

## Recommended next work

1. Keep provider expansion frozen at 67.
2. Continue the adapter audit: identify fields that are ignored, declare conservative capabilities, and add one focused test per change.
3. Expand rich result metadata and merge rules before implementing more image/news/video verticals.
4. Improve locale trait coverage where source evidence and tests support it.
5. Decide whether provider health and cookie/session state need per-provider configuration, persistence or tenant isolation for the intended hosting model.
6. Complete SearXNG compatibility conformance and result-limit behaviour.
7. Harden authentication, quotas, API rate limits, request bounds and outbound request policy before internet exposure.
8. Keep CoderInBox as an independent HTTP consumer; do not move provider implementations into that repository.

## Challenge safety boundary

Challenge classification is not challenge bypass. AI vision may classify or explain a challenge for a human operator, but must not automatically solve CAPTCHA or human-verification workflows. Permitted provider-specific protocols such as Mojeek Altcha may be implemented with bounded work and explicit resource limits. See [challenge classification notes](PORTING/CHALLENGE_CLASSIFICATION.md).

## Important distinction

The objective is **behavioural compatibility with selected SearXNG engines and architecture**, not a mechanical translation of every Python file. SearXNG is a mature metasearch application with settings, categories, network/session behaviour, result types, ranking and engine lifecycle. This repository is a .NET search-provider API with its own service boundary. The shortest path is to preserve the upstream behaviour that matters, explain deliberate differences, and keep the shared .NET abstractions small and testable.
