# Current handover status

Updated: 2026-11-22

## Repository

- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest verified code commit: 48413e42f3f37efd24417d14e19b07d5ec083c2d
- Full project overview and roadmap: [README.md](../README.md)

## Working vertical slice

- Native ASP.NET Core 10 Web API.
- GET /health and GET /api/search?q=...
- Search query/result/provider contracts.
- Parallel provider aggregation with per-provider timeout and provider-failure isolation.
- Caller cancellation is propagated.
- HTTP(S)-only result URL validation, duplicate removal, deterministic score/title ordering, and result limit.
- Eight adapters: Bing Web HTML, Brave Search API (optional key), Google Web HTML, DuckDuckGo Web HTML (first page), Qwant JSON API, Yahoo Web HTML, Yandex Search API (optional paid key), and Wikipedia MediaWiki search API.
- Brave is registered only when BRAVE_SEARCH_API_KEY is configured.
- Yandex is registered only when both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID are configured.
- xUnit test project and GitHub Actions .NET 10 CI.

## Verification

- CI run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008975175
- Commit tested: 48413e42f3f37efd24417d14e19b07d5ec083c2d
- Restore: passed.
- Build: passed.
- Tests: 64 passed, 0 failed, 0 skipped.
- Previous failed build and correction: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008940032
- Local .NET SDK is not installed in the orchestration environment; verification was performed by GitHub Actions.
- Tests use deterministic fixtures and fake HTTP handlers. They verify parsing/request behaviour, not live acceptance by upstream providers.

## Provider implementation priorities

The agreed approach is major general-purpose search engines first, then work down the list:

1. Bing Web — implemented and CI-verified; live upstream availability not tested.
2. Brave Search API — implemented and CI-verified; optional, requires BRAVE_SEARCH_API_KEY.
3. Google Web — implemented and CI-verified via HTML; CAPTCHA/blocking risk.
4. DuckDuckGo Web — implemented and CI-verified for first page; CAPTCHA/bot-blocking risk, no pagination/VQD.
5. Wikipedia — implemented and CI-verified; reference JSON/API adapter.
6. Qwant — implemented and CI-verified experimentally; undocumented API and DataDome access risk.
7. Yahoo Web — implemented and CI-verified experimentally; HTML, sB cookie and bounded YBV redirect handling; live access unverified.
8. Yandex Search API — implemented and CI-verified; optional paid provider, requires API key and folder ID.
9. Mojeek — deferred; current SearXNG settings mark it inactive due to proof-of-work CAPTCHA.
10. Startpage — deferred; current SearXNG settings mark it inactive due to Anubis proof-of-work.

Target: first 5–10 genuinely implemented and tested providers. A provider is not counted as ported merely because its name appears in configuration.

## Agreed delivery sequence

1. Provider foundation and major search engines.
2. First 5–10 provider adapters.
3. SearXNG-compatible JSON API.
4. Browser search interface, administration and diagnostics.
5. Operational hardening: SQL Server configuration where useful, API keys, quotas, rate limiting, metrics and deployment guidance.

Do not divert into the admin UI or compatibility layer before the core provider coverage is useful, except where a small architectural change is necessary to support adapters safely.

## Important limitations

Bing Web, Brave Search API, Google Web, DuckDuckGo Web (first page), Qwant, Yahoo, Yandex and Wikipedia are wired in. Fixture-based CI tests pass; live upstream acceptance is not verified. Brave requires a key and is not registered when BRAVE_SEARCH_API_KEY is absent. Yandex is paid and is not registered without both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID. DuckDuckGo pagination is not implemented. Qwant's API is undocumented and may return DataDome challenges. Yahoo's cookie and redirect flow is only fixture-tested. Mojeek and Startpage are not wired in because of upstream proof-of-work challenges.

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

1. Evaluate Naver, Seznam and other useful regional/general-web providers, prioritising those with maintainable access routes.
2. Add suitable providers with fixture tests and explicit credential/cost requirements.
3. Review Mojeek and Startpage proof-of-work challenges; defer if no sustainable route is available.
4. Reach ten useful, genuinely implemented adapters.
5. Add SearXNG-compatible JSON API after provider coverage is useful.
6. Build browser search, administration and diagnostics after the compatibility API.
7. Keep SearchBox.NET.Forge as a separate follow-on project for upstream monitoring and AI-assisted porting proposals; require tests and human review, never auto-merge.
