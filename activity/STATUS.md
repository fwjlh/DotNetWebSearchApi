# Current project handover

## Latest update — 2026-11-24: full search evidence assessment
- Added an opt-in live integration test that resolves all `ISearchProvider` services registered by the actual application host, orders them alphabetically and runs them sequentially.
- Added `scripts/Test-LiveProvidersInBatches.ps1`: Release build once, then blocks of 20; every provider in a block is attempted, and any failure stops all subsequent blocks.
- Each request uses the provider's own timeout and validates result titles and absolute HTTP(S) URLs. Empty result sets are allowed when the provider returns a valid response.
- Only locally registered providers are tested. Optional providers requiring API keys are included only when real credentials are configured.
- User-confirmed baseline: 368 active Release tests passed and the three initial live provider checks passed. The new all-provider batch runner still requires local execution.
- No GitHub Actions were used or changed.


**Repository:** `fwjlh/DotNetWebSearchApi`  
**Branch:** `main`  
**Last reviewed:** 2026-11-24

## Current state

- **67 unique providers:** 52 JSON definitions, 2 RSS/Atom adapters and 13 dedicated registrations.
- **83 C# files; 35 test files.** Largest C# file: 19,463 bytes. All are below 20 KB.
- JSON catalogue definitions are split by family and have a uniqueness/count regression.
- `_LookAtMe.MD` contains the project coding, file-size, structure and testing rules.
- `activity/LOG.md` records actions, reasons, test additions, commits and next steps.

## Implemented

- Search aggregation, provider capability checks, category filtering and SearXNG-compatible response mapping.
- **Images:** typed Openverse metadata, including image/thumbnail URLs, dimensions, format, source, file size, licence and creator.
- **Videos:** typed Dailymotion, Bilibili and PeerTube embed, thumbnail and descriptive metadata.
- **Music:** Deezer previews, RadioBrowser streams and MusicBrainz artist/country metadata. MusicBrainz does not invent a playable audio URL.
- **Papers:** typed metadata for arXiv, Crossref, OpenAlex, Europe PMC, Semantic Scholar, DataCite, Zenodo, OpenAIRE and PLOS, including authors, dates, DOI, journal, PDF/HTML links and subjects where supplied.
- **News:** publication dates from supported RSS/Atom, Wikinews and Hacker News responses.
- Specialist image/video/paper templates are selected only when the matching typed metadata and safe HTTP(S) source/embed URL exist. Otherwise the default template is used.
- Capability corrections include arXiv science-only, Mojeek/Seznam general+web, and correct safe-search mapping for Mojeek and Brave.

## Known limits

- Wikimedia Commons remains a general result until its adapter can return a direct, validated image URL.
- MusicBrainz returns artist metadata, not a playable stream; it therefore uses the default result template.
- Optional Exa, Yandex, Kagi and Brave providers require configuration. The 67-provider integration test supplies dummy values to verify registration.

## Latest regression verification

- User ran the full Release solution build and test suite locally on 2026-11-23.
- **Result reported:** build succeeded; 319 tests passed, 0 failed, 0 skipped (2.2 seconds test duration; 4.6 seconds build duration).
- This confirms the fixes through Openverse integration test correction, including the 67-provider registration and Openverse category-filtering regression.
- The Openverse integration test now preserves production provider registrations by using a plain `WebApplicationFactory<Program>`.
- No GitHub Actions were invoked; the user has disabled workflows to conserve minutes.
- **Coverage tooling:** Coverlet collector 6.0.4 is now referenced by the test project. Run `dotnet test .\\DotNetWebSearchApi.sln -c Release --collect:\"XPlat Code Coverage\"` to generate a Cobertura report under `tests\\DotNetWebSearchApi.Tests\\TestResults`.
- **Live integration tests:** the original opt-in Wikipedia, Crossref and GitHub checks remain available. The new batch test resolves the actual registered provider adapters, runs them sequentially in groups of up to 20, and stops on a failed group. API-key providers are included only when configured with real credentials.
- **Next:** user should run the live test class locally and share provider outcomes. Expand the representative set only after confirming stability and reviewing upstream terms/rate limits. Then use measured line/branch coverage to target uncovered high-risk logic.

## Latest compile corrections

