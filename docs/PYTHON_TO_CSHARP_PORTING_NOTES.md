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
| `EngineTraits` locale maps | `SearchQuery.Language` plus static `EngineLocaleRegistry` (Bing subset first) | Keep provider-specific language/market codes out of individual request builders without making search depend on live trait downloads. |
| Engine flags such as `paging`, `safesearch`, `time_range_support` | `EngineCapabilities` | Reject unsupported requests before sending traffic rather than silently ignoring options. |
| `EngineCache` and provider session state | Explicit cache/session state with bounded lifetime | Make state ownership, expiry and secret handling visible. |
| Engine exceptions and suspension metadata | Typed provider outcomes and health/cooldown | Distinguish a timeout, a CAPTCHA, a rate limit and a configuration failure. |

These are translation targets, not assertions that every row is already implemented. See [PORTING_AUDIT.md](PORTING_AUDIT.md) for the shared-framework gap analysis, [challenge classification notes](PORTING/CHALLENGE_CLASSIFICATION.md) for challenge handling, and [locale traits notes](PORTING/LOCALE_TRAITS.md) for the first static Bing market map.

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
- Python maps locale and time range into form fields and cookies. C# now resolves a curated region subset through `EngineLocaleRegistry`, sends `kl` in the form and request cookie, and maps day/week/month/year to `df=d/w/m/y` in both. This is a deliberate first slice, not the full dynamic region/language map or cookie-jar lifecycle.
- Python sets browser-navigation headers, Firefox impersonation, referrer and a locale-derived Accept-Language value. C#'s typed HttpClient configuration supplies User-Agent, Accept, Accept-Language, Referer and Sec-Fetch headers, but .NET's default handler does not reproduce Firefox TLS impersonation and the Accept-Language header is not yet varied per request locale.
- Python raises a CAPTCHA-specific engine exception with suspension semantics and a zero-second suspension for this DDG CAPTCHA path. C# raises a typed `Challenge` outcome with `InteractiveCaptcha`; the shared health policy currently applies its general challenge cooldown, so exact per-engine suspension tuning remains future work.
- Python can emit a separate answer/zero-click result. The current C# `SearchResult` parser only returns ordinary web links.

**Interpretation:** the current adapter is a bounded first-page HTML search implementation with locale and time-range request mapping. It preserves the basic form and result extraction, but not the stateful/paged SearXNG workflow, VQD cache or zero-click answer result. Do not enable paging by simply adding an offset.

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
- [EngineLocaleRegistry.cs](../src/DotNetWebSearchApi/Search/EngineLocaleRegistry.cs)

**Why the C# code has this shape**

- The upstream engine explicitly uses Google's HTML web page, not the Custom Search API. It does not require an API key. C# therefore makes a GET request to www.google.com/search and parses HTML with HtmlAgilityPack.
- Python's google_request composes common query parameters for several Google verticals. The web path appends the unusual adtest parameters and Samsung user-agent marker to obtain direct HTTPS result URLs. C# preserves those web request parameters rather than replacing the route with an unrelated API.
- Both implementations identify result cards by the zMzFAb / fuLhoc classes, read the CVA68e title and FrIlee / fYyStc snippet, exclude the YVIcad sub-element and unwrap the /url?q= destination.
- The provider and parser are separate because the provider owns HTTP status/challenge handling while the parser can be tested against captured HTML without network access.
- The C# CAPTCHA check follows the same signals as Python: redirect to sorry.google.com, a /sorry path, redirect status, or a short body containing /sorry/. The result is now a typed InteractiveCaptcha outcome.

**What has now been carried across from Python**

- Google locale traits map a locale to hl (interface language), lr (document-language restriction) and, when an explicit country is present, cr=countryXX.
- SearchQuery.Page maps to start=(page-1)*10; the provider declares the upstream maximum of 50 pages.
- TimeRange maps to tbs=qdr:d/w/m/y.
- Safe-search 1/2 maps to medium/high; level 0 leaves Google's default parameter absent, matching Python's conditional request construction.
- The provider declares general/web categories, paging, time range, safe-search and locale support. Capability filtering therefore rejects unsupported verticals and out-of-range pages before making a request.

