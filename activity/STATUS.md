# Current handover status

Updated: 2026-11-21

## Current state

- Public repository verified: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: `main`
- Initial commit: `4ed379468e6c1584d7b9d4d0876d06a9fbb0ba6e`
- Root `README.md` committed. It records the project purpose, .NET 10 target, staged architecture, security constraints, and links to the activity diary.
- No application source or tests have been committed yet.
- The execution environment previously reported no installed .NET SDK. Do not claim local compilation or test execution unless rechecked. CI will be the authoritative build/test environment.

## Non-negotiable working rules

1. Record every repository action in `activity/LOG.md`: action, reason, result, commit SHA (when applicable), and next step.
2. Update `activity/STATUS.md` at each handover.
3. Each implementation increment must include its tests and the activity-log update in the same commit.
4. Push each completed increment to `main`; verify the resulting commit/ref.
5. Keep source files preferably below 15–20 KB and never above 25 KB.
6. Maximum two levels of nesting. Extract nested blocks over five lines into named functions.
7. Small, single-purpose functions; named functions preferred over trivial lambdas; immutable/pure logic where practical.
8. Maximum four parameters; use an options/parameter type otherwise.
9. Explicit types and return types for exported/public functions; early returns; no dead code.
10. No side effects in constructors or property getters. Avoid null where practical; enable nullable reference types.
11. Aim for complete logic and meaningful edge-case coverage; tests should have clear names.

## Decisions to preserve

- Native C# / ASP.NET Core 10; no Python or Docker runtime.
- SearXNG-inspired independent .NET metasearch implementation, not a claim of being a complete line-by-line port.
- Explicit provider adapters first; configurable HTTP providers and SQL Server provider definitions in later increments.
- Treat CSHTML/Razor only as a constrained template option, never arbitrary untrusted code execution.
- Provider secrets must be external secret references, not plaintext in ordinary provider configuration.
- SSRF protection, caller API-key validation, quotas, and rate limits are required before public untrusted deployment.
- Upstream provenance and license obligations must be explicit and verified; see `UPSTREAM.md` when added.

## Next action

Create the legal/provenance and build foundation, then the first API/provider/aggregation vertical slice with tests and .NET 10 CI. Verify CI results before reporting tests as passed.
