# DotNetWebSearchApi — current handover

**Repository:** `fwjlh/DotNetWebSearchApi`  
**Branch:** `main`  
**Last updated:** 2026-11-23  

## Working contract

Read root [`_LookAtMe.MD`](../_LookAtMe.MD) before changing code. Keep changes small, use named/pure helpers, preserve explicit types, and add meaningful fixture tests. Log every change and reason in [`LOG.md`](LOG.md).

- Direct commits to `main`.
- **Do not use GitHub Actions.** There are no files under `.github/workflows/`; do not recreate them.
- No live upstream calls in unit tests; use fixtures and stub handlers.
- Keep C# source/test files below 20 KB (25 KB is the hard ceiling).
- Provider inventory is frozen at **67**; do not add providers without an explicit change to scope.

## Provider inventory

- 52 generic JSON definitions across the three catalogue files.
- 2 RSS/Atom adapters.
- 13 dedicated adapters.
- Total: **67 providers**. The production-host integration test asserts 67 unique providers when optional credentials are configured.

## Implemented areas

- Shared request context, provider capabilities, health/cooldown, typed outcomes, challenge classification, transport and result ranking/provenance.
- Locale, paging, time-range and safe-search semantics for the dedicated adapters where upstreams support them.
- Specialist category preservation and safe template selection:
  - Images: Openverse typed image metadata; Wikimedia Commons remains general until direct image URLs are available.
  - Video: Bilibili, Dailymotion and PeerTube typed metadata; embed URLs are HTTPS and host-validated.
  - Audio: Deezer previews and Radio Browser streams; typed artist/station metadata and safe URLs.
  - News: RSS/Atom, Wikinews and Hacker News publication dates/categories.
  - Scholarly papers: arXiv, Crossref, OpenAlex, Europe PMC, Semantic Scholar, DataCite, Zenodo, OpenAIRE and PLOS.
- SearXNG Paper mapping includes publication date/year, abstract, authors, publisher/journal, DOI, identifiers and available PDF/HTML links. Year-only values are not converted into invented full dates.
- Mojeek and Brave declare and apply safe-search behaviour; multi-vertical JSON category selection is deterministic.

## Repository health

- 83 C# source/test files; **none exceeds 20 KB**.
- JSON catalogue files: base 15.6 KB, media 14.2 KB, Wave7 7.8 KB.
- 35 test source files, including provider fixtures, category selection, media safety, paper metadata and API integration.
- `_LookAtMe.MD`, `activity/LOG.md` and this file are present.
- No `.github/workflows/` files exist. No GitHub Actions have been invoked since the instruction to stop; no workflow files currently exist.

## Test status and known limits

**Tests have not been executed in this environment:** the .NET SDK is not installed. Do not claim the suite passes until a local build/test run has completed.

Known deliberate gaps:
- OpenAlex inverted-index abstracts are not reconstructed.
- Crossref and DataCite year-only publication data is preserved as a year, not a fabricated date.
- Some providers do not expose PDF links; the validated result URL is used as the HTML fallback.
- Wikimedia Commons remains general until its API response includes direct image metadata.

## Exact next step

At the planned local build/test session:

1. Use the .NET 10 SDK and run `dotnet test` for the solution.
2. Fix compile and fixture failures in small commits directly to `main`.
3. Measure actual coverage for the new parser, capability and result-mapping logic.
4. Continue validating the configured provider field paths against their real response schemas, without live calls in tests.

Never use GitHub Actions or workflow minutes. Keep this file and `LOG.md` current so the next session can resume without relying on chat history.