**Known divergence / omissions**

- The locale maps are a curated static subset, not Google's dynamically fetched complete language/country lists. Unknown languages fall back to lang_en; unknown regions do not invent a country.
- Python sends a CONSENT=YES+ cookie. C# does not yet send that cookie.
- Python emits suggestion result types from a separate XPath. C# only emits ordinary web links.
- Python's request helper is shared by web, image, news and video verticals. C# currently implements web only.
- The broad HTTP redirect challenge check may classify a non-CAPTCHA redirect as a challenge; keep this under review with captured response fixtures.

**Verification:** Google request tests cover locale, region, paging, time range, safe-search, challenge classification and HTML extraction. GitHub Actions [run 45030058998](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45030058998) passed: 223 tests, 0 failed, 0 skipped. No live Google requests were made.

**Interpretation:** a close HTML extraction/request-shape port for Google's web vertical, now with the main query-context fields mapped. It is not a port of image/news/video verticals or suggestion result types.


### Qwant Web

**Python source:** [searx/engines/qwant.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/qwant.py)

**C# source:**
- [QwantSearchProvider.cs](../src/DotNetWebSearchApi/Search/QwantSearchProvider.cs)
- [QwantSearchResultParser.cs](../src/DotNetWebSearchApi/Search/QwantSearchResultParser.cs)
- [QwantSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/QwantSearchProviderTests.cs)
- [EngineLocaleRegistry.cs](../src/DotNetWebSearchApi/Search/EngineLocaleRegistry.cs)

**Why the C# code has this shape**

- SearXNG uses Qwant's undocumented v3 JSON API, reverse-engineered from the browser's network requests. C# uses the same /v3/search/web endpoint and JSON response family rather than scraping the visible page.
- The web response is not a flat list. Results live below data.result.items.mainline; each row has a type and nested items. C# traverses those layers and accepts only rows typed web, so advertisements and other result types do not leak into ordinary web results.
- System.Text.Json is used because the response is JSON and the C# parser can validate object/array/string shapes explicitly.
- Python maps locale to a Qwant region tag, page to offset, and safe-search directly to the safesearch parameter. C# now maps a curated region subset, page 1–5 to offsets 0–40, and safe-search 0/1/2 directly. It declares general/web, locale, safe-search and pagination through page 5; time-range is not supported.

**Failure and session semantics**

- Python distinguishes error code 24 as rate limiting, a challenge URL as CAPTCHA (15-second suspension), HTTP 403 as access denied, and other API errors separately. C# now maps error code 24 to RateLimited, a challenge URL or DataDome/captcha marker to BrowserVerification, plain 403 to AccessDenied, and other API error objects to InvalidResponse.
- Python caches the rotating DataDome cookie. The registered C# typed HttpClient uses the handler's automatic cookie container; this is handler-scoped session state, not a separately managed 24-hour DataDome cache. Its lifetime and isolation follow HttpClientFactory handler lifetime.
- Python sends browser-like CORS/fetch headers in a defined order. C# sets Origin, Referer, Accept and Sec-Fetch headers in the typed client, but does not reproduce the exact Chrome header order or TLS fingerprint.

**Known divergence / omissions**

- The locale map is a curated static subset, not the dynamic locale list fetched from Qwant's INITIAL_PROPS.
- Python also supports news, images and videos through different response shapes and result types. C# implements web only.
- Qwant's test-group value is randomly chosen by Python; C# keeps tgp=1 for deterministic request construction.
- The C# parser returns empty results when the expected mainline structure is absent. That can hide a changed upstream response shape as an empty search rather than a parser/schema failure.

**Verification:** Qwant request tests cover locale, paging, safe-search, DataDome classification and API error code 24. GitHub Actions [run 45030233313](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45030233313) passed: 230 tests, 0 failed, 0 skipped. No live Qwant requests were made.

**Interpretation:** same undocumented JSON web endpoint, now with the main query-context fields and more faithful failure classification. It is still a web-only adapter, not a full port of Qwant's news/image/video engines or dynamic locale discovery.


### Yahoo Web

**Python source:** [searx/engines/yahoo.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/yahoo.py)

