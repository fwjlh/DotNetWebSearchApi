# Current handover status

Updated: 2026-11-21

## Current state

- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: `main`
- Bootstrap README commit: `4ed379468e6c1584d7b9d4d0876d06a9fbb0ba6e`
- Initial status commit: `3bfda0a45e42e1e278bcfa5220135b931b46cd59`
- Project documentation and provenance baseline are being established.
- No application source or automated tests have been committed yet.
- The execution environment previously reported no installed .NET SDK. Recheck before claiming local build/test execution. GitHub Actions is intended to be the authoritative .NET 10 build/test environment.

## Non-negotiable working rules

1. Record every material repository action in `activity/LOG.md`: action, reason, result, commit SHA (when applicable), and next step.
2. Update this file at every handover.
3. Each implementation increment must include its tests and activity-log update in the same commit.
4. Push each completed increment to `main`; verify the resulting commit/ref.
5. Source files preferably below 15–20 KB; hard maximum 25 KB.
6. Maximum two nesting levels. Extract nested blocks over five lines into named functions.
7. Small, single-purpose functions; named functions preferred over trivial lambdas; immutable/pure logic where practical.
8. Maximum four parameters; use an options/parameter type otherwise.
9. Explicit types and return types for public APIs; early returns; no dead code.
10. No side effects in constructors or property getters. Nullable reference types enabled; avoid null where practical.
11. Cover logic and meaningful edge cases; use clear test names.

## Architecture decisions

- Native C# / ASP.NET Core 10; no Python or Docker runtime.
- Independent .NET metasearch implementation inspired by SearXNG; do not claim complete feature parity.
- Explicit provider adapters first; configurable HTTP providers and SQL Server provider definitions in later increments.
- CSHTML/Razor only as a constrained template option, never arbitrary untrusted code execution.
- Provider secrets are external secret references, not plaintext in ordinary provider rows.
- SSRF protections, API-key validation, quotas and rate limits are required before public untrusted deployment.
- Read [UPSTREAM.md](../UPSTREAM.md) for provenance and licensing boundary.

## Next action

Implement the first tested vertical slice: ASP.NET Core 10 search API, provider contracts, parallel aggregation with isolation/timeouts, deterministic normalisation/deduplication, unit tests, and GitHub Actions CI. Check actual CI status before reporting success.