- Fixed the missing class-closing brace in `JsonApiSearchProvider.cs`, made its declaration `partial` to match the metadata partial declaration, and moved `JsonApiSearchDefinition` back to namespace scope.
- Removed the redundant closing brace in `JsonApiSearchProvider.Metadata.cs` before the metadata path records.
- Added file-local imports for `System.Net` and `System.Text.RegularExpressions` because `CleanText` in the metadata file uses `WebUtility` and `Regex`; imports in another partial source file are not shared.
- Commits: `7af858f951ce5a1d949aa4cb549ada91137cf51c`, `290574e51997fb185ba0858ec2f999a7d05809e1`, and `cdf4157c6963fbe7eae45087a0c16a9aee615e24`.
- Source structure and imports reviewed; build/test verification still needs to be run locally.

## Previous compile correction

- Corrected the invalid final declaration in `src/DotNetWebSearchApi/Program.cs` from `public partial class Program;` to `public partial class Program { }`.
- The missing `obj\\Debug\\net10.0\\ref\\DotNetWebSearchApi.dll` was a downstream result of that source compilation failure.
- Commit: `a81416bc8b69fd3c0a48e00aac27ad3e1396dcc6`.
- The correction has not been built in this environment; local .NET 10 build/test verification is still required.

## Workflow and test rules

- No files currently exist under `.github/workflows/`.
- Do not recreate, invoke or rely on GitHub Actions/workflow minutes. Commit directly to `main`.
- Ordinary unit/regression tests use local fixtures. Real upstream calls are confined to opt-in live tests and the explicit batch runner.
- Fixture and integration tests have been added, but **the suite has not been executed** because the .NET 10 SDK is unavailable in this environment. Do not claim tests have passed.

## Latest test compile correction

- Removed a duplicate `SearchAsync_MapsDataCiteCreatorsAndPublicationYear` declaration from `PaperResultMetadataTests.cs`. The richer version remains in `PaperResultMetadataAdditionalTests.cs` and additionally asserts authors, tags and landing-page metadata.
- Commit: `1bc2f934084a47d8e3d32bd50ab1aee6beb3ca7f`.
- The application project now compiles in the user's local Debug build; the test project exposed this duplicate declaration. The duplicate was source-reviewed and removed; the test project has not yet been rebuilt.

## Exact next step

1. Pull the latest `main` commit and run `dotnet build .\\DotNetWebSearchApi.sln -c Debug --no-incremental`.
2. Run `dotnet test .\\DotNetWebSearchApi.sln -c Release`.
3. Pull current main, clean/rebuild, rerun. Use the first missing optional-provider assertion to diagnose configuration; compare local Crossref source if PublicationYear still reports null.
4. Measure actual coverage for parser, capability and result-mapping logic.

Keep this file and `LOG.md` current so the next session can resume without chat history. Final category/template audit is recorded in LOG entry 144.


## Latest test-host configuration correction

- The test project reported CS1061 because `ConfigureHostConfiguration` is not an `IWebHostBuilder` extension. More importantly, `Program.cs` reads optional API keys and conditionally registers providers during top-level startup, before a late web-host callback can reliably affect those decisions.
- The two production-provider integration tests now set temporary Exa/Yandex/Kagi/Brave environment values before creating the test host and restore the prior values on disposal. Their xUnit collection disables parallel execution because environment variables are process-wide.
- Removed the invalid host-configuration call. Commits: `ef13f5f0`, `49e82bc2`; activity entry 158 records the correction and validation limits.
- **Not yet verified:** no local .NET SDK is available here. The user must build and run tests locally; do not treat provider registration as passing until the 67-provider test succeeds.


## Latest Openverse integration test correction

- The test was accidentally using the parameterless `CreateFactory()` overload, which removes all production `ISearchProvider` registrations and adds none. As a result, Openverse was treated as an unknown engine and the endpoint correctly returned 400 before category filtering.
- The test now uses a plain `new WebApplicationFactory<Program>()`, preserving production registrations while temporary environment variables enable the optional providers. Openverse should then be recognized and filtered as unsupported for `general` category without network access.
- Activity entry 159 records the diagnosis. Source change is on `main`; test execution remains pending.


## Latest incremental change — 2026-11-23

