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



### Google Web

**Python source:** [searx/engines/google.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/google.py)

**C# source:**
- [GoogleSearchProvider.cs](../src/DotNetWebSearchApi/Search/GoogleSearchProvider.cs)
- [GoogleSearchResultParser.cs](../src/DotNetWebSearchApi/Search/GoogleSearchResultParser.cs)
- [GoogleSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/GoogleSearchProviderTests.cs)

**Why the C# code has this shape**

- The upstream engine explicitly uses Google's HTML web page, not the Custom Search API. It does not require an API key. C# therefore makes a GET request to `www.google.com/search` and parses HTML with HtmlAgilityPack.
- Python's `google_request` composes common query parameters for several Google verticals. The web path appends the unusual `adtest` parameters and Samsung user-agent marker to obtain direct HTTPS result URLs. C# preserves those web request parameters rather than replacing the route with an unrelated API.
- Both implementations identify result cards by the `zMzFAb` / `fuLhoc` classes, read the `CVA68e` title and `FrIlee` / `fYyStc` snippet, exclude the `YVIcad` sub-element and unwrap the `/url?q=...` destination.
- The provider and parser are separate because the provider owns HTTP status/challenge handling while the parser can be tested against captured HTML without network access.
- The C# CAPTCHA check follows the same signals as Python: redirect to `sorry.google.com`, a `/sorry` path, redirect status, or a short body containing `/sorry/`.

**Known divergence / omissions**

- Python advertises paging to page 50, time-range filtering, locale traits, safe-search levels and suggestion extraction. C# currently hardcodes `hl=en`, `lr=lang_en`, `adlt=off` and does not use `SearchQuery.Page`, `TimeRange`, `Language` or `SafeSearch`.
- Python maps a SearXNG locale through Google's fetched language and region traits, and sends a consent cookie. C# does not implement those traits or the consent cookie.
- Python emits suggestion result types from a separate XPath. C# only emits ordinary web links.
- The C# CAPTCHA is currently represented by a generic `HttpRequestException`; it does not preserve Python's typed CAPTCHA/suspension signal. A broad HTTP redirect check may also classify non-CAPTCHA redirects as a block. Typed outcomes should replace this.
- The parser's CSS class selectors are inherently subject to Google's markup changes. Passing fixture tests demonstrates parser behaviour for the fixture, not that Google currently serves this DOM or accepts the request profile.

**Interpretation:** a close HTML extraction/request-shape port for the web vertical, but not a port of Google's locale, paging, time-range, safe-search or suggestion behaviour.

### Qwant Web

**Python source:** [searx/engines/qwant.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/qwant.py)

**C# source:**
- [QwantSearchProvider.cs](../src/DotNetWebSearchApi/Search/QwantSearchProvider.cs)
- [QwantSearchResultParser.cs](../src/DotNetWebSearchApi/Search/QwantSearchResultParser.cs)
- [QwantSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/QwantSearchProviderTests.cs)

**Why the C# code has this shape**

- SearXNG uses Qwant's undocumented v3 JSON API, reverse-engineered from the browser's network requests. C# uses the same `/v3/search/web` endpoint and JSON response family rather than scraping the visible page.
- The web response is not a flat list. Results live below `data.result.items.mainline`; each row has a `type` and nested `items`. C# deliberately traverses those layers and accepts only rows typed `web`, so advertisements and other result types do not leak into ordinary web results.
- `System.Text.Json` is used because the response is JSON and the C# parser can validate object/array/string shapes explicitly without a third-party dynamic object model.

**Known divergence / omissions**

- Python supports up to five pages, uses 10 results per web page, derives locale from region traits, forwards the requested safe-search level and uses a variable test-group value. C# hardcodes count 10, locale `en_GB`, offset 0, `tgp=1` and `safesearch=1`; the current query model fields are ignored.
- Python caches the rotating DataDome cookie and sends browser-like CORS/fetch headers in a defined order. C# does not acquire/cache DataDome state or reproduce that header profile.
- Python distinguishes error code 24 as rate limiting, a challenge URL as CAPTCHA (15-second suspension), HTTP 403 as access denied, and other API errors separately. C# maps 403 to a generic `HttpRequestException` and treats other non-success HTTP statuses generically; it does not preserve those distinctions.
- Python also supports news, images and videos through different response shapes and result types. C# implements web only.
- The C# parser returns empty results when the expected mainline structure is absent. That can hide a changed upstream response shape as an empty search rather than a parser/schema failure.

