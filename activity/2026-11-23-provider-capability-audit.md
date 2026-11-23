# 2026-11-23 — Capability inventory corrections

## Reason
The category audit compared provider capability declarations with the actual result payloads. Wikimedia Commons was marked as an image provider despite returning only a file-page URL and snippet, with no direct image URL or dimensions. Bilibili also had an unsupported `page_size` parameter and lacked the Referer documented by its legacy search endpoint.

## Changes
- Removed the `images` capability from Wikimedia Commons; it now uses the safe default `general` category until direct image metadata is retrieved.
- Added capability and aggregator regression tests proving Commons is skipped for image-only searches before any HTTP request.
- Bilibili no longer sends `page_size`; it uses the endpoint's default page size and sends `Referer: https://search.bilibili.com`.
- Added a definition test for the Bilibili Referer and pagination settings.
- Updated provider rationale and porting audit to explain both corrections.
- The generic JSON catalog remains 52 definitions; the wider provider inventory remains frozen at 67.

## Commits
- `8527f2d67b26bdef8e9c0868af47fdbfb39e1859` — keep Commons file pages out of image vertical.
- `4ce43d8f1b4836ea78ca2374dab360d0cffcb8d5` — test Commons capability and aggregator skip.
- `824af2cf250c52a5b2f52e4ea7defb4ac821117f` — set Bilibili Referer and remove unsupported page size.
- `d890e17d3ccda059a22b0c419fef13f8b56991e1` — test Bilibili request compatibility settings.
- `2da10316b9309511caeaaa128b858cc98251ee9a` — document Commons and Bilibili corrections.
- `4ab279e10e5c0ca9300d8ff2cfbb10bc005ddbb5` — update capability audit.

## Validation
- Inspected all 52 generic JSON definitions across the three catalog files.
- Verified all specialist categories are intentional; Commons was the image-category mismatch found in this pass.
- Existing capability theory covers image/news/video/music definitions; new test proves Commons is no longer selected for image searches.
- Test execution remains unavailable because `dotnet` is not installed. No GitHub Actions or live upstream requests were used.

## Next step
Continue auditing the 15 dedicated/search adapters and the full 67-provider registration list. Check that provider-specific request requirements, capability declarations and result mapping agree. Add registration/query-selection tests where a mismatch is found.
