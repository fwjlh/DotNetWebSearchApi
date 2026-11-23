# DotNetWebSearchApi

[![Build and test](https://github.com/fwjlh/DotNetWebSearchApi/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/fwjlh/DotNetWebSearchApi/actions/workflows/ci.yml)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/)

**A self-hosted metasearch API, written in native C# and ASP.NET Core.**

DotNetWebSearchApi brings the useful metasearch ideas behind [SearXNG](https://github.com/searxng/searxng) into a straightforward .NET deployment. It queries multiple independent search providers, combines and normalises their results, and gives applications one API that they can host and control.

The goal is not to build another search index. It is to provide a practical, extensible aggregation layer that can run on infrastructure you already operate: particularly Windows Server, IIS and SQL Server environments.

> **Project status: shared-framework refactoring.** Sixty-seven provider adapters are implemented and fixture/CI-tested. Provider expansion is frozen while the request context, engine capabilities, provider health, shared transport, challenge classification and result aggregation are generalised. The service remains independent; CoderInBox is its first major consumer, not a dependency. See [the architecture decisions](docs/ARCHITECTURE_DECISIONS.md) and [the porting audit](docs/PORTING_AUDIT.md).

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
| Request capabilities | Initial | Pre-flight support checks; Kagi is the first provider with explicit page/locale/safe-search/time-range support |
| Result ranking | Initial | Provider positions and weighted score; current engine weights default to 1.0 |
| Parallel provider execution | Working | Providers run concurrently |
| Timeout and failure isolation | Working | Partial results can survive provider failures; typed outcomes distinguish failure classes |
| Provider health | Initial implementation | Cooldown/backoff and one half-open recovery probe; process-local state |
| Shared HTTP transport | Initial implementation | Main provider paths use bounded responses, per-host concurrency and Retry-After capture; cookies/redirects remain provider-specific |
| Result normalisation | Working | HTTP(S) URLs, deduplication, contributing engines, per-engine positions and deterministic ordering |
| Result limit | Working | Default 10; maximum 50 |
| Wikipedia adapter | Working | MediaWiki search API |
| Bing Web adapter | Working | HTML result parsing; live availability not tested in CI |
| Brave Search API adapter | Working (optional) | Requires BRAVE_SEARCH_API_KEY |
| Google Web adapter | Working (HTML) | CAPTCHA/blocking remains an upstream risk |
| DuckDuckGo Web adapter | Working (first page) | HTML POST; CAPTCHA detection; no pagination/VQD |
| Qwant Web adapter | Working (experimental) | Undocumented JSON API; DataDome may deny requests |
| Yahoo Web adapter | Working (experimental) | HTML parser, sB preferences and bounded redirect handling; YBV cache is not implemented |
| Yandex Cloud Search API adapter | Working (optional, account-based) | Requires YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID; not the same route as SearXNG's public HTML scraper |
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
| Wiby | Working (JSON API) | Independent small-web index; includes required Wiby attribution |
| TVmaze | Working (JSON API) | Television show search |
| Radio Browser | Working (JSON API) | Internet radio station search; free API, best-effort service |
| PeerTube | Working (JSON API) | Video search on the configured public instance |
| Wikimedia Commons | Working (JSON API) | MediaWiki file namespace search |
| Wikinews | Working (JSON API) | English-language news archive search |
| Spanish Wikipedia | Working (JSON API) | Regional MediaWiki search |
| Openverse | Working (JSON API) | Openly licensed image search; anonymous API rate limits apply |
| Mwmbl | Implemented (JSON API) | Independent web index; throttled to its documented anonymous request rate |
| Marginalia Search | Implemented (JSON API) | Independent small-web index; public API key may be rate limited |
| Yep | Implemented (JSON API) | Yep search API; requires YEP_API_KEY |
| Kagi | Implemented (dedicated API provider) | General search with page, locale, safe-search and time-range support; requires KAGI_API_KEY and paid API balance |
| Exa | Implemented (JSON API) | Search API; requires EXA_API_KEY and account credits |
| Google News | Implemented (RSS) | UK-edition news search feed; undocumented feed contract |
| Bing News | Implemented (RSS) | UK-edition news search feed; publisher links are unwrapped |
| Wiktionary | Implemented (JSON API) | English dictionary and etymology search |
| Automated build and tests | Locally verified | .NET 10 / xUnit; GitHub Actions disabled by repository owner |
| Major web-search adapters | Initial wave complete | Bing, Brave, Google, DuckDuckGo and other priority providers are implemented |
| Provider catalogue | Frozen | Sixty-seven adapters are implemented; no new ports until shared-framework work is complete |
| SearXNG-compatible JSON API | Initial surface working | JSON response and engine selection; unsupported features return explicit status codes |
| Browser search interface | Working (initial) | Search page at / with selectable providers and result cards |
| Read-only administration and diagnostics | Working (initial) | /admin, provider health counters, last outcome and active provider list |
| SQL Server configuration store | Planned | Optional persistence and administration |
| API keys, quotas and rate limiting | Planned | Required before untrusted public exposure |

The status above is deliberately conservative: a provider is only marked **Working** once its adapter is implemented and tested. Planned integrations are not represented as available features.

## Search provider roadmap

The provider catalogue is currently frozen at **67 adapters**. No additional provider ports are planned until the shared search framework has been generalised and the existing catalogue has been reviewed against the common contracts.

The initial major-engine wave includes Bing, Brave, Google, DuckDuckGo, Wikipedia, Mojeek, Startpage (deferred), Qwant, Yahoo, Yandex, Naver and Seznam. This is a record of the original porting scope, not an active queue; provider work is paused while shared capabilities and infrastructure are generalised.

The full provider inventory, implementation status, API-key requirements, rate limits and wave-by-wave progress are maintained in [docs/PROVIDERS.md](docs/PROVIDERS.md). The reasoning behind how upstream Python/SearXNG engine behaviour maps to each C# implementation is recorded in the [Python-to-C# porting rationale corpus](docs/PYTHON_TO_CSHARP_PORTING_NOTES.md); the [porting audit](docs/PORTING_AUDIT.md) tracks shared-framework gaps.



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

The `format` parameter defaults to JSON; `format=html` is not supported. The `engines` parameter accepts a comma-separated list of configured provider names, for example `engines=bing,google`. Unknown engines return HTTP 400. Both `/search` and `/api/search` accept request context: `categories`, `pageno` (1–50), `language`, `safesearch` (0–2) and `time_range` (`day`, `week`, `month`, `year`). Invalid values return HTTP 400. Providers are pre-filtered against declared capabilities; unsupported providers are skipped rather than silently ignoring requested options. The SearXNG-shaped response reports these separately in `unsupported_engines`; `/api/search` reports `unsupportedProviders`. At this stage, providers without explicit capability declarations receive a conservative general-category, first-page, default-locale profile, so non-default request options may legitimately produce no eligible providers. The query model and API now carry these values. Kagi currently declares page/safe-search/locale/time-range support; Bing declares safe-search and a curated locale map; DuckDuckGo declares locale and time-range support; Wikipedia declares locale support and uses a mapped wiki host plus Accept-Language. Google declares locale, safe-search, time-range and paging through page 50. Qwant declares locale, safe-search and paging through page 5. Yahoo declares locale, safe-search, paging and Day/Week/Month time ranges. Naver declares paging and all four time ranges using its fixed 15-result general page size. Seznam remains first-page-only with its two-step hidden-form and browser-verification flow. Other providers retain the conservative default until their request builders genuinely consume those options. Other providers retain the conservative default until their request builders genuinely consume those options. See [locale traits notes](docs/PORTING/LOCALE_TRAITS.md).

Provider discovery and read-only diagnostics are also available at GET /api/providers and GET /api/diagnostics. Search responses include typed provider outcomes, including failure kind and challenge subtype, while diagnostics retain the last challenge subtype. This is classification/observability only; interactive CAPTCHA and human verification are not automatically solved or bypassed. Diagnostics include provider health state (`Closed`, `Open`, `HalfOpen`), consecutive failures and cooldown expiry alongside success/failure counters. The /admin page displays provider status, success/failure counts, last duration and last error type. These are process-local counters/state and reset when the service restarts.

The response uses the familiar SearXNG field names: `query`, `number_of_results`, `results`, `answers`, `corrections`, `infoboxes`, `suggestions` and `unresponsive_engines`, plus additive `unsupported_engines` and `cooling_down_engines` lists. Result entries include `url`, `title`, `content`, `engine`, `engines`, `positions`, `score`, `category`, `template`, `parsed_url` and `publishedDate`. Empty answer/infobox/suggestion arrays are returned until those result types are implemented. `positions` is taken from each provider's returned order (one-based) and duplicate URLs preserve all contributing engines and their first positions. The merged score now uses the SearXNG-style weighted position formula; adapter score selects the primary display record. Provider weights currently default to 1.0, and high/low priority modes plus rich-result merging are not yet implemented. This is response-shape compatibility, not full SearXNG feature parity.

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

To measure line and branch coverage locally:

    dotnet test .\\DotNetWebSearchApi.sln -c Release --collect:\"XPlat Code Coverage\"

The Coverlet collector writes a Cobertura report beneath `tests/DotNetWebSearchApi.Tests/TestResults`. Use the report to target uncovered logic and branches; a passing test count alone is not a coverage measure.

GitHub Actions workflows are intentionally disabled to conserve minutes. Run restore, build and tests locally using the commands above.

The last fully verified provider-catalogue baseline passed CI with **156 tests**, 0 failures and 0 skipped, with 0 build warnings and 0 errors ([run 45020266512](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45020266512)). This predates the current request-context/capability changes; those newer commits are awaiting CI verification. The 156-test result must not be treated as proof that the latest code is green.

## Planned delivery sequence

Provider-count expansion is frozen. The active sequence is:

1. **Provider capability declarations.** API request context and pre-flight filtering are now implemented; declare only the features each C# request builder genuinely supports, with fixture coverage.
2. **Typed outcomes and provider health.** Initial classification and process-local cooldown/half-open recovery are implemented; refine provider-specific policies and persistence only where deployment needs it.
3. **Shared transport and sessions.** Main provider paths now use the bounded transport; Mojeek and Yahoo cookie stores are separated and Seznam keeps its own handler jar. Finish per-caller session isolation, full provider header profiles, redirect policy and safe retries; default headers, privacy-safe transport diagnostics and provider-keyed minimum intervals are now centralised.
4. **Challenge handling.** Distinguish blocks, rate limits, browser verification, permitted proof-of-work and human CAPTCHA; AI may classify challenges but must not bypass them.
5. **Result aggregation and traits.** Retain contributing engines/positions, improve weighted ranking and add locale/region mappings.
6. **API and operational hardening.** Define compatibility tests, then authentication, quotas, rate limiting and outbound request controls before public exposure.
7. **Consumer integration.** Add a general HTTP search client to CoderInBox while keeping DotNetWebSearchApi independent and WebView2 optional.

See [docs/ARCHITECTURE_DECISIONS.md](docs/ARCHITECTURE_DECISIONS.md) for the consumer boundary and definition of done.

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