**Interpretation:** same undocumented JSON endpoint and broad web-result mapping, but the C# adapter is a fixed first-page, fixed-locale implementation without the upstream DataDome/session and typed failure workflow.

### Yahoo Web

**Python source:** [searx/engines/yahoo.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/yahoo.py)

**C# source:**
- [YahooSearchProvider.cs](../src/DotNetWebSearchApi/Search/YahooSearchProvider.cs)
- [YahooSearchResultParser.cs](../src/DotNetWebSearchApi/Search/YahooSearchResultParser.cs)
- [YahooSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/YahooSearchProviderTests.cs)

**Why the C# code has this shape**

- Both implementations use Yahoo's HTML web search, not an API-key service. The `algo-sr` result card, `compTitle` link, title and `compText` description are structural DOM data, so C# uses HtmlAgilityPack.
- Yahoo wraps result URLs in tracking paths containing `/RU=...` and suffix markers such as `/RS` and `/RK`. The C# parser extracts and URL-decodes the embedded destination, matching Python's `parse_url` intent.
- Yahoo uses a cookie to shape search behaviour. C# injects a `CookieContainer` and sets the `sB` cookie before the search. The explicit redirect loop bounds the tracking/geo redirect chain rather than allowing an unbounded sequence.
- The fixture test checks both redirect following and result URL unwrapping; these are separate behaviours and should remain separate tests.

**Known divergence / omissions**

- Python documents a YBV bot-detection cookie cached for 24 hours. It captures YBV during redirect hops and reuses it on subsequent requests. C# does not implement YBV acquisition/cache; it only supplies the `sB` preference cookie.
- Python derives the regional Yahoo host from locale, maps language, maps safe-search values and maps time ranges. It also implements page offsets. C# always calls `search.yahoo.com`, hardcodes the `sB` cookie values, and ignores page, language, safe-search and time range.
- Python follows at most four redirect hops and explicitly carries cookies/headers into each manual request. C# also caps redirects at four, but cookie persistence depends on the injected `HttpClient` handler's CookieContainer configuration. That lifetime and isolation need to be verified in DI.
- Python extracts `AlsoTry` suggestions. C# only returns web results.
- Python treats both 302 and 307 as redirect hops. C# recognises 302 and 307; any other redirect status is left to normal HTTP error handling.

**Interpretation:** useful HTML/parser port with tracking URL handling and a bounded redirect loop, but the Yahoo session cookie and locale/paging/safe-search behaviour are not equivalent yet.

### Yandex Web

**Python source:** [searx/engines/yandex.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/yandex.py)

**C# source:**
- [YandexSearchProvider.cs](../src/DotNetWebSearchApi/Search/YandexSearchProvider.cs)
- [YandexSearchResultParser.cs](../src/DotNetWebSearchApi/Search/YandexSearchResultParser.cs)
- [YandexSearchOptions.cs](../src/DotNetWebSearchApi/Search/YandexSearchOptions.cs)
- [YandexSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/YandexSearchProviderTests.cs)

**Important: this is a protocol substitution, not a direct port.**

- SearXNG's Python implementation uses the public Yandex HTML web page at `yandex.com/search/site/`, does not require an API key, and parses `li.serp-item` cards. It also checks the `x-yandex-captcha` response header and raises a CAPTCHA-specific exception.
- The C# implementation instead calls Yandex Cloud Search API v2 at `searchapi.api.cloud.yandex.net/v2/web/search`. It sends a POST JSON request with an API key and folder ID, requests XML output, receives that XML encoded in the JSON `rawData` field, Base64-decodes it and parses the XML with LINQ to XML.
- This API route has a different authentication, account, quota and deployment model. The C# parser's XML handling is a response to that API contract; it is not a translation of Python's HTML XPath selectors.
- The C# parser extracts `doc` elements, prefers `headline` and falls back to joined `passage` elements. It treats Yandex XML error code 15 as no results and other XML errors as failures.

**Known divergence / omissions**

