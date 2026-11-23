# Current handover status

Updated: 2026-11-23

## Repository
- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest code/test commit: `16549750308dcdf759c32dd077d90fa35c88fe0d` (Yandex Cloud capability declaration and pre-flight tests; build-and-test passed in [run 45144264756](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144264756)). Latest porting-corpus update: `2577287a5860ec87cac396ea89ae7cb0f7d65b95` (Yandex capability boundaries); activity log update: `1c41a7619bb593d58c375b3ccbd5de11d5bb8a0b`.
- Current provider catalogue: **67 adapters, frozen** pending shared-framework refactoring.
- Previous provider-catalogue baseline: build 0 warnings/errors; 156 tests passed (0 failed, 0 skipped), run [45020266512](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45020266512). The subsequent request-context/capability code has now passed its GitHub Actions build-and-test job in [run 45022944719](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45022944719); the workflow job reports success.
- Current direction and consumer boundary: [docs/ARCHITECTURE_DECISIONS.md](../docs/ARCHITECTURE_DECISIONS.md).
- Python-to-C# rationale corpus: [docs/PYTHON_TO_CSHARP_PORTING_NOTES.md](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md), with focused shared-framework notes in [docs/PORTING](../docs/PORTING). It now covers Bing, DuckDuckGo, Brave, Mojeek, Google, Qwant, Yahoo, Yandex, Naver, Seznam, Wikipedia, generic JSON APIs and RSS/Atom, plus shared request-context/capability translation and typed exception/outcome mapping against pinned SearXNG source revision `c6ee3e9fd0235004a694e63720efc869bb1e3594` where a direct upstream engine exists.
- Naver general-web results now preserve non-circular thumbnail URLs in `SearchResult.ThumbnailUrl` and SearXNG `thumbnail`; non-HTTP thumbnail values are rejected. Model/parser tests passed in [run 45143993872](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45143993872), and response mapping passed in [run 45144068228](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144068228). No live Naver requests.
- Yandex Cloud explicitly declares general/web, first-page-only capability with no request-level locale, safe-search or time-range support; unsupported options are pre-flight rejected. Build/test passed in [run 45144264756](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144264756). This remains a distinct authenticated API integration, not a port of SearXNG's public HTML engine.
- Added a capturing-provider integration test proving `/search` passes categories, page, language, safe-search and time range intact into `SearchQuery`; CI passed in [run 45144835861](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144835861).
- RSS provider now has Atom feed parsing coverage and an external-DTD rejection test; CI passed in [run 45145077265](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145077265). No live feed requests.
- Caller cancellation now has a health-regression assertion: it propagates without incrementing provider failures or opening cooldown. CI passed in [run 45145286054](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145286054).
- `docs/PORTING_AUDIT.md` was reconciled with the implemented architecture; it now separates completed foundations from process-local/session limitations, rich-result gaps and pre-public-exposure security work. CI passed in [run 45145666616](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145666616).
- P0 request context now flows from `/api/search` and SearXNG-shaped `/search` into `SearchQuery`: categories, page, language, safe-search and time range. Invalid values are rejected; unsupported providers are skipped and reported separately (`unsupportedProviders` / `unsupported_engines`), not marked unresponsive. Conservative legacy-provider capabilities remain general/first-page/default-locale. Integration tests added; CI passed with 160 tests in run 45024120851.
- Typed provider outcomes distinguish success, failure and skipped; failure kinds include timeout, rate limit, transient HTTP, invalid response, authentication, access denied, challenge, configuration and unknown. Explicit provider failures can carry RetryAfter. Caller cancellation still propagates.
- Challenge subtypes distinguish temporary block, browser verification, proof-of-work, interactive CAPTCHA, interactive verification, login-required and unknown. Current detectors map Google/DuckDuckGo/Yandex CAPTCHA, Qwant DataDome, Seznam verification and Mojeek Altcha/fallback CAPTCHA. `/api/search` and `/search` expose typed outcomes; health snapshots retain the last challenge subtype. CI passed in [run 45028609771](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45028609771). This does not automate CAPTCHA solving or bypass.
- `EngineLocaleRegistry` now provides static, versioned Bing and DuckDuckGo region maps. Bing maps locale to `setlang`/`cc`, preserves `zh-HK` → `en-HK`, maps safe-search 0/1/2, and declares only general/web, locale and safe-search support. DuckDuckGo maps locale to `kl` and time-range to `df`, including matching request cookies; it declares locale/time-range but not paging/safe-search. Both remain first-page-only. CI passed for Bing in [run 45028984202](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45028984202) and DuckDuckGo in [run 45029215535](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45029215535).
- ProviderHealthRegistry now adds process-local Closed/Open/HalfOpen states, failure thresholds, failure-kind cooldowns, capped exponential backoff and one half-open recovery probe. Cooling-down providers are skipped and reported separately; diagnostics expose health snapshots. CI passed with 174 tests in run 45025031994. Policy is global and resets on process restart.
- Shared HTTP transport now covers Bing, Brave, DuckDuckGo, Google, Mojeek, Naver, Qwant, Seznam, Yahoo, Yandex Cloud, Wikipedia, arXiv, generic JSON and RSS/Atom paths. It enforces a 4 MiB response limit, per-host concurrency, response-header capture and Retry-After. CI passed with 182 tests in run 45026001326.
- Mojeek and Yahoo now have separate keyed CookieContainers; Seznam retains its own typed-client handler jar. These are provider-process sessions, not per-user sessions. Added DI/integration coverage and Qwant DataDome challenge classification. CI passed with 184 tests in run 45026298077.
- Generic JSON provider minimum intervals have moved from JsonApiSearchProvider's local semaphore/clock into SearchHttpTransport's provider-keyed rate gate. CI passed with 185 tests in run 45026506065.
- Shared transport now adds fallback User-Agent and Accept headers only when neither the request nor HttpClient supplies them. Debug/warning diagnostics record method, host, status, byte count, elapsed time and exception type without URL query strings, bodies, cookies or tokens. Tests passed with 187 tests in run 45026662957.
- SearchResult now carries one-based provider position, contributing engine names and per-engine positions. SearchAggregator assigns position from provider result order, merges provenance for duplicate URLs, and SearXNG mapper emits actual positions instead of inferring them from score. Ranking now follows the SearXNG weighted-position formula; adapter score selects the primary display record. Engine weights are configurable through EngineCapabilities.Weight but all current providers still default to 1.0. CI passed with 188 tests in run 45027513125.
- Kagi was extracted from JsonApiProviderCatalog into dedicated KagiSearchProvider. It supports general search, page 1–10, locale region, safe-search and time-range filters; other verticals remain omitted. It is the first production provider to declare a non-default capability profile. Exa was retained in the generic JSON catalogue. The generic JSON and RSS factories now inject the singleton shared transport instead of constructing isolated transports. CI passed with 190 tests in run 45028349848.
- Project overview and roadmap: [README.md](../README.md)

