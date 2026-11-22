# Current handover status

Updated: 2026-11-22

## Repository

- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest verified code commit: b037b44e9b7d00debdcae9eab1a910a125da7c9b (Brave adapter CI correction pending)
- Documentation refresh is being committed separately.
- Full project overview and roadmap: [README.md](../README.md)

## Working vertical slice

- Native ASP.NET Core 10 Web API.
- GET /health and GET /api/search?q=...
- Search query/result/provider contracts.
- Parallel provider aggregation with per-provider timeout and provider-failure isolation.
- Caller cancellation is propagated.
- HTTP(S)-only result URL validation, duplicate removal, deterministic score/title ordering, and result limit.
- Three adapters in the current branch: Bing Web HTML, Brave Search API (optional key), and Wikipedia MediaWiki search API.
- xUnit test project and GitHub Actions .NET 10 CI.

## Verification

- CI run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45007833897\n- Previous foundation-only run: https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/44942530782
- Commit tested: 7e088b4112c0424fad411f42ddf9f4861ddff103
- Restore: passed.
- Build: passed.
- Tests: 15 passed, 0 failed, 0 skipped.
- Local .NET SDK is not installed in the orchestration environment; verification was performed by GitHub Actions.
- The first two CI runs exposed compiler and test-import errors. Both were corrected; the third run passed. See [LOG.md](LOG.md).

## Provider implementation priorities

The agreed approach is major general-purpose search engines first, then work down the list:

1. Bing Web — implemented using the current SearXNG HTML route; live upstream availability remains to be exercised.
2. Brave Search — establish API/key requirements.
3. Google Search — select a supported access method and document quotas/costs.
4. DuckDuckGo — validate current public interface and limitations.
5. Wikipedia — already implemented; retain as the reference adapter.
6. Mojeek — next tier.
7. Further SearXNG engines, selected by usefulness, reliability and maintainability.

Target: first 5–10 genuinely implemented and tested providers. A provider is not counted as ported until its adapter and tests are present and CI verifies them.

## Agreed delivery sequence

1. Provider foundation and major search engines.
2. First 5–10 provider adapters.
3. SearXNG-compatible JSON API.
4. Browser search interface, administration and diagnostics.
5. Operational hardening: SQL Server configuration where useful, API keys, quotas, rate limiting, metrics and deployment guidance.

Do not divert into the admin UI or compatibility layer before the core provider coverage is useful, except where a small architectural change is necessary to support adapters safely.

## Important limitations

Wikipedia is wired in. The Bing HTML adapter has been added but is pending CI verification. There is no SQL Server provider store, general configurable HTTP provider, API-key authentication, quotas, admin UI or SearXNG-compatible JSON API yet. Do not expose this endpoint as a public unrestricted search service before authentication, rate limiting and outbound request policy are implemented.

Some major search providers may require credentials or have changed/retired access routes. Validate provider terms, access, quotas and cost before implementation; do not promise free or unrestricted access.

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

1. Investigate current supported access routes and terms for Bing, Brave Search, Google Search and DuckDuckGo; record key/quota/cost requirements.
2. Add isolated adapter tests before or alongside the first provider implementation.
3. Implement the first viable major-engine adapter and register it with the aggregator.
4. Repeat provider-by-provider until the first 5–10 adapters are real and tested.
5. Add SearXNG-compatible JSON API after provider coverage is useful.
6. Build browser search, administration and diagnostics after the compatibility API.
7. Keep SearchBox.NET.Forge as a separate follow-on project for upstream monitoring and AI-assisted porting proposals; require tests and human review, never auto-merge.