- Python's public HTML route supports paging, language selection and time-range date windows without a Cloud API key. C# requires configured API key and folder ID, requests page `0`, fixes localization to English and uses moderate family mode.
- C# returns an empty list for queries over 400 characters without making a request. Python source does not use that same API-specific validation rule.
- Python can detect the `x-yandex-captcha` header and supports an images vertical with image result metadata. The Cloud API implementation has no equivalent CAPTCHA classification and only maps web documents to ordinary `SearchResult`.
- C# caps page size at 100 but does not use `SearchQuery.Page`; the request body fixes page to `0`.
- A fixture test confirms the API-key header, folder ID, JSON body, Base64 XML decoding and XML parsing. It does not establish that a configured Cloud account has permission/quota or that the public HTML route is interchangeable.

**Interpretation:** document this as a separate authenticated Yandex Cloud adapter. Do not describe it as a faithful port of SearXNG's no-key Yandex HTML engine.

### Naver Web

**Python source:** [searx/engines/naver.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/naver.py)

**C# source:**
- [NaverSearchProvider.cs](../src/DotNetWebSearchApi/Search/NaverSearchProvider.cs)
- [NaverSearchResultParser.cs](../src/DotNetWebSearchApi/Search/NaverSearchResultParser.cs)
- [NaverSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/NaverSearchProviderTests.cs)

**Why the C# code has this shape**

- The selected upstream category is Naver general/web search, which is HTML rather than an official API. C# calls `search.naver.com/search.naver` with `where=web` and parses the current web result card family `fds-web-normal-doc-root`.
- The parser reads the headline span and body span, rejects Naver's `keep.naver.com` save links as result destinations, and only emits absolute HTTP(S) links.
- This is intentionally a small web-only provider/parser split; the HTML fixture can be tested independently of Naver's live page.

**Known divergence / omissions**

- Python supports general, images, news and videos with different parsers and result types. C# implements only general web.
- Python calculates page offsets using category-specific page sizes (15 for general, 50 images, 10 news, 48 videos) and maps time ranges into the `nso` parameter. C# hardcodes `start=1` and does not use page or time range.
- Python emits thumbnails for general results. C# currently drops the thumbnail even though the source card may contain one.
- Python declares Korean as its language. C# does not map `SearchQuery.Language` or a region into the request.
- The C# request uses the same broad endpoint and selectors, but its fixed first-page URL does not preserve the original query-state semantics.

**Interpretation:** web-card extraction only. It is not a port of Naver's multi-vertical engine, pagination or time-range support.

### Seznam Web

**Python source:** [searx/engines/seznam.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/seznam.py)

**C# source:**
- [SeznamSearchProvider.cs](../src/DotNetWebSearchApi/Search/SeznamSearchProvider.cs)
- [SeznamSearchResultParser.cs](../src/DotNetWebSearchApi/Search/SeznamSearchResultParser.cs)
- [SeznamSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/SeznamSearchProviderTests.cs)

**Why the C# code has this shape**

- Seznam's search request is preceded by a homepage request. Python extracts all hidden input fields from that page, includes them with `q`, `oq` and `noredirect=1`, and forwards the homepage response cookies into the search request.
- C# preserves the two-step flow: read the homepage, parse hidden inputs, merge them with the search query parameters, then request the result page.
- The result parser follows the same nested result container under `searchpage-root`, `Layout--left` and `f2c528`, then reads the title link and description block.
- The verification redirect is treated as an access/verification condition rather than parsed as if it were an empty result page.

**Known divergence / omissions**

- Python explicitly passes the homepage cookies into the search request. C# relies on the configured `HttpClient` handler's cookie processing; the provider does not itself capture and forward the homepage response cookie collection. Verify that the registered handler uses one cookie container across both requests and that the container is isolated to the intended provider/session.
- Python raises an access-denied exception when the response path starts with `/verify`. C# detects the path but currently reports a generic `HttpRequestException`; typed verification/challenge outcomes are still required.
- The C# provider does not map any language, paging, time range or safe-search options. The Python engine declares Czech as its language and only advertises general/web categories; it does not declare paging/time-range support in this module.
- If the homepage has no hidden inputs, C# proceeds with the three fixed query fields. This is resilient, but it may conceal a changed session/bootstrap contract; tests should include a missing/rotated hidden token case.