## Working application
- Native ASP.NET Core 10 Web API; no Python or Docker runtime.
- GET /health and GET /api/search?q=...
- Initial SearXNG-shaped JSON API: GET /search, GET / and form POST /search.
- Browser search page at root; provider selection and result cards.
- Read-only diagnostics at /admin.
- GET /api/providers and GET /api/diagnostics.
- Parallel provider execution, per-provider timeout and failure isolation.
- Provider failures surfaced as unresponsive_engines.
- HTTP(S)-only result URL validation, deduplication, deterministic ordering and result limiting.
- Optional Brave and Yandex providers require their configured credentials.

## Provider count and current batch
- **67 provider adapters** are registered in the current catalogue. This count is frozen; the dated batch entries below are historical milestones, not the current provider count.
- The first 42 providers passed CI fixture tests in run [45013727838](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013727838): build passed with 0 warnings/errors; 130 tests passed.
- Batch 5 and the full 50-provider catalogue passed CI in run [45019256660](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019256660): build passed with 0 warnings/errors; 138 tests passed.
- Batch 6 and the full 58-provider source catalogue passed CI in run [45019669530](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019669530): build passed with 0 warnings/errors; 146 tests passed.
- Mojeek and the 59-provider catalogue passed CI in run [45019896375](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019896375): build passed with 0 warnings/errors; 148 tests passed.
- Batch 7 and the 67-provider catalogue passed CI in run [45020266512](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45020266512): build passed with 0 warnings/errors; 156 tests passed.
- Architecture audit added at [docs/PORTING_AUDIT.md](../docs/PORTING_AUDIT.md). No code changed in the audit.
- Rationale corpus records preserved behaviour, deliberate alternatives, omissions and known divergences; it explicitly avoids claiming full Python feature parity. The typed-outcome rationale passed CI in [run 45024568368](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024568368); the latest porting-audit reconciliation is running in [run 45024625117](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024625117).
- Batch 7 adds eight scholarly and open-knowledge providers with eight fixtures. CI passed: build 0 warnings/errors; 156 tests passed (0 failed, 0 skipped).
- Mojeek is implemented with bounded Altcha proof-of-work; fixture tests do not establish live challenge acceptance.
- Batch 6 adds eight adapters and eight fixture tests. CI passed: build 0 warnings/errors; 146 tests passed (0 failed, 0 skipped).
- Batch 5 adds Wiby, TVmaze, Radio Browser, PeerTube, Wikimedia Commons, Wikinews, Spanish Wikipedia and Openverse. All eight fixture cases passed in CI.
- No new live upstream API requests have been run. Fixture tests verify request construction and response mapping only.

