# Current handover status

Updated: 2026-11-22

## Repository

- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest verified code commit: 93bb6aea3338b0848d119691701c4cca680caf19
- Full project overview and roadmap: [README.md](../README.md)

## Working vertical slice

- Native ASP.NET Core 10 Web API.
- GET /health and GET /api/search?q=...
- Search query/result/provider contracts.
- Parallel provider aggregation with per-provider timeout and provider-failure isolation.
- Caller cancellation is propagated.
- HTTP(S)-only result URL validation, duplicate removal, deterministic score/title ordering, and result limit.
- Five concrete adapters: Bing Web HTML, Brave Search API (optional key), Google Web HTML, DuckDuckGo Web HTML (first page), and Wikipedia MediaWiki search API.
- Brave is registered only when BRAVE_SEARCH_API_KEY is configured.
- xUnit test project and GitHub Actions .NET 10 CI.

## Verification

- CI run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008393329
- Commit tested: 93bb6aea3338b0848d119691701c4cca680caf19
- Restore: passed.
- Build: passed.
- Tests: 41 passed, 0 failed, 0 skipped.
- Local .NET SDK is not installed in the orchestration environment; verification was performed by GitHub Actions.
- Tests use deterministic fixtures and fake HTTP handlers. They verify parsing/request behaviour, not live acceptance by Bing, Brave, Google or DuckDuckGo.

## Provider implementation priorities

The agreed approach is major general-purpose search engines first, then work down the list:

1. Bing Web — implemented and CI-verified; live upstream availability not tested.
2. Brave Search API — implemented and CI-verified; optional, requires BRAVE_SEARCH_API_KEY.
3. Google Web — implemented and CI-verified via HTML; CAPTCHA/blocking risk.
4. DuckDuckGo Web — implemented and CI-verified for first page; CAPTCHA/bot-blocking risk, no pagination/VQD.
5. Wikipedia — implemented and CI-verified; reference JSON/API adapter.
6. Qwant — next candidate; SearXNG uses an undocumented JSON API and DataDome cookie handling.
7. Mojeek — deferred; current SearXNG settings mark it inactive due to proof-of-work CAPTCHA.
8. Startpage — deferred; current SearXNG settings mark it inactive due to Anubis proof-of-work.

Target: first 5–10 genuinely implemented and tested providers. A provider is not counted as ported merely because its name appears in configuration.

## Agreed delivery sequence

1. Provider foundation and major search engines.
2. First 5–10 provider adapters.
3. SearXNG-compatible JSON API.
4. Browser search interface, administration and diagnostics.
5. Operational hardening: SQL Server configuration where useful, API keys, quotas, rate limiting, metrics and deployment guidance.

Do not divert into the admin UI or compatibility layer before the core provider coverage is useful, except where a small architectural change is necessary to support adapters safely.

## Important limitations

Bing Web, Brave Search API, Google Web, DuckDuckGo Web (first page) and Wikipedia are wired in. Fixture-based CI tests pass; live upstream acceptance is not verified. Brave requires a key and is not registered when BRAVE_SEARCH_API_KEY is absent. DuckDuckGo pagination is not implemented. Qwant, Mojeek and Startpage are not wired in.

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

1. Evaluate Qwant's undocumented JSON API, result schema, rate limits and DataDome cookie flow.
2. Implement and test Qwant if its access route is maintainable and acceptable.
3. Review Mojeek and Startpage proof-of-work challenges before deciding whether to port them.
4. Continue adding useful SearXNG providers until the target of 5–10 adapters is reached.
5. Add SearXNG-compatible JSON API after provider coverage is useful.
6. Build browser search, administration and diagnostics after the compatibility API.
7. Keep SearchBox.NET.Forge as a separate follow-on project for upstream monitoring and AI-assisted porting proposals; require tests and human review, never auto-merge.
