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
