# 2026-11-23 — Openverse typed image metadata and URL safety

## Reason
The typed image model needed a real provider implementation. Openverse's documented image result includes a direct media URL, thumbnail, dimensions, file size, file type, licence, creator, provider, source and attribution. The API must not trust arbitrary schemes for image, thumbnail, licence or creator URLs.

## Changes
- Added configurable image metadata paths to the generic JSON definition model.
- Wired the Openverse definition to its image metadata fields while retaining `foreign_landing_url` as the clickable result URL.
- Parsed direct image URL, thumbnail, width/height, file type, source, file size, licence, licence URL, creator, creator URL, provider, licence version, attribution and descriptive media category.
- Validated image, thumbnail, licence and creator URLs as absolute HTTP(S); unsafe or relative values are discarded.
- Parsed only non-negative file sizes and formatted them for the SearXNG `filesize` field.
- Added Openverse fixture tests for metadata mapping and rejection of javascript:, data: and relative URLs.
- Updated the porting notes and architecture audit.

## Source evidence
- Openverse API image result fields: https://api.openverse.org/v1/
- Openverse media property definitions: https://docs.openverse.org/meta/media_properties/api.html
- Openverse image API example and response schema: https://wordpress.github.io/openverse-api/reference/api/docs.html

## Commits
- `2778045ac01cc65656bd1a5f06972701cebe4ee2` — add attribution and source fields to image metadata.
- `38a607715dd4f1defe9881d8fac6d36ac3edca4d` — parse typed image metadata in generic JSON providers.
- `21702bde6bcd6f28eff052ea20b67d6181b5da02` — map Openverse response fields.
- `26def3c7e5f89f7c12babb55a68975a8c5f52c7a` — format image file sizes.
- `494cae163beb9d736c37714a73b5fb9c4910e83a` — reject negative file sizes.
- `d7a6f647de31289ac45a5390be174e610908b413` — add Openverse mapping and URL-safety tests.
- `bd03560fd2ea479b6578907ae961aaa45ea8f170` — align the file-size test expectation with midpoint rounding.
- `54e6ce21bd3503b70a7de1f4ffc7f3af086b2fc6` — document Openverse mapping.
- `d552b42e198bee84d228767e5100d17ba5c0ec2d` — update the porting audit.

## Validation
- Inspected the final provider definition, parser, image model, mapper and fixture tests.
- Fixture tests cover all mapped Openverse metadata and reject non-HTTP(S) image-related URLs.
- Local execution is still blocked because `dotnet` is not installed. No GitHub Actions were run or recreated.
- No live Openverse requests were made.

## Next step
Add video-specific typed metadata, beginning with the video embed URL and existing thumbnail fields. Keep embed URLs restricted to explicit HTTP(S) destinations and do not automatically embed arbitrary provider URLs. Then add a provider-specific fixture for one video adapter.
