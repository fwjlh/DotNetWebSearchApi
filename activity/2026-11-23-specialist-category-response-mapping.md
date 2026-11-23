# 2026-11-23 — Preserve specialist categories in SearXNG responses

## Reason
The provider capability audit correctly separated image, video, music and news providers from general-web providers, but the SearXNG response mapper still hard-coded every result's category to `general`. That discarded the category after provider selection and made specialist responses misleading to compatible clients.

## Changes
- Added `SearchResult.Category`, defaulting to `general` to preserve existing provider behaviour.
- Generic JSON results now take their category from the provider definition.
- SearXNG response mapping now emits the result's category rather than hard-coding `general`.
- Added a fixture test using Bilibili to verify `videos` survives provider parsing and SearXNG mapping.
- Existing general-provider and specialist-provider capability tests remain in place.

## Commits
- `2f822c38f6c8cfb459cffde138298cb6a615f115` — retain result category in the model.
- `e19be5faee20c862556b2e636ea13bf4d33f7e98` — assign configured category to generic JSON results.
- `9dacb6a1c2f0f0ebcb1527dfee55a233829ba8ad` — preserve category in SearXNG mapping.
- `d6de9de0d8252b8f536684fdf097fb7d8ff824e8` — add category round-trip test.
- `b8ba727a517183dc9a3af515ed80ea6911272d20` — correct the JSON fixture string in that test.

## Validation
- Inspected the changed model, provider, mapper and test source after each commit.
- Confirmed the fixture JSON string is correctly escaped in C# source.
- Attempted local SDK discovery with `dotnet --info`; the command is unavailable in this environment (`dotnet: command not found`).
- No GitHub Actions workflows were run or recreated. No live provider requests were made.

## Next step
Plan the richer result model for image/news/video metadata (image URL, licence/attribution, publication date and media-specific fields) without forcing provider-specific data into untyped dictionaries. Add model and API mapping tests before changing provider parsers.