**Interpretation:** the two-request bootstrap and DOM shape are preserved, but cookie transfer and typed verification handling are important unresolved differences.

### Wikipedia

**Python source:** [searx/engines/wikipedia.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/wikipedia.py)

**C# source:** [WikipediaSearchProvider.cs](../src/DotNetWebSearchApi/Search/WikipediaSearchProvider.cs)

**Important: the two implementations do different jobs.**

- SearXNG's Python engine is a page-summary lookup, not a search-results query. It calls the Wikimedia REST summary endpoint for a title, chooses the language-specific Wikipedia host using fetched language/region traits, and supports language-converter variants through the `Accept-Language` header.
- Python's result is richer than a normal link: depending on display configuration and page type, it can return a list result and/or an infobox containing extract text, thumbnail and links. It treats 404 and a specific invalid-title 400 as empty results.
- The C# implementation calls the English Wikipedia Action API `w/api.php?action=query&list=search`. It therefore performs a genuine text search and maps each hit's `pageid` to a `?curid=` URL. It parses HTML markup embedded in the API snippet and strips the tags.
- This is a deliberate change in product semantics: C# offers a search provider where Python's SearXNG module offers a single-page summary/infobox lookup. The two should not be compared as equivalent ports.

**Known divergence / omissions**

- C# is fixed to English and does not fetch or map Wikimedia language traits, non-standard language codes or Chinese script/region variants.
- It does not call the REST summary API, does not return an infobox, thumbnail or the canonical desktop page URL, and does not implement Python's invalid-title status handling.
- The C# search API request has no explicit limit parameter, so it uses the Action API default result count rather than `SearchQuery.Limit`.
- The repository tree contains no dedicated `WikipediaSearchProviderTests.cs` at this review point. This adapter needs fixture tests for valid search JSON, missing properties, HTML snippets, HTTP 404 and query encoding before it should be treated as covered.

**Interpretation:** retain the C# implementation as an English Wikipedia search adapter, but document the semantic difference and do not call it a port of SearXNG's summary engine.

### Generic JSON API adapters

**Python source:** [searx/engines/json_engine.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/json_engine.py)

**C# source:**
- [JsonApiSearchProvider.cs](../src/DotNetWebSearchApi/Search/JsonApiSearchProvider.cs)
- [JsonApiProviderCatalog.cs](../src/DotNetWebSearchApi/Search/JsonApiProviderCatalog.cs)
- [JsonApiProviderCatalog.Wave7.cs](../src/DotNetWebSearchApi/Search/JsonApiProviderCatalog.Wave7.cs)
- [JsonApiSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/JsonApiSearchProviderTests.cs)

**Why the C# code has this shape**

- Python's `json_engine` is a configurable engine template: a provider is described by URL, method, optional request body, headers/cookies, query parameter mappings, response paths, URL prefix, text cleanup rules and optional suggestion/thumbnail paths.
- C# makes that metadata explicit in immutable-ish `JsonApiSearchDefinition` records and registers each definition through one shared `JsonApiSearchProvider`. The catalog is data; the provider owns request construction, rate spacing, JSON traversal, URL validation and result mapping.
- The C# path reader accepts dot-separated object paths and numeric array indexes. It is deliberately simpler than Python's recursive `query/do_query` traversal, which can search recursively through nested mappings and iterables when a key appears at multiple levels.
- C# supports GET query parameters and JSON POST bodies, fixed parameters, configured headers/API-key headers, optional per-provider minimum request intervals, URL prefixes and a common title/URL/snippet result contract.
- The test suite uses parameterised fixture cases across many definitions to verify endpoint/query mapping and JSON response extraction without making live API calls.

**Known divergence / omissions**

- Python supports paging with configurable first-page number/page size, time-range substitutions, safe-search maps, language placeholders, cookies, soft redirect limits, status codes treated as empty, optional HTML-to-text handling, thumbnails and suggestions. The current C# definition/provider model does not implement most of those features.
- Python supports arbitrary method/body formatting with placeholders. C# POST support builds a JSON object from fixed body values plus query/limit fields; it is not a general template engine.
- Python's recursive JSON query semantics are more flexible than C#'s direct path traversal. C# also expects the configured results path to resolve to an array; it does not implement Python's result-query fallback behaviour for every possible shape.
- C# clamps result limits to 50, and definitions with no limit parameter simply do not send one. It does not currently propagate `SearchQuery.Page`, language, safe-search or time-range.
- The current test suite proves fixture mapping for the listed definitions. It does not prove remote API availability, authentication entitlements, quota or rate policy correctness. Some providers need per-provider error mapping and retry/cooldown rather than the shared generic `EnsureSuccessStatusCode`.