**C# source:**
- [YahooSearchProvider.cs](../src/DotNetWebSearchApi/Search/YahooSearchProvider.cs)
- [YahooSearchResultParser.cs](../src/DotNetWebSearchApi/Search/YahooSearchResultParser.cs)
- [EngineLocaleRegistry.cs](../src/DotNetWebSearchApi/Search/EngineLocaleRegistry.cs)
- [YahooSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/YahooSearchProviderTests.cs)

**Why the C# code has this shape**

- Both implementations use Yahoo's HTML web search, not an API-key service. The algo-sr result card, compTitle link, title and compText description are structural DOM data, so C# uses HtmlAgilityPack.
- Yahoo wraps result URLs in tracking paths containing /RU=... and suffix markers such as /RS and /RK. The C# parser extracts and URL-decodes the embedded destination, matching Python's parse_url intent.
- Yahoo uses an sB preference cookie. C# now maps safe-search 0/1/2 to p/i/r and maps the query language to the vl=lang_xx cookie value.
- The explicit redirect loop bounds the tracking/geo redirect chain to four hops, matching Python's _YBV_HOPS.

**Request-context translation**

- Python maps locale to a regional Yahoo host through region2domain and language through yahoo_languages. C# now uses EngineLocaleRegistry region/custom host and language maps.
- Python maps page 1 to iscqry= and later pages to b=pageno*7+1, pz=7, bct=0 and xargs=0. C# reproduces this offset shape and applies a conservative max page of 20.
- Python maps day/week/month to btf=d/w/m. C# maps the same three values and declares SupportedTimeRanges explicitly; year is rejected before the provider is called.
- Python maps safe-search 0/1/2 to p/i/r in the sB cookie. C# reproduces those values.
- The provider declares general/web, locale, safe-search and paging support. It supports only Day, Week and Month time ranges, not Year.

**YBV session lifecycle**

- Python documents YBV as a bot-detection cookie cached for 24 hours. During each manual redirect hop it captures the cookie and caches it only when the value starts with v0.2; the next request sends the cached value.
- C# now reads YBV Set-Cookie headers during each redirect response, stores the cookie in the keyed Yahoo CookieContainer with an explicit 24-hour expiry when it starts with v0.2, and relies on the same container for subsequent requests. The DI registration uses a dedicated keyed Yahoo container and a manual-redirect handler.
- This is handler/container-scoped state rather than Python's EngineCache abstraction. Its lifetime is bounded by the registered singleton container and HttpClient handler configuration; it is not shared with other providers.

**Known divergence / omissions**

- Python extracts AlsoTry suggestions. C# only returns ordinary web results.
- The regional host/language map is a curated subset of Python's complete region2domain/yahoo_languages tables.
- Python carries cookies and request headers explicitly between redirect hops. C# relies on CookieContainer for domain/path matching; custom test handlers do not automatically send cookies, so tests assert the container state.
- The maximum page 20 is a C# operational cap; Python advertises paging without this same explicit maximum.

**Verification:** Yahoo tests cover URL unwrapping, regional host, page offset, time range, safe-search/language cookie, four-hop redirect handling and 24-hour YBV cache. GitHub Actions [run 45030498602](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45030498602) passed: 234 tests, 0 failed, 0 skipped. No live Yahoo requests were made.

**Interpretation:** this is now a much closer request/session port for Yahoo web search. It still omits suggestions and uses a curated locale map and a conservative paging cap.


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
- C# caps page size at 100 but does not use `SearchQuery.Page`; the request body fixes page to `0`. The provider now declares its actual capability profile: general/web only, first page only, no request-level locale, time-range or safe-search support. The aggregator therefore rejects those non-default options before making an API call rather than silently discarding them.
- Fixture tests confirm the API-key header, folder ID, JSON body, Base64 XML decoding, XML parsing and capability pre-flight rejection for page, locale, safe-search and time-range requests. They do not establish that a configured Cloud account has permission/quota or that the public HTML route is interchangeable.

**Interpretation:** document this as a separate authenticated Yandex Cloud adapter. Do not describe it as a faithful port of SearXNG's no-key Yandex HTML engine.

### Wikipedia

**Python source:** [searx/engines/wikipedia.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/wikipedia.py)