## Provider inventory
Original general web adapters: Bing, Brave Search API, Google, DuckDuckGo, Wikipedia, Qwant, Yahoo, Yandex Search API, Naver and Seznam.

Specialist providers (batch 1): Crossref, OpenAlex, Hacker News, Stack Overflow, npm, crates.io, NuGet and Open Library.

Specialist providers (batch 2): Europe PMC, Semantic Scholar, GitHub repository search, Docker Hub, Google Books, Internet Archive, RubyGems and Hugging Face Models.

Developer/package providers (batch 3): Super User, Server Fault, Ask Ubuntu, GitLab Projects, Gitea, Maven Central, Packagist and pub.dev.

Media/knowledge/academic providers (batch 4): arXiv, Bilibili, Dailymotion, Deezer, Steam, MusicBrainz, Wikidata and German Wikipedia.

Search/media/regional providers (batch 5): Wiby, TVmaze, Radio Browser, PeerTube, Wikimedia Commons, Wikinews, Spanish Wikipedia and Openverse.

## Important access and quality limits
- Bing, Google, DuckDuckGo, Qwant, Yahoo, Naver and Seznam are HTML/scrape-based or undocumented paths; CAPTCHA, bot blocking, cookies, locale and egress IP can affect availability.
- Brave is optional and requires BRAVE_SEARCH_API_KEY.
- Yandex is optional and requires YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID.
- Stack Exchange quotas, GitHub unauthenticated API limits, Semantic Scholar rate limits, Radio Browser best-effort availability and Openverse anonymous limits apply.
- PeerTube currently searches the configured public instance and its local scope; it is not a global PeerTube index.
- Wiby attribution is appended to returned snippets to satisfy its requirement to link to Wiby.
- Openverse results link to the media landing page, not a direct image download.
- No adapter should be described as live-tested based only on fixture tests.

## SearXNG compatibility
- GET /search?q=...&format=json.
- GET /?q=...&format=json when query present; bare / serves browser page.
- POST /search using application/x-www-form-urlencoded.
- Engine selection via engines=provider1,provider2.
- Response includes query, number_of_results, results, answers, corrections, infoboxes, suggestions and unresponsive_engines.
- Initial compatibility only: JSON, general category, first page; no full language/safe-search/time-range support, no answer/infobox/suggestion generation, inferred positions and no merged engine positions for duplicate URLs.

## Verification history
- Latest fully verified batch before batch 5: commit 92a34b06ec137dfea1625af2d881803c90bc3045, CI run 45013727838, build 0 warnings/errors, 130 tests passed.
- Local .NET SDK is unavailable in the orchestration environment; GitHub Actions is the build/test verification source. Latest code run 45024534370 reports build success and 167 passed, 0 failed, 0 skipped. The latest porting-audit documentation run is in progress.
- Batch 5 verification is pending; do not count its eight providers as CI-verified until the workflow succeeds.

## Working rules
1. Record every material action in activity/LOG.md and update this handover.
2. Keep implementation, fixtures, README and activity updates in the same commit.
3. Use the current main commit's tree SHA as the base for every Git tree; never reuse stale tree SHAs.
4. Keep source files preferably below 15–20 KB, hard maximum 25 KB.
5. Small single-purpose functions, max four parameters, early returns, no dead code.
6. Never claim live upstream acceptance from fixture-only tests.
7. Push each increment to main and inspect the resulting GitHub Actions run before continuing.

## Next actions
1. Keep provider expansion frozen at 67; no new adapters during shared-framework completion.
2. Audit the remaining adapters for request fields silently ignored by the current implementation; add truthful capability declarations and one focused test per small change.
3. Expand locale/region traits only where the pinned Python source and C# request behaviour justify the mapping.
4. Plan a richer result model for images/news/videos so vertical-specific metadata is not flattened into ordinary web links.
5. Complete API compatibility tests, then harden the public API before internet exposure: authentication, quotas, request limits and outbound destination policy.
6. Keep CoderInBox as an independent HTTP consumer; do not move provider code into that repository.
7. Resume provider additions only when the shared-framework definition of done is met.


