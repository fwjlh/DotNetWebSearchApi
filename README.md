# DotNetWebSearchApi

A self-hosted, privacy-oriented metasearch API for .NET 10.

The goal is to bring the useful ideas behind [SearXNG](https://github.com/searxng/searxng) to a native .NET deployment: query multiple independent search providers, normalise and combine their results, and expose one consistent API that you control.

This repository is an independent .NET implementation. It is not affiliated with, endorsed by, or a drop-in replacement for the SearXNG project. See [UPSTREAM.md](UPSTREAM.md) for provenance and attribution, and [LICENSE](LICENSE) for licensing.

## Project status

**Initial implementation in progress.** The first milestone establishes the API, provider contracts, aggregation behaviour, tests, and automated .NET 10 build. SQL-backed provider configuration, API-key management, provider-specific adapters, and the browser-facing search interface follow as separate increments.

## Design goals

- Native C# and ASP.NET Core 10; no Python runtime or Docker dependency.
- Self-hosted. Your API callers connect to your server; configured providers receive requests from your server.
- Provider adapters isolate differences between search engines.
- Parallel provider execution with bounded timeouts and partial-result handling.
- Normalised, deduplicated results with deterministic ranking.
- Provider definitions can evolve towards SQL Server/T-SQL administration without embedding secrets in ordinary configuration rows.
- Small source files, shallow structure, explicit contracts, and meaningful automated tests.
- Privacy by design: do not forward caller IP addresses, API keys, or unrelated request headers to upstream providers.

## Planned architecture

`HTTP API -> Search orchestration -> Provider registry -> Provider adapters -> External search providers`

The first release uses explicit C# provider implementations. A later configurable-provider layer will support common HTTP GET/POST patterns and structured response extraction. Complex providers remain free to use dedicated C# adapters. Razor/CSHTML is intended for controlled server-side templates where it provides real value, not as an unrestricted code-execution mechanism.

SQL Server will be an optional configuration store, not a requirement for running the initial API. Provider secrets should be supplied through a secret store or environment-specific configuration and referenced by name; they must not be stored as plaintext in provider-definition rows.

## Build

Install the .NET 10 SDK, then run:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

The GitHub Actions workflow runs restore, build, and tests on pushes and pull requests.

## API

The initial API surface is being developed around:

- `GET /api/search?q={query}` — aggregate results from enabled providers.
- `GET /health` — process health.

Authentication, quotas, provider administration, and operational metrics will be added before recommending exposure to an untrusted public network.

## Security notes

This service makes outbound HTTP requests based on provider configuration. Any configurable endpoint feature must defend against SSRF: restrict schemes, validate destinations, block loopback/link-local/private ranges where appropriate, revalidate redirects, and avoid allowing arbitrary caller-supplied URLs. Provider configuration is trusted administrative input, not a public query parameter. Apply outbound network controls as an additional layer.

Do not expose an unauthenticated instance to the public internet until authentication, rate limiting, request limits, and SSRF protections are implemented and reviewed.

## Development diary

Every implementation increment and handover is recorded under [activity/](activity/). Read [activity/STATUS.md](activity/STATUS.md) first when resuming work.

## License and upstream

This project is distributed under AGPL-3.0-or-later. SearXNG is also licensed under AGPL-3.0-or-later. The relationship, attribution, and scope of the initial independent implementation are documented in [UPSTREAM.md](UPSTREAM.md). This is not legal advice; review licensing obligations before redistribution or commercial deployment.