- Added a shared 512-character query limit in `TryBuildSearchQuery`; both `/search` and `/api/search` reject longer input with HTTP 400 before provider execution.
- Added four boundary test cases: 512 characters accepted and 513 rejected on each endpoint.
- Commits: `c51022e7076621e89fd037940548ff8ddbe553fd`, `eb08ce39a07d16a6f41d7c02bbd01bbea91ef9bd`, and activity record `d12f103d69125fa07fccb1d844ce5e6a01defda5`.
- Verification: source and test changes reviewed, but the four new cases have not been executed here. The user's earlier 319-test Release pass is the pre-change baseline; do not describe the latest HEAD as tested.
- No GitHub Actions were invoked or recreated.
- Next: bound category/provider selection and POST form inputs; then design authentication, per-client quotas and request rate limiting before public exposure. Run the full Release suite and coverage collection locally when the .NET 10 SDK is available.


## Latest incremental change — category input bounds

- Both search endpoints now reject more than 10 categories or any category name longer than 32 characters.
- Four additional boundary cases cover both endpoints; combined with the previous query-length work, eight new test cases are present on `main`.
- Commits: `6676c99d274fc36d555cdf0870a4450ede3055a9`, `a0eb16ab21ef0c12802bf44c1dd03e4d6ff78c63`, and activity record `72441ec18bdf8cb7e24b6c88c676f6086477d277`.
- Execution remains pending local .NET 10 build/test; the 319-test user-reported pass predates these changes. No Actions were run.
- Next: limit explicit provider selection, bound form request bodies before parsing, then design rate limiting/authentication. Run the full Release suite and coverage locally when the SDK is available.


## Current API hardening increment — 2026-11-23

- Explicit provider selection is capped at 20 distinct names, with a 64-character name limit.
- POST /search rejects declared bodies above 16 KiB; form parsing has key/value/count/buffering limits.
- Optional bearer API keys are configured through semicolon-separated `SEARCH_API_KEYS`; configured keys must be at least 32 UTF-8 bytes. Production startup fails closed when no key is configured. Browser search sends a manually entered key in the Authorization header and does not persist it.
- Search endpoints have a fixed-window limit of 60 requests per minute per remote IP; excess requests return HTTP 429.
- Added tests for provider-selection boundaries, oversized POST, valid/missing/invalid API keys and rate-limit rejection. Together with the prior query/category boundary cases, the new test source covers 15 additional scenarios/cases.
- Commits: `1a973264`, `7be08cce`, `b3f8590d`, `f4d5385d`, `f94a403f`, `ef2ade31`, `580684a1`, `7ea3ce50`, `57af8436`, and activity record `0f79ed2e`.
- **Verification:** Code and tests have been reviewed in GitHub. No .NET 10 SDK is available here; none of these latest tests has been executed. The 319-test user-reported baseline predates this increment. No GitHub Actions were used.
- **Security still outstanding:** enforce streamed/chunked body bounds at the server layer; validate outbound redirect destinations and DNS/IP policy; define trusted proxy handling before relying on remote IP for rate limits. Use HTTPS for all non-localhost API-key traffic.
- **Next:** Finish outbound request/redirect policy and tests, inspect actual Coverlet output from a local run, then add the independent HTTP client to CoderInBox.


## Security and request hardening — 2026-11-23

- Provider selection: max 20 distinct engines, max 64 characters per engine.
- POST form: declared content length and host request-body feature capped at 16 KiB; parser limits keys to 128 chars, values to 4 KiB and 32 fields.
- API keys: `SEARCH_API_KEYS` (semicolon-separated; minimum 32 UTF-8 bytes per key); bearer authentication on all search routes. Every non-Development environment fails startup if no key is configured. Browser UI accepts a manually entered key without persisting it.
- Rate limit: 60 requests/minute per remote IP, HTTP 429; no per-key quota and no trusted-proxy configuration yet.
- Outbound transport: rejects non-HTTP(S), embedded URI credentials, localhost/local/internal names and non-public IP literals. DNS-resolved private addresses and automatic redirects remain unprotected; do not describe SSRF protection as complete.
- New tests committed for provider-selection limits, oversized form input, API key success/failure, rate limiting and outbound destination validation.
- Latest commits include `f57123cb`, `972ee56c`, `d8563cf4`, `374ba70c`, `20366be5`, and activity record `f15d1f95`.
- **Test status:** all new source and tests reviewed only. No .NET 10 SDK is available in this session, so no post-change build or test execution is verified. The 319-test baseline is older. GitHub Actions remain unused.
- **Next:** complete redirect/DNS safeguards with provider compatibility fixtures; then analyse actual Coverlet output and add the independent HTTP client in CoderInBox. Do not expose publicly until these remaining controls and a fresh full Release test run are complete.