## 2026-11-23 handover — Actions disabled
- Repository rules: see [`_LookAtMe.MD`](../_LookAtMe.MD).
- GitHub Actions: disabled by deleting `.github/workflows/ci.yml`; there are now no workflow definitions in this repository. Do not recreate or invoke workflows.
- Branch: continue committing directly to `main`.
- Activity: latest policy/configuration change is recorded in [`LOG.md`](LOG.md), entry 115.
- Verification: no application code changed in this policy update. Local tests could not be run in this environment because the .NET SDK is unavailable. Do not substitute GitHub Actions.
- Next work: keep the 67-provider catalogue frozen; continue auditing provider request/capability gaps and the shared-framework completion plan. Each code increment must include focused tests and be locally verified when a .NET SDK is available.


## Latest development — 2026-11-23
- Openverse now declares `images` capability instead of inheriting the generic `general` capability. Generic JSON providers remain general-web by default.
- `JsonApiSearchProvider` exposes definition categories through `ISearchProviderCapabilities`.
- Added two aggregator tests: Openverse is skipped for general search and selected for image search.
- Commit: `b2dc9fd299dfc4eb6b7ed3034e69b54262c8c2fa`.
- Tests are committed but unexecuted: this environment has no .NET SDK. GitHub Actions is disabled and must not be used.
- The action and rationale are recorded in [LOG.md](LOG.md), entry 116.

## Next
1. Continue the category audit of generic JSON adapters: videos, news, audio and other specialist verticals.
2. For each provider, use its actual endpoint and response shape as evidence; do not classify based on provider name alone.
3. Add a focused aggregator regression test with each capability change.
4. Keep the 67-provider catalogue frozen until the shared-framework definition of done is met.
5. Do not recreate or run GitHub Actions. Run `dotnet test` locally when an SDK-equipped environment is available.


## Latest development — specialist provider categories
- Openverse and Wikimedia Commons declare `images`.
- Bilibili, Dailymotion and PeerTube declare `videos`.
- Deezer, MusicBrainz and RadioBrowser declare `music`.
- Wikinews declares `news`.
- Generic JSON definitions without an explicit vertical remain `general`.
- Added a nine-case capability theory covering these declarations.
- Commit: `c1f81202bde134ba51dba961d3ebd006dec63cba`.
- Tests remain unexecuted because this environment has no .NET SDK. GitHub Actions remains disabled.
- Full rationale and validation limits are recorded in [LOG.md](LOG.md), entry 117.

## Next
1. Continue auditing specialist endpoints and their category semantics; do not infer ambiguous classifications.
2. Check whether the API's response model and SearXNG mapper preserve the requested category rather than labelling every result as general.
3. Keep the 67-provider catalogue frozen and the work on `main`.

## Latest development — 2026-11-23 (continued)
- Added the inverse category-boundary regression: Crossref is skipped for image-only searches and the test asserts no outbound request is made.
- Test commit: `bfee32d6c04a0a5178f71baacc26d0b3f6ea277b`.
- Activity record: [general-provider vertical regression](2026-11-23-general-provider-vertical-regression.md).
- Test is committed but not executed because the available environment has no .NET SDK. No GitHub Actions workflow was invoked.
- The test is a regression for request pre-flight filtering, not a claim that Crossref is live-tested.

## Next
1. Audit media-specialist providers individually before assigning vertical capabilities.
2. Add paired aggregator tests proving supported categories are selected and unsupported categories are skipped before HTTP.
3. Keep provider count frozen at 67; do not add providers while shared framework work remains.
4. Keep all work on `main`, and never use GitHub Actions.

## Latest development — specialist category response mapping
- Fixed the response-model gap found during the category audit: specialist results no longer emerge from the SearXNG mapper labelled `general`.
- `SearchResult.Category` defaults to `general`; generic JSON adapters populate it from their declared category; SearXNG mapping preserves it.
- Added a Bilibili fixture regression covering provider parsing through SearXNG mapping.
- Activity record: [specialist category response mapping](2026-11-23-specialist-category-response-mapping.md).
- Source/test commits: `2f822c38`, `e19be5fa`, `9dacb6a1`, `d6de9de0`, `b8ba727a`.
- Local execution attempted; `dotnet` is not installed. No GitHub Actions were used. The test is committed but unexecuted.