**Interpretation:** the C# generic provider is a practical typed catalogue framework inspired by Python's configurable engine, not a feature-complete implementation of every Python `json_engine` setting.

### RSS / Atom feed adapters

**C# source:**
- [RssSearchProvider.cs](../src/DotNetWebSearchApi/Search/RssSearchProvider.cs)
- [RssSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/RssSearchProviderTests.cs)

**Relationship to Python:** this is currently a C#-native feed adapter, not a demonstrated translation of a single SearXNG RSS engine module. The upstream SearXNG catalogue contains individual feed-based engines and generic JSON/XML templates, but the C# code here should be documented as its own shared implementation until each feed source is compared with its actual Python engine.

**Why the C# code has this shape**

- RSS and Atom are XML formats, so the provider uses `XmlReader` and LINQ to XML rather than HTML parsing or JSON traversal.
- The reader explicitly prohibits DTD processing and sets `XmlResolver = null`, avoiding external entity resolution while reading untrusted feed responses.
- It handles RSS `channel/item` and Atom `entry` structures, including Atom's namespace and `link href` form. It normalises title/summary/content text and validates destination URLs as HTTP(S).
- The feed definition is small: endpoint, query parameter and fixed parameters. This keeps feed-specific configuration separate from the common XML parser.
- The Bing News feed redirect is unwrapped from the `url` query parameter so consumers receive the destination article URL rather than the Bing tracking URL.

**Known divergence / omissions**

- The current feed definition does not include locale, pagination, time-range, authentication, custom headers, alternate feed namespaces or per-feed date/author/category metadata.
- The parser currently returns the common web-result shape, not a richer article/news type with publication timestamp, author, thumbnail or feed category.
- The current tests are fixture tests. They do not establish feed endpoint availability or publisher-specific query compatibility.
- Do not claim that this is a direct port of SearXNG's generic RSS engine until the precise upstream feed adapter(s) are linked and compared.

**Interpretation:** a useful secure RSS/Atom foundation, but presently a C#-native abstraction whose upstream provenance is not one-to-one.



### Shared request context and capability gate

**Python source:** SearXNG's engine contract and per-engine flags are distributed across each module's `request()`, `categories`, `paging`, `max_page`, `time_range_support`, `safesearch`, `language_support` and `EngineTraits` declarations. See the pinned [Bing](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/bing.py), [Google](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/google.py) and [Qwant](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/qwant.py) examples.

**C# source:**
- [SearchQuery.cs](../src/DotNetWebSearchApi/Search/SearchQuery.cs)
- [EngineCapabilities.cs](../src/DotNetWebSearchApi/Search/EngineCapabilities.cs)
- [SearchAggregator.cs](../src/DotNetWebSearchApi/Search/SearchAggregator.cs)
- [Program.cs](../src/DotNetWebSearchApi/Program.cs)
- [SearchAggregatorTests.cs](../tests/DotNetWebSearchApi.Tests/SearchAggregatorTests.cs)
- [SearxngApiIntegrationTests.cs](../tests/DotNetWebSearchApi.Tests/SearxngApiIntegrationTests.cs)

**Why the C# code has this shape**

- Python engine support is declared through module-level flags and interpreted by SearXNG's engine processor. C# uses a typed `SearchQuery` request object and an optional `ISearchProviderCapabilities` interface so the core aggregator does not need provider-specific reflection or string-key configuration.
- The original `SearchQuery(string Text, int Limit, IReadOnlySet<string>? ProviderNames = null)` constructor is retained. New context is init-only: categories, language, safe-search level, page and time range. Existing call sites can continue constructing the original query.
- `EngineCapabilities.Supports` performs pre-flight eligibility. A provider that cannot honour a requested option is skipped and named in `UnsupportedProviders`; it is not called and is not falsely labelled as unresponsive.
- Providers that have not declared capabilities receive a conservative `BasicWeb` profile: general category, first page, no non-default locale, no time-range and no enabled safe-search. This prevents old adapters from accidentally claiming support because a query field exists.
- The HTTP endpoints parse the same context and carry it into the query model. Invalid values are rejected as client errors; valid-but-unsupported requests remain successful API responses with a separate unsupported-provider list.
- The SearXNG-shaped response adds `unsupported_engines` without conflating it with `unresponsive_engines`. The native API exposes `unsupportedProviders`.

