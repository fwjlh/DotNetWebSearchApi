# 2026-11-23 — General-provider vertical regression

## Reason
Openverse is now correctly restricted to image searches. The inverse boundary also needs a regression test: a general-web provider must be skipped for an image-only request before any outbound HTTP request is made.

## Change
- Added `SearchDetailedAsync_SkipsGeneralProviderForImageSearch` to `JsonApiProviderCapabilityTests`.
- The test uses Crossref as a representative generic JSON general-web provider.
- It asserts no results, Crossref listed as unsupported, and zero HTTP requests.
- No provider catalogue entries or runtime behaviour were changed.

## Validation
- Reviewed the test against the existing Openverse capability tests and the current aggregator result contract.
- Committed the test directly to `main` in `bfee32d6c04a0a5178f71baacc26d0b3f6ea277b`.
- Test execution is pending: this environment has no .NET SDK. GitHub Actions is disabled by repository-owner instruction and was not used.
- No live upstream request was made.

## Next step
Continue auditing generic JSON provider categories against actual endpoint/response semantics. The next candidate is the video/media group (Bilibili, Dailymotion, PeerTube, TVmaze, Deezer and Radio Browser); classify each only where its search vertical is unambiguous and add aggregator-level skip/selection tests. Keep the catalogue frozen at 67.
