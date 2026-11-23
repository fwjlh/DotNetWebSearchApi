# 2026-11-23 — Dailymotion typed video metadata

## Reason
The category audit had separated video providers, but video results still used the ordinary result template and could not preserve embed URL, thumbnail, duration or upload date. The pinned SearXNG Video type uses the video template and an iframe source. Dailymotion's documented API exposes the relevant fields, subject to its Player ID embed requirement.

## Changes
- Added typed `VideoResultMetadata` and attached it to `SearchResult`.
- Added generic JSON metadata paths for video embed URL, thumbnail, duration, upload date, author and views.
- Dailymotion now requests `embed_url`, `thumbnail_360_url`, `duration` and `uploaded_time`.
- Embed and thumbnail URLs are accepted only when absolute HTTP(S).
- Numeric upload timestamps are parsed as Unix seconds; ISO date strings are also supported.
- SearXNG mapping now selects `videos.html` for video category results and emits `iframe_src`, `length`, `thumbnail` and `publishedDate`.
- Added Dailymotion fixture tests for mapping, timestamp conversion and unsafe URL rejection; image template mapping is also asserted.
- Documented that Dailymotion requires a Player ID context for functional embeds. This project does not yet configure one, so no live playback claim is made.

## Source evidence
- Dailymotion video fields: https://developers.dailymotion.com/v0/reference/video-fields
- Dailymotion embed URL migration: https://developers.dailymotion.com/v0/reference/migration-guide-new-embed-endpoint

## Commits
- `4aefdadaec4c8a08e47f26dfab867eca9a072a97` — add typed video metadata.
- `10f28c9009aa35f7ca8c0812eaf93fef74f8c8d6` — attach video metadata to SearchResult.
- `9ed0dcb52b2379a5d37fe62d41cdd5399dce6202` — expose video fields in SearXNG result.
- `12384866dd081edf7e70af681689e4d834c1ec8a` — map video metadata and vertical templates.
- `13c23577707dbf3a0f7707775789a53a9cf53d42` — retain video publication date.
- `2db08a3c3baa1b1d042c70897bf092c7f3a94225` — parse typed video metadata in generic JSON provider.
- `e645f27629109a6418994283b11873fbbf72fbd3` — resolve image/video thumbnails.
- `99923907497044cafead7deb474409d82dc06afa` — request Dailymotion video metadata.
- `f5fc0d1ad309ae03c51a582740bb0885e8154d18` — add Dailymotion metadata and URL-safety tests.
- `2788f8fdea164ef1916c951d86625dc367e2ddff` — assert image template mapping.
- `8659b7b0f214e0c6bbd1600ea56e17ab3805757d` — make duration assertion type explicit.
- `6a33b8da55a583c90e7c79bcb4ab09cc29baa1f7` — document Dailymotion embed constraints.
- `c2d9fd124bc254023a4ddd3ac149193a1b0f6bc3` — update porting audit.

## Validation
- Inspected final provider metadata paths, response mapping and fixture source.
- Tests cover video category, embed URL, thumbnail, duration, Unix upload timestamp, SearXNG video template and rejection of javascript:/data: embed/thumbnail URLs.
- Local execution is blocked because `dotnet` is not installed. No GitHub Actions were used.
- No live Dailymotion requests or playback tests were performed.

## Next step
Add an explicit Dailymotion Player ID configuration path before claiming functional embeds. Then continue auditing Bilibili and PeerTube for equivalent provider-specific video fields. Keep provider count frozen at 67.