## Next
1. Design typed rich-result metadata for image, news and video results; avoid untyped extension dictionaries.
2. Define compatibility behaviour for fields absent from ordinary web results.
3. Add model and API serialization tests before changing provider-specific parsers.
4. Keep the catalogue at 67 and continue direct commits to `main`; no workflows.


## Follow-up — category fixture correction
- Corrected the Bilibili JSON fixture's C# escaping in commit `982c14c4d7e0a70a450ff367d7fb00b21f6616ee`.
- Updated the detailed activity record and appended LOG entry 118.
- The test remains unexecuted: no .NET SDK is available. Do not use GitHub Actions.
- Next remains typed rich-result metadata design for images, news and videos; keep the 67-provider catalogue frozen.

## Latest development — RSS news metadata
- RSS/Atom providers now declare `news` capability and no longer inherit general-web capability.
- RSS `pubDate` and Atom `published` / `updated` are parsed into `SearchResult.PublishedDate`; SearXNG output preserves `publishedDate` and the `news` category.
- Added tests for news-only capability, RSS and Atom date parsing, and SearXNG mapping.
- Updated [porting audit](../docs/PORTING_AUDIT.md) and [RSS/Atom rationale](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md).
- Activity record: [news result metadata](2026-11-23-news-result-metadata.md).
- Tests are committed but unexecuted: `dotnet` is unavailable in this environment. No GitHub Actions were used.

## Next
1. Continue the typed result model using the pinned SearXNG result types as evidence.
2. Implement image-specific metadata first, then video embed metadata, with bounded HTTP(S) URL validation.
3. Add provider fixture and SearXNG serialization tests before changing adapters.
4. Keep the provider catalogue frozen at 67 and continue direct commits to `main`.

## Latest development — typed image result model
- Added typed `ImageResultMetadata` and `ImageFormatReference` records, attached them to `SearchResult`, and exposed SearXNG image fields (`img_src`, `thumbnail_src`, `resolution`, `img_format`, `source`, `filesize`, `formats`).
- Licence, licence URL, creator and byte size are retained in the explicit `image_metadata` extension; no untyped dictionary was introduced.
- Added a serialization/mapping test for SearXNG field names and typed metadata.
- Updated [porting audit](../docs/PORTING_AUDIT.md).
- Activity record: [typed image result model](2026-11-23-typed-image-result-model.md).
- Tests are committed but unexecuted because `dotnet` is not installed. No GitHub Actions were used.
- No provider parser populates the new model yet; URL validation is the next prerequisite.

## Next
1. Add strict absolute HTTP(S) validation for image source, thumbnail and alternate-format URLs.
2. Wire Openverse response fields into the typed image model with fixture tests.
3. Add a regression for rejecting javascript:, data: and relative image URLs.
4. Keep provider count frozen at 67 and all work on `main`; no workflows.


## Latest development — typed image result metadata foundation
- Added typed `ImageResultMetadata` and `ImageFormatReference` models to preserve image URL, preview, resolution, format, source, byte/display file sizes, alternate formats, licence and creator.
- SearXNG mapping exposes the image-specific fields and selects `images.html` for image results; ordinary results keep `default.html`.
- Added serialization/mapping tests for image metadata and both template paths.
- Activity record: [typed image result metadata](2026-11-23-typed-image-result-model.md).
- Commits include `9cf0be75` through `227231fa`; full details in [LOG.md](LOG.md), entry 120.
- Tests remain unexecuted because the .NET SDK is unavailable. GitHub Actions remains disabled.
- Next: add strict HTTP(S) validation for image source, thumbnail and alternate formats, then map Openverse response fields with fixture tests. Keep provider count frozen at 67.

## Latest development — Openverse image metadata
- Openverse now populates the typed image model: direct image URL, thumbnail, dimensions, format, source, file size, licence, creator, provider and attribution.
- The clickable result URL remains the Openverse foreign landing page.
- Image, thumbnail, licence and creator URLs are accepted only as absolute HTTP(S); negative file sizes are discarded.
- Added fixtures for metadata extraction and rejection of javascript:, data: and relative URLs.
- Updated [porting rationale](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md) and [architecture audit](../docs/PORTING_AUDIT.md).
- Activity record: [Openverse image metadata](2026-11-23-openverse-image-metadata.md).
- Tests remain unexecuted because `dotnet` is unavailable. No GitHub Actions or live upstream requests were used.