## Latest security, coverage and consumer progress — 2026-11-23

- Outbound provider clients now use `PublicNetworkSocketsHandler`: DNS is resolved at connection time, all returned IPs must be public, redirects use the same connection callback, and ambient proxies are disabled. Mojeek/Yahoo keep separate cookie containers.
- Address classification tests cover private/reserved IPv4 and IPv6 plus public address acceptance. Initial URI tests reject unsafe schemes, localhost/internal hosts, embedded credentials and private IP literals.
- Coverage tooling now includes Coverlet plus `scripts/Show-CoverageSummary.ps1` and `docs/COVERAGE.md`. Actual coverage numbers are not yet available; the script has not been run.
- Consumer integration is underway in the separate `fwjlh/CoderInBox` repository. It now has an HTTP-only client, an optional WebResearch-first path, environment configuration and offline tests. No provider implementation code is shared or copied.
- Commits for the latest DNS/redirect policy: `69bc77cc`, `df9900c3`, `3cf99a49`, `6eba3ea6`, `67b3b489`, `99b71d3b`, `2f01cb58`. Coverage commits: `e47185eb`, `7614faeb`, `4541d271`. Activity record: `fba6bda5`.
- **Verification remains pending:** no .NET 10 build/test execution has occurred here. The earlier 319-test pass predates the latest changes. No GitHub Actions have been invoked.
- **Operational note:** the safe outbound handler makes direct connections and disables ambient proxies. A deployment that requires a proxy needs an explicitly trusted proxy integration. Egress firewall restrictions are still recommended.
- **Next:** review and compile both repositories on the operator machine; run the DotNetWebSearchApi Release suite plus Coverlet summary, then run the focused CoderInBox client/WebResearch tests. Fix the first concrete compiler/test diagnostics before expanding the integration.


## Source-size and middleware follow-up

- Extracted API-key validation, bearer authentication, rate-limiter registration and form parsing limits into `ApiRequestSecurity.cs`. `Program.cs` is now approximately 18 KB, below the preferred 20 KB limit.
- Authentication middleware now runs before rate limiting so invalid credentials cannot consume an authenticated client's IP quota.
- Added tests for Staging startup without keys and 61 unauthorized calls followed by a valid call.
- Commits: `aa18e8e6`, `29e1fbdc`, `a0f55af1`, `cae91224`, `4a61202c`, `84b9aa2f`, `cbb29832`, `52d3cdc0`, `0b41a286`, activity record `13ea6774`.
- No new build/test execution has occurred; the latest code remains unverified until the operator runs Release locally.


- Latest source corrections: restored the explicit request-size feature namespace in Program (`1395ad3e`) and made socket connection attempts continue safely across address-family failures (`3ea73e68`). Activity record: `4d4cdc36`.


- POST /search body limits now run in the security middleware before authentication, including Kestrel's streamed-body feature where supported. An offline test asserts an oversized unauthenticated POST returns 413 before the bearer-key challenge. Commits `0246903d`, `7683d587`; activity record `bff3b447`.


- The non-Development startup test now sets both `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` to Staging before host creation, avoiding minimal-host environment precedence ambiguity. Commit `92b279c4`; activity entry `7c993b2b`.


## Compiler correction — 2026-11-24
- Fixed CS8421 in `Program.cs`: `DiagnosticsEndpoint` is no longer a static local function because it reads the enclosing `securityOptions` value.
- This is a compile-only correction; authentication configuration and diagnostics output are unchanged.
- Commit: `d0c854301be653c3cc843270c6cda75e3cf3ffd8`.
- **Verification pending:** run the Release build and full tests locally. CS0006 was a downstream missing-reference error caused by the main-project compilation failure.


## Live-provider test compile correction — 2026-11-24
- Fixed the three reported test-project diagnostics in `LiveProviderIntegrationTests.cs`: two CS9135 URI scheme pattern errors and xUnit2031 on filtered `Assert.Single`.
- Retained strict absolute HTTP/HTTPS URL assertions and exact provider lookup.
- Commit: `64327de4ee2d92261d7a3a0424a7dd2659a334bd`.
- **Verification pending:** rerun Release build and full test suite; live upstream tests remain opt-in.


