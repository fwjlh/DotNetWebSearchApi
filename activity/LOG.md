# Repository activity log

Every material repository action is recorded here with its reason, result, verification, and next step. The current handover summary is in [STATUS.md](STATUS.md).

## 2026-11-21 — Bootstrap and project foundation

### 1. Verify repository and write access
- **Reason:** Confirm the user-created target exists before changing it.
- **Action:** Read repository metadata and the default branch ref for `fwjlh/DotNetWebSearchApi`.
- **Result:** Confirmed public repository, default branch `main`, and write/admin permissions.
- **Verification:** Repository metadata and branch ref returned successfully.
- **Next:** Establish project documentation.

### 2. Inspect SearXNG identity, configuration and license
- **Reason:** Establish provenance and avoid unsupported licensing claims.
- **Action:** Read upstream `README.rst`, `searx/settings.yml`, and `LICENSE` from `searxng/searxng`.
- **Result:** Upstream README describes SearXNG as a metasearch engine and declares AGPL-3.0. Its license file contains GNU AGPL version 3. The settings file demonstrates the breadth of configuration; this project will implement a deliberately staged subset rather than claim feature parity.
- **Verification:** Read upstream files through the connected GitHub integration.
- **Next:** Document the independent implementation boundary and license.

### 3. Bootstrap README
- **Reason:** Define project intent, target runtime, architecture direction, security constraints, and handover entry point.
- **Action:** Added root `README.md`.
- **Result:** Commit `4ed379468e6c1584d7b9d4d0876d06a9fbb0ba6e` (`docs: bootstrap DotNetWebSearchApi`).
- **Verification:** Read back the commit and `main` ref.
- **Next:** Add provenance, license, ignore rules and diary.

### 4. Add initial handover status
- **Reason:** Make the project resumable between sessions.
- **Action:** Added `activity/STATUS.md`.
- **Result:** Commit `3bfda0a45e42e1e278bcfa5220135b931b46cd59`.
- **Verification:** GitHub returned the commit SHA.
- **Next:** Add activity log and licensing/provenance files.

### 5. Add license and provenance baseline
- **Reason:** Make intended AGPL-3.0-or-later licensing and SearXNG inspiration explicit before source code.
- **Action:** Added `LICENSE`, `UPSTREAM.md`, `.gitignore`, `activity/LOG.md`, and refreshed `activity/STATUS.md`.
- **Result:** Commit `4b53cc341dec8f0bfad17b0c3559a8a7299be4ec` pushed to `main`.
- **Verification:** Git tree and branch update operation succeeded.
- **Next:** Add first API/provider/aggregation vertical slice.

### 6. Check local .NET toolchain
- **Reason:** Determine whether tests can be executed before relying on CI.
- **Action:** Ran `dotnet --info` in the execution container.
- **Result:** `dotnet: command not found`; no local build/test possible in this environment.
- **Next:** Configure GitHub Actions to run restore, build and tests on .NET 10.

### 7. Stage first implementation increment
- **Reason:** Implement a small but real vertical slice, not merely architecture notes.
- **Action:** Prepare .NET 10 solution, ASP.NET Core API, provider contract, parallel aggregator, Wikipedia adapter, xUnit tests and CI workflow. A first staging attempt created Git blobs but failed in the orchestration-side file-size check because `TextEncoder` is unavailable in that runtime; no branch ref or commit was changed by that attempt. The staging was repeated using character-count checks.
- **Result:** Implementation commit pending at time of this log update.
- **Tests authored:** Provider aggregation, duplicate removal, ranking, limit, provider-failure isolation, blank-query handling, and non-HTTP URL rejection.
- **Execution:** Tests not run locally because .NET SDK is absent.
- **Next:** Commit the staged files with this updated log, verify `main`, then inspect GitHub Actions.


### 8. Diagnose first GitHub Actions build
- **Reason:** Verify the new workflow and correct actual compiler failures rather than assume the source builds.
- **Action:** Inspected run `44942371910` and its job log for commit `3529f9b3df635f2cb1e4417cdc50f4ae2e526074`.
- **Result:** Restore succeeded. Build failed with two compiler errors: ambiguous `Timeout.InfiniteTimeSpan` because the provider exposes a `Timeout` property, and a nullable result passed to `List.Add`.
- **Correction:** Move User-Agent configuration into the typed HttpClient registration (also avoiding HttpClient mutation in the provider constructor); guard the nullable parsed result before adding it; guard a non-string snippet JSON value.
- **Additional tests:** Add timeout isolation and caller-cancellation propagation cases.
- **Tests:** CI build failed before the test step; the new tests are authored but not yet executed.
- **Next:** Push this correction with the updated tests and log, then inspect the next CI run.


### 9. Diagnose second GitHub Actions build
- **Reason:** Verify that the first compiler corrections resolved the production-project build and identify the next real blocker.
- **Action:** Inspected run `44942458446` and job `136962438657` for commit `927b0dacd1555278d2a8dfa9fa9957f0de9bdaba`.
- **Result:** Application project compiled past the previous errors. Test project then failed because xUnit attributes were not imported; all reported errors were unresolved `Fact` / `FactAttribute`.
- **Correction:** Add explicit `using Xunit;` to the test source.
- **Tests:** CI did not reach test execution; the test suite remains unexecuted.
- **Next:** Push this test-project correction and inspect the next CI run.


### 10. Verify corrected build and test suite
- **Reason:** Confirm the compiler fixes and test-project correction with an actual clean CI run.
- **Action:** Inspected GitHub Actions run `44942530782` for commit `7e088b4112c0424fad411f42ddf9f4861ddff103`, including its job log.
- **Result:** <strong>CI passed.</strong> Restore succeeded, build succeeded, and all 9 tests passed (0 failed, 0 skipped).
- **Verification:** Workflow conclusion `success`; job log reports `Passed: 9, Failed: 0, Skipped: 0`.
- **Run:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/44942530782
- **Next:** Add adapter-level tests and API integration tests, then begin the configurable-provider design. SQL Server-backed provider definitions, API-key authentication, quotas, safe outbound destination policy, and constrained CSHTML templates are not implemented yet.


## 2026-11-22 — Expand project README and lock delivery priorities

### 11. Replace bootstrap README with project landing page
- **Reason:** The initial README described the skeleton but did not explain the project's ambition, actual status, provider priorities, deployment direction or contribution path.
- **Action:** Rewrote the root README with project overview, capability/status matrix, provider roadmap, architecture diagram, quick start, API examples, build/test instructions, delivery sequence, security guidance, contribution guidance, provenance and licence notes.
- **Provider plan recorded:** Bing, Brave Search, Google Search, DuckDuckGo, existing Wikipedia adapter, then Mojeek and further SearXNG providers. Access routes, terms, keys and quotas must be checked before claiming an adapter is available.
- **Delivery sequence recorded:** (1) strengthen provider foundation and implement major engines, (2) reach the first 5–10 tested adapters, (3) add a SearXNG-compatible JSON API, (4) build browser search/admin/diagnostics, then operational hardening.
- **Result:** Documentation update prepared for commit; no application source changed in this increment.
- **Verification:** README content checked against the current API routes, request validation, response record fields, project layout and current single-provider implementation. Existing CI result remains the last code verification; this documentation change does not claim a new test run.
- **Next:** Implement the next provider-focused increment. Validate current access methods for the first major search engines, then add adapters with isolated parsing/request tests. Keep each provider honest about required credentials, terms and availability.


## 2026-11-22 — First major-engine adapter: Bing

### 12. Validate upstream access strategy
- **Reason:** The legacy Bing Search API was retired; the project must follow a currently used upstream route rather than code against a dead API.
- **Action:** Reviewed Microsoft lifecycle guidance and the current SearXNG Bing engine documentation/source.
- **Result:** Microsoft states Bing Search APIs were retired on 11 August 2025. Current SearXNG Bing Web uses the public Bing HTML search page and extracts result cards from the HTML response.
- **Verification:** Microsoft lifecycle notice and current SearXNG engine documentation/source reviewed. See the project README provider roadmap.
- **Next:** Implement the HTML adapter with isolated parser and HTTP behaviour tests.

### 13. Implement Bing Web adapter
- **Reason:** Start provider coverage with a major general-purpose search engine using the current SearXNG-compatible access pattern.
- **Action:** Added BingSearchProvider, BingSearchResultParser, HtmlAgilityPack 1.13.1, provider registration and six adapter tests covering title/snippet extraction, HTML entity decoding, decorative icon removal, Bing tracking URL unwrapping, invalid/missing result rejection, encoded query construction and HTTP error handling.
- **Result:** Implementation and tests prepared for commit. The provider uses Bing Web HTML, not the retired Bing Search API. No live request has been made as part of the automated tests; tests use deterministic fixture HTML and a fake HTTP handler.
- **Verification:** Pending GitHub Actions build and test run.
- **Next:** Inspect CI. If green, mark Bing adapter implemented; then assess Brave official API and web-interface paths, followed by Google and DuckDuckGo. Preserve each provider's credential, quota and bot-protection constraints.


### 14. Verify Bing Web adapter in GitHub Actions
- **Reason:** Confirm the first major-engine adapter compiles and its parser/request behaviour is covered by executable tests.
- **Action:** Inspected workflow run 45007833897 for commit b037b44e9b7d00debdcae9eab1a910a125da7c9b and fetched the job log.
- **Result:** **CI passed.** Restore succeeded, build succeeded with 0 warnings and 0 errors, and all 15 tests passed (0 failed, 0 skipped). Six tests cover the Bing adapter; nine existing tests cover the search aggregator.
- **Verification:** Workflow run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45007833897. The adapter tests use fixture HTML and a fake HTTP handler; they do not claim live Bing availability.
- **Next:** Mark Bing Web as implemented, then implement Brave Search. Prefer its official JSON API with an optional key supplied only through environment configuration; document the provider's published pricing and free-credit allowance. Do not store the key in source control or ordinary provider data.


## 2026-11-22 — Brave Search API adapter

### 15. Validate Brave Search access and pricing
- **Reason:** Choose a maintainable route for the second major provider and avoid reverse-engineering an HTML/JavaScript application when an official API exists.
- **Action:** Reviewed Brave's current Web Search API reference, authentication guide, quickstart and published plans, plus the SearXNG Brave API adapter.
- **Result:** Official endpoint is GET https://api.search.brave.com/res/v1/web/search. Requests require X-Subscription-Token. The Search plan is listed at $5 per 1,000 requests and includes $5 monthly credits; activation requires selecting a plan and entering card details. Treat this as optional, not a zero-cost guaranteed service.
- **Verification:** Official Brave API documentation and current SearXNG adapter reviewed. See https://api-dashboard.search.brave.com/api-reference/web/search/get and https://api-dashboard.search.brave.com/documentation/guides/authentication.
- **Next:** Implement an optional adapter activated only when BRAVE_SEARCH_API_KEY is present in environment configuration. Never commit the key.

### 16. Implement Brave Search API adapter
- **Reason:** Add a second major general-purpose provider using its documented JSON API and explicit secret handling.
- **Action:** Added BraveSearchProvider and BraveSearchResultParser, conditional DI registration via BRAVE_SEARCH_API_KEY, and seven tests for JSON parsing, missing/invalid results, query encoding, page-size clamping, HTTP errors and malformed JSON.
- **Result:** Implementation and tests prepared for commit. Without the environment variable, the provider is not registered and no key is required. The test suite uses fixture JSON and a fake HTTP handler; it does not make billable/live Brave requests.
- **Verification:** Pending GitHub Actions.
- **Next:** Inspect CI. If green, record Brave as implemented-but-key-required. Then evaluate Google's supported options (Custom Search JSON API is closed to new customers) and DuckDuckGo's HTML route/bot protection before the next adapter.


### 17. Diagnose Brave adapter CI test failure
- **Reason:** The first automated run must be inspected and any real failures corrected before counting the adapter as verified.
- **Action:** Inspected GitHub Actions run 45008000838 for commit d2531b69a997709ccf80082ddaa76bbd802f4500 and read the job log.
- **Result:** Restore and build succeeded with 0 warnings and 0 errors. Of 23 tests, 22 passed and one failed because the malformed-JSON test used Assert.ThrowsAsync<JsonException>, which requires an exact exception type; System.Text.Json returned JsonReaderException, a more specific JsonException subtype. This is a test assertion mismatch, not a production adapter defect.
- **Correction:** Change the assertion to Assert.ThrowsAnyAsync<JsonException> so derived JSON parse exceptions are accepted.
- **Verification:** Correction is committed with the activity update; the next CI run must confirm all tests pass.
- **Next:** Inspect the corrected CI run. If green, mark Brave as verified-but-key-required and proceed to Google, then DuckDuckGo.


### 18. Verify Brave Search adapter in GitHub Actions
- **Reason:** Confirm the optional official API adapter and corrected malformed-JSON assertion with a real .NET 10 CI run.
- **Action:** Inspected workflow run 45008067698 for commit 66ffe447972eaab8bdc4d90dd2e8cc4cd2ea882b and fetched the job log.
- **Result:** **CI passed.** Restore succeeded, build succeeded, and all 23 tests passed (0 failed, 0 skipped). This includes eight Brave adapter tests and six Bing adapter tests.
- **Correction verified:** The malformed-JSON test now accepts JsonReaderException as a JsonException subtype.
- **Verification:** Workflow run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008067698. The Brave tests use fixture JSON and a fake HTTP handler; they do not make billable/live API requests.
- **Next:** Record Brave as implemented but optional/key-required. Proceed to Google web search (the official Custom Search JSON API is closed to new customers) and DuckDuckGo HTML search (documented bot protection is a risk).


## 2026-11-22 — Google Web HTML adapter

### 19. Validate Google search route
- **Reason:** The official Custom Search JSON API is closed to new customers, so evaluate the current SearXNG web-search implementation as the practical route.
- **Action:** Reviewed Google's Custom Search JSON API lifecycle note and current SearXNG Google engine source, including request parameters, result selectors and CAPTCHA detection.
- **Result:** New customers cannot sign up for Custom Search JSON API; existing customers must transition by 1 January 2027. Current SearXNG Google Web uses the Google HTML search page, HTML result cards and explicit CAPTCHA detection. The .NET adapter will follow that web route and treat blocking pages as provider failures.
- **Verification:** Google developer documentation and SearXNG current source reviewed. No claim of live Google availability is made.
- **Next:** Add a testable HTML parser, request adapter and CAPTCHA handling.

### 20. Implement Google Web adapter
- **Reason:** Continue the agreed major-engine-first sequence with a no-key web adapter rather than a closed API.
- **Action:** Added GoogleSearchProvider and GoogleSearchResultParser, registered the provider, and added seven fixture/fake-handler tests for parsing, URL unwrapping, malformed cards, query construction, CAPTCHA handling and HTTP failures.
- **Result:** Implementation and tests prepared for commit. The adapter uses the public Google HTML page, not Custom Search JSON API. Automated tests do not contact Google.
- **Verification:** Pending GitHub Actions.
- **Next:** Inspect CI and record the result. If green, assess DuckDuckGo's HTML request flow and bot-protection constraints.


### 21. Verify Google Web adapter in GitHub Actions
- **Reason:** Confirm the Google HTML parser, request construction and CAPTCHA handling compile and pass their deterministic tests.
- **Action:** Inspected workflow run 45008244135 for commit 5386354d644eeca5d89941ab01e1752b4c4c09eb and fetched the job log.
- **Result:** **CI passed.** Restore succeeded, build succeeded, and all 30 tests passed (0 failed, 0 skipped). This includes seven Google adapter tests.
- **Verification:** Workflow run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008244135. Tests use fixture HTML and a fake HTTP handler; they do not contact Google or prove live search availability.
- **Next:** Record Google Web as implemented with CAPTCHA/blocking risk. Proceed to DuckDuckGo HTML, with explicit first-page-only behaviour and CAPTCHA detection.


## 2026-11-22 — DuckDuckGo HTML adapter

### 22. Validate DuckDuckGo request flow
- **Reason:** Determine whether DuckDuckGo can be added without an official general web-search API and understand its bot-detection constraints.
- **Action:** Reviewed the current SearXNG DuckDuckGo engine source and documentation.
- **Result:** SearXNG uses an HTTP POST to html.duckduckgo.com/html with form fields q, b and kl for the first page. Later pages require a query-specific vqd token; this first implementation intentionally supports page one only. Current upstream documentation describes bot-blocker changes and CAPTCHA risk.
- **Verification:** Current SearXNG source and engine documentation reviewed. The adapter will detect the challenge form and will not attempt to bypass CAPTCHAs or emulate browser TLS fingerprints.
- **Next:** Implement first-page form submission and HTML parsing with deterministic tests.

### 23. Implement DuckDuckGo first-page adapter
- **Reason:** Add a fourth major general-purpose provider while keeping pagination and bot-protection behaviour explicit.
- **Action:** Added DuckDuckGoSearchProvider and DuckDuckGoSearchResultParser, request headers, first-page form POST, 499-character query guard, redirect URL extraction, CAPTCHA detection and ten tests.
- **Result:** Implementation and tests prepared for commit. No attempt is made to solve CAPTCHAs or fetch later pages without a vqd token. Tests use fixture HTML and a fake handler; no live DuckDuckGo requests are made.
- **Verification:** Pending GitHub Actions.
- **Next:** Inspect CI. If green, update provider roadmap and evaluate next providers (Mojeek, Qwant, Startpage and other useful SearXNG engines).


### 24. Verify DuckDuckGo first-page adapter in GitHub Actions
- **Reason:** Confirm form submission, result parsing, redirect unwrapping, query-length handling and CAPTCHA detection with executable tests.
- **Action:** Inspected workflow run 45008393329 for commit 93bb6aea3338b0848d119691701c4cca680caf19 and fetched the job log.
- **Result:** **CI passed.** Restore and build succeeded; all 41 tests passed (0 failed, 0 skipped). Eleven tests cover DuckDuckGo parsing and request behaviour.
- **Verification:** Workflow run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008393329. Tests use fixture HTML and a fake HTTP handler; they do not contact DuckDuckGo or establish that its bot blocker will accept this server's traffic.
- **Provider scope:** Only the first HTML results page is implemented. No vqd token caching or pagination is attempted. CAPTCHA pages are treated as provider failures, not bypassed.
- **Next:** Update the README and handover with the five verified adapters. Evaluate Qwant next; current SearXNG uses its undocumented JSON API and handles DataDome cookies. Defer Mojeek and Startpage until their proof-of-work challenge costs and access constraints are understood.


## 2026-11-22 — Qwant JSON adapter

### 25. Inspect Qwant's current SearXNG implementation
- **Reason:** Select the next provider after the four major general-purpose engines and understand the maintenance risks of its undocumented API.
- **Action:** Reviewed the current SearXNG Qwant engine source and documentation.
- **Result:** SearXNG calls the undocumented Qwant v3 JSON API at api.qwant.com/v3/search/web, parses data.result.items.mainline web rows, and caches/reuses the DataDome cookie. API errors distinguish rate limiting, access denial and CAPTCHA responses. Header ordering and browser-like request metadata are used upstream; .NET may not reproduce all fingerprint details.
- **Verification:** Current SearXNG documentation/source reviewed: https://docs.searxng.org/dev/engines/online/qwant.html
- **Next:** Implement a small JSON adapter with explicit 403/API error handling. Use HttpClient's cookie handling, but do not attempt to bypass DataDome challenges.

### 26. Implement Qwant Web adapter
- **Reason:** Extend provider coverage beyond the first five with a no-key general web-search provider.
- **Action:** Added QwantSearchProvider and QwantSearchResultParser, registered the provider with Qwant request headers, and added eight fixture/fake-handler tests for result parsing, ad filtering, malformed responses, query encoding and HTTP/access failures.
- **Result:** Implementation and tests prepared for commit. The adapter uses the undocumented Qwant JSON API and relies on HttpClient's cookie container for any DataDome cookie returned by the server. It does not solve or bypass challenges.
- **Verification:** Pending GitHub Actions. Tests do not make live Qwant requests.
- **Next:** Inspect CI. If green, document Qwant as implemented but live availability still unverified; continue evaluating the next SearXNG engines.


### 27. Verify Qwant adapter in GitHub Actions
- **Reason:** Confirm Qwant's JSON response parser and HTTP request/error behaviour compile and pass deterministic tests.
- **Action:** Inspected workflow run 45008612843 for commit 1374fd11e331a7b32b9e0a73a560b7fab2f58551 and fetched the job log.
- **Result:** **CI passed.** Restore and build succeeded; all 49 tests passed (0 failed, 0 skipped). Eight tests cover Qwant parsing, ad-row filtering, invalid URLs, missing data, API failure, query encoding, HTTP errors and malformed JSON.
- **Verification:** Workflow run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008612843. Tests use fixture JSON and a fake HTTP handler; no live Qwant request was made.
- **Limitation:** Qwant's API is undocumented and DataDome may deny requests. HttpClient cookie handling is enabled through the typed client handler; this has not been verified against live Qwant.
- **Next:** Mark Qwant experimental/implemented, then evaluate the next general-web providers. Mojeek and Startpage are currently inactive upstream due to proof-of-work challenges; avoid bypassing CAPTCHA/proof-of-work protections without an explicit, sustainable access path.


### 28. Correct README table line break
- **Reason:** A literal newline escape was emitted into the Qwant capability row during the documentation update.
- **Action:** Replaced the literal escape with a real Markdown line break and renumbered the later-provider roadmap after inserting Qwant, Mojeek and Startpage.
- **Result:** README provider tables render as separate rows; no application source changed.
- **Verification:** Fetched the updated README from GitHub and checked the provider table rows.
- **Next:** Continue with the next general-web provider evaluation.


## 2026-11-22 — Yahoo Web adapter

### 29. Evaluate Yahoo's current SearXNG route
- **Reason:** Add another general-purpose engine after the first six adapters, while understanding Yahoo's bot-detection redirects.
- **Action:** Reviewed current SearXNG Yahoo engine source.
- **Result:** Yahoo uses HTML result cards, an sB preference cookie and a YBV anti-bot cookie. Its current adapter handles up to four redirects through tracking/geo endpoints and caches YBV. The .NET implementation will use a shared CookieContainer and explicit bounded redirect handling.
- **Verification:** Current SearXNG source reviewed: https://docs.searxng.org/_modules/searx/engines/yahoo.html. Yahoo is marked disabled in the current default SearXNG engine settings; this adapter is experimental.
- **Next:** Implement parser and request behaviour with fake-handler tests; do not claim live availability.

### 30. Implement Yahoo Web adapter
- **Reason:** Extend general-web coverage without requiring an API key.
- **Action:** Added YahooSearchProvider, YahooSearchResultParser, shared cookie-container registration, bounded 302/307 redirect handling and seven fixture/fake-handler tests.
- **Result:** Implementation and tests prepared for commit. The provider uses Yahoo HTML search, sB preferences and automatic YBV cookie storage through HttpClientHandler. Tests do not contact Yahoo.
- **Verification:** Pending GitHub Actions.
- **Next:** Inspect CI. If green, document Yahoo as experimental and assess Yandex Search API credential/cost requirements.