## Next
1. Add typed video metadata, starting with embed URL and thumbnail handling.
2. Require explicit HTTP(S) validation for embed URLs; do not embed arbitrary destinations.
3. Add a focused fixture for one video provider and a serialization test.
4. Keep provider count frozen at 67; continue small commits on `main`.

## Latest development — Dailymotion video metadata
- Added typed `VideoResultMetadata` and Dailymotion mappings for embed URL, thumbnail, duration and upload timestamp.
- SearXNG mapping now selects `videos.html` and emits `iframe_src`, `length`, `thumbnail` and `publishedDate`.
- Embed and thumbnail URLs are restricted to absolute HTTP(S); invalid Unix timestamps are ignored.
- Added fixture tests for video metadata, mapping, date conversion and unsafe URL rejection. Image results now also assert `images.html`.
- Updated [porting rationale](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md) and [architecture audit](../docs/PORTING_AUDIT.md).
- Activity record: [Dailymotion video metadata](2026-11-23-dailymotion-video-metadata.md).
- Tests remain unexecuted because `dotnet` is not installed. No GitHub Actions or live provider requests were used.
- Dailymotion requires a Player ID context for a functional embed; configuration is not implemented yet.

## Next
1. Add optional Dailymotion Player ID configuration and only claim functional embeds when configured.
2. Audit Bilibili and PeerTube metadata fields against their documented response formats.
3. Add fixture tests for each supported mapping and reject unsafe embed/thumbnail URLs.
4. Keep provider count frozen at 67 and continue direct commits to `main`; no workflows.

## Latest development — Bilibili and PeerTube video metadata
- Bilibili now maps thumbnail, author, views and Unix upload date. Duration is intentionally omitted until the provider's string format is unambiguous.
- PeerTube now maps same-origin embed path, current thumbnail collection, duration, author, views and publication date.
- Embed URLs require HTTPS and either the result origin or an explicit provider-host allowlist. Dailymotion's documented embed host is allowed; cross-origin relative paths, foreign hosts and HTTP embeds are rejected.
- Added provider fixture tests for metadata and URL safety.
- Updated [provider rationale](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md) and [porting audit](../docs/PORTING_AUDIT.md).
- Activity record: [Bilibili and PeerTube video metadata](2026-11-23-bilibili-peertube-video-metadata.md).
- Tests remain unexecuted because `dotnet` is not installed. No GitHub Actions or live upstream requests were used.

## Next
1. Audit all provider capability declarations against their response types and categories.
2. Check SearXNG templates are emitted only when the corresponding result type is populated.
3. Add provider registration/query-selection tests without live network calls.
4. Keep provider count frozen at 67 and continue direct commits to `main`; no workflows.


## Latest development — JSON provider catalogue file-size refactor
- Split the oversized JSON catalogue into base, media and tail definition groups.
- `JsonApiProviderCatalog.cs` is now 12,634 bytes; `JsonApiProviderCatalog.Media.cs` is 13,443 bytes.
- Added a regression asserting 52 unique JSON definitions and preserved media/tail ordering.
- Commit: `dff35fc2ae21255d118ce424db993e59518d0833`.
- Verified the repository still contains no GitHub Actions workflow definitions. Tests remain unexecuted because the .NET SDK is unavailable.
- Rationale and file-size validation are in [LOG.md](LOG.md), entry 121.

## Latest development — capability inventory corrections
- Completed a first pass over all 52 generic JSON definitions.
- Corrected Wikimedia Commons: file-page search is general until direct image URLs/metadata are retrieved. Added tests proving it is skipped for image-only queries before HTTP.
- Corrected Bilibili request settings: required Referer is now sent and unsupported `page_size` removed. Added a definition regression test.
- Updated [provider rationale](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md) and [capability audit](../docs/PORTING_AUDIT.md).
- Activity record: [provider capability audit](2026-11-23-provider-capability-audit.md).
- Generic JSON catalogue remains 52; overall provider count remains 67. No additions in this pass.
- Tests are committed but not executed because `dotnet` is unavailable. No GitHub Actions or live upstream calls were used.

## Next
1. Audit the dedicated search adapters and provider registration for request headers, endpoint parameters, and category declarations.
2. Add provider registration/query-selection tests for any discovered gaps.
3. Recheck SearXNG templates for image/video/music/news result families.
4. Keep provider count frozen at 67; direct commits to `main`, no workflows.


