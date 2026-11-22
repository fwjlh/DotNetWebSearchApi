# Current handover status

Updated: 2026-11-22

## Repository

- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest commit: 6255aa7c36e52f9c973da8528e8b734bb5157905 (provider batch compile correction; two test failures remain, correction in progress)
- Full project overview and roadmap: [README.md](../README.md)

## Working vertical slice

- Native ASP.NET Core 10 Web API.
- GET /health and GET /api/search?q=...
- Initial SearXNG-shaped JSON API: GET /search, GET / and form POST /search.
- Browser search page at the root URL with provider selection and result cards.
- Read-only diagnostics page at /admin.
- GET /api/providers lists active providers.
- GET /api/diagnostics returns in-memory provider health counters.
- Eighteen search adapters: the ten original providers plus Crossref, OpenAlex, Hacker News, Stack Overflow, npm, crates.io, NuGet and Open Library.
- Parallel provider aggregation with per-provider timeout and provider-failure isolation.
- Detailed search execution reports failed providers as unresponsive_engines.
- HTTP(S)-only result URL validation, duplicate removal, deterministic score/title ordering and result limit.
- Brave is registered only when BRAVE_SEARCH_API_KEY is configured.
- Yandex is registered only when both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID are configured.
- xUnit tests and GitHub Actions .NET 10 CI.

## Verification

- CI run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45010292934
- Commit tested: 3a4970a3d58a5e0ddac89676539bd605d662bbd7
- Restore: passed.
- Build: passed.
- Tests: 95 passed, 0 failed, 0 skipped (latest verified baseline before this increment). The first specialist-provider build failed before test execution; see activity log.
- Previous failed run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45010196457 (93 passed, two static file path failures); corrected in the verified commit.
- Local .NET SDK is not installed in the orchestration environment; verification is performed by GitHub Actions.
- Provider tests use deterministic fixtures and fake HTTP handlers. They verify parsing/request behaviour, not live acceptance by upstream providers.

## Provider status

The eight specialist JSON API adapters in the current increment are implemented but must not be treated as CI-verified until the new workflow run completes. All fixture tests use fake HTTP handlers; they do not establish live upstream acceptance.

1. Bing Web — implemented and CI-verified; live upstream availability not tested.
2. Brave Search API — implemented and CI-verified; optional, requires BRAVE_SEARCH_API_KEY.
3. Google Web — implemented and CI-verified via HTML; CAPTCHA/blocking risk.
4. DuckDuckGo Web — implemented and CI-verified for first page; CAPTCHA/bot-blocking risk, no pagination/VQD.
5. Wikipedia — implemented and CI-verified; reference JSON/API adapter.
6. Qwant — implemented and CI-verified experimentally; undocumented API and DataDome access risk.
7. Yahoo Web — implemented and CI-verified experimentally; HTML, sB cookie and bounded YBV redirect handling; live access unverified.
8. Yandex Search API — implemented and CI-verified; optional paid provider, requires API key and folder ID.
9. Naver Web — implemented and CI-verified; Korean regional first-page HTML adapter; disabled by default upstream.
10. Seznam Web — implemented and CI-verified; Czech regional HTML adapter with homepage bootstrap; disabled by default upstream.

## Specialist provider adapters

The JSON API provider catalog currently includes Crossref, OpenAlex, Hacker News, Stack Overflow, npm, crates.io, NuGet and Open Library. Each definition supplies its real upstream endpoint, query/limit parameters and response paths. The common adapter builds encoded requests, maps provider-specific JSON paths and rejects non-HTTP(S) result URLs. Stack Overflow uses the public Stack Exchange API and remains subject to its published quota rules. Live upstream access has not been tested.

## SearXNG-compatible JSON API

Initial response-compatible surface is implemented:

- GET /search?q=...&format=json.
- GET /?q=...&format=json when a query is present; bare / serves the search page.
- POST /search using application/x-www-form-urlencoded.
- Engine selection through engines=bing,google.
- Response fields include query, number_of_results, results, answers, corrections, infoboxes, suggestions and unresponsive_engines.
- Result fields include url, title, content, engine, engines, positions, score, category, template, parsed_url and publishedDate.
- Failed providers are listed in unresponsive_engines.

Known compatibility limits: JSON only; general category; first page only; no time_range, non-default language or safe-search overrides; no answer/infobox/suggestion generation; positions are inferred from current provider scores; duplicate URLs retain one highest-scoring provider rather than merging engine positions. This is not full SearXNG feature parity.

## Browser and diagnostics

- Root URL serves the search page; /admin serves the read-only diagnostics page.
- Provider list is discovered from /api/providers.
- Search UI allows selection of active providers and displays result title, snippet, provider and score.
- /api/diagnostics reports status, active providers, success/failure counts, last duration, last result count, last checked time and last exception type.
- Counters are in-memory and reset on process restart.
- No provider editing or secret management is implemented in the UI.
- Upstream result text is inserted with textContent; result links are restricted to HTTP(S).

## Important limitations

Eighteen providers are wired in after this increment; the eight new JSON API adapters have fixture tests. CI verification: build now passes, but 2 of 105 tests failed; correction is in progress. Live upstream acceptance is not verified. Brave requires a key and is not registered when BRAVE_SEARCH_API_KEY is absent. Yandex is paid and is not registered without both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID. DuckDuckGo pagination is not implemented. Qwant's API is undocumented and may return DataDome challenges. Yahoo's cookie and redirect flow is only fixture-tested. Naver and Seznam are regional and disabled by default in current SearXNG settings. Mojeek and Startpage remain deferred due to upstream proof-of-work challenges.

There is no SQL Server provider store, general configurable HTTP provider, API-key authentication, quotas or editable admin configuration yet. Do not expose this endpoint as a public unrestricted search service before authentication, rate limiting and outbound request policy are implemented.

## Working rules

1. Record every material action in activity/LOG.md, including reason, outcome, verification, commit and next step.
2. Update this file at each handover.
3. Implementation code and its tests/log update go in the same commit.
4. Push each completed increment to main and verify the ref.
5. Source files preferably below 15–20 KB; hard maximum 25 KB.
6. Maximum two nesting levels; extract nested blocks over five lines into named functions.
7. Small single-purpose functions, explicit public types, max four parameters, early returns, nullable enabled, no dead code.
8. Never describe authored tests as run, or tests as passing, without actual CI evidence.

## Next actions

1. Continue provider ports from 18 towards 100, prioritising high-value general search, specialist search, developer, academic, news, image, video and regional sources. Count only implemented, registered and fixture-tested adapters.
2. Keep provider definitions explicit and review authentication, quotas, rate limits, terms and bot protection for each upstream.
3. After the 100-provider milestone, pull and build locally and perform live/integration testing before exposing the service publicly.
4. Then add SQL Server configuration, API-key authentication, quotas and safe admin editing.
5. Continue UI refinement and deployment guidance for Kestrel/IIS.
6. Keep SearchBox.NET.Forge as a separate follow-on project for upstream monitoring and AI-assisted porting proposals; require tests and human review, never auto-merge.


## Latest CI failure and correction

- Run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45012798957
- Commit: `f30ba0eca15e07370e47951602ce6d9bce82bdbe`
- Restore passed; build failed because the shared adapter referenced a missing JSON-path helper and lacked the `System.Net` import for HTML decoding. No tests executed.
- Correction: add the JSON-path traversal helper and required import; also make the fixed-parameter dictionary copy compatible with `IReadOnlyDictionary`.
- The corrected build is not yet verified.


- Second CI run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45012899977. Build passed with 0 warnings/errors; 103 tests passed and 2 failed. One test expected the NuGet `$top` parameter before URL encoding; the assertion now decodes the query. The other exposed relative Open Library paths being interpreted as absolute file URIs; URL handling now accepts only absolute HTTP(S) URLs before applying the configured prefix.