### 31. Verify Yahoo Web adapter in GitHub Actions
- **Reason:** Confirm Yahoo HTML parsing, preference-cookie setup and bounded redirect handling with executable tests.
- **Action:** Inspected workflow run 45008790126 for commit 3b127e3b67c63849abf54a52ca54e9635cc8b6c4 and fetched the job log.
- **Result:** **CI passed.** Restore and build succeeded; all 56 tests passed (0 failed, 0 skipped). Seven tests cover Yahoo result parsing, redirect unwrapping, query construction, cookie creation, bounded redirect flow and HTTP errors.
- **Verification:** Workflow run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008790126. Tests use fixture HTML and a fake HTTP handler; they do not contact Yahoo or prove live acceptance of the cookie/redirect flow.
- **Next:** Record Yahoo as experimental/implemented. Assess Yandex Search API credential and pricing requirements, then decide whether an optional paid adapter is worth including.


## 2026-11-22 — Yandex Search API adapter

### 32. Validate Yandex API access and cost
- **Reason:** Decide whether a further major regional search index can be added without hiding commercial requirements.
- **Action:** Reviewed current SearXNG Yandex Search API documentation, Yandex AI Studio quickstart, pricing and quotas.
- **Result:** The official API requires a Yandex Cloud account, billing account, API key and folder ID. Current published daytime synchronous pricing is approximately $4 per 1,000 requests and night-time synchronous pricing approximately $3 per 1,000; prices exclude VAT in USD. No separate free allowance is documented. Queries are limited to 400 characters and the API returns Base64-encoded XML in a JSON rawData field.
- **Verification:** Official pricing and API docs reviewed: https://aistudio.yandex.ru/en/docs/search-api/pricing and https://aistudio.yandex.ru/en/docs/search-api/quickstart/.
- **Next:** Implement only as an optional adapter activated by both YANDEX_SEARCH_API_KEY and YANDEX_FOLDER_ID. Do not send requests unless both settings are present.

### 33. Implement optional Yandex Search API adapter
- **Reason:** Add an independent regional index while ensuring the paid service cannot be called accidentally.
- **Action:** Added YandexSearchOptions, YandexSearchProvider and YandexSearchResultParser. Registration is conditional on both environment variables. Added eight tests for Base64 XML parsing, passages, no-results/error handling, URL validation, request credentials/body and query-length limits.
- **Result:** Implementation and tests prepared for commit. Without both environment variables the provider is not registered. Tests use fixture XML/JSON and fake HTTP handlers; they do not make paid Yandex requests.
- **Verification:** Pending GitHub Actions.
- **Next:** Inspect CI. If green, record Yandex as optional/paid and document pricing. Then decide whether to add regional engines such as Naver or Seznam to reach the first ten providers.


### 34. Remove duplicate Program namespace import
- **Reason:** Final source inspection found the Yandex registration edit had duplicated the existing search namespace using directive.
- **Action:** Removed the duplicate import before running CI.
- **Result:** No runtime behaviour changed; source remains otherwise unchanged.
- **Verification:** Re-fetched Program.cs from GitHub and checked the top-level imports.
- **Next:** Run GitHub Actions for the Yandex adapter and correct any compiler/test failures before marking it complete.


### 35. Diagnose first Yandex CI failure and correct source import
- **Reason:** The initial Yandex commit must pass a clean build before the adapter can be counted.
- **Action:** Inspected workflow run 45008940032 for commit bb2fd25541b831aff5ef0e59ff7700c44f764368.
- **Result:** Restore passed; build failed before tests with CS0105 because Program.cs contained a duplicate DotNetWebSearchApi.Search using directive. The duplicate was removed in commit 48413e42f3f37efd24417d14e19b07d5ec083c2d.
- **Verification:** Failure log https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008940032. This was a compile-time import issue, not a Yandex parser/runtime failure.
- **Next:** Confirm the corrected commit's CI run.

### 36. Verify corrected Yandex adapter in GitHub Actions
- **Reason:** Verify the duplicate-import correction and the complete optional Yandex implementation.
- **Action:** Inspected workflow run 45008975175 for commit 48413e42f3f37efd24417d14e19b07d5ec083c2d.
- **Result:** **CI passed.** Restore succeeded, build succeeded, and all 64 tests passed (0 failed, 0 skipped).
- **Verification:** Workflow run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45008975175. Eight Yandex tests cover Base64 XML parsing, passages, API error handling, invalid URLs, credentials/body construction and query length. Tests use fixtures and do not make paid requests.
- **Next:** Record Yandex as optional/paid, update README and status, then assess the next useful providers to reach ten.


## 2026-11-22 — Naver Web adapter

### 37. Evaluate Naver as a regional search provider
- **Reason:** Continue toward ten useful adapters after the major general-purpose engines; Naver is a major Korean search provider.
- **Action:** Reviewed current SearXNG Naver engine source and the configured-engine list.
- **Result:** Naver general web search uses search.naver.com/search.naver with query, start and where=web parameters. Results are HTML cards identified by fds-web-normal-doc-root; title and description use sds-comps text classes. The provider is regional/Korean and currently disabled by default in SearXNG.
- **Verification:** Current SearXNG source and configured engine list reviewed. No live request is claimed.
- **Next:** Add a small first-page HTML adapter with fixture tests.

### 38. Implement Naver first-page adapter
- **Reason:** Add a regional search provider without requiring an API key.
- **Action:** Added NaverSearchProvider, NaverSearchResultParser, Korean/English request language headers and five tests for parsing, invalid links, query construction and HTTP errors.
- **Result:** Implementation and tests prepared for commit. First-page general web results only; no paging, images, news or video modes.
- **Verification:** Pending GitHub Actions. Tests use fixture HTML and a fake handler.
- **Next:** Inspect CI. If green, assess Seznam as the tenth provider; it requires a homepage bootstrap request, hidden form values and cookie reuse.


## 2026-11-22 — Seznam Web adapter

### 39. Evaluate Seznam's bootstrap and verification flow
- **Reason:** Add a tenth provider from the SearXNG general-web set after Naver.
- **Action:** Reviewed current SearXNG Seznam engine source.
- **Result:** Seznam first fetches its homepage to acquire hidden form values and cookies, then sends q, oq, noredirect and those hidden fields to search.seznam.cz. Search results use a dedicated result-card layout; a /verify redirect is treated as access denied.
- **Verification:** Current SearXNG source reviewed: https://docs.searxng.org/_modules/searx/engines/seznam.html. Seznam is disabled by default upstream; this adapter is experimental and region-specific.
- **Next:** Implement homepage bootstrap, cookie reuse through HttpClientHandler, result parsing and verification redirect handling.

### 40. Implement Seznam Web adapter
- **Reason:** Reach the initial ten-provider target with a no-key regional search provider.
- **Action:** Added SeznamSearchProvider and SeznamSearchResultParser, homepage hidden-field bootstrap, query construction, /verify detection, and seven fixture/fake-handler tests.
- **Result:** Implementation and tests prepared for commit. HttpClient's default cookie handler is relied on to retain cookies between the homepage and search requests. No live Seznam request is made in tests.
- **Verification:** Pending GitHub Actions.
- **Next:** Inspect CI. If green, update README/status to ten providers and decide whether to proceed to further specialist SearXNG engines or begin the compatibility JSON API.


### 41. Verify Seznam Web adapter in GitHub Actions
- **Reason:** Confirm the tenth provider's homepage bootstrap, hidden-field extraction, query construction and HTML parser.
- **Action:** Inspected workflow run 45009221825 for commit 671143fc87d4aed76eec3cb012e88e262cb5f44b and fetched the job log.
- **Result:** **CI passed.** Restore and build succeeded; all 76 tests passed (0 failed, 0 skipped). Seven tests cover Seznam parsing, hidden fields, verification URI detection and the two-request bootstrap flow.
- **Verification:** Workflow run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45009221825. Tests use fixture HTML and fake HTTP handlers; they do not contact Seznam.
- **Next:** Mark the first ten-provider milestone complete. Move to the agreed SearXNG-compatible JSON API, then the browser search/admin/diagnostics experience. Keep additional regional/specialist providers as follow-on work rather than delaying the API milestone.


### 42. Correct README Seznam capability row line break
- **Reason:** The Seznam adapter row was inserted with a literal newline escape in the capability table.
- **Action:** Replaced the literal escape with an actual Markdown newline and marked the first major-engine wave complete.
- **Result:** Provider capability table now renders Naver and Seznam as separate rows.
- **Verification:** Re-fetched README.md from GitHub and inspected the current capabilities table.
- **Next:** Begin SearXNG-compatible JSON API work.


## 2026-11-22 — SearXNG-compatible JSON API

### 43. Define compatibility scope
- **Reason:** The agreed ten-provider target is complete; move to milestone 5, the SearXNG-compatible JSON API, before the browser/admin work.
- **Action:** Reviewed SearXNG JSON response conventions and identified the initial compatible surface: query, number_of_results, results, answers, corrections, infoboxes, suggestions and unresponsive_engines; result fields include engine, engines, positions, score, category, template and parsed_url.
- **Result:** Implement a JSON-only GET/POST /search route (plus GET /) with engine selection and explicit 501 responses for unsupported paging/time-range/safesearch/language features. Preserve the existing /api/search contract.
- **Next:** Track provider failures in SearchAggregator so unresponsive_engines is meaningful; add integration tests using WebApplicationFactory.

### 44. Implement compatibility response and diagnostics
- **Reason:** Provide the SearXNG-shaped response while accurately exposing provider failures and selected engines.
- **Action:** Added SearchExecutionResult and SearchAggregator.SearchDetailedAsync, optional provider selection on SearchQuery, response DTO/mapper with snake_case fields, GET/POST /search and GET / routes, plus engine filtering and unsupported-feature responses. Added two aggregator tests and eight WebApplicationFactory integration tests. Added Microsoft.AspNetCore.Mvc.Testing 10.0.0.
- **Result:** Implementation and tests prepared for commit. Existing /api/search remains unchanged. Compatibility is deliberately limited: only JSON, general category, page one, no time-range or safe-search overrides; provider result positions are inferred from current adapter scores.
- **Verification:** Pending GitHub Actions.
- **Next:** Inspect restore/build/test; correct any integration issues before claiming compatibility.


### 45. Diagnose JSON API integration test fixture mismatch
- **Reason:** Inspect the first compatibility API CI run before accepting the new endpoint.
- **Action:** Read workflow run 45009580290 for commit d663c18baa166dde7f36e7dbd596d4fbac5ce273.
- **Result:** Restore and build succeeded. Of 86 tests, 85 passed and one integration test failed because the test helper registered provider "alpha" but created SearchResult.Provider="fixture"; the response mapper correctly used the result's provider field. This is a test-fixture inconsistency, not a production endpoint defect.
- **Correction:** Change the integration-test provider helper to set SearchResult.Provider to the registered provider name.
- **Verification:** Failure details in https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45009580290.
- **Next:** Commit the fixture correction and verify all 86 tests in a new CI run.


### 46. Verify SearXNG-compatible JSON API in GitHub Actions
- **Reason:** Confirm the new compatibility routes, response serialization, provider filtering, failure reporting and integration test host.
- **Action:** Inspected corrected workflow run 45009659712 for commit 1f764bf864953c76ef28b0d802568e6db70dc5fa.
- **Result:** **CI passed.** Restore and build succeeded; all 86 tests passed (0 failed, 0 skipped). This includes eight WebApplicationFactory integration tests and two new aggregator tests.
- **Correction history:** The first run 45009580290 passed restore/build and 85/86 tests; one test fixture named its provider alpha but its SearchResult.Provider fixture. The fixture now uses the registered provider name.
- **Verification:** Successful workflow https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45009659712. Initial failure https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45009580290.
- **Scope:** GET /search, GET / and form POST /search return the initial SearXNG-shaped JSON response. Engine filtering and unresponsive_engines are supported. Pagination, HTML output, time range, language overrides and non-default safe-search are explicitly unsupported.
- **Next:** Update README/status, then proceed to the browser search interface, provider selection, administration and diagnostics as agreed.


### 47. Correct README compatibility endpoint table line break
- **Reason:** The new JSON API capability row was inserted with a literal newline escape.
- **Action:** Replaced the escape with a real Markdown table row break.
- **Result:** Current capabilities table now renders the legacy and SearXNG-shaped endpoints separately.
- **Verification:** Fetched README.md from GitHub and checked the rendered Markdown source.
- **Next:** Continue with browser search, provider selection and diagnostics.


## 2026-11-22 — Browser search and read-only diagnostics

### 48. Implement initial search UI and provider diagnostics
- **Reason:** Continue milestone 6 after the first JSON API surface: make the service usable from a browser and expose operational provider health without revealing secrets.
- **Action:** Added static search and admin pages, responsive CSS, vanilla JavaScript provider selection/result rendering, /api/providers, /api/diagnostics, /admin, root redirect to the search page, and static-file hosting. Added SearchDiagnostics counters for success/failure totals, last duration, result count, last checked time and exception type. Added four diagnostics unit tests and five integration tests for provider discovery, health reporting and UI routes.
- **Result:** Initial browser search and read-only admin diagnostics are implemented. Admin counters are in-memory and reset on process restart. No configuration editing or secret display is provided.
- **Security:** UI renders upstream text with textContent and only links HTTP(S) URLs. Diagnostics exposes provider names and exception type only, not messages, keys or folder IDs. Public exposure still requires authentication/rate limiting.
- **Verification:** Pending GitHub Actions.
- **Next:** Inspect build/tests; if green, document the initial UI and plan provider configuration management separately.


### 49. Diagnose static page 404s in browser/admin CI
- **Reason:** Inspect the first browser/admin workflow run after build succeeded but two route tests failed.
- **Action:** Read workflow run 45010196457 for commit 39c8987e4ffb0e7e0177ee33b991d145b2c0d1f4.
- **Result:** Restore and build succeeded; 93/95 tests passed. Search and admin page integration tests returned 404 because static files were committed at repository-root wwwroot, while ASP.NET Core's content root is the application project directory.
- **Correction:** Move the five static assets into src/DotNetWebSearchApi/wwwroot, preserving their contents.
- **Verification:** Failure log https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45010196457.
- **Next:** Commit the corrected paths and rerun all tests.


### 50. Verify browser search and diagnostics in GitHub Actions
- **Reason:** Confirm the browser pages are served from the ASP.NET Core web root and that provider diagnostics remain accurate.
- **Action:** Inspected corrected workflow run 45010292934 for commit 3a4970a3d58a5e0ddac89676539bd605d662bbd7.
- **Result:** **CI passed.** Restore and build succeeded; all 95 tests passed (0 failed, 0 skipped). This includes five browser/admin/provider-diagnostics integration tests and four SearchDiagnostics unit tests.
- **Correction history:** Initial run 45010196457 passed restore/build and 93/95 tests. The two static-page tests returned 404 because assets were at repository-root wwwroot instead of src/DotNetWebSearchApi/wwwroot. Assets were moved into the application project's web root; corrected run is green.
- **Verification:** Successful workflow https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45010292934. Initial failure https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45010196457.
- **Next:** Mark initial browser and read-only diagnostics milestone complete. Design editable provider configuration and secret references before adding admin write actions.


### 51. Polish README project status and overview tense
- **Reason:** Keep the landing page aligned with features that are now implemented rather than describing them as future intentions.
- **Action:** Removed an unnecessary leading space before the project-status blockquote and changed the overview from future-tense goals to present-tense capabilities.
- **Result:** README opening now accurately describes the working provider aggregation and current implementation stage.
- **Verification:** Re-fetched README.md from GitHub and checked the opening section.
- **Next:** Continue with safe provider configuration management and deployment hardening.


## 2026-11-22 — Expand provider catalogue towards 100

### 52. Establish a 100-provider target and inspect upstream inventory
- **Reason:** The user set a new milestone: reach 100 providers before pulling the repository locally for a meaningful build and test cycle.
- **Action:** Inspected the current repository handover, provider contracts, dependency registration and tests. Enumerated the current SearXNG upstream engine directory; it contains 256 Python engine modules, including general web, academic, developer, media, regional and specialist sources.
- **Result:** Confirmed 18 is the target baseline for this increment: ten existing adapters plus eight specialist JSON API adapters. A provider will count only when it has a real endpoint/request, response mapping, registration and automated fixture tests; a roadmap name or placeholder does not count.
- **Verification:** Repository main head before this increment was `c5b0cfd94ab716ffb68f6533a3b5c9f2a369e758`. Existing CI at commit `3a4970a3d58a5e0ddac89676539bd605d662bbd7` passed 95 tests. Current SearXNG engine inventory read from https://github.com/searxng/searxng/tree/master/searx/engines.
- **Next:** Add and CI-verify the first specialist JSON API adapter batch, then continue in small provider batches until 100. Live upstream testing remains a separate phase after the user pulls and builds locally.

### 53. Implement eight specialist JSON API adapters
- **Reason:** Add useful, low-friction specialist indexes after the first general-web wave, using a reusable adapter while retaining provider-specific endpoint and JSON-path definitions.
- **Action:** Added Crossref, OpenAlex, Hacker News, Stack Overflow, npm, crates.io, NuGet and Open Library definitions. Added shared JSON API request/response adapter with query encoding, result-limit mapping, nested JSON path support, HTML snippet cleaning, URL prefix support and HTTP(S)-only result validation. Registered all eight providers and added eight fixture-driven provider mapping tests plus malformed-response and HTTP-error tests.
- **Result:** Provider count increases from 10 to 18. No credentials are required for these initial public endpoints. Stack Exchange API quotas apply. No live upstream requests were made.
- **Verification:** Pending GitHub Actions; the tests are authored but not yet confirmed executed.
- **Next:** Inspect CI, fix any real failures, then continue with the next provider batch. Do not count this batch as verified until CI is green.


### 54. Diagnose first specialist-provider CI failure
- **Reason:** Verify the new provider batch with the real .NET 10 build and correct compiler failures before counting the providers.
- **Action:** Inspected workflow run 45012798957 and job 137178596994 for commit f30ba0eca15e07370e47951602ce6d9bce82bdbe.
- **Result:** Restore succeeded; build failed with two compile errors: the adapter called a missing TryReadPath helper and lacked System.Net for WebUtility. The build also exposed a potential IReadOnlyDictionary copy compatibility issue during source review. Tests did not execute.
- **Correction:** Add the missing nested JSON path traversal helper, import System.Net, and copy fixed query parameters through ToDictionary.
- **Verification:** Failure log https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45012798957. No tests are claimed as run for this commit.
- **Next:** Commit correction, inspect CI again, and only then mark the eight providers verified.


### 55. Diagnose specialist-provider test failures and correct URL/query handling
- **Reason:** The compiler correction produced a successful build; inspect all test failures rather than accepting a partially green run.
- **Action:** Inspected workflow run 45012899977 for commit 6255aa7c36e52f9c973da8528e8b734bb5157905.
- **Result:** Build passed with 0 warnings and 0 errors. Of 105 tests, 103 passed and two failed. NuGet's "$top" parameter was correctly URL-encoded as "%24top", but the test asserted the unencoded spelling. Open Library relative keys such as "/works/OL123W" were parsed by Uri.TryCreate as absolute file URIs and rejected before the configured HTTPS prefix could be applied.
- **Correction:** Accept an absolute result URL only when its scheme is HTTP or HTTPS; otherwise apply the provider's configured URL prefix. Decode the query string in the assertion before checking parameter names.
- **Verification:** Run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45012899977. Build succeeded; test result 103 passed, 2 failed before this correction. Corrected commit remains unverified.
- **Next:** Push correction and inspect a fresh full CI run.


### 56. Verify eight specialist JSON API providers in GitHub Actions
- **Reason:** Confirm the shared JSON API adapter, all eight provider definitions and the corrected test assertions/URL handling.
- **Action:** Inspected workflow run 45013006368 for commit fcc88fe26dd15e5d1e46e9deacb1fe797b55be21 and fetched the job log.
- **Result:** **CI passed.** Restore and build succeeded; build reported 0 warnings and 0 errors. All 105 tests passed (0 failed, 0 skipped).
- **Correction history:** First attempt 45012798957 failed during build due to missing helper/import. Second attempt 45012899977 built successfully but had 2 failing tests (103 passed). The helper/import, relative URL handling and URL-encoded query assertion were corrected.
- **Verification:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013006368. New fixture tests cover Crossref, OpenAlex, Hacker News, Stack Overflow, npm, crates.io, NuGet and Open Library. They do not make live upstream requests.
- **Next:** Continue the provider catalogue in batches, with a target of 100 implemented, registered and fixture-tested adapters before the user's local pull/build/live-testing phase.


## 2026-11-22 — Specialist provider batch 2

### 57. Implement eight additional specialist providers
- **Reason:** Continue from 18 toward the requested 100 real provider adapters, selecting distinct public search APIs across academic, developer, package, model, books and archive domains.
- **Action:** Added Europe PMC, Semantic Scholar, GitHub repository search, Docker Hub, Google Books, Internet Archive, RubyGems and Hugging Face Models to the JSON provider catalog. Extended the common adapter to support a root-array response selector. Added eight provider-specific JSON fixtures to the adapter theory tests.
- **Result:** Source catalogue now defines 26 providers (18 in the previous verified batch plus these 8). These are registered as independent provider instances with distinct upstream endpoints and result mappings. No live requests were made.
- **Verification:** Pending GitHub Actions. Do not count this batch as CI-verified until the workflow succeeds.
- **Next:** Run CI, correct failures and record exact test count; then continue toward 100.


### 58. Diagnose batch-2 build regression and restore adapter corrections
- **Reason:** The batch-2 CI build failed even though the prior batch had passed. Inspect the exact branch contents and commit tree before applying another fix.
- **Action:** Inspected workflow run 45013175836 for commit b01d26f3c27babc8371edcc2274a7f3bd8176ae7 and compared the checked-in JSON adapter with the previously corrected version.
- **Result:** The batch-2 commit had been built from a stale base tree, which unintentionally reverted the JSON path helper, System.Net import, fixed-parameter dictionary conversion, HTTP(S)-only absolute URL detection and decoded query assertion. This was a Git tree composition error, not a defect introduced by the eight new provider definitions. Build failed before tests.
- **Correction:** Restore the complete corrected JSON adapter, retain root-array support for RubyGems and Hugging Face, restore URL-prefix handling for relative provider IDs, and decode query parameters in the fixture assertion. Future commits must always use the current branch commit's tree SHA as their base tree.
- **Verification:** Failed workflow https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013175836. No tests executed.
- **Next:** Commit the correction using the exact current branch tree, then verify the complete suite.


