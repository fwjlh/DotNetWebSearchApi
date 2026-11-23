# Current handover status

Updated: 2026-11-23

## Repository
- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest code/test commit: `16549750308dcdf759c32dd077d90fa35c88fe0d` (Yandex Cloud capability declaration and pre-flight tests; build-and-test passed in [run 45144264756](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144264756)). Latest porting-corpus update: `2577287a5860ec87cac396ea89ae7cb0f7d65b95` (Yandex capability boundaries); activity log update: `1c41a7619bb593d58c375b3ccbd5de11d5bb8a0b`.
- Current provider catalogue: **67 adapters, frozen** pending shared-framework refactoring.
- Previous provider-catalogue baseline: build 0 warnings/errors; 156 tests passed (0 failed, 0 skipped), run [45020266512](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45020266512). The subsequent request-context/capability code has now passed its GitHub Actions build-and-test job in [run 45022944719](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45022944719); the workflow job reports success.
- Current direction and consumer boundary: [docs/ARCHITECTURE_DECISIONS.md](../docs/ARCHITECTURE_DECISIONS.md).
- Python-to-C# rationale corpus: [docs/PYTHON_TO_CSHARP_PORTING_NOTES.md](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md), with focused shared-framework notes in [docs/PORTING](../docs/PORTING). It now covers Bing, DuckDuckGo, Brave, Mojeek, Google, Qwant, Yahoo, Yandex, Naver, Seznam, Wikipedia, generic JSON APIs and RSS/Atom, plus shared request-context/capability translation and typed exception/outcome mapping against pinned SearXNG source revision `c6ee3e9fd0235004a694e63720efc869bb1e3594` where a direct upstream engine exists.
- Naver general-web results now preserve non-circular thumbnail URLs in `SearchResult.ThumbnailUrl` and SearXNG `thumbnail`; non-HTTP thumbnail values are rejected. Model/parser tests passed in [run 45143993872](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45143993872), and response mapping passed in [run 45144068228](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144068228). No live Naver requests.
- Yandex Cloud explicitly declares general/web, first-page-only capability with no request-level locale, safe-search or time-range support; unsupported options are pre-flight rejected. Build/test passed in [run 45144264756](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144264756). This remains a distinct authenticated API integration, not a port of SearXNG's public HTML engine.
- Added a capturing-provider integration test proving `/search` passes categories, page, language, safe-search and time range intact into `SearchQuery`; CI passed in [run 45144835861](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144835861).
- RSS provider now has Atom feed parsing coverage and an external-DTD rejection test; CI passed in [run 45145077265](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145077265). No live feed requests.
- Caller cancellation now has a health-regression assertion: it propagates without incrementing provider failures or opening cooldown. CI passed in [run 45145286054](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145286054).
- `docs/PORTING_AUDIT.md` was reconciled with the implemented architecture; it now separates completed foundations from process-local/session limitations, rich-result gaps and pre-public-exposure security work. CI passed in [run 45145666616](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145666616).
- P0 request context now flows from `/api/search` and SearXNG-shaped `/search` into `SearchQuery`: categories, page, language, safe-search and time range. Invalid values are rejected; unsupported providers are skipped and reported separately (`unsupportedProviders` / `unsupported_engines`), not marked unresponsive. Conservative legacy-provider capabilities remain general/first-page/default-locale. Integration tests added; CI passed with 160 tests in run 45024120851.
- Typed provider outcomes distinguish success, failure and skipped; failure kinds include timeout, rate limit, transient HTTP, invalid response, authentication, access denied, challenge, configuration and unknown. Explicit provider failures can carry RetryAfter. Caller cancellation still propagates.
- Challenge subtypes distinguish temporary block, browser verification, proof-of-work, interactive CAPTCHA, interactive verification, login-required and unknown. Current detectors map Google/DuckDuckGo/Yandex CAPTCHA, Qwant DataDome, Seznam verification and Mojeek Altcha/fallback CAPTCHA. `/api/search` and `/search` expose typed outcomes; health snapshots retain the last challenge subtype. CI passed in [run 45028609771](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45028609771). This does not automate CAPTCHA solving or bypass.
- `EngineLocaleRegistry` now provides static, versioned Bing and DuckDuckGo region maps. Bing maps locale to `setlang`/`cc`, preserves `zh-HK` → `en-HK`, maps safe-search 0/1/2, and declares only general/web, locale and safe-search support. DuckDuckGo maps locale to `kl` and time-range to `df`, including matching request cookies; it declares locale/time-range but not paging/safe-search. Both remain first-page-only. CI passed for Bing in [run 45028984202](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45028984202) and DuckDuckGo in [run 45029215535](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45029215535).
- ProviderHealthRegistry now adds process-local Closed/Open/HalfOpen states, failure thresholds, failure-kind cooldowns, capped exponential backoff and one half-open recovery probe. Cooling-down providers are skipped and reported separately; diagnostics expose health snapshots. CI passed with 174 tests in run 45025031994. Policy is global and resets on process restart.
- Shared HTTP transport now covers Bing, Brave, DuckDuckGo, Google, Mojeek, Naver, Qwant, Seznam, Yahoo, Yandex Cloud, Wikipedia, arXiv, generic JSON and RSS/Atom paths. It enforces a 4 MiB response limit, per-host concurrency, response-header capture and Retry-After. CI passed with 182 tests in run 45026001326.
- Mojeek and Yahoo now have separate keyed CookieContainers; Seznam retains its own typed-client handler jar. These are provider-process sessions, not per-user sessions. Added DI/integration coverage and Qwant DataDome challenge classification. CI passed with 184 tests in run 45026298077.
- Generic JSON provider minimum intervals have moved from JsonApiSearchProvider's local semaphore/clock into SearchHttpTransport's provider-keyed rate gate. CI passed with 185 tests in run 45026506065.
- Shared transport now adds fallback User-Agent and Accept headers only when neither the request nor HttpClient supplies them. Debug/warning diagnostics record method, host, status, byte count, elapsed time and exception type without URL query strings, bodies, cookies or tokens. Tests passed with 187 tests in run 45026662957.
- SearchResult now carries one-based provider position, contributing engine names and per-engine positions. SearchAggregator assigns position from provider result order, merges provenance for duplicate URLs, and SearXNG mapper emits actual positions instead of inferring them from score. Ranking now follows the SearXNG weighted-position formula; adapter score selects the primary display record. Engine weights are configurable through EngineCapabilities.Weight but all current providers still default to 1.0. CI passed with 188 tests in run 45027513125.
- Kagi was extracted from JsonApiProviderCatalog into dedicated KagiSearchProvider. It supports general search, page 1–10, locale region, safe-search and time-range filters; other verticals remain omitted. It is the first production provider to declare a non-default capability profile. Exa was retained in the generic JSON catalogue. The generic JSON and RSS factories now inject the singleton shared transport instead of constructing isolated transports. CI passed with 190 tests in run 45028349848.
- Project overview and roadmap: [README.md](../README.md)