## Latest development — specialist template eligibility
- SearXNG now uses `images.html` only when image metadata includes an absolute HTTP(S) image source.
- SearXNG now uses `videos.html` only when video metadata includes an absolute HTTP(S) embed URL.
- Missing or unsafe vertical metadata falls back to `default.html`.
- Added regression cases for missing metadata, javascript: URLs, relative image URLs and protocol-relative video embeds.
- Commit: `3d911356c7c59858d18ac2d679d823975820d89a`.
- Activity rationale: [LOG.md](LOG.md), entry 122. Tests remain unexecuted because the .NET SDK is unavailable; no workflows were used.

## Repository rules verification
- Confirmed the root `_LookAtMe.MD` already exists in this repository and matches the supplied rules; no duplicate edit was needed.
- Corrected two accidental file additions in `sqlDiffAi` by removing them; verified both paths are absent there.
- Logged the correction as entry 123 in [LOG.md](LOG.md).
- Confirmed this repository has no GitHub Actions workflows and no source files over 25 KB. No workflow was used.

## Next
1. Continue the dedicated provider registration/capability audit.
2. Add local fixture tests for any request/header or category mismatches.
3. Keep provider count at 67 and all work on `fwjlh/DotNetWebSearchApi:main`; no workflows.


## Latest development — provider registration and pre-flight tests
- Added a production-host integration test asserting 67 unique providers register when optional Exa, Yandex, Kagi and Brave keys are supplied as dummy values.
- Added a production-host test proving Openverse is rejected for general search before an upstream request.
- Commit: `ef0cdb3cbdab7f1b42cbed7a79d2ba2d08c8c6df`.
- Activity rationale: [LOG.md](LOG.md), entry 123.
- Tests are committed but unexecuted because no .NET SDK is available. No GitHub Actions were used.

## Dedicated provider registration audit
- Reviewed the 13 dedicated adapters plus RSS against their capability declarations and actual request parameters.
- Added a no-network DI test proving all 52 JSON definitions register when Exa's key is supplied and that Bilibili's Referer is applied to its named client.
- Corrected Bilibili's test to expect `default.html` when no usable embed URL exists; specialist templates now require typed media URLs.
- Logged as entry 124 in [LOG.md](LOG.md).
- Tests remain unexecuted because `dotnet` is not installed. No GitHub Actions used.

## Next
1. Review music/audio and news result metadata against the pinned SearXNG result types.
2. Add typed music metadata only where upstream payloads support it; do not assign audio templates to ordinary links.
3. Add news date/category mapping for JSON news providers where timestamp fields are available.
4. Keep the 67-provider inventory frozen and commit directly to `main`; no workflows.

## Latest development — music/audio metadata
- Added typed `MusicResultMetadata` for audio URL, thumbnail, artist, album, duration, country, tags, codec and bitrate.
- Deezer maps previews and track/album details; Radio Browser maps resolved streams and station metadata. MusicBrainz remains a metadata-only link.
- SearXNG emits `audio_src` and `music_metadata`; music results retain the default template.
- Audio and thumbnail URLs are restricted to absolute HTTP(S).
- Split the generic JSON metadata parser into a partial file to keep both source files below 15 KB.
- Added Deezer/Radio Browser fixtures and unsafe URL tests. Updated [porting rationale](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md) and [audit](../docs/PORTING_AUDIT.md).
- Logged as entry 125 in [LOG.md](LOG.md). Tests remain unexecuted because `dotnet` is unavailable; no GitHub Actions or live requests used.

## Next
1. Add generic JSON publication-date paths and parsing.
2. Map Wikinews timestamps into `SearchResult.PublishedDate` and SearXNG `publishedDate`.
3. Review other news providers for documented timestamps; never synthesize missing dates.
4. Keep 67 providers, small files, direct commits to `main`; no workflows.

## Latest development — Wikinews publication dates
- Added generic JSON `PublishedDatePath` support using invariant ISO/Unix parsing.
- Wikinews now requests the MediaWiki timestamp and preserves it through `SearchResult.PublishedDate` and SearXNG `publishedDate`.
- Missing or malformed timestamps remain unset; no current/crawl time is invented.
- Added fixture tests for valid, missing and malformed dates.
- Updated [provider rationale](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md) and [porting audit](../docs/PORTING_AUDIT.md).
- Logged as entry 126 in [LOG.md](LOG.md). Tests remain unexecuted because `dotnet` is unavailable; no workflows or live requests used.

## Next
1. Audit remaining news providers for documented publication timestamps.
2. Recheck all vertical capability declarations and typed result mappings together.
3. Add tests for any news provider whose category/date metadata does not match its payload.
4. Keep provider count frozen at 67 and continue direct commits to `main`; no workflows.

