# Architecture decisions and consumer integration

Updated: 2026-11-23

## Product boundary

DotNetWebSearchApi is an independent, self-hosted search aggregation service. It must remain usable without CoderInBox, WinForms, WebView2 or any CoderInBox-specific assembly. CoderInBox is the first major consumer, not a host for the provider implementations.

Consumers should depend on a stable HTTP/JSON contract. Provider selection, upstream quirks, cookies, challenge classification, cooldowns, aggregation and ranking remain internal to the search service. The public API should expose stable result and provider-outcome models, not provider implementation types.

Do not merge DotNetWebSearchApi provider code into CoderInBox.

## Provider freeze

The current catalogue is frozen at **67 provider adapters** while shared framework work is underway. Do not add providers, increase the count, or treat another adapter as the next milestone until the architecture work below is complete and the existing catalogue has been reviewed against the shared contracts.

The freeze does not imply that all 67 providers have been live-tested. Existing fixture/CI coverage verifies implementation behaviour against captured responses and fake HTTP handlers; it does not prove that upstream services currently accept live automated requests.

## CoderInBox integration findings

Repository: https://github.com/fwjlh/CoderInBox

The inspected CoderInBox search path currently has a sequential fallback chain in `WebResearch.SearchAsync`:

1. DuckDuckGo HTML via direct HTTP and HTML parsing.
2. SearXNG through `SearxngClient`, using a configured instance or instance discovery.
3. Wikipedia OpenSearch JSON.
4. Optional Presearch API using a bearer key.
5. Google through a real WebView2 browser hosted by the WinForms harness.

The Tools layer defines `IBrowserSearchService`; the concrete `WebView2BrowserSearch` lives in the Harness project. `MainShell` creates it and assigns it to the static `WebResearch.BrowserSearch` property, then clears/disposes it when the host closes.

### Recommended consumer boundary

- Add a general `IWebSearchService` abstraction in CoderInBox.Tools for ordinary query-based search.
- Implement it with a small HTTP client that calls DotNetWebSearchApi's stable endpoint, initially `GET /api/search?q=...&limit=...`.
- Keep `IBrowserSearchService` for actual browser automation/interactive user verification. It is not the general metasearch contract.
- Keep WebView2 and WinForms references in the Harness project; the search API and Tools abstraction must not depend on them.
- Move away from the static `WebResearch.BrowserSearch` seam incrementally through host composition/constructor injection. Do not make this a prerequisite for the first HTTP integration.
- Preserve CoderInBox's fallback behaviour during migration. The service should first replace the direct DuckDuckGo/SearXNG search paths; Wikipedia/Presearch can remain fallbacks until their value and duplication are assessed. WebView2 remains an optional browser fallback, not a dependency of the service.

CoderInBox currently targets .NET 8 Windows/WinForms, while DotNetWebSearchApi targets .NET 10. An HTTP boundary avoids forcing a framework/runtime alignment or sharing provider assemblies.

## Shared architecture work

Keep the API host thin and keep core search contracts independent of ASP.NET Core. Establish clear namespaces and dependency boundaries first; do not split the solution into multiple projects merely for appearance. Extract a class library only when the contracts and dependency direction are stable and tests demonstrate the boundary.

Planned sequence:

1. **Search request context and engine capabilities** — query, locale, categories, safe-search, page, time range, requested providers and continuation state; explicit per-engine capabilities and pre-flight eligibility.
2. **Typed provider outcomes and health** — distinguish success/empty, timeout, rate limit, transient HTTP, invalid response, configuration/authentication, access denied and challenge. Add cooldown/backoff and half-open probes; caller cancellation is not provider failure.
3. **Shared transport and sessions** — typed request construction, consistent timeout/cancellation, headers, cookies/session isolation, redirect policy, response-size limits, safe retries, Retry-After and concurrency/rate policies. Retain provider-specific request/parser strategies where needed.
4. **Challenge classification and handling** — detect and distinguish temporary blocks, browser verification, proof-of-work, genuine CAPTCHA, login and rate limits. Allow bounded provider-specific protocol handlers only where permitted. Offer human-operated verification as an optional consumer/browser workflow. AI vision may classify and explain a challenge; it is not an automatic CAPTCHA solver or bypass. Never log or return session cookies, tokens or challenge secrets.
5. **Result aggregation and ranking** — retain contributing engines, per-engine positions, engine weights and useful metadata; merge duplicates deliberately with deterministic scoring and tie-breaks.
6. **Engine traits and strategy composition** — locale/region mappings, request strategies, parser strategies and provider state. Keep simple JSON/RSS definitions simple; do not turn the generic definition into a giant bag of nullable options.
7. **API contract and operational hardening** — explicit compatibility target and conformance tests; authentication, quotas, rate limiting and outbound request policy before untrusted public exposure.