## Latest outbound-address policy correction — 2026-11-24
- Added two ORCHIDv2 (`2001:20::/28`) negative address-classification cases. Review confirmed the existing broader `2001:0000::/23` exclusion already blocks this range; a redundant explicit predicate was removed.
- Commits: `db625feee2405309f279c22780145c52c56cdb86`, `126baa3fb6c5ba5ba64978ee4ccdea908d94ed02`, `cbe338f403df2ee89aa86b10ea0f1061ef4dfd45`.
- Source reviewed only; .NET 10 build/test still requires the operator's local SDK. No Actions or live calls.


## Latest live-assessment reliability corrections — 2026-11-24
- Full-search evidence runner now fails if no providers are registered, verifies provider inventory stability and exact batch selection counts, and terminates after the last provider without an empty extra batch. Commit: `5720c81b0bb5dcbe4cbea2eed963812c28b57c3b`.
- End-to-end live API test now supplies a configured bearer key from effective host configuration without logging or persisting it. Commit: `1d494e02b37cd50d42969307b826929ac3826b49`.
- Static review only; fresh build/test and live execution remain pending on the operator's machine. No Actions were used.


## Latest authentication-path hardening — 2026-11-24
- Search middleware now normalizes trailing slashes before applying bearer authentication and streamed POST body limits. This closes the route/middleware path mismatch on `/search/` and `/api/search/`.
- Added two trailing-slash authentication cases and an oversized trailing-slash POST case.
- Commits: `57f7fdd1d3d836a8b2c2bda28a48e7f679068372`, `994dcf6754e3a079e0bde6fdf7d0150158780cb2`.
- Static review only; build and test execution remain pending on the local .NET 10 environment.


## Latest endpoint-authentication hardening — 2026-11-24
- Security middleware now classifies protected search routes from ASP.NET Core routed endpoint metadata rather than comparing raw path strings. This protects route-equivalent path spellings and keeps POST body-size enforcement aligned with the selected `/search` endpoint.
- Added a root `/?q=test` bearer-authentication regression test; trailing-slash cases remain.
- Commits: `32dc8f2252e0c40c501e7c15a96b91b6018a4aa6`, `7f3b1a722914d404381e9e788a6eda8c8e3fda8c`.
- Static review only; local .NET 10 build/test pending.


- Expanded outbound-address regression cases to cover shorthand/integer IPv4 loopback hosts, IPv4-mapped IPv6 loopback, documentation space and limited broadcast. Commit: `18c1d1793e87fadc4b807b77dd94613785910994`. Execution remains pending the local .NET 10 test run.


- Added a non-seekable, unknown-length streaming response test proving the provider HTTP transport enforces its response cap without relying on Content-Length. Commit: `939bb2a9234770e7aa3132f235b9c5128dab6508`. Execution pending local .NET 10.


- Full-search evidence runner no longer has a hard-coded 200-provider ceiling; it terminates using the registered inventory and exact batch coverage checks. Commit: `462be91f74ba0fccc29a42882ad879795c5e0453`. PowerShell execution pending locally.


## Latest API result sanitization — 2026-11-24
- Search aggregation now rejects blank titles, empty-host/non-HTTP URLs and result URLs containing embedded credentials before merging provider output.
- Added a fixture test proving credential-bearing and title-less results are dropped while a valid result survives.
- Commits: `e4effd7dfa42a1739a5bc4d3acb41f1037cb626c`, `490d7222e1d3c7b88a326c0d72f5c32f0d16a1e4`.
- Static review only; local .NET 10 build/test remains pending.


## Latest authentication abuse protection — 2026-11-24
- Added a separate 10 failed bearer attempts/minute/IP limiter, applied only to invalid/missing credentials. Authenticated search quota remains 60 requests/minute/IP; failed attempts do not consume it. Limiter lifetime is owned by the host DI container.
- Updated regression test: 10 invalid attempts return 401, the 11th returns 429, and a valid request still succeeds.
- Commits: `c2593812e3bc4f3fdf5b2eecaf0e74e73292fbc5`, `97d4df048bd483c63d5cf02214e680df11ae069d`, `7a489af8dbba4f392f8acd7c7aaddfa0036b9ab5`.
- README now documents both limits and removes the stale Actions badge. Commit: `33724bbc9172a128488ec5c7141a072612db1549`.
- Static review only; local .NET 10 build/test pending. No workflows used.


