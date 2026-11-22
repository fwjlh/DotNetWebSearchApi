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
