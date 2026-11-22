# Provider catalogue and implementation priority

Updated: 2026-11-22

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
| 11–18 | Specialist JSON APIs | CI-verified | Crossref, OpenAlex, Hacker News, Stack Overflow, npm, crates.io, NuGet and Open Library |
| 19–26 | Specialist API wave 2 | CI-verified | Europe PMC, Semantic Scholar, GitHub, Docker Hub, Google Books, Internet Archive, RubyGems and Hugging Face |
| 27–34 | Developer and package API wave 3 | CI-verified | Super User, Server Fault, Ask Ubuntu, GitLab, Gitea, Maven Central, Packagist and pub.dev |
| 35–42 | Media, knowledge and academic wave 4 | CI-verified | arXiv, Bilibili, Dailymotion, Deezer, Steam, MusicBrainz, Wikidata and German Wikipedia |
| 43–50 | Search, media and regional wave 5 | CI-verified | Wiby, TVmaze, Radio Browser, PeerTube, Wikimedia Commons, Wikinews, Spanish Wikipedia and Openverse |
| 51–58 | General search and news wave 6 | CI-verified | Mwmbl, Marginalia Search, Yep, Kagi, Exa, Google News, Bing News and Wiktionary |
| 59–100 | Further SearXNG providers | Planned | Prioritise by usefulness, reliability, accessibility, terms and maintenance cost; count only tested request/parser/registration adapters |

Provider ordering is a development priority, not a claim that every upstream service offers a free or unrestricted API. Current SearXNG settings mark Mojeek inactive because of its proof-of-work CAPTCHA and Startpage inactive because of its Anubis proof-of-work challenge; those are deferred until their CPU and access implications are understood. Brave's current Search plan is listed at $5 per 1,000 requests with $5 in monthly credits; activating a plan requires payment-card details. Check the [current Brave plans](https://brave.com/search/api/) before use. Each adapter must respect the provider's current access rules, terms, rate limits and attribution requirements. Where a provider requires credentials, keys will be configured as secrets and never committed to source control.

## Wave 7 additions (providers 60–67)

| Provider | Status | Notes |
|---|---|---|
| DataCite | Implemented; CI pending | Public DOI metadata API; no authentication |
| Zenodo | Implemented; CI pending | Public research record search API |
| OpenAIRE Graph | Implemented; CI pending | Research-products Graph API v4; public rate limits apply |
| PLOS | Implemented; CI pending | Solr search API; throttled to one request every six seconds |
| Wikisource | Implemented; CI pending | English MediaWiki source-text search |
| Wikibooks | Implemented; CI pending | English MediaWiki textbook search |
| Wikiversity | Implemented; CI pending | English MediaWiki learning-resource search |
| Wikivoyage | Implemented; CI pending | English MediaWiki travel-guide search |

## Verification rule

A provider is only called CI-verified when its request construction and response mapping have fixture coverage and the full GitHub Actions workflow passes. CI does not prove live upstream availability, quota compliance under load, or ongoing compatibility with an upstream service.
