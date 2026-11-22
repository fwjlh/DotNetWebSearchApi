# Python-to-C# Porting Rationale Corpus

Updated: 2026-11-22

## Purpose

This is the decision record for how upstream Python/SearXNG behaviour became the current C# implementation. It is not a list of provider descriptions and it is not a claim of line-for-line or feature parity.

For each port, preserve the reasoning chain:

1. **Python evidence** — the upstream behaviour, request sequence, parser rules, state and failure semantics.
2. **C# shape** — the concrete classes and methods that represent that behaviour.
3. **Translation decision** — what was preserved, replaced, simplified, omitted or deliberately deferred, and why.
4. **Observable consequences** — what the current implementation can and cannot do.
5. **Verification** — fixtures/tests, live checks, and known gaps.
6. **Provenance** — exact upstream revision and C# commit/blob references.

The key rule is: a C# adapter is not considered faithful merely because it returns the same basic title/URL/snippet fields. Request state, locale, paging, challenge handling, rate limits and special result types are part of provider behaviour too.

## Source baseline and provenance

- Upstream: [SearXNG](https://github.com/searxng/searxng)
- Upstream revision reviewed: [c6ee3e9fd0235004a694e63720efc869bb1e3594](https://github.com/searxng/searxng/commit/c6ee3e9fd0235004a694e63720efc869bb1e3594), dated 2026-11-21.
- Review date: 2026-11-22.
- C# repository: [fwjlh/DotNetWebSearchApi](https://github.com/fwjlh/DotNetWebSearchApi)
- Provider count at review: 67; frozen while shared framework work proceeds.

All upstream source links in the entries below are pinned to that revision. When a port is revisited, compare against a new pinned revision and record the delta; do not silently treat moving `master` as a stable specification.

## Translation rules used across the corpus

| Python/SearXNG shape | C# translation | Why |
|---|---|---|
| Engine module with `request()` and `response()` | Provider class plus focused parser | Keep network orchestration and parsing testable as separate responsibilities. |
| Mutable `params` dictionary | Typed query/request models | Make supported fields explicit and avoid hidden string-key contracts. |
| Shared `searx.network` client | Injected `HttpClient` / planned shared transport | Reuse .NET connection pooling, cancellation and DI; centralise policy as the framework matures. |
| Python `EngineResults` / result subclasses | Current `SearchResult`, with richer result model still planned | Preserve the common web-result contract first; do not pretend images, answers and suggestions are ordinary links. |
| `EngineTraits` locale maps | Planned typed locale/region traits registry | Keep provider-specific language/market codes out of individual request builders. |
| Engine flags such as `paging`, `safesearch`, `time_range_support` | `EngineCapabilities` | Reject unsupported requests before sending traffic rather than silently ignoring options. |
| `EngineCache` and provider session state | Explicit cache/session state with bounded lifetime | Make state ownership, expiry and secret handling visible. |
| Engine exceptions and suspension metadata | Typed provider outcomes and health/cooldown | Distinguish a timeout, a CAPTCHA, a rate limit and a configuration failure. |

These are translation targets, not assertions that every row is already implemented. See [PORTING_AUDIT.md](PORTING_AUDIT.md) for the shared-framework gap analysis.

## Provider notes

### Bing Web

**Python source:** [searx/engines/bing.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/bing.py)

**C# source:**
- [BingSearchProvider.cs](../src/DotNetWebSearchApi/Search/BingSearchProvider.cs)
- [BingSearchResultParser.cs](../src/DotNetWebSearchApi/Search/BingSearchResultParser.cs)

**Why the C# code has this shape**

- SearXNG uses Bing's HTML web results rather than the official API (`use_official_api: false`). The C# adapter therefore performs an HTTP GET and parses the result DOM instead of requiring an API key.
- The Python module separates request construction from DOM parsing. C# keeps that split as a small provider plus a dedicated `BingSearchResultParser`.
- The Python parser selects `li.b_algo`, reads the heading link and paragraph text, strips decorative `algoSlug_icon` elements, and unwraps Bing's `/ck/a` redirect URL. The C# parser preserves those important result-shape details, including the Base64URL `u=a1...` target decoding.
- The parser uses HtmlAgilityPack/XPath rather than regex over the whole page because the upstream contract is HTML structure, not a stable JSON API.

**Deliberate simplification / current gap**

- Python maps locale through `EngineTraits` into `setlang` and `cc`; C# currently sends neither.
- Python maps safe-search levels 0/1/2 to off/moderate/strict; C# hardcodes `adlt=off`.
- The Python source explicitly documents that paging and time-range results are not supported because they depend on JavaScript. The C# provider is effectively first-page only, but this limitation is not yet expressed through a provider capability declaration.
- Python removes decorative icons before extracting snippet text; C# mirrors this to avoid leaking UI artefacts into the snippet.

**Interpretation:** this is a useful structural port of Bing's HTML result extraction and redirect handling, not a complete port of Bing's locale and request semantics.

### DuckDuckGo HTML

**Python source:** [searx/engines/duckduckgo.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/duckduckgo.py)

**C# source:**
- [DuckDuckGoSearchProvider.cs](../src/DotNetWebSearchApi/Search/DuckDuckGoSearchProvider.cs)
- [DuckDuckGoSearchResultParser.cs](../src/DotNetWebSearchApi/Search/DuckDuckGoSearchResultParser.cs)

**Why the C# code has this shape**

- The selected upstream route is the no-JavaScript HTML service. Python describes it as an HTML form workflow; the C# provider therefore sends `application/x-www-form-urlencoded` POST data to `https://html.duckduckgo.com/html/`, not a browser-driven JavaScript request.
- The 499-character maximum is an upstream constraint and is checked before sending the request.
- Python treats HTTP 303 as an empty result and detects the `challenge-form` CAPTCHA markup. C# retains both checks.
- Python selects only `web-result` blocks and reads the heading link and `result__snippet`; the C# parser uses the same structural selectors.
- DuckDuckGo's redirect URL stores the destination in the `uddg` query parameter. C# resolves that target before returning the result so consumers receive the destination URL rather than the tracking wrapper.

**Important missing behaviour**

- Python's second-page flow requires a cached `vqd` token, uses additional form fields and calculates offsets. It deliberately refuses to request a later page without that token because doing so can trigger bot protection. The current C# implementation only submits the first-page form and has no VQD acquisition/cache/pagination.
- Python maps locale and time range into form fields and cookies. C# currently hardcodes `kl=wt-wt` and does not implement `df`, cookie persistence or the full locale mapping.
- Python sets browser-navigation headers and documents the relationship between user-agent, referrer, Sec-Fetch headers and bot detection. C# currently relies on the configured HttpClient defaults and does not reproduce this request profile.
- Python raises a CAPTCHA-specific engine exception with suspension semantics. C# currently throws a generic `HttpRequestException`; typed challenge outcomes and cooldown are framework work still to do.
- Python can emit a separate answer/zero-click result. The current C# `SearchResult` parser only returns ordinary web links.

**Interpretation:** the current adapter is a bounded first-page HTML search implementation. It preserves the basic form and result extraction, but not the stateful/paged SearXNG workflow. Do not enable paging by simply adding an offset.

### Brave Search

**Python source:** [searx/engines/brave.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/brave.py)

**C# source:**
- [BraveSearchProvider.cs](../src/DotNetWebSearchApi/Search/BraveSearchProvider.cs)
- [BraveSearchResultParser.cs](../src/DotNetWebSearchApi/Search/BraveSearchResultParser.cs)

**Why the C# code has this shape**

- This is not a direct translation of SearXNG's current Brave web scraper. SearXNG's Python engine parses Brave's HTML page and extracts embedded SvelteKit data; it has to handle plain objects, JavaScript IIFEs and category-dependent response paths.
- The C# implementation instead uses Brave's documented JSON Search API at `api.search.brave.com/res/v1/web/search`. That is a deliberate protocol substitution: stable JSON parsing is materially simpler and less brittle than reproducing the embedded JavaScript data extractor.
- The API requires a key, so the provider is optional and registered only when `BRAVE_SEARCH_API_KEY` is configured. This differs from the no-key HTML scrape used by the Python module.
- The parser reads the `web.results` array and maps title, URL and description into the common C# result type.

**Consequences and gaps**

- The C# adapter is an API integration inspired by the same provider, not a behavioural port of the Python HTML scraper.
- Python's Brave engine supports multiple categories (web/search, news, images, videos and goggles) with different result types and parsers. C# currently implements web search only.
- Locale/region traits and rich result fields such as thumbnails, publication dates, image dimensions, video duration/views and related-query suggestions are not yet represented by the current C# adapter.
- The API key, billing/quota and account entitlement requirements are new operational constraints introduced by the chosen C# route.

**Interpretation:** this is an intentional alternative implementation. Keep the distinction explicit in provider documentation and do not claim feature parity with the Python Brave engine.

### Mojeek

**Python source:** [searx/engines/mojeek.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/mojeek.py)

**C# source:** [MojeekSearchProvider.cs](../src/DotNetWebSearchApi/Search/MojeekSearchProvider.cs)

**Why the C# code has this shape**

- The Python engine first obtains a challenge from `/captcha/challenge`, solves the documented Altcha proof-of-work parameters, submits a base64-encoded JSON payload as multipart field `altcha` to `/captcha/verify`, and reuses the returned `chllg` cookie.
- C# preserves this protocol sequence with typed JSON parsing, PBKDF2 via `Rfc2898DeriveBytes.Pbkdf2`, multipart form submission and an explicit `CookieContainer`.
- The semaphore prevents concurrent first requests from all solving the same missing cookie at once. The cookie check is repeated after acquiring the gate to avoid duplicate work.
- The proof search is bounded to 1,000 counters and fails closed when no solution is found or the expected cookie is not issued. This is a deliberate resource bound, not a general CAPTCHA solver.
- The HTML result parser follows the same `results-standard` structure and extracts title, URL and snippet.

**Differences to retain as explicit notes**

- Python caches the challenge token in its engine cache and sends the cached cookie on requests. C# uses the injected cookie container; its lifetime and isolation depend on DI/handler configuration and must be checked to avoid sharing one user's challenge session across unrelated clients.
- Python's solution payload chooses a random `time` value in the 100–200 range; C# currently sends a fixed value of 100. This is a behavioural difference that should be verified against the protocol, not silently assumed equivalent.
- Python advertises paging (up to page 10), safe-search, locale and time-range support; C# currently sends only query and `safe=1`, so those flags are not yet ported.
- Python has general, image and news result paths; C# currently parses general web results only.
- Python's protocol helper is called `solve_altcha`; the C# implementation is a local bounded implementation. Keep test vectors for each supported hash algorithm and challenge parameter shape.

**Interpretation:** this is the closest of these four examples to a protocol-level port, but it still covers only the general web path and has request/trait differences that matter.

## How to add the next corpus entry

Copy this outline for every provider or shared framework concept:

### Provider name

- **Python source:** pinned URL and upstream commit.
- **C# source:** provider, parser, tests and related shared components.
- **Original Python shape:** configuration, request sequence, state, parser, result types and failure behaviour.
- **Why the C# shape:** explain each important design choice; distinguish direct translation from alternative protocol.
- **Preserved:** observable behaviours retained.
- **Changed:** behaviours intentionally implemented differently, with reason.
- **Omitted/deferred:** exact gaps and what would be needed to close them.
- **Tests:** fixture names/coverage; identify untested cases.
- **Live status:** never infer live acceptance from fixture tests.
- **Follow-up:** shared abstraction or provider-specific correction.

Use explicit labels: **preserved**, **adapted**, **omitted**, **known divergence**, **unverified**. Avoid vague statements such as "ported from Python" without explaining which behaviour was actually carried over.

## Immediate corpus work queue

1. Compare the remaining priority web engines against the pinned upstream source: Google, Qwant, Yahoo, Yandex, Naver and Seznam.
2. Record the exact upstream and C# behaviour for Wikipedia and the JSON/RSS definition adapters.
3. Add test links and named test cases to each provider entry.
4. After shared transport and typed outcomes exist, revisit the adapters whose current implementations hide challenge, rate-limit or session semantics.
5. Re-pin upstream and record source deltas before each substantive correction.

## Legal and attribution note

SearXNG is AGPL-3.0-or-later. This corpus records architectural and behavioural analysis and links to upstream source; it is not a licence determination. Before copying or translating substantial upstream code, review the applicable licence obligations and preserve required notices. The C# project itself is also distributed under AGPL-3.0-or-later; consult the repository's `UPSTREAM.md` and licence files.