## Working application
- Native ASP.NET Core 10 Web API; no Python or Docker runtime.
- GET /health and GET /api/search?q=...
- Initial SearXNG-shaped JSON API: GET /search, GET / and form POST /search.
- Browser search page at root; provider selection and result cards.
- Read-only diagnostics at /admin.
- GET /api/providers and GET /api/diagnostics.
- Parallel provider execution, per-provider timeout and failure isolation.
- Provider failures surfaced as unresponsive_engines.
- HTTP(S)-only result URL validation, deduplication, deterministic ordering and result limiting.
- Optional Brave and Yandex providers require their configured credentials.

## Provider count and current batch
- **67 provider adapters** are registered in the current catalogue. This count is frozen; the dated batch entries below are historical milestones, not the current provider count.
- The first 42 providers passed CI fixture tests in run [45013727838](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013727838): build passed with 0 warnings/errors; 130 tests passed.
- Batch 5 and the full 50-provider catalogue passed CI in run [45019256660](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019256660): build passed with 0 warnings/errors; 138 tests passed.
- Batch 6 and the full 58-provider source catalogue passed CI in run [45019669530](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019669530): build passed with 0 warnings/errors; 146 tests passed.
- Mojeek and the 59-provider catalogue passed CI in run [45019896375](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019896375): build passed with 0 warnings/errors; 148 tests passed.
- Batch 7 and the 67-provider catalogue passed CI in run [45020266512](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45020266512): build passed with 0 warnings/errors; 156 tests passed.
- Architecture audit added at [docs/PORTING_AUDIT.md](../docs/PORTING_AUDIT.md). No code changed in the audit.
- Rationale corpus records preserved behaviour, deliberate alternatives, omissions and known divergences; it explicitly avoids claiming full Python feature parity. The typed-outcome rationale passed CI in [run 45024568368](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024568368); the latest porting-audit reconciliation is running in [run 45024625117](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024625117).
- Batch 7 adds eight scholarly and open-knowledge providers with eight fixtures. CI passed: build 0 warnings/errors; 156 tests passed (0 failed, 0 skipped).
- Mojeek is implemented with bounded Altcha proof-of-work; fixture tests do not establish live challenge acceptance.
- Batch 6 adds eight adapters and eight fixture tests. CI passed: build 0 warnings/errors; 146 tests passed (0 failed, 0 skipped).
- Batch 5 adds Wiby, TVmaze, Radio Browser, PeerTube, Wikimedia Commons, Wikinews, Spanish Wikipedia and Openverse. All eight fixture cases passed in CI.
- No new live upstream API requests have been run. Fixture tests verify request construction and response mapping only.