**Current boundary / important caveat**

- The request context is now parsed and transported, but that does not mean the existing provider request builders honour it. No provider should advertise paging, locale, safe-search or time-range until its actual outbound request uses the corresponding value and a fixture test proves it.
- The current conservative fallback means many non-default requests can legitimately produce zero eligible providers. This is safer than silently returning first-page English results for a French or time-filtered request.
- The first capability interface is intentionally small. Future work should add provider-specific category mappings and locale traits without putting Python's mutable module globals or `EngineTraits` object model directly into C#.

**Verification:** [SearchAggregatorTests.cs](../tests/DotNetWebSearchApi.Tests/SearchAggregatorTests.cs) verifies that a provider lacking page support is not called and is reported separately. [SearxngApiIntegrationTests.cs](../tests/DotNetWebSearchApi.Tests/SearxngApiIntegrationTests.cs) verifies request-context parsing, invalid values and unsupported-provider response mapping. The API integration suite passes as part of CI run [45024120851](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024120851).

**Interpretation:** this is a deliberate translation of SearXNG's declarative engine metadata into explicit C# contracts and pre-flight checks. It is the foundation for accurate support declarations, not a claim that the providers have already gained all upstream features.



### Provider exceptions, typed outcomes and cancellation

**Python source:** SearXNG provider modules raise distinct exception classes such as `SearxEngineCaptchaException`, `SearxEngineTooManyRequestsException`, `SearxEngineAccessDeniedException`, `SearxEngineAPIException` and `SearxEngineResponseException`. Some challenge exceptions carry a suspension duration. Network and processor layers interpret these to decide whether to retry, suspend or report an engine as unavailable.

**C# source:**
- [ProviderOutcome.cs](../src/DotNetWebSearchApi/Search/ProviderOutcome.cs)
- [SearchExecutionResult.cs](../src/DotNetWebSearchApi/Search/SearchExecutionResult.cs)
- [SearchAggregator.cs](../src/DotNetWebSearchApi/Search/SearchAggregator.cs)
- [SearchAggregatorTests.cs](../tests/DotNetWebSearchApi.Tests/SearchAggregatorTests.cs)

**Why the C# code has this shape**

- A generic exception string is insufficient to drive health policy. C# defines `ProviderFailureKind` values for timeout, rate limit, transient HTTP, invalid response, authentication, access denied, challenge, configuration and unknown failure.
- `ProviderFailureException` lets an adapter report a provider-specific condition and optional `RetryAfter` without coupling the aggregator to Mojeek, Qwant or another engine's exception type.
- `ProviderOutcome` records success or failure independently of result count. An empty successful response is not the same as a failed request.
- `SearchExecutionResult.ProviderOutcomes` exposes the typed outcome for each provider that was actually executed. Unsupported providers are still reported separately and do not get an execution outcome.
- Caller cancellation is rethrown and does not become an engine failure. Cancellation from the provider timeout token is classified as a provider timeout.
- The initial classifier maps HTTP 401/403/429/408/425/5xx to authentication/access-denied/rate-limit/transient categories, and JSON/XML/format/invalid-operation parsing failures to invalid response.

**Current limits**

- This is the first classification layer; the initial provider-health registry is now implemented separately. It is not yet a configurable per-provider retry policy.
- A plain `HttpRequestException` carries a status code but not response headers. A generic 429 therefore has no `Retry-After` value at this layer. Shared transport should translate the actual response into a typed failure while it still has access to response headers.
- Provider-specific challenge detection must throw `ProviderFailureException(Challenge, ...)`; generic 403 is classified as access denied, not automatically assumed to be CAPTCHA.
- The current diagnostics panel still stores the exception type string. The typed outcome is available to the execution result, but diagnostic persistence/UI should move to failure-kind and retry-after fields in a later slice.
- Fixture tests cover the classification mapping; no provider is newly claimed to emit all these typed outcomes until its adapter is wired to do so.