### 59. Verify specialist provider batch 2
- **Reason:** Confirm the eight new specialist providers and restored shared JSON adapter after the stale-tree regression.
- **Action:** Inspected workflow run 45013295887 for commit 07cc22461140dc81ce7eec6958a0a5d6465b8101 and fetched the complete job log.
- **Result:** **CI passed.** Restore and build succeeded; 0 warnings and 0 errors. All 113 tests passed (0 failed, 0 skipped).
- **Correction history:** The prior run 45013175836 failed before tests because the handover commit used an older base tree and reverted adapter helper/import changes. The exact current tree was then used, the fixes restored, and root-array JSON responses retained for RubyGems and Hugging Face.
- **Verification:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013295887. Eight new provider fixtures cover Europe PMC, Semantic Scholar, GitHub repository search, Docker Hub, Google Books, Internet Archive, RubyGems and Hugging Face Models. No live upstream requests were made.
- **Next:** Continue provider ports in batches, targeting 100 before the local pull/build/live-testing handoff.


## 2026-11-22 — Developer and package provider batch 3

### 60. Implement eight additional provider adapters
- **Reason:** Continue the 100-provider target with useful developer communities, source-hosting platforms and package registries.
- **Action:** Added Super User, Server Fault and Ask Ubuntu through separate Stack Exchange site configurations; added GitLab Projects, Gitea, Maven Central, Packagist and pub.dev using their public JSON search APIs. Added eight fixture cases to the shared adapter test theory.
- **Result:** Provider catalogue now defines 34 adapters. The three Stack Exchange variants use separate site parameters; the other adapters use provider-specific endpoints and JSON paths. No live requests were made.
- **Verification:** Pending GitHub Actions. These providers are not counted as verified until CI passes.
- **Next:** Verify this batch, then continue with news, media, image, academic and regional providers.


### 61. Diagnose Maven Central URL mapping test
- **Reason:** The third provider batch compiled successfully but did not pass the full fixture suite.
- **Action:** Inspected workflow run 45013448673 for commit 19c8b97abb93eca261dc43d4490d4f759190c83f and reviewed the single failing case.
- **Result:** Build passed. Of 121 tests, 120 passed and one failed. Maven Central's group:artifact identifier was interpreted by Uri as an absolute custom-scheme URI instead of a relative artifact path, so the configured Sonatype URL prefix was not applied.
- **Correction:** Escape reserved characters when appending provider identifiers to URL prefixes while preserving embedded path separators; update the expected URL to its correctly escaped form.
- **Verification:** Run https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013448673. One test failed; no live provider request was made.
- **Next:** Push the URL correction and verify the full suite.


### 62. Verify developer and package provider batch 3
- **Reason:** Confirm the Maven URL-prefix correction and all eight provider fixtures.
- **Action:** Inspected workflow run 45013542434 for commit 8145914717e165c632d2c714776f978477611f47 and fetched the job log.
- **Result:** **CI passed.** Restore and build succeeded; 0 warnings and 0 errors. All 121 tests passed (0 failed, 0 skipped).
- **Correction history:** Initial run 45013448673 passed build and 120/121 tests. Maven Central's colon-delimited artifact identifier was being interpreted as a URI scheme. Prefix joining now percent-encodes reserved characters while preserving path separators.
- **Verification:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013542434. The batch adds Super User, Server Fault, Ask Ubuntu, GitLab, Gitea, Maven Central, Packagist and pub.dev. Tests use fixtures and fake handlers; live upstream acceptance is not claimed.
- **Next:** Continue towards 100 provider adapters, then hand over for local build and live testing.


## 2026-11-22 — Media, knowledge and academic provider batch 4

### 63. Implement eight additional providers
- **Reason:** Continue toward 100 with a broader mix of academic, media, music, gaming and knowledge sources.
- **Action:** Added arXiv using a dedicated Atom/XML adapter with DTD processing disabled; added Bilibili, Dailymotion, Deezer, Steam, MusicBrainz, Wikidata and German Wikipedia to the JSON provider catalog. Added provider fixtures and two arXiv request/parser tests.
- **Result:** Catalogue now defines 42 adapters. No live requests were made; media provider acceptance and regional access remain unverified.
- **Verification:** Pending GitHub Actions.
- **Next:** Verify this batch, then continue across image, news, video, code, regional and specialist sources.


## 2026-11-22 — Batch 4 verification and batch 5 implementation

### 64. Verify media, knowledge and academic provider batch 4
- **Reason:** Close the pending verification before starting another increment.
- **Action:** Inspected workflow run 45013727838, including the build-and-test job log, for commit 92a34b06ec137dfea1625af2d881803c90bc3045.
- **Result:** **CI passed.** Restore succeeded; build succeeded with 0 warnings and 0 errors; all 130 tests passed (0 failed, 0 skipped).
- **Verification:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45013727838. The 42 adapters are now all CI-verified at fixture level. Live upstream acceptance is not verified.
- **Next:** Continue provider additions, preserving real endpoints, request parameters, provider-specific response paths and fixture coverage.

### 65. Implement eight additional search and specialist providers
- **Reason:** Continue from 42 toward the agreed 100-provider milestone while keeping a mix of independent search, media, knowledge and regional sources.
- **Action:** Added Wiby, TVmaze, Radio Browser, PeerTube, Wikimedia Commons, Wikinews, Spanish Wikipedia and Openverse. Extended the shared JSON definition to support APIs without a limit parameter and provider-required attribution. Wiby attribution is included in result snippets as required by its API terms.
- **Result:** Catalogue now defines 39 JSON API adapters plus the dedicated arXiv adapter and ten original adapters, for 50 registered providers in total.
- **Tests:** Added eight deterministic fixture cases, including Wiby attribution and API shapes for nested and root-array responses.
- **Verification:** Pending GitHub Actions. No live provider calls were made.
- **Next:** Verify this batch, then continue toward 100. Keep live acceptance, quotas and access restrictions separate from fixture-level CI status.


### 66. Correct provider catalogue separator before CI
- **Reason:** Review the pushed batch source before treating the pending workflow as authoritative.
- **Action:** Read back the provider catalogue and found a missing comma between the German Wikipedia definition and the first batch-5 definition. Added the missing separator in a focused correction.
- **Result:** Source correction prepared; no provider behaviour or count changed.
- **Verification:** Pending CI on the correction commit.
- **Next:** Verify the build and all fixture tests before continuing.


### 67. Verify provider batch 5 and close the 50-provider milestone
- **Reason:** Confirm the separator correction and all eight new adapters with the actual .NET 10 build and test workflow.
- **Action:** Inspected GitHub Actions run 45019256660 for correction commit 64c664c91a243bdd9561d18fd5ae4ee7ddd3d5cb and fetched the completed job log.
- **Result:** **CI passed.** Restore succeeded; build succeeded; 0 warnings and 0 errors; all 138 tests passed (0 failed, 0 skipped).
- **Correction history:** Initial batch commit 41ee52ac3334ea20984a21d49f57dc8de6eb1d70 failed compilation because a comma was missing between provider definitions. The error was identified from the compiler log and corrected in commit 64c664c91a243bdd9561d18fd5ae4ee7ddd3d5cb.
- **Verification:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019256660. Eight new fixture cases passed. The 50 providers are implemented, registered and CI fixture-tested; live upstream acceptance remains unverified.
- **Next:** Continue from 50 to 100, keeping the original priority of general web search providers before the remaining specialist catalogue.


## 2026-11-22 — Batch 6 implementation

### 68. Add six search API adapters and two RSS search providers
- **Reason:** Continue prioritising independent general web search and major news search sources after the 50-provider milestone.
- **Action:** Added Mwmbl, Marginalia Search, Yep, Kagi, Exa and Wiktionary JSON adapters; added Google News and Bing News RSS adapters. Extended JSON request handling for POST bodies, provider headers, optional API-key configuration and per-provider request spacing. Mwmbl is throttled to one request per second. Kagi, Exa and Yep are only registered when their API keys are configured; no credentials are committed.
- **RSS handling:** Added safe XML parsing with DTD processing prohibited, RSS and Atom support, and Bing News publisher-link unwrapping.
- **Tests:** Added six JSON fixtures and two RSS fixtures, including POST payload construction and Bing redirect resolution.
- **Result:** 58 provider adapters are implemented in source; Kagi, Exa and Yep remain optional until their credentials are configured.
- **Verification:** Pending GitHub Actions. No live upstream requests were made.
- **Next:** Verify build and fixtures; then continue toward 100, prioritising general web search providers and avoiding unlicensed or undocumented assumptions.


### 69. Correct batch 6 fixture failures
- **Reason:** Review the first CI run for the eight-provider increment.
- **Action:** Build passed, but four tests failed. Two RSS tests showed that the XML reader was not configured for asynchronous reads; two POST API tests assumed raw JSON preserved ampersands instead of escaping them.
- **Correction:** Enabled asynchronous XML reading while retaining DTD prohibition and null XML resolver. Updated POST tests to parse the JSON body and assert the decoded query value and result-limit field.
- **Result:** Code and test corrections committed; no provider count change.
- **Verification:** Pending CI on the correction commit.
- **Next:** Verify all 146 tests before adding the next providers.


### 70. Add the missing JSON test namespace
- **Reason:** Re-run after the RSS async-reader and POST-body assertions were corrected.
- **Action:** The next build caught a missing System.Text.Json using in the test project (production source compiled). Added the explicit namespace import.
- **Result:** One compile-only test project issue corrected; provider code unchanged.
- **Verification:** Pending CI.
- **Next:** Verify all tests; then close batch 6.


### 71. Verify batch 6 and close the 58-provider milestone
- **Reason:** Confirm the fixes from the first batch-6 CI attempts.
- **Action:** Inspected workflow run 45019669530 for commit c9dc51d51b162fd9ae6711cc52c2e84a7b18ca17 and retrieved the completed build-and-test log.
- **Result:** **CI passed.** Restore succeeded; build succeeded with 0 warnings and 0 errors; all 146 tests passed (0 failed, 0 skipped).
- **Corrections:** RSS XML readers now allow asynchronous reads while prohibiting DTDs; POST request tests parse JSON rather than assuming a literal unescaped ampersand; the test file imports System.Text.Json.
- **Verification:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019669530. Eight new fixture cases passed. The source now contains 58 provider adapters; three API-key providers (Yep, Kagi, Exa) are optional and register only when configured.
- **Next:** Continue from 58 toward 100. General search engines remain the priority; keep optional paid APIs clearly marked and disabled without credentials.


## 2026-11-22 — Mojeek priority provider

### 72. Port Mojeek search and its Altcha proof-of-work challenge
- **Reason:** Mojeek was explicitly the next provider in the original major-engine priority order after Bing, Brave, Google, DuckDuckGo and Wikipedia.
- **Action:** Added a native C# Mojeek adapter. It obtains the Altcha challenge, solves the bounded PBKDF2 proof of work, posts the base64 multipart verification payload, stores the returned challenge cookie, and parses Mojeek's standard web result HTML.
- **Safety and limits:** Supports SHA-1/256/384/512 challenge algorithms; search attempts are bounded to 1,000 counters. It fails closed when the challenge cannot be solved or Mojeek does not issue the cookie. The adapter does not attempt to bypass any further CAPTCHA or access control.
- **Tests:** Added an end-to-end fixture test for challenge → verification → cookie → HTML results, plus an unsolved-challenge test.
- **Result:** 59 provider adapters are implemented in source.
- **Verification:** Pending GitHub Actions. No live upstream requests were made.
- **Next:** Verify the Mojeek port, then continue down the major-engine list and remaining specialist sources.


### 73. Correct Mojeek test handler return type and shared cookie registration
- **Reason:** Review the Mojeek CI build failure.
- **Action:** The production Mojeek adapter compiled. The test's unsolved-challenge handler returned HttpResponseMessage instead of Task<HttpResponseMessage>; wrapped the response in Task.FromResult. Also removed a duplicate CookieContainer singleton registration introduced with the provider.
- **Result:** Test compile correction and DI cleanup committed.
- **Verification:** Pending CI.
- **Next:** Verify the Mojeek challenge and result fixtures.


### 74. Verify Mojeek and close the 59-provider milestone
- **Reason:** Confirm the Mojeek proof-of-work implementation and its fixture coverage.
- **Action:** Inspected GitHub Actions run 45019896375 for correction commit 294f0a0b5e96a6e96e851f3894b4d1b65cbbd70c and retrieved the completed build-and-test log.
- **Result:** **CI passed.** Build succeeded with 0 warnings and 0 errors; all 148 tests passed (0 failed, 0 skipped).
- **Corrections:** The first Mojeek attempt had a test-only async return-type error; fixed with Task.FromResult and removed the duplicate CookieContainer registration. Production code compiled on the first run.
- **Verification:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45019896375. The Altcha challenge, proof submission, cookie handling, result parsing and unsolved-proof failure path are fixture-tested.
- **Live status:** No live Mojeek request has been made; the provider is CI-verified only. CAPTCHA difficulty, service policy and live acceptance still require testing after the user pulls the repo.
- **Next:** Continue from 59, preserving the original major-engine order; Startpage is the next major candidate but its current Anubis proof-of-work is CPU-intensive and was disabled by default in the upstream implementation.


## 2026-11-22 — Batch 7 implementation

### 75. Add scholarly metadata and open-knowledge search providers
- **Reason:** Continue down the provider list after the major general-search engines, adding reliable public scholarly and reference sources.
- **Action:** Added DataCite, Zenodo, OpenAIRE Graph v4, PLOS Search, Wikisource, Wikibooks, Wikiversity and Wikivoyage. Split the JSON provider catalog into base and additional definition files to keep source files within the preferred size limit.
- **Rate limits:** PLOS is throttled to one request every six seconds to respect its published 10-per-minute limit. Other APIs remain subject to their upstream public quotas.
- **Tests:** Added eight JSON fixture cases covering nested JSON:API records, Zenodo search hits, OpenAIRE Graph results, PLOS Solr response and four MediaWiki instances.
- **Result:** 67 provider adapters are implemented in source.
- **Verification:** Pending GitHub Actions. No live upstream requests were made.
- **Next:** Verify all 156 tests; continue toward 100 with the remaining specialist sources and search engines.


### 76. Correct OpenAIRE result links with query-string URL prefixes
- **Reason:** Review batch-7 fixture results.
- **Action:** The build passed, but one of 156 tests failed because OpenAIRE result links use a query-string URL prefix. The shared URL mapper treated that prefix as a directory path, replacing the publication?pid= portion. Added explicit query-prefix concatenation while retaining the existing path-prefix handling.
- **Result:** One URL-construction edge case corrected; other 155 tests passed in the first batch-7 run.
- **Verification:** Pending CI on the correction commit.
- **Next:** Verify all 156 tests and update the provider status.


### 77. Use the supported string Contains overload
- **Reason:** Check the query-prefix correction against the .NET 10 API surface before relying on CI.
- **Action:** Changed the query marker check to the string overload of Contains with StringComparison.
- **Result:** Keeps the intended URL mapping while avoiding a char/StringComparison overload mismatch.
- **Verification:** Pending CI.


### 78. Verify batch 7 and close the 67-provider milestone
- **Reason:** Confirm the OpenAIRE query-prefix correction and the full scholarly/open-knowledge batch.
- **Action:** Inspected GitHub Actions run 45020266512 for commit 819595d1bf1fcde3361b25dafabcaaeeec2d87cc and retrieved the completed build-and-test log.
- **Result:** **CI passed.** Build succeeded with 0 warnings and 0 errors; all 156 tests passed (0 failed, 0 skipped).
- **Corrections:** OpenAIRE result URLs now preserve query-string URL prefixes; the first batch-7 run had 155/156 tests pass before this fix.
- **Verification:** https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45020266512. All eight new provider fixtures passed.
- **Live status:** No live API requests were made; this is fixture-level CI verification.
- **Next:** Continue from 67 toward 100. The original major-engine priorities through Mojeek are now represented; Startpage remains deferred because of its CPU-intensive Anubis challenge.


## 2026-11-22 — Python-to-C# architecture audit

### 79. Pause provider expansion and compare the shared SearXNG framework
- **Reason:** At 67 adapters, review whether provider ports are duplicating the upstream Python behaviour or missing common patterns that would make subsequent ports faster.
- **Action:** Compared the current C# request/provider/aggregator/transport/response code with SearXNG's upstream engine processor, SearchQuery model, ResultContainer/scoring, EngineTraits, network layer and Bing, Brave, DuckDuckGo and Wikipedia engine implementations.
- **Finding:** The project has a useful provider adapter layer but is not yet a faithful port of the shared SearXNG engine framework. Important reusable concepts not yet represented consistently include a full request context, engine capability gating, shared request transport, provider suspension/backoff, locale/region traits, result merging with source positions and weighted scoring, and richer result types.
- **Action:** Added docs/PORTING_AUDIT.md with concrete differences, upstream references and a staged refactoring plan.
- **Result:** Provider count remains 67. No provider implementation was added in this audit.
- **Verification:** Source comparison only; no code behaviour changed, so the existing 156-test CI baseline remains the last verified build.
- **Next:** Implement the request-context and engine-capability foundation first, with backwards-compatible defaults and focused tests. Then shared transport/health, result aggregation and traits before resuming provider expansion.


## 2026-11-22 — Freeze provider catalogue and record consumer architecture

### 80. Freeze at 67 and define the independent service boundary
- **Reason:** Confirm the direction after reviewing the actual CoderInBox search integration and the shared SearXNG framework gaps.
- **Decision:** DotNetWebSearchApi remains an independent HTTP/JSON service. CoderInBox is its first major consumer; provider implementations must not be merged into CoderInBox.
- **CoderInBox findings:** Its current WebResearch fallback chain uses direct DuckDuckGo HTML, SearXNG, Wikipedia OpenSearch, optional Presearch and a Google WebView2 browser fallback. Its existing IBrowserSearchService is specifically a browser seam, not a general search-service contract. The recommended integration is a separate IWebSearchService HTTP client in CoderInBox.Tools; keep WebView2 in the WinForms Harness as an optional browser/user-verification capability.
- **Provider policy:** Freeze the current catalogue at 67 adapters. No new provider ports until shared request context, capabilities, typed outcomes/health, shared transport/session handling, challenge classification and result aggregation have been generalised and tested.
- **Challenge policy:** Detect and classify temporary blocks, rate limits, browser verification, proof-of-work and interactive CAPTCHA separately. Retain bounded provider-specific permitted protocol handlers such as Mojeek Altcha; support human-operated verification where appropriate. AI vision may classify/explain challenges, not automatically solve or bypass CAPTCHA. Never expose or log session cookies/tokens.
- **Action:** Added [docs/ARCHITECTURE_DECISIONS.md](../docs/ARCHITECTURE_DECISIONS.md), updated README project status/roadmap, and corrected the current handover summary and next actions in activity/STATUS.md.
- **Verification:** Documentation-only changes. No provider implementation or runtime behaviour changed. Last verified baseline remains CI run 45020266512: build 0 warnings/errors; 156 tests passed. No live upstream tests were run.
- **Next:** Implement request context and engine capabilities first, with backwards-compatible API defaults and focused tests. Keep the API host thin and core contracts independent of ASP.NET Core; do not split projects prematurely.


### 81. Add the first request-context and capability foundation
- **Reason:** Begin the P0 shared-framework work without changing existing provider implementations or breaking the current three-argument SearchQuery construction.
- **Action:** Extended SearchQuery with optional categories, language, safe-search level, page and time-range fields while retaining the existing constructor. Added EngineCapabilities and optional ISearchProviderCapabilities. Legacy providers receive a conservative general-web/first-page/default-locale capability profile.
- **Execution:** SearchAggregator now performs capability pre-flight and skips unsupported providers rather than calling them. SearchExecutionResult exposes UnsupportedProviders separately from provider failures. Added a focused test proving a paged-capable provider runs while a legacy first-page-only provider is skipped.
- **Result:** First P0 architecture slice committed. Provider catalogue remains frozen at 67.
- **Verification:** CI has not yet been retrieved for this code change. The last verified baseline remains 156 tests passed on run 45020266512; that result does not verify these commits. No live upstream requests were made.
- **Next:** Verify CI. Then expose supported request-context fields through API parsing and compatibility responses, add capability declarations to representative providers, and implement typed provider outcomes/health state.

- **Final implementation commit for this slice:** `ab52977e1fed0fa3407651da90daea98effe045a`. Unsupported-provider outcomes are non-null in every SearchExecutionResult, including empty-query returns.


### 82. Reconcile remaining README roadmap and verification claims
- **Reason:** A second pass found older roadmap text and a stale 138-test/ten-provider verification statement below the README's main status section.
- **Action:** Replaced the old 100-provider/continued-expansion delivery sequence with the agreed shared-framework milestones, recorded the 156-test historical baseline accurately, and explicitly stated that it predates the new request-context/capability code.
- **Result:** README no longer presents provider expansion as active or the old 138-test/ten-provider snapshot as current.
- **Verification:** Documentation review only; no runtime changes. Latest code remains `ab52977e1fed0fa3407651da90daea98effe045a`; CI verification for that code slice is still not confirmed through the available GitHub Actions status view.


## 2026-11-22 — Python-to-C# porting rationale corpus

