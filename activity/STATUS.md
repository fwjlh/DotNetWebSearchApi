# Current handover status

Updated: 2026-11-22

## Repository
- Public repository: https://github.com/fwjlh/DotNetWebSearchApi
- Default branch: main
- Latest verified code commit: 64c664c91a243bdd9561d18fd5ae4ee7ddd3d5cb
- Current verified milestone: 50 providers; batch 6 implementation raises the source catalogue to 58, with CI pending.
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
- **50 registered providers** after the current batch.
- The first 42 providers passed CI fixture tests in run [45013727838](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013727838): build passed with 0 warnings/errors; 130 tests passed.
- Batch 5 and the full 50-provider catalogue passed CI in run [45019256660](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019256660): build passed with 0 warnings/errors; 138 tests passed.
- Batch 6 adds eight adapters and eight fixture tests; CI is pending.
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
- Local .NET SDK is unavailable in the orchestration environment; GitHub Actions is the build/test verification source.
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
1. Verify batch 5 and correct any build/test failures.
2. Continue real provider ports from 50 to 100, prioritising general web search first, then specialist and regional providers.
3. Keep API keys optional and outside source control; record quotas, rate limits and access restrictions.
4. At 100, stop feature additions and prepare the pull/build/integration/live-provider testing handover.
5. After live tests, harden authentication, rate limiting, outbound request policy, SQL Server configuration and admin editing.
