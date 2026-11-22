# Current handover status

Updated: 2026-11-22

## Repository
- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest code commit: `0f1a01c9956409b5b61a9efe95dc50be9e73a38a` (provider health registry, cooldown/half-open recovery, diagnostics and API reporting; build-and-test passed with 174 tests, 0 skipped in [run 45025031994](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45025031994)). Latest repository changes are documentation commits expanding the Python-to-C# health rationale and porting audit.
- Current provider catalogue: **67 adapters, frozen** pending shared-framework refactoring.
- Previous provider-catalogue baseline: build 0 warnings/errors; 156 tests passed (0 failed, 0 skipped), run [45020266512](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45020266512). The subsequent request-context/capability code has now passed its GitHub Actions build-and-test job in [run 45022944719](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45022944719); the workflow job reports success.
- Current direction and consumer boundary: [docs/ARCHITECTURE_DECISIONS.md](../docs/ARCHITECTURE_DECISIONS.md).
- Python-to-C# rationale corpus: [docs/PYTHON_TO_CSHARP_PORTING_NOTES.md](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md). It covers Bing, DuckDuckGo, Brave, Mojeek, Google, Qwant, Yahoo, Yandex, Naver, Seznam, Wikipedia, generic JSON APIs, RSS/Atom, shared request-context/capability translation and typed exception/outcome mapping against pinned SearXNG source revision `c6ee3e9fd0235004a694e63720efc869bb1e3594` where a direct upstream engine exists.
- P0 request context now flows from `/api/search` and SearXNG-shaped `/search` into `SearchQuery`: categories, page, language, safe-search and time range. Invalid values are rejected; unsupported providers are skipped and reported separately (`unsupportedProviders` / `unsupported_engines`), not marked unresponsive. Conservative legacy-provider capabilities remain general/first-page/default-locale. Integration tests added; CI passed with 160 tests in run 45024120851.
- Typed provider outcomes distinguish success, failure and skipped; failure kinds include timeout, rate limit, transient HTTP, invalid response, authentication, access denied, challenge, configuration and unknown. Explicit provider failures can carry RetryAfter. Caller cancellation still propagates.
- ProviderHealthRegistry now adds process-local Closed/Open/HalfOpen states, failure thresholds, failure-kind cooldowns, capped exponential backoff and one half-open recovery probe. Cooling-down providers are skipped and reported separately; diagnostics expose health snapshots. CI passed with 174 tests in run 45025031994. Policy is global and resets on process restart.
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
1. Keep provider expansion frozen at 67; do not add adapters during the shared-framework milestone.
2. Complete request-context API parsing and map the current C# providers' actual capabilities; do not advertise upstream Python features that the C# request builder does not yet implement.
3. Add typed provider outcomes, health/cooldown and half-open recovery tests.
4. Introduce shared HTTP request/transport/session policies, retaining provider-specific strategies where necessary.
5. Add challenge classification and safe handling; distinguish rate limits, temporary blocks, browser verification, proof-of-work and interactive CAPTCHA. No automatic CAPTCHA bypass.
6. Improve duplicate merging, source-engine/position retention and weighted ranking.
7. Record locale/region traits and API compatibility conformance tests.
8. Reconcile README, status and activity log after each milestone; resume provider ports only when the architecture definition of done is met.
9. Keep CoderInBox as an independent HTTP consumer; do not move provider code into that repository.
