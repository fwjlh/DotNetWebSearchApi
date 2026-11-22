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
