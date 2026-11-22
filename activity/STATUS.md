# Current handover status

Updated: 2026-11-22

## Repository

- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest verified code commit: 671143fc87d4aed76eec3cb012e88e262cb5f44b (SearXNG JSON API pending CI)
- Full project overview and roadmap: [README.md](../README.md)

## Working vertical slice

- Native ASP.NET Core 10 Web API.
- GET /health and GET /api/search?q=...
- Search query/result/provider contracts.
- Parallel provider aggregation with per-provider timeout and provider-failure isolation.
- Caller cancellation is propagated.
- HTTP(S)-only result URL validation, duplicate removal, deterministic score/title ordering, and result limit.
- Ten adapters: Bing Web HTML, Brave Search API (optional key), Google Web HTML, DuckDuckGo Web HTML (first page), Qwant JSON API, Yahoo Web HTML, Yandex Search API (optional paid key), Naver Web HTML, Seznam Web HTML, and Wikipedia MediaWiki search API.
- Brave is registered only when BRAVE_SEARCH_API_KEY is configured.
- Yandex is registered only when both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID are configured.
- xUnit test project and GitHub Actions .NET 10 CI.

## Verification

- CI run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45009221825
- Commit tested: 671143fc87d4aed76eec3cb012e88e262cb5f44b
- Restore: passed.
- Build: passed.
- Tests: 76 passed, 0 failed, 0 skipped.
- Local .NET SDK is not installed in the orchestration environment; verification was performed by GitHub Actions.
- Tests use deterministic fixtures and fake HTTP handlers. They verify parsing/request behaviour, not live acceptance by upstream providers.

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

Target: first 5–10 genuinely implemented and tested providers. **Initial target met: ten adapters.** Additional providers should be selected for usefulness and sustainable access, not count alone.

## Agreed delivery sequence

1. Provider foundation and major search engines — complete.
2. First 5–10 provider adapters — initial ten-provider target complete.
3. SearXNG-compatible JSON API — next priority.
4. Browser search interface, administration and diagnostics — after the compatibility API.
5. Operational hardening: SQL Server configuration where useful, API keys, quotas, rate limiting, metrics and deployment guidance.

Do not divert into the admin UI before the compatibility API and core search surface are useful.

## Important limitations

Ten providers are wired in and fixture-tested. Live upstream acceptance is not verified. Brave requires a key and is not registered when BRAVE_SEARCH_API_KEY is absent. Yandex is paid and is not registered without both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID. DuckDuckGo pagination is not implemented. Qwant's API is undocumented and may return DataDome challenges. Yahoo's cookie and redirect flow is only fixture-tested. Naver and Seznam are regional and disabled by default in current SearXNG settings. Mojeek and Startpage remain deferred due to upstream proof-of-work challenges.

There is no SQL Server provider store, general configurable HTTP provider, API-key authentication, quotas, admin UI or SearXNG-compatible JSON API yet. Do not expose this endpoint as a public unrestricted search service before authentication, rate limiting and outbound request policy are implemented.

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

1. Verify the SearXNG-compatible JSON API implementation and integration tests in GitHub Actions.
2. Expand response compatibility only where supported by real provider capabilities.
3. Build the browser search interface and provider selection.
4. Add administration and diagnostics, including provider health and configuration.
5. Design SQL Server provider configuration, secret references, API keys, quotas and rate limiting before exposing an instance to untrusted traffic.
6. Keep SearchBox.NET.Forge as a separate follow-on project for upstream monitoring and AI-assisted porting proposals; require tests and human review, never auto-merge.