## Provider inventory
Original general web adapters: Bing, Brave Search API, Google, DuckDuckGo, Wikipedia, Qwant, Yahoo, Yandex Search API, Naver and Seznam.

Specialist providers (batch 1): Crossref, OpenAlex, Hacker News, Stack Overflow, npm, crates.io, NuGet and Open Library.

Specialist providers (batch 2): Europe PMC, Semantic Scholar, GitHub repository search, Docker Hub, Google Books, Internet Archive, RubyGems and Hugging Face Models.

Developer/package providers (batch 3): Super User, Server Fault, Ask Ubuntu, GitLab Projects, Gitea, Maven Central, Packagist and pub.dev.

Media/knowledge/academic providers (batch 4): arXiv, Bilibili, Dailymotion, Deezer, Steam, MusicBrainz, Wikidata and German Wikipedia.

Search/media/regional providers (batch 5): Wiby, TVmaze, Radio Browser, PeerTube, Wikimedia Commons, Wikinews, Spanish Wikipedia and Openverse.

## Important access and quality limits
- Bing, Google, DuckDuckGo, Qwant, Yahoo, Naver and Seznam are HTML/scrape-based or undocumented paths; CAPTCHA, bot blocking, cookies, locale and egress IP can affect availability.
- Brave is optional and requires BRAVE_SEARCH_API_KEY.
- Yandex is optional and requires YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID.
- Stack Exchange quotas, GitHub unauthenticated API limits, Semantic Scholar rate limits, Radio Browser best-effort availability and Openverse anonymous limits apply.
- PeerTube currently searches the configured public instance and its local scope; it is not a global PeerTube index.
- Wiby attribution is appended to returned snippets to satisfy its requirement to link to Wiby.
- Openverse results link to the media landing page, not a direct image download.
- No adapter should be described as live-tested based only on fixture tests.

## SearXNG compatibility
- GET /search?q=...&format=json.
- GET /?q=...&format=json when query present; bare / serves browser page.
- POST /search using application/x-www-form-urlencoded.
- Engine selection via engines=provider1,provider2.
- Response includes query, number_of_results, results, answers, corrections, infoboxes, suggestions and unresponsive_engines.
- Initial compatibility only: JSON, general category, first page; no full language/safe-search/time-range support, no answer/infobox/suggestion generation, inferred positions and no merged engine positions for duplicate URLs.

## Verification history
- Latest fully verified batch before batch 5: commit 92a34b06ec137dfea1625af2d881803c90bc3045, CI run 45013727838, build 0 warnings/errors, 130 tests passed.
- Local .NET SDK is unavailable in the orchestration environment; GitHub Actions is the build/test verification source. Latest code run 45024534370 reports build success and 167 passed, 0 failed, 0 skipped. The latest porting-audit documentation run is in progress.
- Batch 5 verification is pending; do not count its eight providers as CI-verified until the workflow succeeds.

## Working rules
1. Record every material action in activity/LOG.md and update this handover.
2. Keep implementation, fixtures, README and activity updates in the same commit.
3. Use the current main commit's tree SHA as the base for every Git tree; never reuse stale tree SHAs.
4. Keep source files preferably below 15–20 KB, hard maximum 25 KB.
5. Small single-purpose functions, max four parameters, early returns, no dead code.
6. Never claim live upstream acceptance from fixture-only tests.
7. Push each increment to main and inspect the resulting GitHub Actions run before continuing.

## Next actions
1. Keep provider expansion frozen at 67; no new adapters during shared-framework completion.
2. Audit the remaining adapters for request fields silently ignored by the current implementation; add truthful capability declarations and one focused test per small change.
3. Expand locale/region traits only where the pinned Python source and C# request behaviour justify the mapping.
4. Plan a richer result model for images/news/videos so vertical-specific metadata is not flattened into ordinary web links.
5. Complete API compatibility tests, then harden the public API before internet exposure: authentication, quotas, request limits and outbound destination policy.
6. Keep CoderInBox as an independent HTTP consumer; do not move provider code into that repository.
7. Resume provider additions only when the shared-framework definition of done is met.