**C# source:**
- [WikipediaSearchProvider.cs](../src/DotNetWebSearchApi/Search/WikipediaSearchProvider.cs)
- [EngineLocaleRegistry.cs](../src/DotNetWebSearchApi/Search/EngineLocaleRegistry.cs)
- [WikipediaSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/WikipediaSearchProviderTests.cs)

**Important: this is an API and result-shape substitution, not a direct endpoint port.**

- Python calls the Wikimedia REST page-summary endpoint for a title, with the language-specific wiki host selected from dynamically fetched Wikimedia traits. It can return a list result, an infobox, or both according to `display_type`; the standard summary includes an extract and optional thumbnail.
- C# instead calls the MediaWiki Action API `action=query&list=search` endpoint. This returns multiple matching pages for a search phrase, which fits the common multi-result `ISearchProvider` contract better than treating the whole query as one page title.
- Both routes are official Wikimedia APIs and need no API key, but their semantics differ: REST summary resolves a page title, while Action API search finds candidate pages. The C# result URL uses the returned page ID (`?curid=`) rather than the REST summary's canonical desktop page URL.
- C# chooses the wiki host from `EngineLocaleRegistry`, then sends the requested locale and base language in `Accept-Language`. This is important for Chinese LanguageConverter variants: the host remains `zh.wikipedia.org`, while the header communicates the desired script/region variant.
- The provider declares locale support but not paging, time-range or safe-search. Its request limit is mapped to `srlimit`; it does not claim that this is SearXNG's page-summary behaviour.

**Known divergence / omissions**

- Python's locale traits are dynamically fetched from Wikimedia's sortable list and include non-standard language aliases and script variants. C# currently uses a static, deliberately smaller mapping in `EngineLocaleRegistry`; unsupported languages fall back to English.
- Python's standard summary result can include an infobox extract and thumbnail. C# currently emits ordinary search results only and does not request page summaries, thumbnails, infoboxes or page descriptions.
- Python can treat 404 and the specific invalid-title 400 response as empty results. C# uses the Action API search response shape and does not reproduce those REST-summary status rules.
- The C# parser strips HTML tags from the Action API snippet and returns the page ID URL. It does not reproduce the full Wikimedia REST response's `titles.display`, `content_urls` or summary fields.

**Verification:** tests cover language-specific host selection, `Accept-Language` (including `zh-TW`), automatic English fallback and declared capabilities. These fixture tests verify the Action API request and response mapping, not live Wikimedia availability or full dynamic language-trait parity.

**Interpretation:** the C# implementation is a multi-result MediaWiki search adapter, while the Python implementation is a page-summary/infobox adapter. Keep this distinction explicit; do not claim the REST summary result types have been ported.

### Naver Web

**Python source:** [searx/engines/naver.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/naver.py)

**C# source:**
- [NaverSearchProvider.cs](../src/DotNetWebSearchApi/Search/NaverSearchProvider.cs)
- [NaverSearchResultParser.cs](../src/DotNetWebSearchApi/Search/NaverSearchResultParser.cs)
- [NaverSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/NaverSearchProviderTests.cs)

**Why the C# code has this shape**

- The selected upstream category is Naver general/web search, which is HTML rather than an official API. C# calls search.naver.com/search.naver with where=web and parses the current web result card family fds-web-normal-doc-root.
- The parser reads the headline span and body span, rejects Naver's keep.naver.com save links as result destinations, and only emits absolute HTTP(S) links.
- Naver cannot set an arbitrary result count per page. Python uses a fixed category-specific page size; for general web it is 15, so page N starts at (N-1)*15+1. C# now maps SearchQuery.Page using that exact offset.
- Python maps day/week/month/year to nso=p:1d/1w/1m/1y. C# now maps all four time ranges and declares pagination/time-range support, with a conservative max page of 20.
- Naver's Python engine language is Korean. C# does not claim locale support or alter the request language.

**Known divergence / omissions**