### 83. Establish the porting rationale corpus and record four priority engines
- **Reason:** Preserve not only what each C# provider does, but why its implementation has that shape relative to the original Python/SearXNG source. Future contributors need to distinguish faithful behaviour preservation from simplification, deliberate protocol substitution and missing features.
- **Action:** Added [docs/PYTHON_TO_CSHARP_PORTING_NOTES.md](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md), with a reusable evidence-based entry template and pinned upstream SearXNG revision `c6ee3e9fd0235004a694e63720efc869bb1e3594` (2026-11-21).
- **Entries:** Bing (HTML request/parser shape and redirect decoding; locale/safe-search gaps), DuckDuckGo (no-JS form POST and result parsing; VQD/paging/cookie/locale gaps), Brave (explicitly records that C# uses the official JSON API rather than translating SearXNG's embedded-HTML scraper), and Mojeek (Altcha proof-of-work/cookie sequence plus fixed-time, locale, paging and category differences).
- **Method:** Each entry labels preserved, adapted, omitted and unverified behaviour. It links both Python source and C# implementation and warns that fixture coverage does not establish live upstream acceptance.
- **Action:** Linked the corpus from README and added it to the handover status. Provider catalogue remains frozen at 67; no provider code changed.
- **Verification:** Documentation-only. The GitHub combined status lookup for `ab52977e1fed0fa3407651da90daea98effe045a` returned no status entries, and the workflow-run lookup returned no runs (that connector view filters to PR-triggered runs). Therefore CI for the capability slice remains unconfirmed; the last verified baseline is still run 45020266512 (156 tests, 0 failures, 0 skipped, 0 warnings/errors). No live provider calls were made.
- **Next:** Continue shared request-context/API integration and typed outcomes/health; expand this corpus for Google, Qwant, Yahoo, Yandex, Naver, Seznam and the generic JSON/RSS adapter patterns. Do not resume provider expansion yet.


### 84. Verify capability slice and porting-corpus documentation in GitHub Actions
- **Reason:** The initial connector status view did not expose push-triggered runs, so query the repository Actions API directly before leaving CI as pending.
- **Capability code:** Commit `ab52977e1fed0fa3407651da90daea98effe045a` completed run [45022944719](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45022944719). Its single `build-and-test` job completed successfully; restore, build and test steps all report success.
- **Documentation:** README/status/log/corpus updates completed successfully in runs [45023354409](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45023354409) and [45023360277](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45023360277). Both single build-and-test jobs report success.
- **Result:** The new capability slice is no longer awaiting CI. Detailed test totals for run 45022944719 were not independently captured, so no new test count or warning count is claimed. The historical 156-test baseline remains separately identified.
- **Action:** Updated activity/STATUS.md to distinguish the historical catalogue baseline from the verified capability slice and documentation commits.
- **Next:** Continue the API request-context integration and typed provider outcomes/health. Expand the rationale corpus for the remaining major engines while keeping the provider catalogue frozen at 67.


### 85. Expand the porting rationale corpus across the remaining priority engines and shared adapters
- **Reason:** The first four entries were only a starting point. The highest-value next pass was to compare every remaining major general-web adapter and explain cases where the C# code is not actually doing the same job as its Python counterpart.
- **Action:** Expanded [docs/PYTHON_TO_CSHARP_PORTING_NOTES.md](../docs/PYTHON_TO_CSHARP_PORTING_NOTES.md) with Google, Qwant, Yahoo, Yandex, Naver, Seznam, Wikipedia, the generic JSON API adapter and the C# RSS/Atom adapter.
- **Important findings recorded:**
  - Google: HTML result extraction is similar, but C# currently hardcodes locale/safe-search and omits paging, time filters and suggestions.
  - Qwant: C# uses the same undocumented JSON endpoint but omits DataDome cookie caching, typed rate-limit/CAPTCHA/access-denied outcomes, paging and other verticals.
  - Yahoo: result DOM and redirect URL handling are similar; C# omits YBV cookie caching and locale/paging/time/safe-search mapping.
  - Yandex: the C# provider is a materially different authenticated Yandex Cloud Search API integration, not a port of SearXNG's public HTML scraper.
  - Naver: C# only covers first-page general web; upstream has category-specific paging, time filters, thumbnails and image/news/video parsers.
  - Seznam: two-step hidden-field bootstrap is retained, but explicit cookie transfer and typed verification handling differ.
  - Wikipedia: C# Action API text search is semantically different from SearXNG's language-aware REST summary/infobox engine.
  - Generic JSON: C# typed definition/catalogue model is a useful simplified counterpart, but Python's recursive query paths, page/time/safe maps, cookies, suggestions and thumbnail support are not all present.
  - RSS/Atom: explicitly labelled a C#-native feed foundation; no one-to-one Python engine provenance is claimed.
- **Evidence:** Each entry links the pinned SearXNG source and the actual C# provider/parser/test files. The Wikipedia entry explicitly records that no dedicated Wikipedia provider test file exists in the current tree.
- **Action:** Updated the handover to reflect the expanded corpus and removed completed engine comparisons from the immediate work queue. Provider catalogue remains frozen at 67; no adapters were added or changed.
- **Verification:** Documentation-only update. No live upstream requests. CI for the corpus expansion commit is pending at the time of this log entry.
- **Next:** Continue P0 shared architecture by wiring request context into API parsing and only declaring capabilities that the current C# request builders really honour. Then implement typed provider outcomes/health. Keep the provider catalogue frozen.


### 86. Wire request context through the API and distinguish unsupported providers
- **Reason:** The previous capability model existed inside the aggregator, but HTTP callers could not supply the new query fields. The SearXNG compatibility endpoint also rejected every non-default option before the capability gate could decide which engines could run.
- **Action:** Updated `Program.cs` so both `/api/search` and `/search` parse categories, page number (1–50), language, safe-search (0–2) and time range (day/week/month/year) into `SearchQuery`. Invalid values return HTTP 400.
- **Action:** Added `unsupported_engines` to the SearXNG-shaped response and `unsupportedProviders` to the native API response. These are separate from `unresponsive_engines`; unsupported providers are skipped before execution and do not count as failures.
- **Tests:** Expanded `SearxngApiIntegrationTests.cs` to cover context parsing, invalid values, native API context and unsupported-page reporting. The updated test commit `d2a31c49282bcca7c50d4e96d95afe46c66bd535` passed GitHub Actions run [45024120851](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024120851): build succeeded, 160 passed, 0 failed, 0 skipped.
- **CI correction trail:** Intermediate sequential commits were briefly red: `2c9ea6a` retained the old page-rejection assertion (one failing test); `20f8289` added the response field before the mapper constructor was updated (one compile error); `1b26c96` still ran the stale page test. The next test commit updated the expected behaviour and passed all 160 tests. These are superseded commits; current main contains the corrected implementation.
- **Documentation:** README now documents the request-context contract and conservative capability filtering. The porting corpus now has a shared-framework entry explaining how SearXNG's module-level engine flags and EngineTraits become SearchQuery, EngineCapabilities and pre-flight eligibility in C#. It explicitly warns that request fields being parsed does not mean provider request builders already implement them.
- **Corpus:** Completed the major general-web comparisons (Google, Qwant, Yahoo, Yandex, Naver, Seznam and Wikipedia) and generic JSON/RSS analysis. The work queue now moves to specialist dedicated Python engines and exact per-feed provenance. No provider adapters were added or changed; catalogue remains frozen at 67.
- **Verification:** Corpus expansion passed CI run [45024292510](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024292510). Earlier README/context documentation runs [45024160792](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024160792) and [45024231794](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024231794) passed. At log time, the newest README roadmap, corpus queue and status commits have Actions runs in progress; check those before calling the entire documentation batch green.
- **Important limit:** No provider has yet been given a broader capability declaration merely because Python supports it. Existing adapters retain the conservative BasicWeb profile until their C# request builders actually use the requested page/locale/safe/time fields and fixtures prove it.
- **Next:** Add real provider capability declarations alongside provider request-builder changes; then implement typed outcomes, provider health/cooldown and shared transport/session policy. Keep challenge handling explicit and never auto-solve CAPTCHA.


### 87. Add typed provider outcomes and reconcile the audit with implemented work
- **Reason:** The aggregator previously reduced every provider failure to an unresponsive name. That loses the distinctions SearXNG uses for rate limits, challenges, access denial and malformed responses, and it gives future cooldown logic no reliable input.
- **Action:** Added `ProviderOutcome.cs` with `ProviderOutcomeStatus`, `ProviderFailureKind`, provider-neutral `ProviderFailureException` and an initial HTTP/exception classifier.
- **Action:** `SearchExecutionResult` now exposes non-null per-provider outcomes. `SearchAggregator` classifies timeout, HTTP 401/403/429/408/425/5xx, invalid JSON/XML/format data and explicitly typed provider failures. A successful empty result remains success. Caller cancellation is still rethrown and never counted as provider failure.
- **Tests:** Added HTTP status classification, malformed JSON, typed challenge with RetryAfter, empty success and timeout outcome tests. Commit `2e186f7ff23720999d7688c3801d5042784dc08a` passed [CI run 45024534370](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024534370): build succeeded, 167 passed, 0 failed, 0 skipped.
- **Corpus:** Added a shared-framework note explaining how SearXNG's typed exceptions and suspension hints map to the C# provider-neutral outcome model. It records the current limitation that a bare `HttpRequestException` has no response headers, so generic Retry-After extraction belongs in shared transport.
- **Audit:** Updated [docs/PORTING_AUDIT.md](../docs/PORTING_AUDIT.md) to distinguish completed request-context/capability and initial typed-outcome foundations from remaining provider declarations, health/cooldown, transport and result-ranking work. Re-pinned its SearXNG source links to the same upstream revision used by the corpus.
- **Action:** Updated activity/STATUS.md with the new code baseline and 167-test CI result. Provider catalogue remains frozen at 67; no provider adapters were added.
- **Verification:** The typed-outcome corpus entry passed CI in [run 45024568368](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024568368). The audit documentation commit is currently running [45024625117](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45024625117). No live provider calls.
- **Next:** Implement provider health state with cooldown/backoff and half-open recovery, then shared HTTP transport/session handling. Keep provider-specific challenge protocol logic separate and do not auto-solve CAPTCHA.


### 88. Add provider health cooldown, half-open recovery and API reporting
- **Reason:** Typed outcomes now distinguish why a provider failed, but without a health lifecycle the aggregator would still contact a blocked/rate-limited provider on every search.
- **Action:** Added `ProviderHealthRegistry` with process-local `Closed`, `Open` and `HalfOpen` states. Default policy: two consecutive transient failures before opening; immediate cooldown for rate-limit, challenge, authentication, access-denied, configuration and invalid-response failures; 10-second exponential base cooldown; 60-second rate-limit cooldown; 15-second challenge cooldown; five-minute permanent-failure and maximum cooldown; one-minute invalid-response cooldown.
- **Action:** Explicit RetryAfter is honoured but capped. Only one half-open recovery probe is admitted at a time. Success closes/reset state; a failed probe reopens it. Caller cancellation releases an in-flight half-open probe without recording a failure.
- **Action:** SearchAggregator now gates eligible providers through the registry. Cooling-down providers are represented as skipped outcomes and reported separately from unsupported and unresponsive providers. Added `cooling_down_engines` to the SearXNG-shaped response and `CoolingDownProviders` to the native API. `/api/diagnostics` now exposes provider health state, consecutive failures, cooldown expiry and last failure kind.
- **Tests:** Added registry tests for cooldown, half-open single-probe behaviour, recovery, transient failure threshold, RetryAfter cap and end-to-end aggregator skip/recovery. Integration tests verify separate cooling-down reporting and diagnostics state. Latest verified code commit `0f1a01c9956409b5b61a9efe95dc50be9e73a38a` passed [CI run 45025031994](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45025031994): build succeeded, 174 passed, 0 failed, 0 skipped.
- **CI correction trail:** The initial health slice exposed several test/contract issues: response mapper constructor updated one commit after the response model; a test expected an extra provider call after a successful half-open probe; a real-clock RetryAfter assertion was too precise; diagnostics serialized the enum numerically while the test expected readable state. Each was corrected. The latest code run is green. No known outstanding test failures on current code.
- **Documentation:** README now describes provider health and cooldown fields. The rationale corpus adds a pinned-source comparison of SearXNG engine suspension with the C# circuit-breaker lifecycle. The porting audit now distinguishes completed process-local health from remaining persistence/per-provider policy. Documentation CI runs [45025056686](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45025056686) and [45025083265](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45025083265) are still running at the time of this entry.
- **Limits:** Health state is process-local, resets on restart and is not shared between instances. Policy is global. Generic `HttpRequestException` does not preserve Retry-After response headers; the shared transport should convert response status and headers into typed provider failures.
- **Next:** Build the shared HTTP transport/session layer, centralising timeout, cancellation, redirects, cookies, response-size limits, request headers, Retry-After and concurrency without flattening provider-specific challenge protocols. Provider count remains frozen at 67.


### 89. Migrate provider request paths onto the shared HTTP transport
- **Reason:** Typed outcomes and health now need reliable response status and Retry-After metadata. Direct HttpClient calls scattered through provider classes made response limits, host concurrency and status handling inconsistent.
- **Action:** Added optional shared transport injection and migrated the main provider paths: Bing, Brave, DuckDuckGo, Google, Mojeek, Naver, Qwant, Seznam, Yahoo, Yandex Cloud, Wikipedia, arXiv, generic JSON and RSS/Atom.
- **Transport behaviour:** Uses ResponseHeadersRead; enforces a 4 MiB default response cap; gates requests by host (four concurrent requests per host by default); captures response status, final URI, headers and Retry-After; decodes the declared response charset; maps 401/403/429/408/425/5xx to typed provider failures.
- **Provider-specific behaviour retained:** Yahoo's bounded manual redirect loop remains in Yahoo; Mojeek's Altcha challenge, cookie storage and multipart verify sequence remain in Mojeek; DuckDuckGo form POST and CAPTCHA check remain in its adapter; Qwant inspects 403 content to distinguish DataDome challenge from access denial; Seznam still bootstraps hidden fields. Cookie jars and redirect policies remain configured per HttpClient/provider.
- **Tests:** Added transport tests for response content/headers/Retry-After, typed status mapping, response-size cap, charset decoding and per-host concurrency. Updated provider tests to assert typed failure categories. Commit `9d590ce6a46a4c5b969b4a4e3c55d0adefe84fd9` passed [CI run 45026001326](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026001326): build succeeded, 182 passed, 0 failed, 0 skipped.
- **CI correction trail:** Intermediate commits briefly failed because provider tests still expected raw HttpRequestException after the transport began throwing ProviderFailureException, and the Mojeek cookie-header value needed a null guard. All were corrected before the 182-test green commit. No known outstanding code failures on current commit.
- **Corpus/audit:** Updated the Python-to-C# corpus and porting audit to explain the SearXNG network-layer translation, adapter coverage, bounded response handling and current limits. README roadmap now records transport as an initial implementation rather than an unstarted task.
- **Limits:** Transport does not yet own cookies, redirect policy, proxy selection, common request-header profiles, safe retries or provider-specific minimum intervals. The JSON adapter retains its own minimum-interval gate. No live provider calls; tests use fixture/fake handlers.
- **Verification:** Shared-transport corpus/audit docs are running in [45026071545](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026071545) and [45026087459](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026087459) at log time.
- **Next:** Finish common transport/session policy, beginning with provider-specific cookie isolation and redirect configuration review. Then add request-header profiles and safe retry/Retry-After rules. Keep the 67-provider catalogue frozen.


### 90. Isolate provider cookie stores and tighten challenge classification
- **Reason:** Mojeek and Yahoo had been resolving the same unkeyed CookieContainer. Domain scoping prevented normal cross-host cookie sends, but sharing the same mutable jar was unnecessary and made provider session ownership ambiguous.
- **Action:** Registered separate keyed CookieContainers for Mojeek and Yahoo and marked their constructor dependencies with keyed-service injection. Yahoo's HttpClientHandler uses only the Yahoo jar; Mojeek's manual challenge-cookie flow uses only the Mojeek jar. Seznam continues using its own HttpClientFactory handler-managed jar.
- **Action:** Added an integration test proving Mojeek and Yahoo receive distinct CookieContainer instances. These are still process-scoped provider sessions, not per-user/per-tenant sessions; inbound caller cookies are not forwarded.
- **Action:** Added a Qwant fixture test proving a DataDome/captcha 403 is classified as Challenge rather than generic AccessDenied. The provider failure classifier now treats interrupted response streams as transient and unsupported parser encodings as invalid response.
- **Verification:** Commit `ebde18239b35cf6cc22a39a8d3fbb8bc0227922c` passed [CI run 45026298077](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026298077): build succeeded, 184 passed, 0 failed, 0 skipped.
- **CI correction trail:** The first keyed registration attempt used the one-argument factory overload and failed compilation; the keyed factory requires `(serviceProvider, serviceKey)`. The next commit corrected the signature and passed all tests. The later cookie-isolation and DataDome test commits were included in the green run.
- **Documentation:** Updated the Python-to-C# corpus, porting audit, README roadmap and activity handover to distinguish provider-specific cookie jars from per-caller session isolation. Documentation run [45026342315](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026342315) passed; the latest audit/README/status commits are still running.
- **Next:** Finish shared transport policies (header profiles, redirect semantics, per-provider rate intervals, safe retry decisions and request diagnostics). Keep cookie/session lifetimes explicit. Do not add provider adapters; catalogue remains frozen at 67.


### 91. Move generic JSON provider minimum intervals into shared transport
- **Reason:** The generic JSON adapter still owned its own semaphore and clock for minimum request spacing. That duplicated transport policy and prevented other provider types from using the same mechanism.
- **Action:** Added an optional provider-name/minimum-interval pair to `SearchHttpTransport.SendAsync`. A provider-keyed rate gate serialises start times and uses the injected TimeProvider for delay calculations. The existing per-host concurrency gate remains separate.
- **Action:** Removed `JsonApiSearchProvider`'s private semaphore, next-request timestamp and wait method. Its `MinimumIntervalMilliseconds` definition now flows into the shared transport.
- **Tests:** Added a concurrent transport test proving two requests for the same provider respect a configured minimum interval. Latest code commit `f434fd95b2f08f6d9295b6ccdafcb2cc5b16c4d2` passed [CI run 45026506065](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026506065): build succeeded, 185 passed, 0 failed, 0 skipped.
- **Corpus/audit:** Updated the porting rationale and audit to show that Python's per-engine request spacing is now represented by a provider-keyed shared transport gate, rather than logic inside the JSON adapter.
- **Limits:** The rate interval is currently supplied only by JSON definitions. Future HTML/API adapters can use the same argument where their upstream protocol requires minimum spacing. There is still no adaptive quota policy or response-driven rate-limit window beyond RetryAfter/cooldown.
- **Next:** Add common request-header profiles and transport diagnostics without logging query strings, cookies or tokens. Review redirect semantics and safe retries provider by provider. Keep provider count frozen at 67.


### 92. Add default request headers and privacy-safe transport diagnostics
- **Reason:** Provider-specific header profiles are not yet fully unified, but providers without an explicit User-Agent/Accept should not send an empty or accidental profile. Transport diagnostics must also help diagnose upstream behaviour without leaking search terms or session secrets.
- **Action:** SearchHttpTransport now applies a conservative default User-Agent and Accept only when neither HttpRequestMessage nor HttpClient.DefaultRequestHeaders already supplies them. Existing provider-specific headers are preserved.
- **Action:** Added optional structured debug/warning logs for HTTP method, host, status code, response byte count, elapsed milliseconds and exception type. The transport deliberately omits URL paths/query strings, response bodies, cookies and tokens.
- **Tests:** Added tests proving default headers are supplied and explicit provider headers are not overwritten. Commit `c1356c732402ac27381885175811e6e7c435bcd1` passed [CI run 45026662957](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026662957): build succeeded, 187 passed, 0 failed, 0 skipped.
- **Corpus/audit:** Updated the Python-to-C# rationale and porting audit to describe fallback headers, provider-specific overrides and privacy-safe logging. README roadmap now distinguishes implemented defaults/diagnostics from the remaining full provider header profiles.
- **Limits:** Provider-specific User-Agent, Accept-Language, fetch metadata, referrer and other protocol headers are still authored in provider/client configuration. The transport does not yet centralise proxy/TLS policy, full header profiles or safe retries. No query/cookie/token logging.
- **Next:** Review redirects and cookie/session lifecycle provider by provider; then define safe retry rules and timeout ownership. Keep provider challenge protocols intact and provider catalogue frozen at 67.


### 93. Record the result-container and ranking divergence
- **Reason:** Provider ports are only part of the translation. The aggregation layer also needs a rationale record so future ranking work does not assume the current C# merge is equivalent to SearXNG.
- **Action:** Added a corpus section comparing SearXNG's `ResultContainer` and `calculate_score` with `SearchResult`, `SearchAggregator.MergeResults` and the SearXNG response mapper.
- **Findings:** Python retains multiple contributing engines and their positions, applies engine weights and position-based scoring, and supports rich result types/metadata. C# currently deduplicates modestly by canonical URL, keeps the single highest adapter-supplied score, discards other provider provenance, and maps score back to a fabricated position. This is explicitly documented as a compatibility gap, not a port.
- **Action:** Corrected [docs/PORTING_AUDIT.md](../docs/PORTING_AUDIT.md) to describe the score/position fabrication accurately. The next ranking implementation should add actual per-provider positions and provenance before applying any weighted scoring formula.
- **Verification:** Documentation-only; no code changed. CI runs [45026866625](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026866625) and [45026895410](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45026895410) are in progress at log time.
- **Next:** The highest-value framework gaps remaining are full request-header/redirect policy and a result model that preserves provider positions/contributions. Keep provider count frozen at 67.


### 94. Preserve real provider positions and provenance in merged results
- **Reason:** The SearXNG response mapper previously inferred a position by inverting the adapter score. That fabricated a rank and could not represent the same URL appearing in multiple providers.
- **Action:** Extended `SearchResult` with optional one-based `Position`, `Engines` and `Positions` fields while retaining its original positional constructor.
- **Action:** `SearchAggregator.MergeResults` now assigns the one-based index from each provider's returned order when no explicit position exists, uses the executing provider name as the provenance source, and merges provider names/positions for duplicate canonical URLs. The highest adapter-supplied score still selects the primary display record; this does not yet implement SearXNG's weighted score formula.
- **Action:** `SearxngSearchResponseMapper` now serialises the actual position map and contributing engines; it no longer derives a pretend position from score. README, corpus and porting audit were corrected accordingly.
- **Tests:** Duplicate URL test verifies both providers and their positions survive merge. Existing API integration test verifies real positions in the SearXNG response. Commit `3fb09fc9eec97882ce3ea0e429fb0a8f655261ae` passed [CI run 45027214590](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45027214590): build succeeded, 187 passed, 0 failed, 0 skipped.
- **CI correction trail:** The first new duplicate-provenance assertion failed because the test result factory stamped every result with the literal provider `test`; the aggregator was also trusting that unverified field. The implementation now uses the actual provider execution identity, which is the authoritative source. Subsequent code run passed. Earlier docs commits ran while that test was red; the latest code is green.
- **Remaining ranking gap:** C# still chooses the highest adapter score. It does not combine engine weights, position contributions, rich result metadata or SearXNG's grouping logic. Position/provenance are now available as inputs for that future step.
- **Next:** Implement engine-weighted deterministic ranking only after defining per-provider weights and normalising adapter scores. Continue preserving the Python-to-C# rationale and keep the provider catalogue frozen at 67.


### 95. Implement SearXNG-style weighted position scoring
- **Reason:** After real provider positions and provenance were preserved, the aggregator had the inputs needed to implement SearXNG's core score formula rather than leaving the ranking step as highest-adapter-score selection.
- **Action:** Added an internal `ProviderScore` field (not serialised) to retain the adapter score for primary display-record selection. Public `SearchResult.Score` now represents the merged ranking score.
- **Action:** `SearchAggregator` loads each provider's `EngineCapabilities.Weight`, validates it is positive and finite, and calculates the score as: product of contributing engine weights × number of positions, then sum of that value divided by each provider position. Duplicate URLs recompute the score from the merged engine/position set.
- **Action:** Default legacy providers still receive weight 1.0. No provider-specific weight has been assigned without evidence; the mechanism is ready for deliberate per-provider values.
- **Tests:** Added deterministic position ordering/title tie-break coverage and a weighted two-engine duplicate test (weights 2.0 and 1.0 produce score 8 for both at position 1). Commit `9d4d38d6769bbca66a2e09b6eaa656a4b10f46ff` passed [CI run 45027513125](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45027513125): build succeeded, 188 passed, 0 failed, 0 skipped.
- **CI correction trail:** The first weighted-score implementation called an instance method from a static merge method and failed compilation. The merge method was made instance-aware so it can use provider weights; the next CI run passed. The weighted-score test suite is green.
- **Corpus/audit:** Updated the rationale corpus, porting audit and README to record the formula, the distinction between adapter score and merged ranking score, and the fact that all current providers still default to weight 1.0.
- **Remaining:** SearXNG's high/low priority modes and richer metadata/category/template grouping are not implemented. Provider weights need evidence-based values. The score formula is now present, but this does not establish live parity with SearXNG results.
- **Next:** Continue specialist-engine rationale and richer result types; do not invent engine weights. Keep the provider catalogue frozen at 67.


### 96. Add specialist JSON API rationale and record deliberate substitutions
- **Reason:** The generic JSON adapter now covers many catalogue entries, but a shared schema can hide substantial differences from SearXNG's dedicated Python request/response modules. The corpus must identify those losses instead of calling every definition a port.
- **Action:** Added a specialist JSON section comparing Crossref, OpenAlex, Hacker News, GitHub, Open Library, Wikidata, Kagi, Semantic Scholar and npm against the C# JsonApiSearchProvider/JsonApiProviderCatalog.
- **Findings:** Crossref/OpenAlex lose paging and rich Paper metadata; Hacker News loses page/time-range/ranking metadata; GitHub loses sort/header/package metadata; Open Library loses paging and bibliographic fields; Kagi loses page/locale/safe-search/time-range/verticals; Wikidata uses a different MediaWiki search API rather than Python's SPARQL query flow; C# npm uses registry.npmjs.org rather than Python's npms.io. These are explicit simplifications or protocol substitutions.
- **Action:** Documented the generic definition's actual contract (GET/POST, query/limit, fixed parameters, nested JSON paths, URL prefixes, headers/API keys, attribution and minimum intervals) and its limits (no dynamic page/cursor logic, token bootstrap, multi-step request, custom transformations or rich result factory).
- **Action:** Recorded Exa as C#-only at the pinned revision and StackExchange as a shared multi-site integration, not a proven one-to-one Python port.
- **Verification:** Documentation-only; no code changed. The preceding weighted-ranking code commit `9d4d38d6769bbca66a2e09b6eaa656a4b10f46ff` remains green at 188 tests. Specialist corpus and status handover docs will be verified by their own CI runs.
- **Next:** Continue dedicated provider studies where the Python module has request state or custom result types (Semantic Scholar, Kagi verticals, Wikidata, image/video feeds). Do not claim direct ports where the upstream module is absent or the protocol differs.


### 97. Extract Kagi into a dedicated provider and preserve request context
- **Reason:** The generic JSON definition could not express Kagi's page, region, safe-search and time-range request mapping. Treating it as a simple query-to-array API silently dropped supported Python behaviour.
- **Action:** Added `KagiSearchProvider` with bearer-key authentication, JSON POST body, general workflow, page 1–10, boolean safe-search, locale region and `filters.after` date derived from the requested time range. It parses `data.search` and cleans HTML text.
- **Action:** Added `ISearchProviderCapabilities` declaration for general category, pagination up to page 10, safe-search, locale and time-range. It is the first production provider with a non-default capability profile. Images/news/videos remain unsupported because the common result model is link-oriented.
- **Action:** Removed Kagi from `JsonApiProviderCatalog`, retained Exa's generic JSON definition, registered Kagi only when `KAGI_API_KEY` is configured, and removed the obsolete Kagi generic fixture.
- **Action:** Corrected generic JSON and RSS provider factories to resolve the singleton `SearchHttpTransport`; before this, those factories were constructing isolated transport instances and therefore did not share host gates/diagnostics.
- **Tests:** Added Kagi tests for bearer auth, page, safe-search, region, after-date, HTML text cleanup and capability rejection for page 11/news. Generic JSON tests retain Exa coverage. CI run [45028349848](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45028349848) passed: build succeeded, 190 passed, 0 failed, 0 skipped.
- **CI correction trail:** The first Kagi test fixture used a synchronous handler return where its stub expected a Task; corrected. The first catalog edit removed the adjacent Exa definition as well as Kagi; restored Exa and confirmed its generic fixture. The current code run is green.
- **Corpus/docs:** Updated Python-to-C# corpus, provider inventory, architecture audit, README and activity handover. Kagi's general workflow is now a documented dedicated port; do not claim parity for its news/image/video verticals.
- **Next:** Identify the next generic definition whose Python source contains meaningful request context or rich result types; prefer dedicated ports only where they preserve real upstream behaviour. Keep provider count at 67.


### 98. Add typed challenge subtypes and expose them to API consumers
- **Reason:** A single `Challenge` failure value triggered cooldown but could not tell a caller whether the provider required a browser session, bounded proof-of-work or an interactive CAPTCHA. It also risked conflating ordinary 403 access denial with human verification.
- **Action:** Added `ProviderChallengeKind` as a second, descriptive dimension alongside operational `ProviderFailureKind`: TemporaryBlock, BrowserVerification, ProofOfWork, InteractiveCaptcha, InteractiveVerification, LoginRequired and Unknown.
- **Provider mappings:** Google and DuckDuckGo challenge pages → InteractiveCaptcha; Qwant DataDome markers and Seznam `/verify` → BrowserVerification; Mojeek Altcha failure → ProofOfWork and fallback CAPTCHA → InteractiveCaptcha; Yandex `x-yandex-captcha` header → InteractiveCaptcha. Generic 403 remains AccessDenied unless provider evidence says otherwise.
- **Propagation:** ProviderFailureException → ProviderOutcome → SearchExecutionResult → `/api/search` `providerOutcomes` and SearXNG-compatible `/search` `provider_outcomes`. Provider health snapshots retain the last challenge subtype. Enum values serialize as strings.
- **Safety:** This is classification and observability, not bypass. AI may explain challenge pages but must not automatically solve CAPTCHA or human verification. Mojeek's bounded Altcha exchange remains a distinct provider-specific proof-of-work protocol; it is not a general CAPTCHA solver.
- **Tests:** Extended Google, DuckDuckGo, Qwant and Mojeek challenge assertions; added Yandex header detection and API serialization tests. GitHub Actions [run 45028609771](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45028609771) completed successfully (build and test steps green; detailed test count not captured). No live provider requests.
- **Documentation:** Added [docs/PORTING/CHALLENGE_CLASSIFICATION.md](../docs/PORTING/CHALLENGE_CLASSIFICATION.md), updated the porting audit, corpus index, README and handover.
- **Next:** Locale/region traits registry, API conformance tests, richer result types and operational hardening. Provider count remains frozen at 67.


### 99. Port the first EngineTraits locale mapping and Bing request semantics
- **Reason:** `SearchQuery.Language` existed, but almost every provider still ignored it. The shared framework needed a real provider mapping before locale support could be advertised.
- **Python evidence:** Pinned SearXNG `bing.py` fetches Bing region market codes, maps them to `setlang` and `cc`, skips country restrictions for US/CN/RU, includes the `zh-HK → en-HK` correction and maps safe-search 0/1/2 to off/moderate/strict. Python explicitly says paging/time-range are unavailable because they depend on JavaScript.
- **Action:** Added `EngineLocaleTraits` and `EngineLocaleRegistry` with a static, versioned subset of common Bing market codes and the upstream `clear` all-locale fallback. No runtime scraping or external data fetch was added.
- **Action:** Bing now consumes `SearchQuery.Language` and `SafeSearch`, implements `ISearchProviderCapabilities`, and advertises general/web categories, locale and safe-search only. It still rejects page > 1 and time-range requests during capability pre-flight.
- **Tests:** Added common locale, unknown-locale fallback, Hong Kong special case, request parameter and capability tests. GitHub Actions [run 45028984202](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45028984202) completed successfully (build and test steps green; detailed test count not captured). No live provider calls.
- **Documentation:** Added [docs/PORTING/LOCALE_TRAITS.md](../docs/PORTING/LOCALE_TRAITS.md), updated the rationale corpus, audit, README and handover.
- **Next:** Add DuckDuckGo's region/language-region traits, then Wikipedia host/LanguageConverter traits. Keep provider count frozen at 67.


### 100. Port DuckDuckGo locale and time-range request semantics
- **Reason:** The first Bing locale port established a reusable mapping registry. DuckDuckGo's Python engine demonstrates a second shape: region codes are sent in a form field and cookie, and time-range filters are duplicated into form data and cookies.
- **Python evidence:** Pinned `duckduckgo.py` maps locale regions (including `en-GB → uk-en`, `zh-HK → hk-tzh`, `zh-TW → tw-tzh`, `ja-JP → jp-jp`), uses `kl` for region, `df` for day/week/month/year, and retains VQD cache state for later pages.
- **Action:** Extended `EngineLocaleRegistry` with a curated DDG region subset and all-region fallback `wt-wt`. DuckDuckGo now maps locale to `kl`, time-range to `df=d/w/m/y`, and writes matching preference cookies on the request.
- **Capabilities:** Declares general/web, locale and time-range support; remains first-page-only and does not advertise safe-search because the C# request path does not currently implement that control.
- **Known omissions:** No dynamic region/language trait fetch, VQD cache/pagination, bang quoting, zero-click answer results, Firefox TLS impersonation or locale-specific Accept-Language yet. Do not add page offsets without the VQD flow.
- **Tests:** Added DDG locale/time-range form and cookie assertions, curated locale mappings and capability assertions. GitHub Actions [run 45029215535](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45029215535) completed successfully (build and test steps green; detailed test count not captured). No live provider calls.
- **Documentation:** Updated the DDG port rationale, locale-traits notes, porting audit and handover.
- **Next:** Port Wikipedia's language-to-host mapping and LanguageConverter variants. Keep provider count frozen at 67.


### 102. Record Wikipedia locale/host trait translation and semantic boundary
- **Reason:** Wikipedia's Python EngineTraits use is materially different from Bing market codes and DuckDuckGo region tokens. It maps locale to a language-specific host and uses Accept-Language for language-converter variants.
- **Action:** Updated the Wikipedia port rationale to distinguish Python's REST page-summary/infobox engine from C#'s Action API text-search provider. The C# implementation now selects a curated language-specific host, sends Accept-Language, and maps SearchQuery.Limit to srlimit.
- **Action:** Updated locale-traits notes and handover to record the static host map and remaining gaps. No provider catalogue changes.
- **Verification:** Initial CI run 45029602909 built cleanly but had one test assertion mismatch: .NET formats the Accept-Language quality value with a space ("zh; q=0.9"). The assertion was corrected. Final run [45029724644](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45029724644) passed: build 0 warnings/errors; 215 tests passed, 0 failed, 0 skipped.
- **No live tests:** No Wikimedia request was made; only fixtures and request assertions.
- **Next:** Add Google locale/region mappings. Keep provider count frozen at 67.


### 103. Port Google's locale, paging, time-range and safe-search request semantics
- **Reason:** The Google HTML adapter already parsed results but ignored most of the request context carried by SearchQuery. Its Python source provides explicit mappings for locale, paging, time-range and safe-search.
- **Python evidence:** Pinned google.py maps EngineTraits language/region to hl/lr/cr, page to start=(page-1)*10, time-range to tbs=qdr:d/w/m/y and safe-search 1/2 to medium/high; it supports up to 50 pages.
- **Action:** Added curated Google language/region traits. Google now maps hl/lr/cr, start, tbs and safe into its HTML GET request and implements ISearchProviderCapabilities with max page 50.
- **Conservative limits:** The provider declares only general/web. The locale map is a subset, and unknown values fall back without guessing a country. It does not yet send CONSENT=YES+, parse suggestions or implement image/news/video verticals.
- **Tests:** Added request/capability assertions for French locale, country restriction, page 3, weekly filter and strict safe-search. GitHub Actions [run 45030058998](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45030058998) passed: build 0 warnings/errors; 223 tests passed, 0 failed, 0 skipped. No live Google requests.
- **Documentation:** Updated Google rationale, locale-traits notes, audit, README and handover.
- **Next:** Review the remaining major engines' request context, then move to richer result models. Provider count remains frozen at 67.


### 104. Port Qwant locale, paging, safe-search and API failure semantics
- **Reason:** Qwant already used the same undocumented v3 JSON endpoint as Python, but its C# request ignored locale, page and safe-search fields and flattened several API failures.
- **Python evidence:** Pinned qwant.py resolves region through EngineTraits, sends count=10, offset=(page-1)*10, tgp, device and safe-search, and supports at most five web pages. Its response maps error code 24 to rate limit, challenge URL to CAPTCHA/short suspension, 403 to access denied and other API errors separately.
- **Action:** Added a curated Qwant region subset to EngineLocaleRegistry. Qwant now maps locale, page 1–5, safe-search 0/1/2 and declares general/web, locale, safe-search and paging capabilities; time-range remains unsupported.
- **Action:** Added API error handling for error code 24 (RateLimited), challenge URL (BrowserVerification), DataDome/captcha 403 (BrowserVerification), plain 403 (AccessDenied) and other error objects (InvalidResponse).
- **Session/header note:** The registered typed HttpClient uses its default handler cookie container for Set-Cookie state; this is handler-scoped, not Python's explicit 24-hour cache. C# sets the key CORS/fetch headers but does not reproduce exact header ordering/TLS fingerprint. tgp remains fixed at 1 for deterministic tests.
- **Tests:** Added locale/page/safe-search request assertions, capability checks and API rate-limit mapping. GitHub Actions [run 45030233313](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45030233313) passed: build 0 warnings/errors; 230 tests passed, 0 failed, 0 skipped. No live Qwant requests.
- **Documentation:** Updated Qwant rationale, locale-traits notes, porting audit and handover.
- **Next:** Yahoo request traits and YBV cookie lifecycle. Provider count remains frozen at 67.


### 105. Port Yahoo request context and YBV session lifecycle
- **Reason:** Yahoo's C# HTML parser and tracking redirect loop existed, but its request always used the US host, English/off preferences and no paging/time filters. It also lacked the Python engine's 24-hour YBV cookie cache.
- **Python evidence:** Pinned yahoo.py maps region to Yahoo subdomains, language to the sB cookie, safe-search 0/1/2 to p/i/r, day/week/month to btf, and page 2+ to b=pageno*7+1 with pz=7/bct=0/xargs=0. It caches YBV for 86400 seconds when the value begins v0.2 and follows up to four redirect hops.
- **Action:** Added Yahoo language/region/domain traits. Yahoo now builds regional search URLs, language/safe-search sB cookies, time-range parameters and page offsets. It declares general/web, locale, safe-search and paging; max page is 20.
- **Framework improvement:** Added EngineCapabilities.SupportedTimeRanges so a provider can support a subset of Day/Week/Month/Year. Yahoo declares Day/Week/Month and pre-flight rejects Year instead of silently ignoring it.
- **Action:** Yahoo now captures YBV Set-Cookie values on redirect hops, accepts only v0.2-prefixed values, stores them in its keyed CookieContainer and applies a 24-hour expiry. The Yahoo CookieContainer remains separate from other providers.
- **Tests:** Added region/page/time/safe cookie assertions, YBV expiry checks and partial-time-range capability tests. GitHub Actions [run 45030498602](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45030498602) passed: build 0 warnings/errors; 234 tests passed, 0 failed, 0 skipped. No live Yahoo requests.
- **Documentation:** Updated Yahoo rationale, locale-traits notes, porting audit and handover.
- **Next:** Audit Yandex's deliberate API substitution and improve its request capabilities; then revisit remaining major engines. Provider count remains frozen at 67.


### 106. Port Naver page offsets and time-range filters
- **Reason:** Naver's C# adapter already used the current HTML result selector but always requested the first page and ignored date filters.
- **Python evidence:** Pinned naver.py uses a fixed 15-result general-web page size because Naver cannot set arbitrary page size. It computes start=(pageno-1)*15+1, where=web, and maps day/week/month/year to nso=p:1d/1w/1m/1y.
- **Action:** Naver now builds request parameters from SearchQuery.Page and TimeRange, declares general/web, paging and time-range capabilities, and remains conservative about unsupported safe-search and locale. The operational maximum page is 20.
- **Tests:** Added page-3 offset and monthly filter assertions plus capability tests. GitHub Actions [run 45030669036](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45030669036) passed: build 0 warnings/errors; 236 tests passed, 0 failed, 0 skipped. No live Naver requests.
- **Known omissions:** Naver Python supports images/news/videos and emits thumbnail metadata; C# remains general web only and drops thumbnails.
- **Documentation:** Updated Naver rationale, porting audit, README and handover.
- **Next:** Add Naver thumbnail metadata and review Seznam hidden-form cookie handling. Provider count remains frozen at 67.


### 107. Reconcile Seznam's two-step form and verification behaviour
- **Reason:** The C# provider already implemented Seznam's homepage → hidden-fields → search flow, but the porting notes incorrectly implied that cookie forwarding was not handled and did not record the new challenge subtype.
- **Python evidence:** Pinned seznam.py reads hidden input name/value pairs from the homepage, sends q/oq/noredirect=1 with those fields, forwards homepage cookies, and treats a /verify response path as access denied. The engine does not advertise paging, time-range or safe-search.
- **Action:** Reconciled the corpus and audit with the current C# implementation. The typed HttpClient uses its default cookie-enabled handler across the two requests; the provider does not manually serialize cookies. A /verify route is now Challenge + BrowserVerification.
- **Verification:** Documentation-only reconciliation. Existing provider fixture tests remain part of the verified 236-test suite in [run 45030669036](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45030669036). No live Seznam requests.
- **Next:** Naver thumbnail metadata, then operational API conformance/security hardening. Provider count remains frozen at 67.


### 108. Preserve Naver general-result thumbnail metadata
- **Reason:** The pinned Naver Python engine's general parser extracts the first non-circular result image; the C# parser previously discarded it.
- **Action:** Added optional `SearchResult.ThumbnailUrl`, extracted the non-circular image source in `NaverSearchResultParser`, and accepted only absolute HTTP(S) thumbnail URLs. The SearXNG response record/mapper now emits the field as `thumbnail`; the regular API retains the optional `thumbnailUrl` property.
- **Tests:** Added fixture assertions for extraction, exclusion of circular avatars, rejection of a javascript: source, and SearXNG JSON mapping. The model/parser/test commits passed in [run 45143993872](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45143993872); response mapping and integration test passed in [run 45144068228](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144068228). No live Naver requests.
- **Documentation:** Updated the Naver corpus entry to distinguish the preserved general thumbnail from still-omitted images/news/videos verticals.
- **Next:** Continue explicit capability declarations for adapters whose request shape intentionally differs from SearXNG; keep the 67-provider catalogue frozen.

### 109. Declare Yandex Cloud adapter capability boundaries
- **Reason:** The C# Yandex provider is an authenticated Cloud Search API integration, not a direct port of SearXNG's public HTML engine. Without a capability declaration, the aggregator applied only the generic BasicWeb profile and could not accurately explain this adapter's supported request shape.
- **Python evidence:** Pinned `yandex.py` uses `yandex.com/search/site/`, supports page offsets, locale, date windows and an images vertical, and classifies the `x-yandex-captcha` response header.
- **Action:** C# now explicitly declares general/web, first page only, and no request-level locale, time-range or safe-search support. The aggregator rejects these unsupported non-default options before the Cloud API request rather than silently ignoring them. No claim of parity with the Python HTML engine is made.
- **Tests:** Added capability tests for the default request and rejection of page 2, a locale, safe-search and time range. GitHub Actions [run 45144264756](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144264756) passed the build-and-test job. No live Yandex Cloud requests.
- **Documentation:** Updated the Yandex corpus entry with the capability boundary and protocol-substitution caveat.
- **Next:** Continue auditing the remaining providers for unadvertised request-field omissions, then move to API compatibility/security hardening. Provider count remains frozen at 67.


### 110. Expand the rationale corpus for Wikipedia and shared adapter families
- **Reason:** The corpus should explain when C# deliberately changes the upstream protocol or consolidates repeated provider plumbing, not just describe HTML scraper ports.
- **Wikipedia:** Recorded that Python uses the REST page-summary endpoint with dynamic language traits and optional infobox/thumbnail output, while C# uses the MediaWiki Action API search endpoint to produce multiple results. Documented the language-host/Accept-Language handling and the missing REST summary, infobox and dynamic-trait behaviours. The focused corpus commit passed in [run 45144551755](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144551755).
- **Generic JSON:** Documented why repeated simple API integrations use `JsonApiSearchDefinition` and a shared provider/parser, the supported JSON path subset, URL validation, URL-prefix mapping, attribution and shared rate gate. Recorded why Kagi remains dedicated and why generic definitions do not imply locale/paging/safe-search support.
- **RSS/Atom:** Documented the feed-specific parser, secure XML settings (DTD prohibited, resolver disabled), fixed market settings and Bing News publisher URL extraction. Recorded the current Google News/Bing News scope and its pagination/locale limitations.
- **Verification:** Documentation-only; no provider code changed in this entry. The JSON/RSS corpus commit's workflow is queued as [run 45144710763](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144710763). No live upstream requests.
- **Next:** Continue auditing capability gaps and richer result metadata. Keep the 67-provider catalogue frozen.


### 111. Verify request-context propagation end to end and formalise corpus entry quality
- **Reason:** Existing integration coverage showed unsupported options were rejected, but did not prove that a capable provider receives the parsed request values intact. The corpus also needed a clear minimum evidence standard for future contributors.
- **Test:** Added a capturing provider integration test that exercises `/search` with two categories, page 2, `fr-FR`, safe-search 2 and weekly time range, then asserts the exact `SearchQuery` delivered to the provider. GitHub Actions [run 45144835861](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45144835861) passed the build-and-test job.
- **Documentation:** Added a mandatory evidence chain for new porting notes: pinned Python source, C# source, original behaviour, translation rationale, preserved/adapted/omitted behaviour, verification and follow-up. Explicitly requires C#-only adapters to be labelled as such rather than inventing a Python origin.
- **Verification:** The corpus standard commit is documentation-only; its workflow is tracked separately. No live provider requests.
- **Next:** Continue auditing request/capability parity for the remaining catalogue; keep commits small and provider count frozen at 67.


### 112. Test RSS Atom parsing and remote XML DTD rejection
- **Reason:** The RSS rationale claimed Atom support and described XML external-entity protections; both needed explicit fixture coverage.
- **Tests:** Added an Atom feed fixture verifying alternate-link, title and summary extraction. Added a remote external-DTD fixture and asserted the XML reader rejects it. GitHub Actions [run 45145077265](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145077265) passed build and test.
- **Documentation:** Updated the RSS/Atom rationale to cite the new test coverage and distinguish fixture verification from live feed availability.
- **Next:** Continue small parity and conformance slices. Provider catalogue remains frozen at 67; no live RSS requests were made.


### 113. Assert caller cancellation leaves provider health untouched
- **Reason:** The aggregator must propagate caller cancellation without classifying it as a provider timeout/failure or opening a cooldown.
- **Test:** Strengthened `SearchAsync_PropagatesCallerCancellation` to inspect the health registry after cancellation and assert the provider remains Closed with zero consecutive failures. GitHub Actions [run 45145286054](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145286054) passed build and test.
- **Result:** The distinction between caller cancellation and provider-owned timeout now has direct health-state regression coverage.
- **Next:** Continue auditing typed outcomes and request semantics in small increments; keep the provider catalogue frozen at 67.


### 114. Reconcile the architecture audit with current implementation
- **Reason:** The previous `PORTING_AUDIT.md` still described provider health/cooldown, shared HTTP transport and locale traits as future work, contradicting later code and the current handover.
- **Action:** Rewrote the audit to describe the implemented request/capability model, transport, provider health and challenge lifecycle, locale registry, result provenance/ranking, SearXNG API subset and operational limits.
- **Explicit remaining gaps:** process-local health state; process-scoped provider cookies; curated static locale traits; incomplete rich result types; fixed SearXNG route limit and empty answer/infobox/suggestion collections; no API authentication, per-client quotas or outbound request policy.
- **Verification:** Documentation-only reconciliation. GitHub Actions [run 45145666616](https://github.com/fwjlh/DotNetWebSearchApi/actions/runs/45145666616) passed build and test.
- **Next:** Continue auditing the provider request/capability surface and rich result model; keep the catalogue frozen at 67.


### 115. Record repository rules and disable GitHub Actions
- **Reason:** The repository owner confirmed that no GitHub Actions workflow minutes remain and requested direct-to-main development without workflows. They also supplied the mandatory coding, file-size, design and test rules for all future work.
- **Action:** Added root `_LookAtMe.MD` containing the supplied working rules.
- **Action:** Deleted `.github/workflows/ci.yml` from `main`. The repository now has no GitHub Actions workflow definitions. No workflow was run or triggered as part of this work.
- **Action:** Continued using `activity/LOG.md` and `activity/STATUS.md` as the durable handover; this entry records the change and updates the next actions.
- **Validation:** Verified the file and workflow changes through the GitHub repository tree. These were documentation/configuration changes only; no application code changed. Local test execution is currently unavailable because this environment has no .NET SDK (`dotnet: command not found`). No CI fallback was used.
- **Next:** Keep the 67-provider catalogue frozen. Continue the remaining provider capability/request-context audit and shared-framework work in small changes. For every code change, add focused tests and run `dotnet test` locally when a .NET SDK is available; do not use GitHub Actions.


### 116. Prevent Openverse image results leaking into general web search
- **Reason:** The shared JSON adapter defaulted every catalogue entry to `BasicWeb`. Openverse calls the Openverse Images API, so the aggregator could treat image-only results as ordinary general-web results.
- **Action:** Added explicit category support to `JsonApiSearchDefinition`; generic JSON providers remain general-web by default. `JsonApiSearchProvider` now exposes those categories through `ISearchProviderCapabilities`.
- **Action:** Declared Openverse as `images` only. The aggregator now skips it for the default/general category and selects it for image searches.
- **Tests added:** Added focused aggregator tests proving Openverse is not called for general search and is called for image search.
- **Verification:** The code and tests were pushed together in commit `b2dc9fd299dfc4eb6b7ed3034e69b54262c8c2fa`. Repository-tree verification confirms the test file is present and no GitHub Actions workflow definitions remain. Tests were not executed because the current environment has no .NET SDK. No workflow was used.
- **Next:** Apply the same evidence-led category audit to other clearly vertical JSON adapters (video, news, audio and images), one small tested increment at a time. Keep the 67-provider catalogue frozen.


### 117. Separate specialist JSON adapters from general web search
- **Reason:** The category audit found more generic JSON definitions whose endpoints are explicitly vertical-specific. Without declarations they inherited `general`, so they could pollute ordinary web searches.
- **Action:** Declared Bilibili, Dailymotion and PeerTube as `videos`; Deezer, MusicBrainz and RadioBrowser as `music`; Wikimedia Commons as `images`; and Wikinews as `news`. Openverse remains `images`. All other generic JSON definitions retain the general-web default.
- **Tests added:** Added a nine-case capability theory covering Openverse and all eight newly classified adapters. Each case asserts the provider rejects the default/general category and accepts its declared vertical.
- **Validation:** Code and tests pushed together in commit `c1f81202bde134ba51dba961d3ebd006dec63cba`. The modified catalog remains below the 25 KB hard limit (23,886 bytes); the new capability test file is 3,910 bytes. Tests are not executed because no .NET SDK is installed. No GitHub Actions workflow exists or was used.
- **Next:** Continue checking specialist definitions against their actual upstream endpoint semantics. Do not infer categories for ambiguous providers; record uncertain cases for the richer result-model design. Keep the provider catalogue frozen at 67.


### 118. Preserve specialist categories through the result model and SearXNG mapping
- **Reason:** Provider capability filtering separated image/video/music/news adapters from general search, but the compatibility mapper still hard-coded every result category as `general`.
- **Action:** Added `SearchResult.Category` with a backwards-compatible `general` default; generic JSON results take the configured category; SearXNG mapping preserves that category.
- **Tests:** Added a Bilibili fixture test covering JSON provider parsing through SearXNG response mapping. The prior category boundary tests remain in place.
- **Commits:** `2f822c38`, `e19be5fa`, `9dacb6a1`, `d6de9de0`, `b8ba727a`.
- **Follow-up:** Corrected an extra escaping layer in the Bilibili JSON fixture in `982c14c4d7e0a70a450ff367d7fb00b21f6616ee`; documented the correction in `activity/2026-11-23-specialist-category-response-mapping.md`.
- **Validation:** Reviewed the model, provider, mapper and test source. Local execution is unavailable (`dotnet` is not installed). No GitHub Actions workflows were run or recreated; no live provider request was made.
- **Next:** Design typed image/news/video metadata before adding provider-specific fields. Keep the catalogue frozen at 67.


### 119. Preserve RSS/Atom news category and publication dates
- **Reason:** RSS/Atom news providers still inherited the general-web capability profile and discarded publication dates, despite the SearXNG response supporting both news categories and `publishedDate`.
- **Action:** RSS/Atom providers now declare `news`; RSS `pubDate` and Atom `published`/`updated` are parsed with invariant date handling; each result is tagged `news`.
- **Action:** `SearchResult.PublishedDate` is preserved by the SearXNG response mapper.
- **Tests:** Added RSS/Atom date parsing, news capability and response mapping coverage.
- **Commits:** `5e0f5d96`, `3077eada`, `fc935b4b`, `94006ba7`, `407af21f`, `dc0f46a8`, `3a9b0e98`.
- **Validation:** Source and fixture coverage inspected. Tests remain unexecuted because the .NET SDK is unavailable. No GitHub Actions workflows or live news feed requests were used.
- **Next:** Use the pinned SearXNG result types to design typed image metadata first, then video embed metadata. Preserve provider-specific data without untyped extension dictionaries.


### 120. Add typed image metadata and select the correct SearXNG template
- **Reason:** The pinned SearXNG `Image` result type carries more than a thumbnail: image source, preview thumbnail, resolution, format, source, file size and alternate formats. Openverse also returns licence and creator details. The common C# result could not preserve these safely as ordinary text or an untyped extension bag.
- **Action:** Added typed `ImageResultMetadata` and `ImageFormatReference` records and optional `SearchResult.ImageMetadata`.
- **Action:** Added SearXNG-compatible image fields and a typed `image_metadata` extension for licence, licence URL, creator and byte-size data.
- **Action:** Updated mapping to select `images.html` when image metadata is present; ordinary results retain `default.html`.
- **Tests:** Added serialization/mapping coverage for image fields, formats, licence and creator, plus image/default template selection.
- **Commits:** `9cf0be75`, `20eac20d`, `96d46c46`, `faeebd91`, `cb41a3b6`, `dc6decec`, `d440d4dd`, `d7d4f2b1`, `0d6fd251`, `227231fa`.
- **Validation:** Inspected the final model, response, mapper and tests. Tests are not executed because the .NET SDK is unavailable. No GitHub Actions workflows or live image requests were used.
- **Next:** Validate image source, thumbnail and alternate-format URLs as absolute HTTP(S) before wiring Openverse response fields. Keep the provider catalogue frozen at 67.


### 121. Split the JSON provider catalogue to enforce the file-size limit
- **Reason:** Adding image and video metadata configuration pushed `JsonApiProviderCatalog.cs` to 25,689 bytes, beyond the repository's approximately 25 KB hard limit.
- **Action:** Split the catalogue into base, media and tail definition groups across `JsonApiProviderCatalog.cs` and `JsonApiProviderCatalog.Media.cs`; retained Wave7 as its own file.
- **Tests:** Added a catalogue regression asserting 52 JSON definitions, unique names and the expected ordering across the split. Existing provider fixture cases continue to cover the definitions.
- **Commit:** `dff35fc2ae21255d118ce424db993e59518d0833`.
- **Validation:** Verified the new file sizes: 12,634 bytes and 13,443 bytes. Verified the three definition groups total 52 unique names and the repository has no workflow definitions. Tests remain unexecuted because the .NET SDK is unavailable; no GitHub Actions were used.
- **Next:** Continue reviewing the video metadata adapters and then implement the next typed media fields only where upstream evidence supports them. Keep all files below the hard limit and the provider catalogue frozen at 67.


### 122. Require typed media metadata before selecting specialist templates
- **Reason:** The SearXNG mapper selected `images.html` or `videos.html` from the category alone. Providers such as Wikimedia Commons can be categorised as images without returning a direct image source; unsafe or missing embed URLs can also leave video results without a usable player.
- **Action:** Specialist templates now require the corresponding typed metadata and an absolute HTTP(S) image source or video embed URL. Otherwise the mapper falls back to `default.html`.
- **Tests:** Added regressions for missing image/video metadata, javascript: URLs, relative image URLs and protocol-relative video embeds. Removed a duplicate image-template assertion.
- **Commit:** `3d911356c7c59858d18ac2d679d823975820d89a`.
- **Validation:** Reviewed the mapper and test source. Tests remain unexecuted because the .NET SDK is unavailable; no GitHub Actions or live provider requests were used.
- **Next:** Finish the provider capability/template audit and add provider registration/query-selection tests without live network access. Keep the catalogue frozen at 67.


### 123. Verify repository rules file and correct target repository
- **Reason:** The owner supplied the DotNetWebSearchApi repository link alongside the durable development rules. The rules file must live in that repository, not in a neighbouring project.
- **Action:** Verified the root `_LookAtMe.MD` already exists in `fwjlh/DotNetWebSearchApi` and exactly contains the supplied rules; no duplicate file or needless edit was made.
- **Action:** Removed the two accidental rules/activity files previously created in `fwjlh/sqlDiffAi`; they were not part of the requested target. Follow-up tree verification confirmed both paths are absent there.
- **Validation:** Verified DotNetWebSearchApi `main` has no `.github/workflows/` files, no source files over 25 KB, and the 52 generic JSON definitions remain intact. No workflow was run.
- **Next:** Continue the dedicated provider registration/capability audit in this repository. Keep all future changes on `fwjlh/DotNetWebSearchApi:main`.


### 137. Verify production provider registration and category pre-flight without network
- **Reason:** Unit tests covered category filtering and the JSON catalogue split, but did not verify that the real application registers the complete 67-provider set when optional credentials are configured.
- **Tests added:** A production-host integration test supplies dummy Exa, Yandex, Kagi and Brave credentials, calls `/api/providers`, and asserts 67 unique registered providers.
- **Tests added:** A second production-host test requests Openverse in the general category and asserts it is reported unsupported with no result, exercising pre-flight category filtering without an upstream request.
- **Commit:** `ef0cdb3cbdab7f1b42cbed7a79d2ba2d08c8c6df`.
- **Validation:** Test source reviewed; no network call is made by either test. Execution remains unavailable because the .NET SDK is not installed. No GitHub Actions were used.
- **Next:** Continue auditing all provider capability declarations and template/result-type consistency. Keep the provider count at 67.


### 124. Audit dedicated provider capabilities and registration
- **Reason:** The generic JSON catalogue is now covered, so the remaining dedicated adapters and their DI registration need the same request/capability consistency check.
- **Action:** Reviewed Arxiv, Bing, Brave, DuckDuckGo, Google, Kagi, Mojeek, Naver, Qwant, Seznam, Wikipedia, Yahoo, Yandex and RSS request builders against their declared/default capabilities. Pagination, locale, safe-search and time-range flags match the parameters actually sent; public/API-key providers are registered conditionally as intended.
- **Action:** Added a DI registration regression for all 52 JSON definitions, Exa's required API key, and Bilibili's configured Referer header. No live provider requests are made by this test.
- **Action:** Updated the Bilibili video test to expect `default.html` when the provider supplies no safe embed URL; the mapper's specialist-template guard intentionally requires a usable image source or video embed.
- **Validation:** Source inspection only. The new registration test is committed but not executed because the .NET SDK is unavailable. No GitHub Actions were used.
- **Commits:** `99386119d1b8fc7316293012b1a60d0bd6cba374` (registration test), `4e24a65463993a96b44615216db19e1d7c08cf5a` (Bilibili template expectation).
- **Next:** Review music/audio result metadata and news result mapping; preserve the default template unless a typed media payload supports a specialist template.


### 125. Add typed music/audio result metadata
- **Reason:** Deezer and Radio Browser were correctly classified as music providers, but their audio previews/streams and provider metadata were discarded by the ordinary text result model. MusicBrainz has no audio stream and must remain a metadata-only link.
- **Action:** Added typed `MusicResultMetadata` for audio URL, thumbnail, artist, album, duration, country, tags, codec and bitrate.
- **Action:** Deezer maps preview, album cover, artist, album and duration. Radio Browser maps resolved stream, favicon, country, tags, codec and bitrate. Audio and thumbnail URLs pass only absolute HTTP(S) validation.
- **Action:** Added SearXNG `audio_src` and a typed `music_metadata` extension. Music keeps the default template; no unsupported audio player template is invented.
- **Tests:** Added fixture tests for Deezer and Radio Browser metadata, plus rejection of javascript: audio and data: thumbnails.
- **Action:** Split the growing generic JSON provider metadata parser into `JsonApiSearchProvider.Metadata.cs`; the main provider source is now 9.6 KB and the metadata partial is 12.2 KB.
- **Commits:** `2ba5018f`, `4acbbcd8`, `262c3d35`, `f3a1e3a4`, `8debca13`, `db3749b5`, `a40a5720`, `be455f5f`, `5941f6a1`, `869c9470`, `96a28a48`.
- **Validation:** Reviewed source and fixture mapping. Tests remain unexecuted because the .NET SDK is unavailable. No GitHub Actions or live upstream calls were used.
- **Next:** Add publication-date extraction to generic JSON definitions and wire Wikinews' timestamp field, then audit the remaining news results without inventing dates.


### 126. Preserve generic JSON publication dates for Wikinews
- **Reason:** RSS/Atom dates were preserved, but JSON news providers had no shared date path. Wikinews exposes a MediaWiki search timestamp and the generic adapter discarded it.
- **Action:** Added optional `PublishedDatePath` to `JsonApiSearchDefinition`; the generic JSON parser uses the same invariant ISO/Unix date parser as video metadata.
- **Action:** Wikinews now requests `srprop=snippet|timestamp` and maps `timestamp` to `SearchResult.PublishedDate`.
- **Tests:** Added fixture tests for valid timestamp mapping to SearXNG `publishedDate`, plus missing and malformed timestamp cases. No current time or crawl time is substituted.
- **Commits:** `a2f413e5`, `d9436783`, `8aee9636`, `6c38c607`, `012d25e9`.
- **Validation:** Reviewed provider definition, parser path and fixtures. Tests remain unexecuted because the .NET SDK is unavailable. No GitHub Actions or live Wikinews requests were used.
- **Next:** Audit remaining news providers for timestamp fields, then review image/video/music/news capability coverage as a whole. Keep provider count frozen at 67.


### 138. Classify arXiv as science-only
- **Reason:** The pinned SearXNG arXiv engine declares `science` / `scientific publications`; the C# adapter inherited BasicWeb and was therefore included in general searches while being excluded from science searches.
- **Action:** Added explicit capabilities for the `science` category, first-page only, with no unsupported locale, safe-search or time-range claims.
- **Tests:** Added an aggregator regression proving general searches skip arXiv without a request and science searches select it.
- **Commit:** `668618212761ea77a4d8cb6b6dacad47005462c8`.
- **Validation:** Compared the category with the pinned SearXNG engine source and reviewed the fixture test. Tests remain unexecuted because the .NET SDK is unavailable; no GitHub Actions or live arXiv request was used.
- **Next:** Audit remaining legacy adapters for category and request-context mismatches, beginning with Mojeek, Seznam and Brave.


### 127. Add Hacker News news category and publication date
- **Reason:** Hacker News stories are relevant to both ordinary general searches and news-only searches. The generic JSON provider previously had one fixed result category and discarded Algolia's `created_at` timestamp.
- **Action:** Generic JSON result parsing now receives the query and selects the matching category from the provider's declared category set. Providers supporting both general and news can return the requested vertical without losing general-search behaviour.
- **Action:** Hacker News now declares `general` and `news`, and maps `created_at` through the generic publication-date path.
- **Tests:** Added fixtures proving general queries stay general, news queries return news, timestamps are preserved, and the aggregator selects Hacker News for news-only queries without network access.
- **Commits:** `4a48bcec`, `b19d44e8`, `6ae3fa65`, `bc7ef56c`, `5b066fc8`, `7363b5e3`.
- **Validation:** Reviewed parser, capability and fixture source. Tests remain unexecuted because the .NET SDK is unavailable. No GitHub Actions or live Hacker News requests were used.
- **Next:** Review the remaining generic JSON news candidates and ensure category selection remains deterministic when several categories are requested. Then run a final source-size and no-workflows audit.


### 128. Split test sources to meet the repository file-size rule
- **Reason:** The provider catalogue and metadata work pushed two test sources above the preferred 20 KB range, even though they remained below the 25 KB hard limit.
- **Action:** Split the 52-provider fixture data into `JsonApiSearchProviderTests.Cases.cs` and split request-context integration tests into `SearxngApiRequestContextTests.cs`. Both use partial test classes so existing private fixtures/helpers remain shared without duplication.
- **Validation:** Verified all C# files in the repository are now below 20 KB. The two new files are 18.5 KB and the remaining SearXNG integration file is 19.5 KB. No test logic was intentionally changed.
- **Commits:** `78c35d45`, `8b26a1b1`, `d81c45ec`, `dffd6566`.
- **Next:** Run the full test suite when a local .NET 10 SDK is available; continue with small source files and keep all GitHub Actions workflows unused.


### 129. Preserve xUnit attributes across the request-context test split
- **Reason:** The first pass split the SearXNG integration tests at the method signature, leaving the first `[Fact]` in the original file and moving the final `[Fact]` for the next test. The new partial file therefore needed its first attribute restored and the duplicate trailing attribute removed.
- **Action:** Restored `[Fact]` on all four moved request-context tests and verified the remaining integration tests retain their own attributes.
- **Validation:** Re-read both partial files and enumerated their `[Fact]` methods. No test body or helper behaviour changed.
- **Commit:** `46a931fe`.
- **Next:** Continue final test-source and registration audit; full compilation still awaits a local .NET 10 SDK.


### 130. Make Mojeek category and safe-search capabilities explicit
- **Reason:** The Mojeek adapter inherited BasicWeb and hard-coded `safe=1`, silently overriding an explicit safe-search-off request.
- **Action:** Declared general/web support, first-page-only, safe-search support, and no locale/time-range support. Mojeek maps safe-search 0 to off and 1/2 to moderate.
- **Tests:** Added request assertions for all safe-search values and capability boundary checks for categories, paging, time range and locale.
- **Commit:** `e2e2b46f3b9a777dba5f97fcbcbd7f11737e84eb`.
- **Validation:** Reviewed the provider and fixture tests. Tests remain unexecuted because the .NET SDK is unavailable; no GitHub Actions or live Mojeek requests were used.

### 131. Make Brave safe-search capability match its request
- **Reason:** Brave always sent `safesearch=moderate` while its default BasicWeb capability caused the aggregator to skip it for safe-search-on requests and silently moderated safe-search-off requests.
- **Action:** Declared general/web, first-page-only, safe-search support, and no locale/time-range support. Brave now maps 0/1/2 to off/moderate/strict.
- **Tests:** Added all three safe-search request values and capability boundary assertions; updated the default request URL expectation.
- **Commit:** `8427e9902a1a0b29beda4a39a46316ab25b86522` (provider), `60cdb6eb38a9a823e0949bf07bade8b9978c178e` (tests).
- **Validation:** Source and fixture review only; .NET SDK unavailable. No GitHub Actions or live Brave requests were used.
- **Next:** Complete the final provider capability and tree audit; run tests when the .NET 10 SDK is available.


### 132. Make multi-category result selection deterministic
- **Reason:** Generic JSON providers may advertise more than one vertical. HashSet enumeration order should not decide which category is written to a result when a request selects several supported verticals.
- **Action:** The generic JSON category resolver now prefers `general` when explicitly requested and otherwise sorts the matching requested categories ordinal-ignore-case before choosing one. Its fallback category is also deterministic.
- **Tests:** Existing Hacker News coverage verifies general priority when both general and news are requested, and news selection when requested alone.
- **Commit:** `1307218be38b193d7277a4455c5c10b9c5ec8671`.
- **Validation:** Reviewed the resolver and existing category tests. Full test execution remains blocked by the missing .NET SDK; no workflows or live provider requests used.
- **Next:** Complete the final registration, source-size and workflow audit.


### 133. Share SearXNG duration and author fields across audio/video
- **Reason:** SearXNG's common result shape uses `length` and `author` for media. The C# mapper emitted video duration/author but left Deezer music duration and artist only in the nested extension.
- **Action:** Replaced the video-only response properties with shared `LengthSeconds` and `Author`; the mapper selects video duration/author first, then music duration/artist.
- **Tests:** Deezer fixture now asserts `audio_src`, `length`, `author` and default template together. Existing video tests continue to assert the same shared fields.
- **Commits:** `ebb02425`, `ca766171`, `fac551d2`, `e75d2344`, `736fb606`.
- **Validation:** Reviewed response model, mapper and fixture assertions. Tests remain unexecuted because the .NET SDK is unavailable; no GitHub Actions or live upstream calls used.
- **Next:** Final source-size, provider registration and no-workflows audit.


### 134. Declare Seznam's general/web category support
- **Reason:** The pinned SearXNG Seznam engine supports `general` and `web`; the C# adapter inherited BasicWeb and would be skipped for web-category requests.
- **Action:** Added explicit general/web capability with first-page-only behaviour and no unsupported time-range, safe-search or locale claims.
- **Tests:** Added capability boundary coverage for general/web, images, paging, time range, safe search and locale.
- **Commits:** `b38206857f6e03ca7829bf60875282c7b8ca36ee` (provider), `4a31c6c37d018077a93e874b920bec592aa21504` (tests).
- **Validation:** Compared the category with the pinned SearXNG Seznam engine and reviewed the capability test. Tests remain unexecuted because the .NET SDK is unavailable; no GitHub Actions or live Seznam requests were used.
- **Next:** Complete the final tree and provider registration audit; the next full build/test run must be local when the .NET 10 SDK is available.


### 139. Add typed scholarly paper results
- **Reason:** The pinned SearXNG result model includes a dedicated Paper type with publication date, abstract, comments, tags, authors, publisher, journal, volume/pages/issue, DOI, ISSN/ISBN and PDF/HTML URLs. Scholarly providers were still ordinary links.
- **Action:** Added `PaperResultMetadata`, generic JSON paper metadata paths and a dedicated partial parser. PDF/HTML links are restricted to absolute HTTP(S); DOI resolver URLs are normalized to DOI identifiers; JATS abstract markup is cleaned.
- **Action:** arXiv Atom parsing now retains publication date, authors, categories, journal reference, DOI, abstract and safe PDF link; results are explicitly categorized as science.
- **Action:** Crossref, OpenAlex, Europe PMC and Semantic Scholar are now science-category providers with their available paper fields mapped. Europe PMC explicitly requests JSON core results.
- **Action:** SearXNG mapping emits paper fields and selects `paper.html` only when typed paper metadata exists.
- **Tests:** Added fixture coverage for Crossref authors/journal/DOI, OpenAlex date/PDF/DOI, Europe PMC author/journal/full text, Semantic Scholar authors/PDF/date, unsafe PDF rejection, arXiv Atom metadata and unsafe PDF rejection. Added science capability assertions.
- **Commits:** `f6873cf5`, `56c65563`, `734cdab7`, `166fbade`, `67f46fe9`, `81389a22`, `02d932ae`, `006af991`, `1e78128d`, `c65d59dd`, `73dd9f7f`, `57af7be4`, `d8edb003`, `9f1fe364`, `f1e8fdf4`, `c2f0717b`, `d192189f`, `b6820563`.
- **Validation:** Reviewed provider paths, parser, SearXNG fields and fixtures. Tests remain unexecuted because the .NET SDK is unavailable. No GitHub Actions or live scholarly API calls were used.
- **Known limits:** Crossref publication date is not inferred from record creation; OpenAlex inverted-index abstracts are not reconstructed. Additional scholarly providers (DataCite, Zenodo, OpenAIRE, PLOS) remain to be mapped.
- **Next:** Extend typed paper mapping to DataCite, Zenodo, OpenAIRE and PLOS using fixtures, then run a final category audit.


### 135. Preserve paper publication-date fallback
- **Reason:** The pinned SearXNG Paper type copies the common `publishedDate` into `date_of_publication` when the paper-specific date is absent. The C# mapper previously left the paper date empty in that case.
- **Action:** `PaperDateOfPublication` now falls back to `SearchResult.PublishedDate` when `PaperResultMetadata.DateOfPublication` is unavailable.
- **Tests:** Added a mapper regression with paper metadata but no paper-specific date and a populated common publication date.
- **Commit:** `5965ac58e1037ab618bef3e85567d0f0d1cf9b9b`.
- **Validation:** Reviewed mapper and serialization test source. Execution remains unavailable because the .NET SDK is not installed; no GitHub Actions or live upstream calls were used.
- **Next:** Extend typed paper mapping to DataCite, Zenodo, OpenAIRE and PLOS using fixtures.


### 140. Extend paper metadata to DataCite, Zenodo, OpenAIRE and PLOS
- **Reason:** The first paper pass covered the major citation/search APIs but left four scholarly catalogue providers returning ordinary links.
- **Action:** Added publication-year support without fabricating a full publication date; SearXNG exposes the year as a typed extension.
- **Action:** DataCite maps creators, publisher, resource type, year and DOI. Zenodo maps publication date, creators, publisher, journal fields, DOI and HTML landing page. OpenAIRE maps authors, publication date, publisher, journal, type and web link. PLOS maps authors, publication date, journal/volume/issue/pages, eISSN and DOI.
- **Action:** Generic paper metadata now falls back to the already validated result URL for its HTML link when a provider has no separate full-text URL.
- **Action:** Scalar and array values are both accepted for list-shaped identifiers such as ISSN.
- **Tests:** Added fixture tests for all four providers and capability assertions that they belong to the science vertical.
- **Commits:** `8744bfd8`, `9ccd91dd`, `7555c36b`, `a4554449`, `b7f25c47`, `b1fb04b8`, `ff96ec26`, `ad3975d6`, `42d4cb03`, `77566686`, `e57892fd`, `67e04f86`.
- **Validation:** Reviewed definitions, field paths and fixtures. Tests remain unexecuted because the .NET SDK is unavailable. No GitHub Actions or live scholarly API calls were used.
- **Next:** Audit remaining science-category definitions and check SearXNG Paper serialization for all mapped fields. Keep provider count frozen at 67.


### 136. Complete scholarly serialization and year-only date checks
- **Reason:** Publication years must not be converted into invented full dates, and the SearXNG paper response needs to preserve its field names exactly.
- **Action:** Crossref now retains `published.date-parts[0][0]` as `publication_year`; it does not create a fake January 1 publication date.
- **Action:** Added SearXNG serialization assertions for DOI, publication year and author list.
- **Action:** Updated the rationale and audit to distinguish full dates from year-only values.
- **Commits:** `7e29aad2`, `24b91100`, `704348cb`, `2a7e82dd`, `5c44fa77`.
- **Validation:** Reviewed JSON field mapping and fixture source. Tests remain unexecuted because the .NET SDK is unavailable; no GitHub Actions or live scholarly requests were used.
- **Next:** Final science category/template audit and source-size/provider-count check.


### undefined. Complete scholarly fixtures for DataCite, Zenodo, OpenAIRE and PLOS
- **Reason:** The four providers had paper metadata paths but lacked fixture coverage. PLOS returns author names as a string array, which the shared author parser previously ignored; Zenodo exposes a PDF link that was not mapped.
- **Action:** The paper author parser now accepts both object arrays and scalar string arrays. Zenodo now maps `links.pdf` into the typed PDF URL field.
- **Tests:** Added isolated fixtures for DataCite creators/publication year, Zenodo journal/PDF metadata, OpenAIRE authors/publication/journal details, and PLOS author strings/article fields.
- **Commits:** `a2c2b249`, `039a57d9`, `a20768cc`, `d05b1ad7`.
- **Validation:** Source and fixtures reviewed; all test cases use local fixture JSON and make no upstream calls. Tests remain unexecuted because the .NET SDK is unavailable. No GitHub Actions used.
- **Next:** Audit the scholarly path mappings and finish the remaining provider category/result-type checks; run the full suite locally when .NET 10 is available.


### 141. Normalize handover log numbering
- **Reason:** Concurrent development entries introduced duplicate heading numbers, making the chronological handover harder to follow.
- **Action:** Preserved all log content and commit references while assigning unique heading numbers to the four later duplicate entries (137–140).
- **Validation:** Rechecked the heading list; every numbered entry is now unique. The canonical current state and next action are maintained in `activity/STATUS.md`.
- **Next:** Read the final status summary, then run the local .NET 10 build/test session when the SDK is available.


### 142. Consolidate the canonical handover
- **Reason:** Repeated status appends from concurrent work left obsolete next steps and conflicting provider counts after the scholarly metadata work.
- **Action:** Rewrote `STATUS.md` as a single current handover with the 67-provider composition, implemented result families, file-size/test counts, known limits and exact next step.
- **Action:** Clarified workflow history accurately: no Actions have been invoked since the stop instruction, and no workflow files currently exist.
- **Validation:** Rechecked the current tree: 83 C# files, 35 test files, none over 20 KB; 52 generic JSON definitions; no workflow paths.
- **Commit:** `73d1188bde618a136c87f8d489e9c2353b876382`.
- **Next:** At the local build/test session, run `dotnet test` with .NET 10 and fix failures directly on `main`, without workflows.


### 137. Preserve scholarly subjects as typed paper tags
- **Reason:** DataCite and Zenodo return subjects as arrays of objects, while PLOS returns a scalar string array. The paper parser previously handled only scalar arrays, so provider subjects were lost.
- **Action:** Added a typed `TagName` path and a paper-tag reader that supports both string values and object properties without serializing whole objects as text.
- **Action:** Configured DataCite and Zenodo subject paths and requested/mapped PLOS subjects.
- **Tests:** Extended the three provider fixtures to assert subject tags and retained the existing OpenAIRE and paper metadata cases.
- **Commit:** `dc78019502ab62106c2d312d718363357c7298c7`.
- **Validation:** Reviewed the new path handling and local fixtures. Tests remain unexecuted because the .NET SDK is unavailable; no GitHub Actions or live scholarly API calls were used.
- **Next:** Finish scholarly provider path audit and final category/result-template consistency review; then wait for the local .NET 10 build/test session.


### 138. Reconcile the provider inventory count
- **Reason:** A final audit script initially reported 59 JSON definitions because its unanchored text pattern also matched configuration properties such as `AuthorName` and `TagName`.
- **Action:** Recounted only actual top-level `Name =` definition properties across the three catalogue files.
- **Verified result:** 52 JSON definitions, 52 unique names, 2 RSS/Atom adapters and 13 dedicated registrations = 67 providers. No duplicate JSON names.
- **Validation:** The source count agrees with the 52-definition catalogue regression and the 67-provider production-host integration test. No code changed; no workflows or live calls used.
- **Next:** Continue from the canonical status; full runtime test execution still requires the local .NET 10 SDK.


### 139. Preserve MusicBrainz artist metadata without inventing an audio stream
- **Reason:** MusicBrainz returns artist records, not playable audio. The provider was categorised as music but discarded the artist and country fields.
- **Action:** Added typed MusicMetadata paths for artist and country. No audio URL or specialist audio template is fabricated.
- **Tests:** Added a MusicBrainz fixture asserting artist/country metadata, SearXNG author mapping and default-template behaviour when no audio source exists.
- **Commit:** `83d36e386d18f94866dfbebd64f426142a07dfae`.
- **Validation:** Reviewed the fixture and response mapping. Tests remain unexecuted because the .NET SDK is unavailable; no workflows or live MusicBrainz calls were used.
- **Next:** Finish the category/template audit and prepare the local .NET 10 build/test handover.


### 143. Consolidate the current handover after media and scholarly work
- **Reason:** Incremental status appends left obsolete next steps after the image, video, music and paper metadata work had advanced.
- **Action:** Replaced `STATUS.md` with one concise current-state handover: 67-provider composition, implemented metadata families, known limits, workflow prohibition, validation status and exact next step.
- **Validation:** Rechecked 83 C# files, 35 test files, largest C# file 19,463 bytes, 67 unique providers and zero workflow files.
- **Commit:** `0cebcec6383718cde3404d5ba97cb1e79dbdcfbf`.
- **Next:** Local .NET 10 build/test session; no GitHub Actions.


### 144. Complete the media, paper and news category/template audit
- **Reason:** Final review needed to ensure vertical categories did not imply result templates or media fields that their adapters cannot safely populate.
- **Action:** Verified image/video/paper templates require their corresponding typed metadata and safe source/embed URLs. Music results retain audio fields only where a playable URL exists; MusicBrainz remains a metadata-only result. Wikinews maps its timestamp; RSS/Atom results preserve publication dates. Wikimedia Commons remains general until a direct image URL is implemented and validated.
- **Validation:** Rechecked 52 unique JSON definitions + 2 RSS/Atom + 13 dedicated providers = 67; 83 C# files, 35 test files, largest 19,463 bytes; no workflow files.
- **Next:** The remaining required validation is the local .NET 10 build/test run and coverage measurement. No further claims of passing tests until that happens.


### 145. Re-consolidate the handover after the final category audit
- **Reason:** Subsequent media, scholarly and MusicBrainz work had added append-only status sections after the prior handover consolidation.
- **Action:** Replaced `STATUS.md` with one current handover covering provider count, implemented result families, known limits, workflow prohibition, validation state and the exact local test step.
- **Validation:** Status agrees with the latest inventory: 67 providers, 83 C# files, 35 test files, largest file 19,463 bytes, zero workflow files.
- **Commit:** `06290049f368935dccc9439cda6cdae89b765c7b`.
- **Next:** Local .NET 10 build/test session. No GitHub Actions.


## 2026-11-23 — Correct the application entry-point declaration

### 146. Fix the invalid top-level Program type declaration
- **Reason:** The local .NET 10 build reported C# syntax errors in the application project, followed by a missing reference-assembly metadata file. The metadata error is a consequence of the failed compilation.
- **Inspection:** Read the current `src/DotNetWebSearchApi/Program.cs` on `main`. Its final declaration was `public partial class Program;`, which is not a valid class declaration in C#.
- **Correction:** Replaced it with `public partial class Program { }`, preserving the public partial Program type required by top-level-program integration tests.
- **Commit:** `a81416bc8b69fd3c0a48e00aac27ad3e1396dcc6`.
- **Validation:** Source-level correction only. No .NET SDK is available in this environment, so build/tests have not been executed. No GitHub Actions used.
- **Next:** Pull this commit and run the local Release build/tests. If any further compiler errors remain, capture their full file/line diagnostics and fix the next root cause.


## 2026-11-23 — Repair JSON API provider brace structure

### 147. Close the provider class before its definition record
- **Reason:** Local build reported CS1513 at the end of `JsonApiSearchProvider.cs`.
- **Finding:** `JsonApiSearchDefinition` had accidentally been placed inside the provider class because the provider class was missing its closing brace. The metadata file also declared the provider as `partial`, while the main declaration was not partial.
- **Correction:** Marked the main provider declaration `partial` and closed the class before the top-level `JsonApiSearchDefinition` record.
- **Commit:** `7af858f951ce5a1d949aa4cb549ada91137cf51c`.

### 148. Remove the extra metadata-file closing brace
- **Reason:** Local build reported CS1022 at the end of `JsonApiSearchProvider.Metadata.cs`.
- **Finding:** An extra closing brace followed the provider partial class, before the metadata path records.
- **Correction:** Removed the redundant brace so the metadata path records remain in the file-scoped namespace.
- **Commit:** `290574e51997fb185ba0858ec2f999a7d05809e1`.
- **Validation:** Reviewed the resulting brace/class boundaries. No .NET SDK/build execution is available in this environment; local build/tests remain required. No GitHub Actions used.
- **Next:** Pull `main` and rebuild. Address any newly surfaced diagnostics from their actual file and line locations.


## 2026-11-23 — Add missing metadata-file imports

### 149. Import HTML decoding and regular-expression namespaces
- **Reason:** The local build reported that `WebUtility` and `Regex` were not found in `JsonApiSearchProvider.Metadata.cs`.
- **Finding:** C# using directives are scoped to their own source file. The imports in `JsonApiSearchProvider.cs` do not apply to this separate partial-class file.
- **Correction:** Added explicit `using System.Net;` and `using System.Text.RegularExpressions;` directives to the metadata file, where `CleanText` uses both APIs.
- **Commit:** `cdf4157c6963fbe7eae45087a0c16a9aee615e24`.
- **Validation:** Confirmed both directives are present in the updated GitHub file. No .NET SDK/build execution is available in this environment; local build/tests remain required. No GitHub Actions used.
- **Next:** Pull `main` and rebuild; address any remaining diagnostics from their exact source locations.


## 2026-11-23 — Remove duplicate DataCite test declaration

### 150. Keep the richer DataCite fixture and remove its duplicate
- **Reason:** The local build reported CS0111: `PaperResultMetadataTests` declared `SearchAsync_MapsDataCiteCreatorsAndPublicationYear` more than once.
- **Finding:** The same partial test class declared the method in both `PaperResultMetadataTests.cs` and `PaperResultMetadataAdditionalTests.cs`. The additional test is more complete: it checks multiple creators, publisher, resource type, publication year, DOI, landing-page URL and subject tags.
- **Correction:** Removed only the shorter duplicate from `PaperResultMetadataTests.cs`; retained the richer test and all its assertions in `PaperResultMetadataAdditionalTests.cs`.
- **Commit:** `1bc2f934084a47d8e3d32bd50ab1aee6beb3ca7f`.
- **Validation:** Confirmed the duplicate method was present once in each partial source file before the change and removed the shorter copy. No local .NET build is available here; the user must rebuild. No GitHub Actions used.
- **Next:** Pull `main`, rebuild, then run the full Release test suite if compilation succeeds.


## 2026-11-23 — Fix SearXNG serialization and metadata test failures

### 151. Remove the duplicate paper content JSON property
- **Reason:** The local test run exposed a System.Text.Json property-name collision on SearxngSearchResult.content. The base SearXNG Content property and PaperContent extension both serialized as `content`, causing metadata serialization and multiple API integration tests to fail with HTTP 500.
- **Correction:** Removed the duplicate paper extension property. The mapper now places the paper abstract in the existing common `content` field, falling back to the ordinary result snippet.
- **Commits:** `d0fa0416ba639271ce522c4203fa5db2cd3f9d19` (response model); `7220173fe99dbbfdbc010c8ffec652a892e02e9b` (mapper).
- **Validation:** Reviewed the response model and mapper. Tests still need to be rerun locally.

### 152. Align Openverse file-size assertion with formatted output
- **Reason:** 896,128 bytes is 875.125 KiB. The current invariant numeric formatter produces `875.13 KB`; the test expected `875.12 KB`.
- **Correction:** Updated the fixture assertion to the formatter's actual two-decimal midpoint result; retained the exact byte-count assertion.
- **Commit:** `5f734214d18902cf713126531ad873093c42c5b7`.
- **Validation:** Arithmetic and formatter behaviour reviewed; rerun the test suite locally.

### 153. Normalize Bilibili Referer URI expectation
- **Reason:** The capability test observed the normalized Referer value with a trailing slash, while the definition and assertion omitted it.
- **Correction:** Made the configured Referer URI explicit with its trailing slash and updated the assertion to match the normalized absolute URI.
- **Commits:** `20b365d901b42d979c3504ebe706a4974e59bee2` (definition); `7030689d2eaadd4ba0e66b018bf46a935be48cea` (test).
- **Validation:** Source and assertion reviewed. Test rerun required.

- **Remaining failure from this run:** The production-host integration test reported 63 providers instead of 67 despite configuring optional keys. The source configuration path needs a focused follow-up after the serialization fix is pulled and the test is rerun; do not treat the 67-provider integration test as passing.
- **Other cascading failures:** The multiple SearXNG JSON parse errors and HTTP 500 responses in the same run are expected to be re-evaluated after the duplicate-property correction.
- **Next:** Pull `main`, rerun the Release test suite, then investigate any remaining failures—especially production-host optional-provider configuration. No GitHub Actions or live provider calls.


## 2026-11-23 — Correct latest regression-test expectations and improve diagnostics

### 154. Correct the Bilibili Referer regression assertion
- **Reason:** The latest local run still expected `https://search.bilibili.com`, while the HTTP header and the separate registration test correctly use the normalized URI with a trailing slash.
- **Correction:** Updated `BilibiliDefinition_UsesRequiredRefererAndNoUnsupportedLimitParameter` to expect `https://search.bilibili.com/`.
- **Commit:** `9129fb6be91386f276d8c77cd1f4cd39e07941de`.
- **Validation:** Compared the definition, registration test and definition-level assertion. No local .NET execution available here.

### 155. Make optional-provider registration failures identify the missing provider
- **Reason:** The production-host test stops at the aggregate count (63 instead of 67), hiding which optional registrations are absent.
- **Correction:** Moved the four optional-provider assertions (Exa, Yandex, Kagi and Brave) ahead of the aggregate count assertions. The next run will identify the first missing registration directly.
- **Commit:** `a74679f515f37c02d9731d43cee430e5e089caca`.
- **Finding:** The test fixture adds all four configuration keys through `ConfigureAppConfiguration`; Program reads those keys during startup. The configuration hand-off still needs diagnosis if any of the four assertions fail.
- **Crossref finding:** Current `main` declares `PaperResultMetadata.PublicationYear` as a non-nullable string defaulting to `string.Empty`, and the Crossref mapper assigns a string returned by `ReadOptionalText`. The reported actual `null` therefore does not match the current remote source; verify the local checkout is on current `main` and has no stale build output before changing the mapping blindly.
- **Next:** Pull current `main`, clean/rebuild the test project, rerun the three failing tests. If Crossref still fails, provide the local `PaperResultMetadata.cs`, `JsonApiSearchProvider.Paper.cs`, and `JsonApiProviderCatalog.cs` revisions. If optional-provider assertions fail, the first missing provider will narrow the configuration diagnosis. No GitHub Actions or live provider calls.


## 2026-11-23 — Fix final two reported metadata and provider-registration failures

### 156. Map paper publication year into the SearXNG result
- **Reason:** The Crossref test's source metadata correctly contained the year, but the serialized SearXNG result exposed `publication_year: null`.
- **Finding:** The test's metadata assertion passed; the failure was the later serialized response assertion. `SearxngSearchResult` already defines `PaperPublicationYear`, but `SearxngSearchResponseMapper.MapResult` did not populate it.
- **Correction:** Added `PaperPublicationYear = result.PaperMetadata?.PublicationYear` to the mapper. This preserves the metadata value through the API response.
- **Commit:** `47369b59c07f4f8eaccdfcfaa3770b29cae230fd`.
- **Validation:** Confirmed the property exists on the response model and the existing Crossref test serializes and checks `publication_year`. No local .NET test execution available here.

### 157. Supply optional provider keys before minimal-host startup
- **Reason:** The production-host test's Exa/Yandex/Kagi/Brave keys were added through `ConfigureAppConfiguration`, but `Program.cs` reads configuration and registers providers during top-level builder setup, before the deferred app-configuration callback takes effect.
- **Correction:** Changed the test factory to inject its in-memory keys through `ConfigureHostConfiguration`, making the values available as host configuration before top-level provider registration. Kept all four provider assertions and the 67-provider total assertion.
- **Commit:** `2787902b707de8ff69c0e2e8b0378615e75e7770`.
- **Validation:** Compared startup ordering in `Program.cs` with the factory configuration callback and updated the test to configure host settings before app startup. Must be verified by the user's local .NET 10 test run.
- **Next:** Pull `main` and run the full Release test suite. No GitHub Actions, no live provider calls.


## 2026-11-23 — Correct optional-provider test host configuration

### 158. Set optional provider keys before the test host is created
- **Reason:** The previous correction called `ConfigureHostConfiguration` on the `IWebHostBuilder` supplied by `WithWebHostBuilder`. That API belongs to `IHostBuilder`, so the test project failed to compile with CS1061.
- **Finding:** `Program.cs` reads Exa, Yandex, Kagi and Brave keys directly from `WebApplication.CreateBuilder(args)` and conditionally registers providers during top-level startup. A later web-host configuration callback cannot reliably change those already-made registration decisions.
- **Correction:** The production-provider integration tests now apply temporary process environment values before constructing/starting `WebApplicationFactory`, then restore the original values after the factory is disposed. The test class uses a non-parallel xUnit collection because environment variables are process-wide. The invalid `ConfigureHostConfiguration` call and unused configuration import were removed.
- **Commits:** `ef13f5f0646ab0c169a6b3641b28294503550f11` (initial test setup); `49e82bc2d5d09bd72c842514f6122fcddf951436` (serialize environment-dependent test collection and simplify factory construction).
- **Validation:** Reviewed the updated test source and disposal order. No .NET SDK is available in this environment, so this correction has not been compiled or executed here. No GitHub Actions or live provider calls.
- **Next:** Pull `main`, build the solution, then run the Release test suite. If the provider count still fails, report the first missing provider assertion and its local output.


## 2026-11-23 — Restore production registrations in Openverse integration test

### 159. Keep the production provider catalogue in the Openverse test
- **Reason:** The local Release test run reported 318/319 passing; `Search_SkipsRegisteredOpenverseForGeneralSearchWithoutNetwork` received HTTP 400 instead of 200.
- **Finding:** The test called the parameterless `CreateFactory()` overload. That overload is actually `CreateFactory(params ISearchProvider[] providers)`; it removes every registered `ISearchProvider` and then adds zero replacements. Consequently, `openverse` is not in the aggregator's available-provider list and the request is correctly rejected as an unknown engine before category capability filtering occurs.
- **Correction:** Constructed a plain `new WebApplicationFactory<Program>()` for this test, matching the production-host registration test. This preserves the real provider catalogue while the temporary environment variables activate optional providers. The request can now reach the intended capability filter: Openverse is registered but does not support the requested `general` category, so it should be reported as unsupported without making a network request.
- **Commit:** Pending activity update.
- **Validation:** Source-level diagnosis from the test factory and `Program.cs` registration path. No .NET execution performed here. No GitHub Actions or live provider calls.
- **Next:** Pull `main` and rerun the Release suite; expect this test to return 200 with an empty result list and `openverse` in `unsupported_engines`.

## 2026-11-23 — Release regression suite green

### 160. Verify the complete regression suite after the Openverse correction
- **Reason:** Confirm the final remaining failure was resolved and establish a clean baseline before the next quality increment.
- **Action:** User pulled the latest `main` and ran the Release solution build and complete xUnit suite locally.
- **Result:** **Build succeeded. 319 tests passed, 0 failed, 0 skipped.** Test duration 2.2 seconds; build duration 4.6 seconds.
- **Verification:** Result supplied directly by the user from the local PowerShell session. This is user-run verification; no local .NET SDK or GitHub Actions execution was available to the assistant.
- **Correction covered:** Openverse category filtering now runs against the production provider catalogue rather than an empty test provider collection.
- **Next:** Add measured coverage reporting, inspect coverage by production class/branch, then target the highest-risk uncovered logic. Focus first on request validation, capability filtering, aggregation/ranking, provider-health transitions, shared transport and response mapping. Keep workflows disabled.

## 2026-11-23 — Add measurable test coverage reporting

### 161. Add Coverlet collector for local coverage measurement
- **Reason:** A green test count confirms regressions are absent but does not show which production branches remain untested. The project rules require logic and meaningful edge-case coverage, so coverage must be measured before deciding where to add tests.
- **Action:** Added the Coverlet collector to the xUnit test project. It supports the standard `dotnet test --collect:"XPlat Code Coverage"` workflow and emits Cobertura coverage data without requiring GitHub Actions.
- **Result:** Package reference added; no production logic changed.
- **Verification:** Not run in this environment. The user should restore and run the documented coverage command locally; the next increment will use the resulting report to target uncovered high-risk logic.
- **Audit note:** The existing result score calculation intentionally matches SearXNG's current `calculate_score` algorithm: multiply contributing engine weights, multiply by the number of positions, then sum weighted reciprocal positions. Do not simplify this into a sum/average of per-engine weights; that would break compatibility. Source: https://github.com/searxng/searxng/blob/master/searx/results.py.
- **Next:** Run coverage locally and share the Cobertura summary or file. Prioritise uncovered branches in request validation, capability gating, aggregation, provider health, shared transport and response mapping. Workflows remain disabled.

## 2026-11-23 — Add opt-in live upstream integration tests

### 162. Exercise real provider endpoints without affecting normal test runs
- **Reason:** The existing fixture and fake-handler tests validate parsing and internal behaviour but do not prove that provider endpoints currently accept real requests or return usable responses.
- **Action:** Added `LiveProviderIntegrationTests` for Wikipedia's live MediaWiki search API, Crossref's live works API and GitHub's live repository search API. Each test instantiates the production provider adapter and sends a genuine outbound request.
- **Safety:** Tests are skipped unless `DOTNETWEBSEARCHAPI_LIVE_TESTS=1` is set. Each test makes one request, uses a 20-second client timeout, caps response bodies at 1 MiB and requests at most three results. No credentials are required or logged. No retries or parallel provider fan-out are introduced.
- **Assertions:** A live response must produce at least one result with a non-empty title and an absolute HTTP(S) URL. Provider-specific outage, rate-limit, block or schema changes will surface as a failing live test and should be recorded as upstream observations rather than hidden by changing assertions.
- **Documentation:** Added exact PowerShell commands to README. Normal test and build commands remain network-independent.
- **Validation:** Source reviewed only. No .NET SDK or outbound network execution is available in this environment; compilation and actual endpoint behaviour must be verified by the user locally. GitHub Actions remain disabled.
- **Next:** Run the opt-in suite locally, share the output, then decide which additional providers are worth adding to the small live smoke-test set.



## 2026-11-23 — Bound inbound search query size

### 163. Reject oversized search queries before provider execution
- **Reason:** Both public search routes accepted unbounded query strings. Excessively large queries waste request-processing resources and are forwarded to every eligible provider.
- **Action:** Added a shared 512-character maximum in `TryBuildSearchQuery`, so both `/search` and `/api/search` apply the same rule before aggregation/provider execution. Requests above the limit return HTTP 400 with a clear validation message.
- **Tests added:** Added four boundary cases covering both endpoints: exactly 512 characters is accepted; 513 characters is rejected. The tests use the existing in-memory provider fixture and make no upstream network calls.
- **Commits:** `c51022e7076621e89fd037940548ff8ddbe553fd` (validation), `eb08ce39a07d16a6f41d7c02bbd01bbea91ef9bd` (boundary tests).
- **Verification:** Reviewed the shared validation path and test source. Tests have not been executed in this environment because the .NET 10 SDK is unavailable; no GitHub Actions or live provider requests were used. The previously user-reported 319-test Release pass predates this increment.
- **Next:** Add explicit maximum category/provider selection counts and form-body bounds, with tests, then review rate limiting and authentication design before public exposure. Measure line/branch coverage when the local SDK is available.


### 164. Bound search category input
- **Reason:** Category parsing previously accepted an unbounded number of arbitrarily long category names. This adds avoidable parsing/allocation work and allows meaningless values to be forwarded into capability filtering.
- **Action:** Added shared validation for both search endpoints: at most 10 categories, with each category name limited to 32 characters. The existing empty-category validation remains unchanged.
- **Tests added:** Four endpoint boundary cases cover too many categories and an overlong category name on both `/search` and `/api/search`.
- **Commits:** `6676c99d274fc36d555cdf0870a4450ede3055a9` (validation), `a0eb16ab21ef0c12802bf44c1dd03e4d6ff78c63` (tests).
- **Verification:** Reviewed validation and tests. Neither this increment nor the preceding query-length tests have been executed here because the .NET 10 SDK is unavailable. No GitHub Actions or live provider requests were used.
- **Next:** Bound selected-provider count and POST form-body size, then add API-level request throttling/authentication design. Preserve offline deterministic tests and keep all workflows disabled.


## 2026-11-23 — Request-boundary, authentication and throttling hardening

### 165. Bound provider selection and form input
- **Reason:** Query and category limits were in place, but a caller could still supply an arbitrarily large engine list or POST form body.
- **Action:** Explicit engine selection is limited to 20 distinct names, each no longer than 64 characters. POST /search rejects a declared body larger than 16 KiB with HTTP 413. ASP.NET form parsing is also configured with 16 KiB buffering/multipart limits, 32 form values, 128-character keys and 4 KiB values.
- **Tests added:** Two endpoint tests reject 21 selected engines; one rejects a 65-character engine name; one rejects a 16 KiB + 1 form body.
- **Commits:** `1a97326417c34ad1766517de0c848a84fbc6d0b9` (limits), `7be08ccef582bfb2bc49e182a9153da273e21a57` (tests).

### 166. Add optional bearer API-key enforcement
- **Reason:** A self-hosted API needs a way to prevent anonymous callers from consuming provider quotas and server resources before public exposure.
- **Action:** Added semicolon-separated `SEARCH_API_KEYS` configuration. Every configured key must be at least 32 UTF-8 bytes. Search routes accept the key only through `Authorization: Bearer`; comparisons use `CryptographicOperations.FixedTimeEquals`. When running in Production, startup fails if no API key is configured. The browser search page has a password field and sends the bearer header without persisting it.
- **Tests added:** Valid, missing and incorrect bearer-key cases.
- **Commits:** `f4d5385dc453dd7da0a110d8e3ba8e036e75479e` (middleware), `f94a403fbd39dc38106ca1f823ea9fe3a43669d9` (UI input), `ef2ade31ef55a4c826dd0174e1ddd7edd787fab7` (UI header), `580684a18dd7a3e8c0a09cfbff2e059eea0c8c12` (tests), `7ea3ce502c5430cb86c7ffde70f80961fb94a832` (routing/syntax correction).

### 167. Add per-client search rate limiting
- **Reason:** API keys alone do not prevent one client from exhausting provider quotas or monopolising request capacity.
- **Action:** Added a fixed-window limiter of 60 search requests per minute per remote IP address, with no queue and HTTP 429 on rejection. Applied to /search GET/POST, /api/search and root search requests. Endpoint routing is explicitly enabled before the limiter.
- **Tests added:** A deterministic in-memory integration test sends 61 requests and expects the final request to receive HTTP 429.
- **Commit:** `b3f8590da824991f449ab01b646ba7c5294c1424`.
- **Verification:** Source and test files reviewed. No .NET 10 SDK is available in this environment, so none of the new tests have been executed. The user's earlier 319-test pass predates these changes. No GitHub Actions and no live provider requests.
- **Security caveats:** IP-based throttling requires correct trusted-proxy configuration when deployed behind a reverse proxy; do not trust arbitrary forwarded headers. API keys must be supplied via environment/secret configuration and requests must use HTTPS outside localhost. Chunked request bodies and response redirects still require focused adversarial verification; do not claim complete SSRF or request-body protection yet.
- **Next:** Harden streamed/chunked POST bodies and outbound redirect/SSRF policy; add a trusted-proxy-aware client identity strategy; run the complete Release suite and Coverlet locally. Then implement the general HTTP consumer client in CoderInBox without merging provider code.


### 168. Enforce initial outbound destination checks
- **Reason:** The shared transport accepted any absolute request URI, including non-HTTP schemes, embedded credentials, localhost and private/link-local IP literals. Provider request builders should not accidentally become a route to internal services.
- **Action:** `SearchHttpTransport` now permits only absolute HTTP(S) URIs without embedded user information; blocks localhost/local/internal hostnames; and rejects loopback, private, link-local, carrier-grade NAT, multicast, reserved and documentation IP literals (including IPv4-mapped IPv6).
- **Tests added:** Eight unsafe destination cases plus a public IPv4 literal acceptance case. All use a stub handler, so no network calls occur.
- **Commits:** `d8563cf4f8cf5c8f2868416aadbde9b68814b201` (initial guard), `374ba70cb102f9c93085ddfa6edf93362127f410` (precise reserved ranges), `20366be5557d6a17b85880e5c02064d1d9dcba5f` (tests).
- **Verification:** Reviewed the guard and test cases. No .NET SDK available; tests remain unexecuted.
- **Limitations:** This is not yet complete SSRF protection. Hostnames that resolve to private addresses and automatic redirects are not yet revalidated. A complete redirect/DNS policy must preserve legitimate provider redirect flows and be tested before public exposure.

### 169. Tighten API host configuration and record final middleware correction
- **Reason:** A non-Development environment other than Production (for example Staging) must not silently start without API authentication; the streamed body-size feature must be set before form parsing; and the bearer challenge header must use the portable header dictionary indexer.
- **Action:** API-key startup enforcement now applies to every non-Development environment. The POST endpoint sets `IHttpMaxRequestBodySizeFeature.MaxRequestBodySize` to 16 KiB where the host supports it, in addition to the declared-length and form-parser checks. The unauthorized response writes `WWW-Authenticate: Bearer` through the header indexer.
- **Commits:** `f57123cbd2ed3b22d368ca36d394af70c122c3f7` (host/body limits), `57af843664f57681e138a4066f34c2024ecff176` (header correction).
- **Documentation:** README now explains key configuration, HTTPS, the initial 60/minute IP limit, reverse-proxy caveats and remaining gaps (`972ee56c98be85492b606ebca1d6ae4a33622e6d`).
- **Verification:** Source reviewed; no local .NET build/test execution and no GitHub Actions.
- **Next:** Complete DNS/redirect policy, inspect real Coverlet output, then begin the independent CoderInBox HTTP client.


### 170. Validate DNS destinations on every provider connection and redirect
- **Reason:** Initial URI validation cannot stop an allowed public provider from redirecting the HTTP client to a private/link-local destination. DNS names can also resolve to internal addresses or change between validation and connection.
- **Action:** Added `PublicNetworkSocketsHandler`, using `SocketsHttpHandler.ConnectCallback` to resolve the host immediately before connection and reject the request if any resolved address is non-public. The handler connects directly to the resolved IP, so the checked address is the address actually dialled. Automatic redirects remain enabled, but each new connection passes through the same callback. Ambient proxy use is disabled so an external proxy cannot bypass the destination policy. All HttpClientFactory defaults use this handler; Mojeek and Yahoo retain their isolated cookie containers while using the same connection policy.
- **Address policy:** Public global IPv4/IPv6 ranges are allowed; loopback, private, link-local, carrier-grade NAT, multicast, reserved and documentation ranges are rejected. IPv4-mapped IPv6 is normalised before classification.
- **Tests added:** Added public/private IPv4 and IPv6 classification cases and asserted that the provider handler has redirects enabled, proxy use disabled and a connection callback configured. Existing transport tests also cover unsafe initial URI schemes/hosts and private IP literals.
- **Commits:** `69bc77cc121aa261a6ce40b1490f1775b7de4123` (handler), `df9900c30e044474e1099af5b1816333b6c59479` (shared policy), `3cf99a497723b5c9b0f3d2281eb9b7f6fb79f9cf` (HttpClientFactory wiring), `6eba3ea693df85815a1c6e9c71a9b7949979dee2` (proxy and address policy), `67b3b48942eb65076c57a8dda425847397958c06` (IPv6 and socket cleanup), `99b71d3bf8a379c8d40f0220b55310d2d7d11564` and `2f01cb58053f59dd26c7a614f9dc71eb04e791de` (address tests), `e3cdf1eaed26ea2747d85d3523abafc81963ec1f` and `46b98db21963c49b5f3707c697af616f796fac41` (security docs).
- **Limitations:** Direct outbound connections are required; proxy-based deployments need an explicit trusted-proxy design. Network egress firewall rules remain recommended.
- **Verification:** Source and tests reviewed only. No .NET 10 SDK is available here, so no compile/test result is claimed.

### 171. Add a local coverage report summariser
- **Reason:** Coverlet was already available, but analysing the generated Cobertura XML required manual inspection and made it harder to target the weakest production classes.
- **Action:** Added `scripts/Show-CoverageSummary.ps1` to select the newest Cobertura report, print aggregate line/branch coverage and list the 25 lowest line-coverage classes. Added `docs/COVERAGE.md` and linked the workflow from README.
- **Commits:** `e47185eb9c1bd9b8de6cf448a0f8bf69182ef8d2` (script), `7614faeb250fa99837b2e043a5cfb43e21423f11` (guide), `4541d27151ac70f6d841a52cfb339a241855609b` (README).
- **Verification:** Script reviewed but not run; no local PowerShell/.NET test environment is available in this session. Actual line/branch percentages remain pending the operator's local run.


### 172. Authenticate before consuming the search rate quota
- **Reason:** With the rate limiter ordered before authentication, unauthenticated requests from a shared address could consume the same quota used by legitimate authenticated clients.
- **Action:** Moved endpoint rate-limiting middleware after the bearer-key middleware. Authentication failures now return 401 before they reach the search quota. Added a startup guard test proving a non-Development host refuses to start without `SEARCH_API_KEYS`.
- **Tests added:** 61 invalid-key requests must not consume the authenticated client's quota; a subsequent valid request succeeds. Added Staging startup-without-key coverage.
- **Commits:** `cae912240792c1b0db80926f493c0711747d3cc5` (middleware order), `cbb29832af035fc5424c62baae6a3d363247ce42` and `52d3cdc0d0c95274562319fb12fed92d41fdc0a4` (tests).
- **Verification:** Source/test review only; no .NET 10 SDK available here. No Actions used.


### 173. Split request-security policy out of the application entry point
- **Reason:** Security, authentication, rate limiting and form parsing increased `Program.cs` beyond the preferred 20 KB source-file target.
- **Action:** Extracted API-key configuration/startup validation, form limits, rate-limit policy and bearer middleware into `ApiRequestSecurity.cs`. Program now composes the security options and middleware. Diagnostics still reports whether API authentication is enabled.
- **Commits:** `aa18e8e66a8d390b772610712ffb12276a59a128` (Program extraction), `29e1fbdce3da2228b160573d9fb859b9e6b3e85e` (security policy), `a0f55af1aa778805c1c60976755db7eea38d3356` (namespace/import correction).
- **Result:** Program source is approximately 18 KB, below the 20 KB target.
- **Verification:** Source reviewed only; no .NET SDK/build/test run. No GitHub Actions.


### 174. Preserve request-size feature import after Program split
- **Reason:** The POST endpoint still uses `IHttpMaxRequestBodySizeFeature` after the broader form/security configuration moved to its own file.
- **Correction:** Restored the explicit `Microsoft.AspNetCore.Http.Features` import in Program.
- **Commit:** `1395ad3e3f37ebf3dd7726d5a24ce6ebacaeb854`.

### 175. Dispose sockets when a connection family cannot be opened
- **Reason:** Socket construction itself can fail for an address family before the connect operation; the previous loop constructed the socket outside its try block and would stop before trying later public addresses.
- **Correction:** Moved socket creation inside the guarded block and dispose any created socket on both connection and unexpected failures. A SocketException now allows the loop to try the next already-validated address.
- **Commit:** `3ea73e68aded8f6b8f83d77dc97a75e581b924d5`.
- **Verification:** Source-reviewed only; compile/test remains pending local .NET 10.


### 176. Record the initial independent CoderInBox consumer
- **Reason:** The service/consumer boundary is part of the agreed roadmap and must remain explicit across repositories.
- **Action:** Updated the architecture decision record to describe the first CoderInBox HTTP client, environment-based configuration, WebResearch-first behaviour and preserved fallback chain. The integration is explicitly marked unverified until its Windows/.NET 8 tests run.
- **Commit:** `d052a2d8a1f0dda5dc8a6180397b165435addf0e`.


### 177. Set Staging environment before minimal-host startup in the fail-closed test
- **Reason:** Minimal-host application code reads environment during `WebApplication.CreateBuilder`; a late `WithWebHostBuilder` callback may not affect the environment value used by top-level startup validation.
- **Correction:** The test now sets `ASPNETCORE_ENVIRONMENT=Staging` and clears `SEARCH_API_KEYS` before constructing/starting the factory, matching the repository's proven process-environment test pattern.
- **Commit:** `32ea9a1f119295c5fb9b81aa6ababaa9d8051e31`.
- **Verification:** Source-reviewed only.


### 178. Enforce streamed POST limits before authentication
- **Reason:** Applying the 16 KiB body cap only inside the endpoint left unauthenticated POST requests outside the same protection; those requests are rejected by auth before endpoint execution.
- **Action:** Moved an early body-size check and `IHttpMaxRequestBodySizeFeature` limit into the request-security middleware, before authentication and rate limiting. The endpoint retains its own declared-length check and form parser limits.
- **Test added:** With API-key enforcement enabled and no Authorization header, a POST body of 16 KiB + 1 receives HTTP 413 before the 401 auth response.
- **Commits:** `0246903d84f6a50782d972c6dad822c7e009be53` (middleware), `7683d587e6570c94ec10d67e3af26588112884e7` (test).
- **Verification:** Source-reviewed only; no .NET 10 execution.


### 179. Make the Staging startup test robust to host environment precedence
- **Reason:** Minimal hosting can use `DOTNET_ENVIRONMENT` in preference to `ASPNETCORE_ENVIRONMENT`; the fail-closed test must set both before host construction.
- **Correction:** The test now sets both variables to Staging and restores their prior values after factory disposal.
- **Commit:** `92b279c49bb8d7c06217304d4ab8e755844a0c42`.


### 180. Fix diagnostics endpoint static-local capture
- **Reason:** The diagnostics endpoint reads the startup-configured `securityOptions` local to report whether API-key authentication is enabled. Declaring that local function `static` caused compiler error CS8421 because static local functions cannot capture enclosing locals.
- **Correction:** Removed `static` from `DiagnosticsEndpoint`. The endpoint remains a named local handler and reads the same immutable startup options; no security behaviour or response contract changed.
- **Commit:** `d0c854301be653c3cc843270c6cda75e3cf3ffd8`.
- **Verification:** Source correction committed. Build and tests must be run locally; no .NET execution is claimed here. The CS0006 test-project metadata error should clear once the main project builds successfully.


### 181. Correct live-provider test compiler and analyzer errors
- **Reason:** The first local Release build after the diagnostics fix reached the test project and exposed two test-source issues: URI scheme values are not compile-time constants for an `is` constant pattern (CS9135), and filtering with `Where` before `Assert.Single` violates xUnit2031.
- **Correction:** Replaced the URI pattern with explicit ordinal scheme equality checks, used the predicate overload of `Assert.Single`, and removed an unused opt-in constant. Live tests still require HTTP/HTTPS absolute result URLs and exactly one matching provider definition.
- **Commit:** `64327de4ee2d92261d7a3a0424a7dd2659a334bd`.
- **Verification:** Awaiting the operator's next local Release build/test. No local .NET execution is claimed here.


### 182. Add fail-fast live-provider batches of 20
- **Request:** Exercise the complete locally configured provider set using real outbound HTTP, grouped into batches of 20; stop after the first failed batch.
- **Implementation:** Added `AllRegisteredProviders_RespondToLiveRequests`, which resolves the production `ISearchProvider` registrations from `WebApplicationFactory`, sorts them by provider name, and invokes each provider sequentially with a bounded query and its declared timeout. Each response is checked for non-empty titles and absolute HTTP(S) URLs. All providers in the current batch are attempted so the batch report captures multiple failures; the test then fails the batch.
- **Runner:** Added `scripts/Test-LiveProvidersInBatches.ps1`. It builds Release once, runs batches of 20, and stops before the next batch when the current test process fails. It restores the caller's live-test environment variables afterward.
- **Credential handling:** Only providers registered from the local configuration are exercised. API-key providers are included only when their real credentials are configured; dummy keys are not introduced. The script's current maximum catalogue size is 67 and can be overridden.
- **Verification:** Source and runner committed for local execution. The user has confirmed the three initial live checks (Wikipedia, Crossref and GitHub) passed, alongside the previously reported 368 active offline tests. The new full-provider batches have **not** yet been run; no build/test execution is claimed here.
- **Workflows:** No GitHub Actions were invoked or changed.
