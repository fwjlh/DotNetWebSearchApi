# Current project handover

**Repository:** `fwjlh/DotNetWebSearchApi`  
**Branch:** `main`  
**Last reviewed:** 2026-11-23

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
- **Live integration tests:** added opt-in real HTTP checks for Wikipedia, Crossref and GitHub. Enable with `DOTNETWEBSEARCHAPI_LIVE_TESTS=1`; ordinary test runs skip them. Each test makes one request with a 20-second timeout and a 1 MiB response bound.
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
- Tests must use local fixtures; no live provider calls in tests.
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
