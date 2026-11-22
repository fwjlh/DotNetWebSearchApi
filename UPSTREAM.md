# Upstream provenance and licensing

## Upstream project

This project takes inspiration from [SearXNG](https://github.com/searxng/searxng), an open-source metasearch engine. Its upstream README identifies it as a metasearch engine and declares the GNU Affero General Public License version 3.

- Project: SearXNG
- Repository: https://github.com/searxng/searxng
- License: AGPL-3.0 (upstream project declaration)
- License file: https://github.com/searxng/searxng/blob/master/LICENSE
- Documentation: https://docs.searxng.org/

## Relationship

DotNetWebSearchApi is an independent C# implementation inspired by the metasearch problem and public concepts of SearXNG. The initial implementation is not represented as a direct source-code translation or official SearXNG port. No SearXNG source files are intentionally copied into the initial codebase.

If later work copies, adapts, or translates protectable upstream implementation material, the affected files and changes must be identified, upstream copyright and license notices preserved as required, and licensing consequences reviewed before distribution. Conceptual similarity alone does not establish that code was copied.

## This repository's license

This repository is intended to be licensed under AGPL-3.0-or-later. The accompanying LICENSE file contains the GNU AGPL version 3 text. AGPL is strong copyleft and includes provisions relevant to modified network services. Read the complete license and obtain legal advice where appropriate. This document is provenance information, not legal advice.

## Third-party components

Before adding a package, provider adapter, copied snippet, or other third-party material:
1. identify its origin and exact version;
2. verify its license and redistribution conditions;
3. record the dependency and license in repository documentation;
4. preserve required notices;
5. avoid dependencies incompatible with this project's goals.

## Attribution boundary

SearXNG and its contributors retain their own names, marks, copyrights, and project identity. This repository is not affiliated with or endorsed by the SearXNG project.


## Porting audit reference
A comparative architecture audit is maintained in [docs/PORTING_AUDIT.md](docs/PORTING_AUDIT.md). It compares the current C# implementation with SearXNG's engine processor, query model, result container, traits and network layer. It records which shared behaviours are implemented, partial or missing and the recommended order for porting them.