- Python supports general, images, news and videos with different parsers and result types. C# implements only general web.
- Python emits the first non-circular sds-comps-image thumbnail for general results. C# now extracts that image into the optional SearchResult.ThumbnailUrl field, rejects non-HTTP(S) thumbnail sources, and keeps circular avatar images out of the result metadata. The optional field is also exposed as `thumbnail` by the SearXNG-compatible response mapper.
- Python has no safe-search flag in this engine; C# likewise does not advertise safe-search.
- The maximum page 20 is a C# operational cap; the Python module declares paging without this same explicit cap.

**Verification:** Naver tests cover page offset, time-range parameter, capabilities, thumbnail extraction, exclusion of circular avatars and rejection of non-HTTP thumbnail URLs. GitHub Actions [run 45143993872](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45143993872) passed for the result model, parser and focused fixture tests. A separate SearXNG response integration test verifies the `thumbnail` field; its CI run is tracked in the current handover. No live Naver requests were made.

**Interpretation:** a closer general-web request port, now preserving pagination, date filtering and general-result thumbnails. It is still not a port of Naver's images/news/videos verticals or their richer vertical-specific metadata.


### Seznam Web

**Python source:** [searx/engines/seznam.py](https://github.com/searxng/searxng/blob/c6ee3e9fd0235004a694e63720efc869bb1e3594/searx/engines/seznam.py)

**C# source:**
- [SeznamSearchProvider.cs](../src/DotNetWebSearchApi/Search/SeznamSearchProvider.cs)
- [SeznamSearchResultParser.cs](../src/DotNetWebSearchApi/Search/SeznamSearchResultParser.cs)
- [SeznamSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/SeznamSearchProviderTests.cs)

**Why the C# code has this shape**

- Seznam's search request is preceded by a homepage request. Python extracts all hidden input fields from that page, includes them with q, oq and noredirect=1, and forwards the homepage response cookies into the search request.
- C# preserves the two-step flow: read the homepage, parse hidden inputs, merge them with the search query parameters, then request the result page.
- The registered typed HttpClient uses the default HttpClientHandler cookie support. Cookies received from the homepage are stored and sent on the search request by the same handler; the provider itself does not manually copy cookie strings.
- The result parser follows the same nested result container under searchpage-root, Layout--left and f2c528, then reads the title link and description block.
- Python raises an access-denied exception when the response path starts with /verify. C# now classifies that known route as Challenge + BrowserVerification, rather than parsing it as an empty result.

**Known divergence / omissions**

- Python has a fixed language value cz and no paging, time-range or safe-search support. C# likewise does not advertise locale, paging, time-range or safe-search.
- Cookie state is owned by the typed client's handler. It is isolated from other provider clients, but its lifetime follows HttpClientFactory handler lifetime rather than an explicit Seznam cache.
- The hidden-input parser copies field names/values but does not model the upstream form as a strongly typed request; a future form change could alter the fields.
- The C# tests verify the two-step request and verification-path classification with fixtures; they do not prove live cookie acceptance or successful Seznam search.

**Interpretation:** this is a structurally faithful two-step form/search port with browser-verification classification. It intentionally remains first-page-only and does not invent unsupported query features.




## Shared C# adapter families

These are C# framework decisions rather than one-to-one translations of a single SearXNG Python engine. Many upstream providers have their own Python modules; the C# project consolidates repeated HTTP and parsing mechanics where providers expose a sufficiently similar contract.

### Generic JSON API providers

**C# source:**
- [JsonApiSearchProvider.cs](../src/DotNetWebSearchApi/Search/JsonApiSearchProvider.cs)
- [JsonApiProviderCatalog.cs](../src/DotNetWebSearchApi/Search/JsonApiProviderCatalog.cs)
- [JsonApiSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/JsonApiSearchProviderTests.cs)
- [SearchHttpTransport.cs](../src/DotNetWebSearchApi/Search/SearchHttpTransport.cs)

**Why the C# code has this shape**

- Rather than create a nearly identical provider class for every straightforward JSON API, provider-specific differences are stored in `JsonApiSearchDefinition`: endpoint, HTTP method, query and limit fields, fixed parameters, result/title/URL/snippet paths, URL prefix, attribution and minimum request interval.
- `JsonApiSearchProvider` owns the shared mechanics: build GET query parameters or a JSON POST body, send through `SearchHttpTransport`, navigate configured JSON paths, validate result destinations as HTTP(S), clean snippets and map into `SearchResult`.
- The JSON path reader is deliberately small and explicit. It supports object properties and array indices; it is not a general JSONPath engine. This bounds the configuration surface and avoids a query-language dependency for simple provider response shapes.
- Relative identifiers are combined with a configured `UrlPrefix` for APIs such as crates.io, Open Library, Europe PMC, Docker Hub and Internet Archive. Wiby attribution is appended to the snippet to meet its attribution requirement.
- Per-provider minimum intervals are delegated to the shared transport's provider-keyed rate gate rather than implemented as a semaphore and clock inside each provider.
- Openverse is an explicit image-only definition. It keeps the foreign landing page as the result URL while mapping the direct image URL, thumbnail, dimensions, file type, source, byte size, licence, creator, provider and attribution into typed image metadata. Image, thumbnail, licence and creator URLs are accepted only when absolute HTTP(S); malformed file sizes are ignored. This mapping is fixture-tested and does not imply live Openverse availability.
- Dailymotion is an explicit video-only definition. Its selected API fields include `embed_url`, `thumbnail_360_url`, `duration` and `uploaded_time`, which are mapped into typed video metadata and the SearXNG `iframe_src`, `length`, `thumbnail` and `publishedDate` fields. Embed URLs must be HTTPS and use the same origin or an explicitly allowed provider host; thumbnail URLs must be absolute HTTP(S). Dailymotion documents that a functional `embed_url` requires a Player ID context; this repository does not yet configure a Player ID, so the adapter only passes through a safe URL if one is returned and does not claim live embed playback. See [Dailymotion video fields](https://developers.dailymotion.com/v0/reference/video-fields) and [embed URL migration guidance](https://developers.dailymotion.com/v0/reference/migration-guide-new-embed-endpoint).
- Bilibili's video search results now map `pic`, `author`, `play` and `pubdate` into typed video metadata. The request sends the documented Referer and omits the unsupported `page_size` parameter; the provider returns its default 20-item page. The adapter deliberately does not guess at the provider's string duration format or construct an embed URL. Newer documentation describes WBI signing, so live availability is not claimed. See [Bilibili search response fields](https://github.com/yi-zhu/bilibili-API/blob/master/docs/search/search_response.md) and [current search request notes](https://github.com/Chilfish/bilibili-API/blob/master/docs/search/search_request.md).
- PeerTube maps `embedPath`, the first current `thumbnails[].fileUrl`, `duration`, `publishedAt`, `account.displayName` and `views`. A relative embed path is resolved only against the result's own origin; protocol-relative cross-origin paths are rejected. The older `thumbnailPath` field is deprecated in PeerTube 8.1, so the adapter prefers the current `thumbnails` collection. See [PeerTube video response fields](https://docs.joinpeertube.org/api-rest-reference/api/operations/getvideos.html).
- Wikimedia Commons file-namespace search remains in `general`, not `images`: the current adapter returns a file-page link and snippet but does not retrieve the direct image URL or dimensions. It must not be advertised as an image result until a follow-up `imageinfo`/page-image mapping is implemented.
- Deezer maps its 30-second preview URL, album cover, artist, album and duration into typed music metadata. Radio Browser maps the resolved station stream, favicon, country, tags, codec and bitrate. Audio and thumbnail URLs are restricted to absolute HTTP(S). MusicBrainz remains a metadata-only link result; it does not invent an audio preview. Music results keep the default SearXNG template while exposing `audio_src` and the typed `music_metadata` extension.
- Wikinews now requests MediaWiki search timestamps and maps the returned ISO timestamp into `SearchResult.PublishedDate` and SearXNG `publishedDate`. Missing or malformed timestamps remain unset; no publication date is inferred from the crawl time.
- Kagi was moved to a dedicated provider because its API has a more involved authentication/request contract and meaningful page, locale, safe-search and time-range capabilities. Exa remains a generic JSON definition because its current request/result shape fits the shared definition model.

**Known limits**

- These definitions currently represent a common general-result contract. They do not automatically inherit paging, locale, category, safe-search or time-range support. Providers without an explicit `ISearchProviderCapabilities` declaration receive the conservative BasicWeb profile.
- The generic parser requires configured title and URL paths; optional snippets can be empty. Rich fields such as authors, publication dates, thumbnails, image dimensions, licence data and provider-specific facets are not represented by the current generic result mapping.
- Different APIs have different quotas, authentication rules, terms and response semantics. A shared parser does not make their operational policies interchangeable; per-provider intervals and credentials remain explicit configuration.
- The fixture suite exercises representative definitions and request/response mapping. It does not establish live acceptance, current quotas or account entitlement for every configured API.

**Interpretation:** this is a deliberate C# abstraction over repeated API plumbing, not a literal port of a Python base class. Keep provider-specific request rules and result semantics in definitions or dedicated adapters; do not force complex APIs into the generic model.

### RSS and Atom providers

**C# source:**
- [RssSearchProvider.cs](../src/DotNetWebSearchApi/Search/RssSearchProvider.cs)
- [RssSearchProviderTests.cs](../tests/DotNetWebSearchApi.Tests/RssSearchProviderTests.cs)
- [SearchHttpTransport.cs](../src/DotNetWebSearchApi/Search/SearchHttpTransport.cs)

**Why the C# code has this shape**

- Google News and Bing News are queried through their RSS endpoints. The C# provider parses RSS 2.0 `channel/item` and Atom `feed/entry` structures into the common result contract.
- XML parsing uses `XmlReader` with DTD processing prohibited and the resolver disabled. This is a deliberate security boundary for remote XML feeds, not just a parsing preference.
- Provider-specific locale/market values are fixed in each `RssSearchDefinition`; the generic implementation adds the query, decodes feed text and validates destination links.
- Bing News wraps publisher destinations in an `apiclick.aspx` URL. The provider extracts the embedded `url` destination so consumers do not receive the Bing tracking wrapper when a target is available.

**Known limits**

- The current catalogue contains Google News and Bing News only. This is not a general RSS crawler or feed-discovery service.
- Query locale/market values are fixed to en-GB in the current definitions. The provider does not yet map `SearchQuery.Language`, paging or time range.
- The limit is applied after parsing valid entries, with an operational maximum of 50; the feed request itself does not necessarily constrain upstream result count.
- RSS 2.0 `pubDate` and Atom `published` / `updated` values are parsed with invariant date handling into `SearchResult.PublishedDate`; malformed or absent dates remain unset. The SearXNG mapper emits this value as `publishedDate` and preserves the `news` category.
- Tests cover Google News RSS mapping, Atom feed parsing, Bing publisher-URL resolution and rejection of an external DTD. GitHub Actions [run 45145077265](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145077265) passed the build-and-test job. They do not establish live feed availability or guarantee that every publisher redirect format is decoded.

**Interpretation:** RSS/Atom is a separate protocol adapter family chosen because these news endpoints return feed documents rather than HTML search pages or JSON APIs. Secure XML settings and redirect normalisation belong in the shared implementation; market-specific request settings remain per definition.

## Standard for future porting entries

Every new entry must contain the following evidence chain:

1. **Pinned Python source:** exact upstream file URL and commit. Do not cite a moving branch as the specification.
2. **C# source:** provider, parser, shared components and focused tests.
3. **Original behaviour:** request method/URL/body, query parameters, locale traits, cookies/session state, pagination, result types, parser selectors and failure/challenge handling as relevant.
4. **Translation rationale:** why the C# implementation uses its current classes, APIs and abstractions. State whether the change is a direct translation, a simplification or an alternative protocol.
5. **Preserved / adapted / omitted:** list observable behaviour in each category. Call out known divergence rather than describing it as complete parity.
6. **Verification:** named fixture/test coverage, CI run when available, and explicit live-test status. A passing fixture test is not evidence that an upstream site accepts the request.
7. **Follow-up:** concrete work required to close material gaps or an explicit decision to leave the divergence.

When no matching Python engine exists (for example, a new C#-only API or RSS adapter family), say so. Do not invent a Python origin story for a C# design.

Keep the shared framework rationale in the relevant family entry or the focused [porting notes](PORTING/). Provider entries should explain why provider-specific rules remain local and which mechanics belong in shared transport, capability, locale, result or health abstractions.
