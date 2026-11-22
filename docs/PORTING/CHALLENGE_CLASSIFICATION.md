# Provider challenge classification and safe handling

Date: 2026-11-22

## Why this is a separate dimension

SearXNG does not treat every blocked request as one generic CAPTCHA. Its engines distinguish rate limiting, access denial, CAPTCHA exceptions, response failures and engine suspension. Providers may also expose concrete challenge protocols: HTML verification routes, browser/session checks, proof-of-work tokens or interactive CAPTCHAs.

The C# implementation previously reduced these cases to `ProviderFailureKind.Challenge`. That was enough to trigger a cooldown, but it lost information needed by diagnostics, clients and a future human-operated verification workflow.

The translation keeps two separate values:

- `ProviderFailureKind` answers **what happened operationally** and drives health/cooldown behaviour.
- `ProviderChallengeKind` answers **what kind of interaction is involved** and is descriptive metadata.

A 429 remains `RateLimited`, not a challenge. A plain 403 remains `AccessDenied` unless provider evidence identifies a verification flow. Do not infer a challenge subtype from HTTP status alone.

## Challenge taxonomy

| Kind | Meaning | Current mapping |
|---|---|---|
| `TemporaryBlock` | Temporary block without an interactive flow | Reserved; no current adapter maps it |
| `BrowserVerification` | Browser/session/JavaScript verification expected | Qwant DataDome; Seznam `/verify` |
| `ProofOfWork` | Provider-supplied bounded computational challenge | Mojeek Altcha |
| `InteractiveCaptcha` | CAPTCHA or explicit human-verification page | Google, DuckDuckGo, Yandex; Mojeek fallback CAPTCHA |
| `InteractiveVerification` | Non-CAPTCHA human confirmation | Reserved |
| `LoginRequired` | Authentication required before search | Reserved |
| `Unknown` | Challenge detected, subtype unknown | Fallback |

## Why each provider is classified this way

- **Google:** SearXNG's `detect_google_sorry` treats `sorry.google.com`, a `/sorry` path, HTTP 302 and a short body containing `/sorry/` as its CAPTCHA exception. C# uses equivalent URL/body markers and classifies the flow as `InteractiveCaptcha`.
- **DuckDuckGo:** Python detects the `challenge-form` CAPTCHA markup. C# retains the same form marker and classifies it as `InteractiveCaptcha`.
- **Qwant:** Python distinguishes API error code 24 (too many requests), CAPTCHA redirect and plain HTTP 403. C# keeps 429/rate-limit and generic 403/access-denied separate; a DataDome/captcha marker on 403 becomes `BrowserVerification`.
- **Seznam:** Python treats a response path beginning `/verify` as access denied. C# upgrades this known route to `Challenge` + `BrowserVerification`, so it receives challenge cooldown rather than permanent access-denied cooldown.
- **Mojeek:** Python's Altcha protocol is a provider-defined proof-of-work exchange, not an interactive CAPTCHA solver. C# continues to solve only that bounded protocol, with a semaphore and counter limit. Failure to find a permitted solution or receive the expected cookie is `ProofOfWork`; a separate CAPTCHA page after verification is `InteractiveCaptcha`.
- **Yandex:** Python explicitly checks `x-yandex-captcha: captcha`. C# now checks the same response header and classifies it as `InteractiveCaptcha`.

## Safe handling policy

1. Classification is not a bypass mechanism. Do not automatically solve or circumvent interactive CAPTCHA, browser verification, login or human confirmation.
2. AI vision may describe a challenge page, identify its likely family and explain what a human needs to do. It must not click through, solve, tokenise or bypass a CAPTCHA.
3. Preserve provider-specific, documented or otherwise permitted protocols such as Mojeek Altcha, with bounded CPU/time/resource use and explicit tests. Do not generalise this into a CAPTCHA-solving subsystem.
4. A future browser hand-off must be optional and human-operated. Keep WebView2/WinForms out of this headless API and do not expose provider cookies or session tokens to clients.
5. Never log challenge bodies, cookie values, tokens, proof payloads or full query URLs. Diagnostics should expose provider name, typed status, safe timing and challenge enum only.
6. Challenge cooldown is based on operational failure kind; subtype is retained for explanation and future policy tuning. Provider-specific cooldowns can be added later without changing the public taxonomy.

## C# implementation and API surface

- `ProviderOutcome.cs`: `ProviderChallengeKind`, challenge metadata on `ProviderFailureException` and `ProviderOutcome`; enum values serialize as strings.
- `ProviderHealthRegistry.cs`: retains `LastChallengeKind` with the last failure for diagnostics.
- `SearchAggregator.cs`: carries challenge metadata through execution and cooldown-skipped outcomes.
- `Program.cs`: `/api/search` exposes `providerOutcomes`.
- `SearxngSearchResponse.cs` / mapper: `/search` adds `provider_outcomes` without changing existing result fields.
- Provider detectors: Google, DuckDuckGo, Qwant, Mojeek, Seznam and Yandex.

The taxonomy is additive. Existing result fields and `unresponsive_engines`, `unsupported_engines` and `cooling_down_engines` semantics remain intact.

## Verification limits

Fixture tests verify marker recognition, typed outcome propagation and JSON serialization. They do not prove that an upstream provider currently emits the same challenge page, that a live session can pass verification, or that a provider will accept the request. No live challenge attempts are part of this work.