**Verification:** [SearchAggregatorTests.cs](../tests/DotNetWebSearchApi.Tests/SearchAggregatorTests.cs) covers HTTP 401/403/429/503, malformed JSON, explicit challenge with retry duration, successful empty responses and provider timeouts. Caller cancellation remains separately tested.

**Interpretation:** this is the C# equivalent of SearXNG's typed engine exceptions and suspension hints, expressed as stable provider-neutral outcomes so the aggregator can implement health policy without knowing individual engines.



### Engine suspension, cooldown and half-open recovery

**Python source:** [SearXNG abstract engine processor](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/search/processors/abstract.py) and the typed engine exceptions described above.

**C# source:**
- [ProviderHealthRegistry.cs](../src/DotNetWebSearchApi/Search/ProviderHealthRegistry.cs)
- [SearchAggregator.cs](../src/DotNetWebSearchApi/Search/SearchAggregator.cs)
- [Program.cs](../src/DotNetWebSearchApi/Program.cs)
- [SearxngSearchResponse.cs](../src/DotNetWebSearchApi/Search/SearxngSearchResponse.cs)
- [ProviderHealthRegistryTests.cs](../tests/DotNetWebSearchApi.Tests/ProviderHealthRegistryTests.cs)
- [SearxngApiIntegrationTests.cs](../tests/DotNetWebSearchApi.Tests/SearxngApiIntegrationTests.cs)

**Why the C# code has this shape**

- SearXNG does not simply log an engine exception and try again on every query. The processor tracks failures and suspension time, then skips engines during the suspension window. The C# aggregator now has a separate `ProviderHealthRegistry` so provider lifecycle policy is not embedded inside every adapter.
- The registry uses the familiar circuit-breaker states: `Closed` (normal calls), `Open` (cooldown, calls skipped) and `HalfOpen` (one recovery probe allowed).
- A successful call resets the failure count and closes the circuit. A failed half-open probe reopens it. Concurrent searches cannot all become recovery probes at once.
- Caller cancellation releases a half-open probe without recording a provider failure. A timeout from the provider's own timeout token is a provider failure.
- Cooldown is failure-kind aware. Defaults are two consecutive transient failures before opening, 10-second base exponential cooldown, 60 seconds for rate limiting, 15 seconds for challenges, five minutes for authentication/access/configuration failures, one minute for invalid responses and a five-minute maximum. A provider-supplied RetryAfter is honoured but capped by the maximum.
- Providers skipped during cooldown are not labelled as newly failed. They appear as `Skipped` outcomes and in the additive `cooling_down_engines` / `coolingDownProviders` response fields.
- `/api/diagnostics` exposes provider health state, consecutive failures, cooldown expiry and last failure kind separately from the older request counters.

**Current limits**

- State is process-local and resets on service restart. It is not persisted to SQL or shared across multiple API instances.
- The default policy is global rather than provider-specific. Real upstream rate limits and challenge suspension durations should eventually be configurable per provider.
- A half-open probe is one request; there is no separate low-cost health-check endpoint for providers.
- The current diagnostics status can remain marked degraded until a successful half-open request proves recovery.
- The C# policy is inspired by SearXNG's engine suspension lifecycle, not a line-for-line copy of every SearXNG suspension threshold or setting.

**Verification:** [ProviderHealthRegistryTests.cs](../tests/DotNetWebSearchApi.Tests/ProviderHealthRegistryTests.cs) covers cooldown, single half-open probe, success recovery, transient failure threshold, RetryAfter cap and aggregator-level skip/recovery. [SearxngApiIntegrationTests.cs](../tests/DotNetWebSearchApi.Tests/SearxngApiIntegrationTests.cs) covers separate cooldown reporting and diagnostics exposure.

**Interpretation:** this ports the reusable engine-lifecycle concept into a provider-neutral .NET circuit breaker. The policy is deliberately isolated so it can later be tuned or persisted without changing provider adapters.



### Shared network layer and bounded HTTP transport

