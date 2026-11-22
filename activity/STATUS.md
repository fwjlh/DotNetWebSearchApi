# Current handover status

Updated: 2026-11-22

## Repository

- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest verified code commit: 39c8987e4ffb0e7e0177ee33b991d145b2c0d1f4 (static asset path correction pending CI)
- Full project overview and roadmap: [README.md](../README.md)

## Working vertical slice

- Native ASP.NET Core 10 Web API.
- GET /health and GET /api/search?q=...
- Initial SearXNG-shaped JSON API: GET /search, GET / and form POST /search.
- Browser search page at the root URL with provider selection and result cards.
- Read-only diagnostics page at /admin.
- GET /api/providers lists active providers.
- GET /api/diagnostics returns in-memory provider health counters.
- Ten search adapters: Bing, Brave, Google, DuckDuckGo, Qwant, Yahoo, Yandex, Naver, Seznam and Wikipedia.
- Parallel provider aggregation with per-provider timeout and provider-failure isolation.
- Detailed search execution reports failed providers as unresponsive_engines.
- HTTP(S)-only result URL validation, duplicate removal, deterministic score/title ordering and result limit.
- Brave is registered only when BRAVE_SEARCH_API_KEY is configured.
- Yandex is registered only when both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID are configured.
- xUnit tests and GitHub Actions .NET 10 CI.

## Verification

- Previous JSON API CI run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45009659712
- Previous verified result: 86 passed, 0 failed, 0 skipped.
- Current browser/admin increment: pending CI.
- Local .NET SDK is not installed in the orchestration environment; verification is performed by GitHub Actions.
- Provider tests use deterministic fixtures and fake HTTP handlers. They verify parsing/request behaviour, not live acceptance by upstream providers.

## Provider status

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

## SearXNG-compatible JSON API

Initial response-compatible surface is implemented:

- GET /search?q=...&format=json.
- GET /?q=...&format=json when a query is present; bare / redirects to the search page.
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

Ten providers are wired in and fixture-tested. Live upstream acceptance is not verified. Brave requires a key and is not registered when BRAVE_SEARCH_API_KEY is absent. Yandex is paid and is not registered without both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID. DuckDuckGo pagination is not implemented. Qwant's API is undocumented and may return DataDome challenges. Yahoo's cookie and redirect flow is only fixture-tested. Naver and Seznam are regional and disabled by default in current SearXNG settings. Mojeek and Startpage remain deferred due to upstream proof-of-work challenges.

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

1. Verify the browser/admin increment in GitHub Actions.
2. Add safe provider configuration management; decide SQL Server schema and secret-reference model before writing configuration UI.
3. Add API-key authentication, quotas and rate limiting before any untrusted public deployment.
4. Expand diagnostics with health-check timing and optional persistence if useful.
5. Continue browser UI polish and operational guidance.
6. Keep SearchBox.NET.Forge as a separate follow-on project for upstream monitoring and AI-assisted porting proposals; require tests and human review, never auto-merge.
