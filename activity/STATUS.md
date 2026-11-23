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

## Latest test failure corrections

- Removed the duplicate SearXNG JSON property collision: paper abstract now maps into the common `content` field instead of a second property with the same JSON name.
- Corrected the Openverse file-size expectation to `875.13 KB` for 896,128 bytes using the current invariant formatter.
- Corrected the Bilibili definition-level test to expect the normalized Referer URI `https://search.bilibili.com/`.
- Commits: `d0fa0416`, `7220173f`, `5f734214`, `20b365d9`, `9129fb6b`, `a74679f5`.
- Latest user run: 3 failures remain — Bilibili expectation, Crossref publication year, and 63 vs 67 production-host registrations. The Bilibili assertion is now corrected on `main`.
- The provider-count test now checks Exa, Yandex, Kagi and Brave before the aggregate count, so the next run identifies which optional registration is missing.
- Crossref discrepancy needs local revision verification: current `main` defines `PublicationYear` as a non-nullable string defaulting to empty and assigns a string in the mapper, so the reported actual `null` is inconsistent with the currently fetched source.
- Fixed the Crossref publication-year response omission: source metadata already contained `2024`, but the SearXNG mapper did not assign `PaperPublicationYear`. Commit: `47369b59`.
- Fixed the optional-provider test configuration timing: the test now injects Exa/Yandex/Kagi/Brave keys via host configuration before minimal-host top-level registration. Commit: `2787902b`.
- Activity entries 156–157 record both fixes and their validation limits.
- No .NET build/tests were executed here. These two changes require the user's next local Release test run.

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
