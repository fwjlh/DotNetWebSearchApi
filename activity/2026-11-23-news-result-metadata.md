# 2026-11-23 — News category and publication-date preservation

## Reason
The specialist category audit found that the generic JSON providers had explicit vertical categories, but the RSS/Atom news adapters still inherited the conservative general-web capability profile. They also discarded publication dates even though SearXNG's common result model and response include them. This meant news-only requests could skip news providers, and selected news results lost useful metadata.

## Changes
- Added optional `SearchResult.PublishedDate` while retaining the existing `general` category default.
- RSS/Atom providers now explicitly declare `news` capability, first-page-only, with no unsupported locale, safe-search or time-range claims.
- RSS 2.0 `pubDate`, Atom `published`, and Atom `updated` are parsed using invariant date handling.
- RSS/Atom results are tagged `news`.
- SearXNG mapping now emits `publishedDate` from `SearchResult.PublishedDate` (category mapping was already fixed in the preceding work).
- Tests cover RSS and Atom dates, news category capability, and category/date preservation through the SearXNG mapper.
- Updated the porting audit and RSS/Atom rationale to reflect the implemented behaviour and remaining gaps.

## Commits
- `5e0f5d968d9a5e92b8933073c79981cf56a05846` — add published date to SearchResult.
- `3077eada835ebf2ce926614a8eeec9709c43989f` — parse RSS/Atom dates and assign news category.
- `fc935b4be6fd562a5b5071366f8a56df5c810c8d` — expose RSS capabilities to the aggregator.
- `94006ba74ca4acce29d05ea77f2adb7c11b17620` — map publication dates into SearXNG responses.
- `407af21f29918db644e547230b3d8cf76c00a54e` — add RSS/Atom category and date tests.
- `dc0f46a808743f3aaf14ff0c1a89e4b2760c2544` — update porting audit.
- `3a9b0e98ad14e19d28a9eb4569644c5992191881` — update RSS/Atom porting rationale.

## Validation
- Inspected the final provider, mapper and test source after the commits.
- Fixture coverage asserts RSS RFC-822 date parsing, Atom ISO-8601 date parsing, news-only capability, and mapped category/date values.
- Attempted local verification; `dotnet --info` fails because the .NET SDK is not installed in this environment.
- No GitHub Actions workflow was invoked or recreated. No live news feed requests were made.

## Next step
Continue the typed result-model work from the pinned SearXNG result types. Prioritise image fields (`img_src`, `thumbnail_src`, resolution, format, source, file size and alternate formats) and video embed URLs, keeping the model typed and source files small. Add fixture tests and API mapping tests with each increment.
