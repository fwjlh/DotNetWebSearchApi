# Current project handover

**Repository:** `fwjlh/DotNetWebSearchApi`  
**Working branch:** `main`  
**Last reviewed:** 2026-11-23

## Current state

- Provider inventory is **67 unique providers**: 52 JSON definitions, 2 RSS/Atom adapters and 13 dedicated registrations. The production-host regression supplies dummy optional credentials and checks the 67-provider count.
- The JSON catalogue is split by family. All 83 C# source/test files are below 20 KB; the largest is 19,463 bytes.
- The catalogue regression checks 52 unique JSON provider names.
- `_LookAtMe.MD` contains the coding, testing, size and structure rules.
- `activity/LOG.md` records actions, reasons, tests, commits and next steps.

## Implemented coverage

- Search aggregation, provider capabilities, category filtering and SearXNG-compatible response mapping.
- Typed image metadata: Openverse URLs, thumbnails, dimensions, formats, source, size, licence and creator.
- Typed video metadata: Dailymotion, Bilibili and PeerTube embed/thumbnail/details.
- Typed music metadata: Deezer previews, RadioBrowser streams and MusicBrainz artist/country details. MusicBrainz does not invent an audio stream.
- Typed paper metadata: arXiv, Crossref, OpenAlex, Europe PMC, Semantic Scholar, DataCite, Zenodo, OpenAIRE and PLOS.
- News dates from supported RSS/Atom and Wikinews/Hacker News responses.
- Paper, image and video templates are selected only when their corresponding typed metadata is usable. Unsafe or missing image/embed URLs fall back to `default.html`.
- Provider capability corrections include arXiv science-only, Mojeek/Seznam general+web, and safe-search mapping for Mojeek and Brave.
- Wikimedia Commons remains general until its adapter returns a direct, validated image URL.

## Provider and workflow constraints

- Keep the provider count frozen at 67 unless explicitly authorised.
- No files currently exist under `.github/workflows/`.
- Do not recreate, invoke or rely on GitHub Actions/workflow minutes. Changes are committed directly to `main`.
- Tests must use local fixtures; do not make live provider calls in tests.

## Validation status

- **83 C# files; 35 test files; largest file 19,463 bytes.**
- Provider inventory verified as 52 + 2 + 13 = 67, with no duplicate JSON names.
- No workflow files present.
- Fixture and integration tests have been added, but **the test suite has not been executed** because the .NET 10 SDK is unavailable in this environment. Do not claim tests have passed.

## Exact next step

At the planned local build/test session:

1. Use the .NET 10 SDK and run `dotnet test` for the solution.
2. Fix compile and fixture failures in small commits directly to `main`.
3. Measure actual coverage for parser, capability and result-mapping logic.
4. Continue validating provider field paths against their response schemas using fixtures, without live calls.
5. Keep this file and `LOG.md` current so the next session can resume without chat history.