## Latest development — Hacker News vertical and dates
- Generic JSON providers can now select the category matching the requested vertical when a definition supports multiple categories.
- Hacker News declares both `general` and `news`; general searches remain general, news-only searches select it as news.
- Algolia `created_at` now maps to `SearchResult.PublishedDate` and SearXNG `publishedDate`.
- Added parser and aggregator fixture tests proving category selection and date preservation without network access.
- Updated [provider rationale](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md) and [audit](../docs/PORTING_AUDIT.md).
- Logged as entry 127 in [LOG.md](LOG.md). Tests remain unexecuted because `dotnet` is unavailable; no workflows/live requests used.

## Next
1. Review any remaining JSON news candidates and avoid misclassifying general discussion forums.
2. Verify category selection is deterministic for multiple requested categories.
3. Final source-size, registration-count and no-workflows audit.
4. Keep 67 providers and continue direct commits to `main`; no workflows.

## Latest development — source file-size cleanup
- Split the large generic JSON fixture data and SearXNG request-context integration tests into partial-class files.
- All C# source and test files are now below 20 KB; no file approaches the 25 KB hard limit.
- No test behaviour was intentionally changed by the split.
- Logged as entry 128 in [LOG.md](LOG.md).
- Full test execution remains blocked by the missing .NET SDK; no GitHub Actions used.

## Next
1. Final audit of news candidates, categories and typed result mappings.
2. Verify all 67 providers remain registered under expected optional-key conditions.
3. Final tree check for source sizes, tests, activity handover and absence of workflows.
4. Keep all changes on `main`; never use GitHub Actions.

## Test split correction
- Rechecked the partial-class test split and restored xUnit attributes to all four moved request-context tests.
- Verified the original integration tests retain their own `[Fact]` attributes.
- Logged as entry 129 in [LOG.md](LOG.md).

## Latest dedicated-provider capability corrections
- Mojeek now declares its real general/web and safe-search capabilities and maps 0 to off, 1/2 to moderate.
- Brave now declares safe-search support and maps 0/1/2 to off/moderate/strict; it remains first-page-only with no locale/time-range support.
- Added request and capability tests for both providers.
- Corrected the duplicate/misnumbered Mojeek entry in [LOG.md](LOG.md); new entries are 130 (Mojeek) and 131 (Brave).
- Tests remain unexecuted because `dotnet` is unavailable. No GitHub Actions or live upstream calls used.

## Latest capability audit — safe search and category determinism
- Mojeek and Brave now advertise safe-search support accurately and map the requested levels to their upstream parameters.
- Generic JSON multi-vertical category selection is deterministic: general is preferred when explicitly requested; otherwise matching categories are sorted ordinal-ignore-case.
- Added provider request/capability tests and a Hacker News multi-category regression.
- Logged as entries 130–132 in [LOG.md](LOG.md).
- No workflows or live upstream calls used; tests remain unexecuted because the .NET SDK is unavailable.

## Final audit — verified on 2026-11-23
- Provider inventory: 52 generic JSON definitions + 2 RSS/Atom adapters + 13 dedicated adapters = 67 total.
- C# files: 79 source/test files; none exceeds 20 KB. The largest test fixture is 18.5 KB.
- JSON catalogue split: 12.8 KB base, 14.2 KB media, 5.2 KB Wave7.
- `_LookAtMe.MD`, `activity/LOG.md` and `activity/STATUS.md` are present.
- No `.github/workflows/` files exist. No GitHub Actions have been invoked.
- Test suite has 33 test source files, but has not been executed because this environment has no .NET SDK.

## Next session
1. Start with this status and the end of [LOG.md](LOG.md).
2. Pull `main` and run `dotnet test` locally with the .NET 10 SDK; report and fix compile/test failures in small commits.
3. Continue typed result coverage with scholarly/paper metadata (arXiv, Crossref, OpenAlex, Europe PMC and Semantic Scholar) using the pinned SearXNG result types as the contract.
4. Keep all work on `main`; do not create or invoke GitHub Actions workflows.

## Latest media response mapping
- SearXNG now uses shared `length` and `author` fields for both video and audio results.
- Deezer previews now emit `audio_src`, duration as `length`, and artist as `author`; nested `music_metadata` remains available.
- Added fixture assertions and updated the porting rationale/audit.
- Logged as entry 133 in [LOG.md](LOG.md).
