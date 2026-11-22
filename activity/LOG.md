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
