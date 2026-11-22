# Current handover status

Updated: 2026-11-22

## Repository
- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest code commit: `ab52977e1fed0fa3407651da90daea98effe045a` (request-context/capability foundation; GitHub Actions build-and-test job passed in [run 45022944719](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45022944719)). Latest repository changes are documentation commits adding the porting rationale corpus.
- Current provider catalogue: **67 adapters, frozen** pending shared-framework refactoring.
- Previous provider-catalogue baseline: build 0 warnings/errors; 156 tests passed (0 failed, 0 skipped), run [45020266512](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45020266512). The subsequent request-context/capability code has now passed its GitHub Actions build-and-test job in [run 45022944719](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45022944719); the workflow job reports success.
- Current direction and consumer boundary: [docs/ARCHITECTURE_DECISIONS.md](../docs/ARCHITECTURE_DECISIONS.md).
- Python-to-C# rationale corpus started: [docs/PYTHON_TO_CSHARP_PORTING_NOTES.md](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md). Four initial entries cover Bing, DuckDuckGo, Brave and Mojeek against pinned SearXNG source revision `c6ee3e9fd0235004a694e63720efc869bb1e3594`.
- First P0 code slice added `SearchQuery` request fields, `EngineCapabilities` and optional `ISearchProviderCapabilities`; `SearchAggregator` now skips unsupported providers and reports them separately. Focused test added; CI build-and-test job passed.
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
- Rationale corpus records preserved behaviour, deliberate alternatives, omissions and known divergences; it explicitly avoids claiming full Python feature parity. Its documentation commits also passed CI in runs [45023354409](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45023354409) and [45023360277](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45023360277).
- Batch 7 adds eight scholarly and open-knowledge providers with eight fixtures. CI passed: build 0 warnings/errors; 156 tests passed (0 failed, 0 skipped).
- Mojeek is implemented with bounded Altcha proof-of-work and two fixture tests; CI pending.
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
- Local .NET SDK is unavailable in the orchestration environment; GitHub Actions is the build/test verification source. The available workflow job summaries confirm success, but detailed test totals for the latest run have not been independently captured.
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
2. Expand the Python-to-C# rationale corpus across the remaining priority web engines and shared framework concepts; attach concrete C# source and fixture-test references.
2. Add immutable search request context and explicit engine capabilities with backwards-compatible defaults.
3. Add typed provider outcomes, health/cooldown and half-open recovery tests.
4. Introduce shared HTTP request/transport/session policies, retaining provider-specific strategies where necessary.
5. Add challenge classification and safe handling; distinguish rate limits, temporary blocks, browser verification, proof-of-work and interactive CAPTCHA. No automatic CAPTCHA bypass.
6. Improve duplicate merging, source-engine/position retention and weighted ranking.
7. Record locale/region traits and API compatibility conformance tests.
8. Reconcile README, status and activity log after each milestone; resume provider ports only when the architecture definition of done is met.
9. Keep CoderInBox as an independent HTTP consumer; do not move provider code into that repository.
