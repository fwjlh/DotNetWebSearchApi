# 2026-11-23 — Typed image result metadata foundation

## Reason
The pinned SearXNG result types distinguish image results from ordinary links. Image results carry an image source, preview thumbnail, resolution, format, source, file size and alternate formats. Openverse also returns licence and creator information. The existing C# model had only a general thumbnail field, so it could not preserve this data without an untyped extension bag.

## Changes
- Added typed `ImageResultMetadata` and `ImageFormatReference` records.
- Added optional `SearchResult.ImageMetadata`.
- Exposed SearXNG-compatible `img_src`, `thumbnail_src`, `resolution`, `img_format`, `source`, `filesize` and `formats` fields.
- Preserved typed licence, licence URL, creator and byte-size metadata under the explicit `image_metadata` extension.
- Added serialization/mapping coverage for the typed metadata and SearXNG field names.
- Updated the porting audit. No provider parser populates the new metadata yet; URL validation is required before wiring provider responses.

## Commits
- `9cf0be75f2a2c0c92693855cc1125da18fb67c03` — add typed image metadata.
- `20eac20da61608ddebdb3e488ef842d3608a040d` — attach metadata to SearchResult.
- `96d46c46b7866cf0f75b34ed38edd3f23a9b234a` — retain human-readable and byte file sizes.
- `faeebd9125fc5ecc7e09bcc778c19de52f31a146` — expose image fields in SearXNG response model.
- `cb41a3b67cdec41aab2cca7c6a0763b17e44301b` — align alternate-format media type field with SearXNG `mtype`.
- `dc6decec4bf8487453ed5e9f566d8cdbeb5d38a4` — map image metadata to response fields.
- `d440d4ddf630671ab2c1e3353e7b37d7aae4188c` — add typed metadata mapping test.
- `d7d4f2b15e3b881b166549c72cc3c06b28cd4df2` — remove unused test import.
- `0d6fd25190ccd04f10b43b18268cde5d2ad5a8ea` — update porting audit.

## Validation
- Inspected the final metadata model, response model, mapper and test source.
- Test asserts SearXNG field names including `mtype`, plus licence and creator values in the typed extension.
- Local test execution remains unavailable because `dotnet` is not installed. No GitHub Actions workflows were run or recreated.
- No live image provider requests were made.

## Next step
Add strict absolute HTTP(S) URL validation for image source, thumbnail and alternate-format URLs, then wire Openverse's documented response fields into `ImageResultMetadata` with fixture tests. Keep provider count frozen at 67.
