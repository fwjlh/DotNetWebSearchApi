# DotNetWebSearchApi

[![Build and test](https://github.com/fwjlh/DotNetWebSearchApi/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/fwjlh/DotNetWebSearchApi/actions/workflows/ci.yml)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/)

**A self-hosted metasearch API, written in native C# and ASP.NET Core.**

DotNetWebSearchApi brings the useful metasearch ideas behind [SearXNG](https://github.com/searxng/searxng) into a straightforward .NET deployment. It queries multiple independent search providers, combines and normalises their results, and gives applications one API that they can host and control.

The goal is not to build another search index. It is to provide a practical, extensible aggregation layer that can run on infrastructure you already operate: particularly Windows Server, IIS and SQL Server environments.

> **Project status: initial search stack implemented.** Forty-two provider adapters, the SearXNG-shaped JSON API, a browser search page and read-only provider diagnostics are in place. The next implementation priority is expanding the SearXNG provider catalogue towards 100 tested adapters, then operational hardening.

This is an independent implementation inspired by SearXNG. It is not affiliated with, endorsed by, or a drop-in replacement for the SearXNG project. See [UPSTREAM.md](UPSTREAM.md) for provenance and [LICENSE](LICENSE) for licensing.

---

## Why this project?

Using several search providers independently creates repeated integration work: different request formats, response structures, ranking signals, timeouts and failure modes.

DotNetWebSearchApi puts that work behind one service:

- **One endpoint for many engines.** Applications make one request and receive a merged result set.
- **Self-hosted control.** Your application calls your server; your server communicates with configured upstream providers.
- **Native .NET.** Built with C# and ASP.NET Core 10. No Python runtime, Docker or separate search stack is required.
- **Provider isolation.** Each engine has a focused adapter. One failing or slow provider should not prevent useful results from the others.
- **Configurable direction.** Provider definitions are intended to move towards SQL Server-backed administration, with secrets kept outside ordinary provider records.
- **Open source development.** The project is structured so that other .NET developers can add, test and improve adapters.

## Current capabilities

| Capability | Status | Notes |
|---|---|---|
| ASP.NET Core 10 API | Working | Minimal API host |
| Health endpoint | Working | GET /health |
| Unified search endpoint | Working | GET /api/search?q=... |
| SearXNG-shaped JSON endpoint | Working (initial compatibility) | GET /search, POST /search and GET /; JSON response with provider status |
| Provider contract | Working | Common query and result types |
| Parallel provider execution | Working | Providers run concurrently |
| Timeout and failure isolation | Working | Partial results can survive provider failures |
| Result normalisation | Working | HTTP(S) URLs, deduplication and deterministic ordering |
| Result limit | Working | Default 10; maximum 50 |
| Wikipedia adapter | Working | MediaWiki search API |
| Bing Web adapter | Working | HTML result parsing; live availability not tested in CI |
| Brave Search API adapter | Working (optional) | Requires BRAVE_SEARCH_API_KEY |
| Google Web adapter | Working (HTML) | CAPTCHA/blocking remains an upstream risk |
| DuckDuckGo Web adapter | Working (first page) | HTML POST; CAPTCHA detection; no pagination/VQD |
| Qwant Web adapter | Working (experimental) | Undocumented JSON API; DataDome may deny requests |
| Yahoo Web adapter | Working (experimental) | HTML parser, sB preferences and bounded YBV redirect handling; live availability not tested |
| Yandex Search API adapter | Working (optional, paid) | Requires YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID |
| Naver Web adapter | Working (regional) | Korean general-web HTML results; first page only |
| Seznam Web adapter | Working (regional) | Homepage bootstrap, hidden fields and cookie reuse; live availability not tested |
| Crossref | Working (JSON API) | Scholarly works; fixture-tested, live acceptance not tested |
| OpenAlex | Working (JSON API) | Scholarly works; fixture-tested, live acceptance not tested |
| Hacker News | Working (JSON API) | Algolia search endpoint; fixture-tested |
| Stack Overflow | Working (JSON API) | Stack Exchange API; fixture-tested, quota rules apply |
| npm | Working (JSON API) | Package registry search; fixture-tested |
| crates.io | Working (JSON API) | Rust package registry; fixture-tested |
| NuGet | Working (JSON API) | NuGet search service; fixture-tested |
| Open Library | Working (JSON API) | Book catalogue; fixture-tested |
| Europe PMC | Working (JSON API) | Biomedical literature; fixture-tested |
| Semantic Scholar | Working (JSON API) | Scholarly papers; subject to upstream rate limits |
| GitHub Repositories | Working (JSON API) | Repository search; unauthenticated API rate limits apply |
| Docker Hub | Working (JSON API) | Container image repository search |
| Google Books | Working (JSON API) | Book catalogue |
| Internet Archive | Working (JSON API) | Archive catalogue |
| RubyGems | Working (JSON API) | Ruby package registry |
| Hugging Face Models | Working (JSON API) | Model catalogue search |
| Super User | Working (JSON API) | Stack Exchange network search |
| Server Fault | Working (JSON API) | Stack Exchange network search |
| Ask Ubuntu | Working (JSON API) | Stack Exchange network search |
| GitLab Projects | Working (JSON API) | Public project search |
| Gitea | Working (JSON API) | Public repository search on gitea.com |
| Maven Central | Working (JSON API) | Java artifact search |
| Packagist | Working (JSON API) | PHP package search |
| pub.dev | Working (JSON API) | Dart/Flutter package search |
| arXiv | Working (Atom API) | Scholarly preprints; XML/Atom parser |
| Bilibili | Working (JSON API) | Video search |
| Dailymotion | Working (JSON API) | Video search |
| Deezer | Working (JSON API) | Music search |
| Steam | Working (JSON API) | Game catalogue search |
| MusicBrainz | Working (JSON API) | Music metadata search |
| Wikidata | Working (JSON API) | Knowledge graph search |
| German Wikipedia | Working (JSON API) | Regional MediaWiki search |
| Automated build and tests | Working | GitHub Actions / .NET 10 |
| Major web-search adapters | Initial wave complete | Bing, Brave, Google, DuckDuckGo and other priority providers are implemented |
| First 5–10 provider adapters | Initial target met | Forty-two provider adapters are implemented and CI-tested |
| SearXNG-compatible JSON API | Initial surface working | JSON response and engine selection; unsupported features return explicit status codes |
| Browser search interface | Working (initial) | Search page at / with selectable providers and result cards |
| Read-only administration and diagnostics | Working (initial) | /admin, provider health counters, last outcome and active provider list |
| SQL Server configuration store | Planned | Optional persistence and administration |
| API keys, quotas and rate limiting | Planned | Required before untrusted public exposure |

The status above is deliberately conservative: a provider is only marked **Working** once its adapter is implemented and tested. Planned integrations are not represented as available features.

## Search provider roadmap

The implementation strategy is to work from the major general-purpose search engines down through specialist and topic-specific providers. The first target is a useful set of 100 tested provider adapters, not a long list of untested names.

| Priority | Provider | Status | Implementation notes |
|---:|---|---|---|
| 1 | Bing Web | Working | SearXNG-style HTML adapter; parser and request tests pass; live availability not tested in CI |
| 2 | Brave Search API | Working (optional) | Official JSON API; requires BRAVE_SEARCH_API_KEY; not enabled without a key |
| 3 | Google Web | Working (HTML) | SearXNG-style HTML adapter; no API key; CAPTCHA/blocking remains an upstream risk |
| 4 | DuckDuckGo Web | Working (first page) | HTML POST adapter; CAPTCHA detection; pagination/VQD not implemented |
| 5 | Wikipedia | Working | First adapter; retain as a useful reference implementation |
| 6 | Qwant | Working (experimental) | Undocumented JSON API; relies on HTTP cookie handling; live availability not tested in CI |
| 7 | Yahoo Web | Working (experimental) | HTML adapter with cookie and redirect handling; live availability not tested in CI |
| 8 | Yandex Search API | Working (optional, paid) | Requires API key, folder ID and billing account; current synchronous price about $4/1,000 daytime and $3/1,000 night-time requests |
| 9 | Naver Web | Working (regional) | Korean first-page HTML results; disabled by default in SearXNG |
| 10 | Seznam Web | Working (regional) | HTML with homepage bootstrap and cookie reuse; disabled by default in SearXNG |
| 11 | Mojeek | Deferred | Current SearXNG settings mark it inactive due to proof-of-work CAPTCHA |
| 12 | Startpage | Deferred | Current SearXNG settings mark it inactive due to Anubis proof-of-work |
| 13–20 | Specialist JSON APIs | Initial wave implemented | Crossref, OpenAlex, Hacker News, Stack Overflow, npm, crates.io, NuGet and Open Library |
| 21–28 | Specialist API wave 2 | Implemented in code; CI pending | Europe PMC, Semantic Scholar, GitHub, Docker Hub, Google Books, Internet Archive, RubyGems and Hugging Face |
| 29–36 | Developer and package API wave 3 | Implemented in code; CI pending | Super User, Server Fault, Ask Ubuntu, GitLab, Gitea, Maven Central, Packagist and pub.dev |
| 37–44 | Media, knowledge and academic wave 4 | Implemented in code; CI pending | arXiv, Bilibili, Dailymotion, Deezer, Steam, MusicBrainz, Wikidata and German Wikipedia |
| 45–100 | Further SearXNG providers | Planned | Prioritise by usefulness, reliability, accessibility, terms and maintenance cost; count only tested request/parser/registration adapters |

Provider ordering is a development priority, not a claim that every upstream service offers a free or unrestricted API. Current SearXNG settings mark Mojeek inactive because of its proof-of-work CAPTCHA and Startpage inactive because of its Anubis proof-of-work challenge; those are deferred until their CPU and access implications are understood. Brave's current Search plan is listed at $5 per 1,000 requests with $5 in monthly credits; activating a plan requires payment-card details. Check the [current Brave plans](https://brave.com/search/api/) before use. Each adapter must respect the provider's current access rules, terms, rate limits and attribution requirements. Where a provider requires credentials, keys will be configured as secrets and never committed to source control.

### What makes an adapter complete?

Each provider should have:

1. A small, isolated C# implementation behind the common provider contract.
2. Explicit request construction and response parsing.
3. Tests for normal responses, empty results, malformed payloads, HTTP failures and cancellation/timeouts.
4. Safe URL handling and sensible result metadata.
5. Documented access requirements, quotas and configuration.
6. Registration in the provider set and verification in CI.

A provider should not be counted as ported merely because its name appears in a configuration file.

## Architecture

The intended request flow is:

    Client application
          |
          v
       Search API
          |
          v
   Search orchestration
          |
          v
     Provider registry
       /     |     \
      v      v      v
   Adapter Adapter Adapter
      |      |      |
      v      v      v
   Engine  Engine  Engine
       \     |     /
        v    v    v
    Normalise, deduplicate
       and rank results
             |
             v
        Search API
             |
             v
       Client application

The first implementation uses explicit C# adapters. A later provider-definition layer is intended to support common HTTP GET/POST patterns and structured response extraction. Providers with unusual protocols or parsing requirements can continue to use dedicated C# code.

SQL Server is planned as an optional configuration store, not a prerequisite for running the initial service. Secrets should be supplied through environment-specific configuration or a secret store and referenced by name; they should not be stored as plaintext in provider-definition rows.

## SearXNG-compatible JSON API

The initial compatibility surface is available at:

- `GET /search?q=dotnet&format=json`
- `GET /?q=dotnet&format=json`
- `POST /search` with `application/x-www-form-urlencoded` fields

The `format` parameter defaults to JSON; `format=html` is not supported. The `engines` parameter accepts a comma-separated list of configured provider names, for example `engines=bing,google`. Unknown engines return HTTP 400. The initial implementation supports the `general` category and first page only. `pageno` values other than 1, non-default language/safesearch and `time_range` currently return HTTP 501 rather than being silently ignored.

Provider discovery and read-only diagnostics are also available at GET /api/providers and GET /api/diagnostics. The /admin page displays provider status, success/failure counts, last duration and last error type. These are process-local counters and reset when the service restarts.

The response uses the familiar SearXNG field names: `query`, `number_of_results`, `results`, `answers`, `corrections`, `infoboxes`, `suggestions` and `unresponsive_engines`. Result entries include `url`, `title`, `content`, `engine`, `engines`, `positions`, `score`, `category`, `template`, `parsed_url` and `publishedDate`. Empty answer/infobox/suggestion arrays are returned until those result types are implemented. `positions` is currently inferred from the adapter score, and duplicate URLs retain the highest-scoring provider rather than combining every engine position. This is response-shape compatibility, not full SearXNG feature parity.

Example:

    GET /search?q=dotnet&format=json

    {
      "query": "dotnet",
      "number_of_results": 1,
      "results": [
        {
          "url": "https://example.com/article",
          "title": "Example result",
          "content": "A short result description.",
          "engine": "bing",
          "engines": ["bing"],
          "positions": { "bing": 1 },
          "score": 1.0,
          "category": "general",
          "template": "default.html",
          "parsed_url": ["https", "example.com", "/article", "", "", ""],
          "publishedDate": null
        }
      ],
      "answers": [],
      "corrections": [],
      "infoboxes": [],
      "suggestions": [],
      "unresponsive_engines": []
    }

The example is illustrative; actual results and unresponsive provider names depend on upstream responses.

## Technology and deployment direction

- **Runtime:** .NET 10 / ASP.NET Core.
- **Language:** C#.
- **Hosting:** Kestrel or IIS hosting; Windows Server is an important target.
- **Configuration:** app configuration initially; optional SQL Server-backed provider configuration later.
- **Tests:** xUnit.
- **Automation:** GitHub Actions.
- **External dependencies:** outbound HTTPS access to whichever providers are enabled.

There is no Docker or Python dependency in the intended deployment model.

## Quick start

### Requirements

- .NET 10 SDK.
- Internet access for the enabled upstream search provider.
- Git to clone the repository.

### Clone and run

    git clone https://github.com/fwjlh/DotNetWebSearchApi.git
    cd DotNetWebSearchApi
    dotnet restore
    dotnet run --project src/DotNetWebSearchApi

The application prints its local listening address when it starts. Open that address in a browser for the search page. Use /admin for read-only provider diagnostics. The admin page shows active providers, success/failure counts, last duration and last error type; it does not expose secrets or edit configuration.

### Optional Brave Search provider

Brave is enabled only when an API key is supplied through the BRAVE_SEARCH_API_KEY environment variable. Without the variable, the service starts normally and skips Brave. In PowerShell, set it for the current session before starting the application:

    $env:BRAVE_SEARCH_API_KEY = "your-brave-api-key"
    dotnet run --project src/DotNetWebSearchApi

For IIS or a Windows service, configure the environment variable in the hosting environment. Never put a real key in appsettings committed to the repository or in source code.

### Optional Yandex Search API provider

Yandex is enabled only when both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID are supplied. It requires a Yandex Cloud billing account and is a paid integration. In PowerShell:

    $env:YANDEX_SEARCH_API_KEY = "your-yandex-api-key"
    $env:YANDEX_FOLDER_ID = "your-folder-id"
    dotnet run --project src/DotNetWebSearchApi

The current published synchronous rates for customers billed in USD are approximately $4 per 1,000 daytime requests and $3 per 1,000 night-time requests, excluding VAT. Rates depend on the contracting entity and can change. See the [official Yandex Search API pricing](https://aistudio.yandex.ru/en/docs/search-api/pricing) and [quick start](https://aistudio.yandex.ru/en/docs/search-api/quickstart/) before enabling it.

### Check health

    GET /health

Example response:

    {
      "status": "ok"
    }

### Search

    GET /api/search?q=dotnet%20web%20api&limit=10

The current response shape is:

    {
      "query": "dotnet web api",
      "results": [
        {
          "title": "Example result",
          "url": "https://example.com/article",
          "snippet": "A short result description.",
          "provider": "wikipedia",
          "score": 1.0
        }
      ]
    }

The example result is illustrative; actual results depend on the upstream response. The current implementation defaults to 10 results and accepts a limit from 1 to 50. A missing or blank query, or an invalid limit, returns HTTP 400.

## Build and test

Run from the repository root:

    dotnet restore
    dotnet build --no-restore
    dotnet test

GitHub Actions runs restore, build and tests for pushes and pull requests. The badge at the top links to the workflow.

The current verified implementation passes CI with **95 tests passing** across the aggregation layer, ten provider adapters, JSON API integration tests and provider diagnostics. Check the Actions history for the latest result; this README does not assume that a previous successful run proves every future commit is green.

## Planned delivery sequence

The work is intentionally staged so the useful search capability comes before the polish:

1. **Provider foundation and major engines.** Keep the provider contract, aggregation and test structure solid; implement the major general-purpose engines first.
2. **First 5–10 adapters.** Initial target met: ten adapters are implemented and tested. Continue expanding only where a provider adds real value.
3. **SearXNG-compatible JSON API.** Initial response-compatible GET/POST surface is working; expand compatibility only where the provider model supports it.
4. **Browser interface and administration.** Initial search and read-only diagnostics pages are working. Next, add safe provider configuration management and richer operational diagnostics.
5. **Operational hardening.** Add SQL Server-backed configuration where useful, API-key authentication, quotas, rate limiting, metrics and deployment guidance before public exposure.

The exact provider access method may affect the order within the first stage. Reliability, legality, maintainability and real-world availability take precedence over simply matching an upstream engine count.

## Security and privacy

This service is designed to be self-hosted, but self-hosting does not automatically make an instance safe.

- Do not expose an unauthenticated instance to the public internet.
- Authentication, rate limits, quotas and request limits must be implemented before untrusted public access.
- Provider endpoints are administrative configuration, never arbitrary URLs supplied by search callers.
- Configurable outbound requests must defend against SSRF: validate schemes and destinations, block loopback/link-local/private ranges where appropriate, revalidate redirects and apply outbound network controls.
- Do not forward caller IP addresses, API keys or unrelated inbound headers to search providers.
- Keep provider credentials out of source control and ordinary provider-definition rows.
- Follow each upstream provider's terms, rate limits, attribution and data-use requirements.

## Contributing

Contributions are welcome, particularly from C# developers interested in search integrations.

Good first contributions include:

- Provider adapters with focused tests.
- Test fixtures for real-world response formats (with secrets and personal data removed).
- Improvements to result normalisation and ranking.
- Documentation of provider access requirements and limitations.
- Deployment and operational guidance for Windows Server and IIS.

Before opening a pull request:

1. Keep changes focused and source files small.
2. Add tests for new behaviour and failure cases.
3. Run dotnet build and dotnet test.
4. Update the provider status and relevant documentation.
5. Never commit API keys, tokens or private response data.

See [activity/STATUS.md](activity/STATUS.md) for the current handover and [activity/LOG.md](activity/LOG.md) for the development diary.

## Project diary

Material implementation actions, test outcomes, decisions and handovers are recorded in the activity folder. This keeps the project history understandable even when development moves between sessions.

## Relationship to SearXNG and licence

DotNetWebSearchApi is an independent .NET implementation inspired by the architecture and provider ecosystem of [SearXNG](https://github.com/searxng/searxng). It is not a complete port and does not claim feature parity. Upstream provenance and attribution are described in [UPSTREAM.md](UPSTREAM.md).

This repository is licensed under **GNU Affero General Public License v3.0 or later (AGPL-3.0-or-later)**. Review the licence and relevant upstream/provider terms before redistributing or deploying modified versions as a network service. This is project documentation, not legal advice.