## Challenge safety and semantics

Challenge handling is classification and legitimate recovery, not a universal CAPTCHA bypass facility.

- A transient HTTP block or rate limit should produce a typed outcome and appropriate cooldown.
- Browser verification may require a real browser session and user action.
- A documented proof-of-work protocol may be implemented in a bounded, provider-specific handler when permitted by the upstream service.
- Interactive image/audio CAPTCHA should be classified and reported as requiring human verification; do not automatically solve it.
- If verification cannot be completed legitimately, mark that provider unavailable for this request and continue with the others.
- Preserve provider session state internally where needed, but do not expose cookies, tokens or credentials through API responses or diagnostics.

Mojeek's existing Altcha proof-of-work implementation is the concrete example to retain as provider-specific protocol behaviour while moving reusable transport/session mechanics into shared infrastructure. Its fixture tests are not evidence of live upstream acceptance.

## Definition of done for the freeze

Provider expansion resumes only after:
- request context and capability gating have focused tests;
- typed outcomes and provider health/cooldown have focused tests;
- shared transport/session policy is used by representative HTML, JSON and multi-step providers;
- challenge classifications have positive and false-positive tests;
- aggregation retains source-engine/position evidence and has multi-provider duplicate tests;
- existing provider tests remain green;
- README, status and activity log agree on the provider count and verified CI baseline.

## Outbound destination policy

All provider HttpClientFactory clients use `PublicNetworkSocketsHandler`. Its `SocketsHttpHandler.ConnectCallback` resolves the requested host immediately before connecting and rejects the connection if any resolved address is non-public. Because automatic redirects use the same handler, each redirected host is subject to the same DNS/address policy. The handler connects directly rather than inheriting ambient proxy settings; deployments that require an outbound proxy need an explicitly trusted proxy design rather than silently bypassing destination checks.

The shared transport also rejects non-HTTP(S) initial request URIs, embedded URI credentials, localhost/local/internal hostnames and non-public IP literals. Tests cover private/reserved IPv4 and IPv6 ranges, public address acceptance, handler configuration and initial URI rejection.

This is not a substitute for network egress firewall rules. Keep provider destinations fixed in provider code/configuration; never accept arbitrary destination URLs from callers. Review any new custom HttpClient handler registration to ensure it retains this policy.

## Live upstream smoke tests

The xUnit project contains an explicitly opt-in live integration suite for Wikipedia, Crossref and GitHub. It calls the real provider adapters with one request per provider, a 20-second timeout, a 1 MiB response cap and a maximum of three results. Set `DOTNETWEBSEARCHAPI_LIVE_TESTS=1` to run it. The default fixture suite remains offline and deterministic. Live smoke tests measure current reachability and response compatibility only; they do not guarantee future availability or validate all 67 adapters.

## Current baseline

- Provider adapters: 67 (frozen).
- Latest user-reported local Release verification (23 November 2026): build succeeded; 319 tests passed, 0 failed, 0 skipped. This is fixture-based local verification, not live upstream verification.
- GitHub Actions are disabled by the repository owner to conserve minutes; do not re-enable or rely on workflow runs.
- Coverlet collector 6.0.4 has been added to the test project; measured coverage is the next quality gate.
- The architecture audit is in [PORTING_AUDIT.md](PORTING_AUDIT.md).