- README now clearly distinguishes historical 156-test CI and 368-test/3-provider local baselines from current HEAD, and directs current verification to the local full-evidence script. Commit: `df85c63713b7522da9c1a6ac5d3f0bf206aa3552`.


## Latest local assessment coverage integration — 2026-11-24
- Full evidence runner now collects XPlat Code Coverage during the offline regression suite into a unique `evidence/coverage` directory, requires a fresh Cobertura report, parses line/branch coverage and embeds metrics in JSON evidence.
- README updated to describe this output.
- Commits: `fcdb5a4911b66650ceb0e662c8f65e29cd5ef168`, `6152fb48cf918e290366dd21017cccd363fa592c`, `c6c51ab7aaf09c0076ac7ef35fd98cb42c050dde`.
- Static PowerShell review only; local Windows execution remains pending. No Actions used.


- Coverage evidence path formatting now avoids `Path.GetRelativePath`, which is unavailable in Windows PowerShell 5.1/.NET Framework; it uses compatible path substring/trim APIs. Commit: `fa9b821f09e09d6f80fb2c76f5018afb85a7f154`.


- README now distinguishes direct-test and full-assessment coverage report locations and gives the correct summary command for `evidence/coverage`. Commit: `1650553067270dbee78a158bf5d40e115b94c9fa`.


- Aggregator now normalizes provider position maps without throwing on case-insensitive duplicate keys; blank keys and non-positive positions are ignored and the best position is retained. Added regression test. Commits: `247c80fd8df94408dafb086c7b5f40e37be11921`, `7644cbd4f2a26d97c6dbe19c8ee177734a887eca`. Execution pending local .NET 10.


- Aggregator now matches explicit provider selections with `OrdinalIgnoreCase`, independent of the supplied set comparer; added a regression test using a default case-sensitive set. Commits: `d4b314be5e2786c0f1e2891ec9d4df4c057f5041`, `0840587e9b0270bd88531d91c13c01575ce64a85`. Execution pending local .NET 10.


## Latest quota model — 2026-11-24
- Failed bearer attempts: 10/minute per direct peer IP.
- Authenticated search: 60/minute per direct peer IP **and** 60/minute per API key. Per-key partitions use SHA-256 digests, not raw keys. Both limiters are process-local fixed windows and are host-owned/disposed.
- Added direct per-key limiter tests proving independent key partitions and invalid-limit rejection.
- Commits: `b54addd11c589de690ca3a61011c70e03bed8663`, `8333e6ad8864b745dd5de101113dfd071e53e752`, `865b33577f0aec3b944a99b69a988a6a9f788e95`, `0d913693b495b929f3eed0864317491b47bb4c79`.
- Proxy caveat: the service currently partitions IP quotas on the direct connection peer; trusted forwarded-header handling remains outstanding for reverse-proxy deployments.
- Static review only; local .NET 10 build/test pending.


## Latest reverse-proxy quota support — 2026-11-24
- Added explicit `TRUSTED_PROXY_IPS` configuration. Only configured immediate proxy addresses are trusted for forwarded client IP/protocol; one hop is processed, and forwarded headers are ignored when unset. Configuration is validated at startup and middleware runs before routing and rate limiting.
- Added tests for invalid proxy configuration, trusted proxy client-IP partitioning, and rejection of spoofed forwarded headers from untrusted peers.
- README documents the configuration and trust boundary.
- Commits: `a9f66a0e38c2f9b36a62f03fdf0b14fd5d31be1c`, `f53dfdd5b0a61cbd1bce4a7d44e2d0f5ef4f1c66`, `b1ae1e639c01b1862d6c610995bf9d4ea961cadf`, `6465e5d1f28bcb0ab3abf4249bf752d3b18729f6`, `10e7e5756a8464f8fbad78447c0caa9eb31ddcc1`.
- Static review only; local .NET 10 build/test pending.


- Proxy/auth integration tests now save, clear and restore inherited `SEARCH_API_KEYS` and `TRUSTED_PROXY_IPS`, preventing developer-machine settings from changing test outcomes. Commits: `845d8e81f16bf3cb277cdfdea05540ae600519e2`, `3b1bb202ce9cb27427c19edd63649208324b32fe`, `5fa73f011dbd3b194c54f6c74241affe38b05d82`.
- Forwarded-header options now clear `KnownIPNetworks` rather than obsolete `KnownNetworks`. Commit: `7b74b1a5ed55a70486d7c1518b346649ef3a810e`.


