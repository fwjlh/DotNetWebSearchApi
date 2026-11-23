# 2026-11-23 — Bilibili and PeerTube video metadata

## Reason
Dailymotion video metadata established the typed model, but the other specialist video adapters still returned ordinary results. The Bilibili and PeerTube response schemas expose useful metadata with different timestamp and embed-path conventions. Embed destinations also need stronger validation than merely accepting any HTTP(S) URL.

## Changes
- Bilibili maps `pic`, `author`, `play` and Unix `pubdate` into typed video metadata. Duration is intentionally not guessed because available response descriptions disagree on its string format; no embed URL is constructed.
- PeerTube maps `embedPath`, `thumbnails[0].fileUrl`, `duration`, `publishedAt`, `account.displayName` and `views`.
- Relative PeerTube embed paths resolve only against the result URL's own origin.
- Absolute embed URLs must use HTTPS and either the result origin or an explicitly allowed provider host. Dailymotion's `geo.dailymotion.com` host is explicitly allowed.
- Thumbnail URLs remain absolute HTTP(S) only.
- Added Bilibili and PeerTube fixture tests for metadata and unsafe thumbnail/embed rejection; added a same-host HTTP embed rejection test.
- Updated provider rationale and porting audit.

## Source evidence
- Bilibili response fields: https://github.com/yi-zhu/bilibili-API/blob/master/docs/search/search_response.md
- Bilibili endpoint request notes: https://github.com/Chilfish/bilibili-API/blob/master/docs/search/search_request.md
- PeerTube video response: https://docs.joinpeertube.org/api-rest-reference/api/operations/getvideos.html
- Dailymotion embed host guidance: https://developers.dailymotion.com/v0/reference/migration-guide-new-embed-endpoint

## Commits
- `ac9e8f7aa50f7aa9b2efc1ee693935be8cb963e6` — map Bilibili video metadata.
- `8b80ed7912c44d54dbfa1750985124d0f8fc914b` — add Bilibili metadata tests.
- `9f34744f69c0351c99fb5f8be5a80586f23dc3ef` — support same-origin relative video embed paths.
- `6054418a2d39b1db94541c9e7955c70d18ac6b77` — map PeerTube video metadata.
- `7bd443e27f36ab52139556e799cad3d94970f86d` — add PeerTube metadata and relative-path tests.
- `d620b250d2002678137f07898ab39d684cadd3a1` — restrict embeds to result-origin or allowed hosts.
- `9e956fac91ed0c296bf0262d3563bf8f7cd27d2c` — allow Dailymotion's documented embed host.
- `972f10de0ae29fd1283d989c9d7f561795524ecf` — test rejection of foreign Dailymotion embed hosts.
- `6305a75943fb575425da97e8f5029842cff4d618` — simplify absolute URL validation.
- `727ca565b32a5e9b819ce7100c3c081b2d9e78da` — enforce same-origin embed scheme/port.
- `ffb9acf050ff6acc71bff877d19881a76efae603` — require HTTPS for embed destinations.
- `ec0064cdf00edd40ed8edb55fb6d9907e686b346` — test rejection of HTTP same-host embeds.
- `724d85a0c1f8e7aedfca7bd11b00b2cf2264e702` — document Bilibili and PeerTube mappings.
- `5f254a2eb768ee1757654147c489766b477f282e` — update video provider coverage audit.
- `570858fb43ae7e7d7c77e10546ef009bf104bde9` — clarify embed URL policy in rationale.
- `95705ef9a3d115637066b9dbc8eadcfc7ed65329` — clarify embed URL policy in audit.

## Validation
- Inspected provider definitions, metadata parser, response mapper and test fixtures.
- Tests cover Bilibili author/views/thumbnail/Unix date; PeerTube author/views/thumbnail/duration/ISO date and same-origin embed; cross-origin relative, foreign absolute, data:, javascript: and HTTP embed rejection across the video tests.
- Local test execution remains unavailable because `dotnet` is not installed. No GitHub Actions or live provider calls were used.
- Bilibili's Referer/WBI availability and PeerTube's fixed public instance remain operational concerns; fixture tests do not prove live availability.

## Next step
Review the entire provider capability inventory for vertical mismatches and verify that the response mapper does not emit specialist templates for results without corresponding metadata. Then add tests for provider registration and query selection without making live upstream calls.