**Python source:** [SearXNG network layer](https://github.com/searxng/searxng/tree/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/network) and the engine request functions linked in the provider entries above.

**C# source:**
- [SearchHttpTransport.cs](../src/DotNetWebSearchApi/Search/SearchHttpTransport.cs)
- [BingSearchProvider.cs](../src/DotNetWebSearchApi/Search/BingSearchProvider.cs)
- [QwantSearchProvider.cs](../src/DotNetWebSearchApi/Search/QwantSearchProvider.cs)
- [Program.cs](../src/DotNetWebSearchApi/Program.cs)
- [SearchHttpTransportTests.cs](../tests/DotNetWebSearchApi.Tests/SearchHttpTransportTests.cs)

**Why the C# code has this shape**

- SearXNG providers build request-specific data but delegate execution to a shared network layer. C# keeps provider-owned `HttpClient` instances and handler configuration, while a shared `SearchHttpTransport` owns the common send/read boundary. This avoids forcing different cookie and redirect strategies into one global handler.
- The transport sends with `ResponseHeadersRead`, applies a bounded response body (4 MiB by default), decodes the declared response charset, and returns status, final request URI, headers and parsed Retry-After metadata.
- A per-host semaphore caps concurrent requests (four by default), so separate adapters hitting the same host share a concurrency limit when they use the singleton transport.
- `SearchHttpResponse.EnsureSuccessStatusCode` maps 401, 403, 429, 408, 425 and 5xx to typed provider failures. Qwant can inspect a 403 body first and distinguish a challenge from ordinary access denial.
- The transport is now used by Bing, Brave, DuckDuckGo, Google, Mojeek, Naver, Qwant, Seznam, Yahoo, Yandex Cloud, Wikipedia, arXiv, the generic JSON API adapter and RSS/Atom. Their parsers remain provider-specific and continue to receive text/JSON/XML rather than transport internals.
- Retry-After is captured while the response headers are available and carried in `ProviderFailureException`; the health registry can then apply the bounded cooldown.
- The response object copies headers for provider-specific protocols, but cookies and tokens must never be logged or exposed in API responses.

**Current limits**

- The main provider implementations now use the shared transport. Future adapters must use it rather than calling HttpClient directly; verify any new provider source before assuming it follows this path.
- Redirect behaviour remains configured per HttpClient handler or implemented in the provider (Yahoo's bounded manual redirect loop); the transport does not yet own a redirect policy.
- Cookie/session ownership remains provider-specific. The transport does not create, persist or share a cookie jar.
- No automatic retries are performed. This is deliberate until retry safety can be decided per request and challenge/access-denied conditions are excluded.
- The current transport does not centralise proxy selection, TLS/HTTP version policy, request-header profiles, request-body limits or per-provider minimum intervals.
- Response-size overflow and unsupported charset are classified as invalid provider responses. The default size limit and host concurrency limit are configurable through `SearchHttpTransportOptions`.

**Verification:** [SearchHttpTransportTests.cs](../tests/DotNetWebSearchApi.Tests/SearchHttpTransportTests.cs) covers response body/headers/Retry-After, typed HTTP status mapping, size limits and charset decoding. Bing and Qwant fixture tests cover their use of the transport and provider-specific status handling.

**Interpretation:** this is the first extraction of the common Python network-layer responsibility into a reusable .NET transport. It is intentionally a small foundation; provider-specific session, redirect and challenge state remain explicit.

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

1. Add named test-method links and explicit fixture/live-verification status to every remaining provider entry; the major web-engine comparisons are now covered.
2. Continue the corpus for specialist JSON providers where the Python implementation uses a dedicated engine rather than the generic `json_engine` template.
3. Compare each RSS feed adapter with its actual upstream Python engine before claiming provenance.
4. After shared transport and typed outcomes exist, revisit adapters whose current implementations hide challenge, rate-limit, cookie or session semantics.
5. Re-pin upstream and record source deltas before each substantive correction.

## Legal and attribution note

SearXNG is AGPL-3.0-or-later. This corpus records architectural and behavioural analysis and links to upstream source; it is not a licence determination. Before copying or translating substantial upstream code, review the applicable licence obligations and preserve required notices. The C# project itself is also distributed under AGPL-3.0-or-later; consult the repository's `UPSTREAM.md` and licence files.