- Failed-auth and authenticated IP quota partitions now normalize IPv4-mapped IPv6 to the corresponding IPv4 address; added an equality regression test. Commits: `77f82746e271f68405313fcf4b510edfb5e8c174`, `40f2fb91b50aa77d30f5aacee3832bc8cfc9e13e`. Execution pending local .NET 10.


- Coverage percentage parsing now uses fully qualified `System.Globalization.CultureInfo.InvariantCulture` references. Commit: `acfc80b201ea96a7e6d251351be55fc7e7725aac`. PowerShell execution pending locally.


- Auth/proxy integration tests now override configuration with empty strings instead of removing environment variables, ensuring the environment provider overrides any appsettings values on developer machines; original process values are restored after the collection. Commits: `c90828b35b7ef2386f60d479631c97fc5998a117`, `3f1ddaa3bb7bbbf3ec6bdb1d9932993d6a21eaaf`.


- All three throttling layers (failed auth, per-key search and IP search) now expose Retry-After seconds when the limiter supplies retry metadata. Regression tests assert the header for failed-auth and IP throttles. Commits: `4b639f399fd65e5cf859afe4f45acf51e638da88`, `ca56e12a614d39d79fe4fa563fed9c43b90944ee`, `1f1d46ecb551e11b8e31c372a022269b8068b75e`.


## Latest transport enforcement — 2026-11-24
- API server now rejects remote search requests over HTTP whenever API keys are configured; loopback HTTP remains allowed for local development. Trusted reverse proxies can supply `X-Forwarded-Proto: https` only when their IP is in `TRUSTED_PROXY_IPS`.
- Added secure-transport decision tests; proxy integration tests now send trusted forwarded scheme.
- Commits: `3b235706d6c8bf189696cc8ff8ddda6a04d6382f`, `26bab43114c8533ae22f73538bcbbdaf966443e1`, `896dc21801e8c232f9ff8e809cd6522ca14f17c7`, `35a8b3601e59c2910c4b5d3a5fb096ccdecfac0a`.
- Static review only; local .NET 10 build/test pending.


- Corrected the loopback transport helper to call the static `IPAddress.IsLoopback(address)` API. Commit: `ac7829318aa756e010ab74a5ddac54e749de15b9`.


- Browser search UI now refuses to attach an API key to requests from remote HTTP pages; loopback hostnames/addresses remain allowed. Server-side HTTPS enforcement remains authoritative. Commits: `9d8fbbbedfb14e420cc11f489a16b01b170b6e17`, `ffdfefe2c16d9f5b0345c81879beaa99ac94d7b6`. Static JS review only; browser verification pending.


- Loopback HTTP is now allowed only in Development. Staging/Production require HTTPS even when the direct peer is localhost, closing the reverse-proxy-to-loopback bypass; trusted TLS proxies must forward X-Forwarded-Proto=https from configured IPs. Commits: `851d2ca26154e7a68576887bb4c991d917dee8d0`, `34a01c4763fefb65a82dcd32a1024f6e0bc53712`, `34608f41f57e3e1b68a91ba4eddad82e98e601cc`.


## Latest live assessment diagnosis — 2026-11-24
- Release build passed with 0 warnings/errors; offline regression suite passed (373 passed, 5 skipped).
- Production test host started and reported 63 registered providers. First live batch selected 20: 16 passed, 4 failed (Bilibili HTTP 412, Europe PMC cancellation/timeout, Google HTTP 403, Google Books HTTP 429).
- Fixed the full evidence runner to capture non-zero `dotnet test` output without PowerShell terminating the stage early. This should preserve provider inventory and failure details and allow subsequent batches plus aggregate API testing to run.
- Commit: `a7c91a20092ffabe2d45126a28ed67554afc2e28`.
- Pending user rerun of `scripts/Test-FullSearchEvidence.ps1`; no local execution or workflows used.


- Auth integration tests now use an HTTPS TestServer client. Trusted-proxy test runs in Staging and relies on trusted forwarded scheme/IP; an untrusted forwarded scheme is explicitly rejected in Staging. Commits: `96943e789f08d88ddd8397027971bc6dcf2192ce`, `c635a9fc6f7d30e4ffb4a9efb9de8374886bb081`, `1aea8c63b8ae2ec4c12c42336aae121dbf9b86ee`.
