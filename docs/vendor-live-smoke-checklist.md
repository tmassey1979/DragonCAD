# Live Provider Smoke Operator Checklist

Issue: [#58](https://github.com/tmassey1979/DragonCAD/issues/58)

This checklist is for manual, opt-in verification that the credentialed Digi-Key and Mouser catalog search paths can reach the live provider APIs. It is intentionally outside default CI. Normal builds and tests must remain offline, deterministic, and runnable without network access or vendor credentials.

## Scope Guardrails

Live smoke is catalog search only. It is not purchasing, cart mutation, checkout, payment, shipping, quote acceptance, file upload, fabrication submission, or order submission.

Do not use personal production purchasing workflows while running this checklist. Do not paste secrets into source files, markdown, test fixtures, screenshots, logs, issue comments, pull requests, or terminal transcripts that will be committed.

## Required Environment Variables

Set these only in the local operator shell or a local secret manager:

| Variable | Required for | Value |
| --- | --- | --- |
| `DRAGONCAD_VENDOR_LIVE_SMOKE` | Enables any live smoke call | `1` or `true` |
| `DRAGONCAD_DIGIKEY_CLIENT_ID` | Digi-Key OAuth client credentials | Placeholder: `<digikey-client-id>` |
| `DRAGONCAD_DIGIKEY_CLIENT_SECRET` | Digi-Key OAuth client credentials | Placeholder: `<digikey-client-secret>` |
| `DRAGONCAD_MOUSER_API_KEY` | Mouser Search API | Placeholder: `<mouser-api-key>` |

Example PowerShell setup for the current process only:

```powershell
$env:DRAGONCAD_VENDOR_LIVE_SMOKE = "1"
$env:DRAGONCAD_DIGIKEY_CLIENT_ID = "<digikey-client-id>"
$env:DRAGONCAD_DIGIKEY_CLIENT_SECRET = "<digikey-client-secret>"
$env:DRAGONCAD_MOUSER_API_KEY = "<mouser-api-key>"
```

## Safe Sample Queries

Use common, low-risk catalog searches with a small result limit:

| Provider | Query | Limit | Reason |
| --- | --- | --- | --- |
| Digi-Key | `LM7805` | `1` | Common linear regulator keyword with broad catalog coverage. |
| Mouser | `LM7805` | `1` | Same low-volume query for comparable provider smoke. |
| Either provider | `NE555` | `1` | Alternate common IC keyword if the first query is temporarily unavailable. |

Do not use project BOMs, customer part lists, private manufacturer part numbers, or purchasing-sensitive quantities for smoke verification.

## Run Command

Run the focused smoke tests from the repository root:

```powershell
dotnet test tests\DragonCAD.Sourcing.Tests\DragonCAD.Sourcing.Tests.csproj --filter "FullyQualifiedName~VendorLiveSmokeTests" --logger "console;verbosity=minimal"
```

When `DRAGONCAD_VENDOR_LIVE_SMOKE` is not set to `1` or `true`, the same focused tests must report disabled smoke results and must not create HTTP clients.

## Expected Non-Secret Diagnostics

Acceptable operator notes:

- Provider name: `Digi-Key` or `Mouser`.
- Run status: disabled, succeeded, or failed.
- Normalized listing count.
- Diagnostic severity, code, provider name, message, and vendor SKU when returned by the app.
- HTTP category if visible through an app diagnostic, such as credential missing, rate limited, transient service outage, or provider validation failure.

Never record or commit:

- Client secrets, API keys, OAuth access tokens, refresh tokens, authorization headers, cookies, or raw credential values.
- Full request URLs that include credentials or query-string keys.
- Raw provider response payloads unless they have been reviewed and redacted.
- Screenshots or logs that reveal secrets, account identifiers, private BOMs, or customer data.

## Cleanup

Clear the process environment after the run:

```powershell
Remove-Item Env:\DRAGONCAD_VENDOR_LIVE_SMOKE -ErrorAction SilentlyContinue
Remove-Item Env:\DRAGONCAD_DIGIKEY_CLIENT_ID -ErrorAction SilentlyContinue
Remove-Item Env:\DRAGONCAD_DIGIKEY_CLIENT_SECRET -ErrorAction SilentlyContinue
Remove-Item Env:\DRAGONCAD_MOUSER_API_KEY -ErrorAction SilentlyContinue
```

Close terminals that previously held live credentials. If credentials were accidentally pasted into a tracked file, shell history, screenshot, issue, or log artifact, stop using them and rotate the affected provider credential before continuing.

## Default Validation Contract

Default CI and local validation must stay offline:

- Do not set `DRAGONCAD_VENDOR_LIVE_SMOKE` in CI.
- Do not require Digi-Key or Mouser credentials for normal build, test, or docs validation.
- Keep live smoke focused on `VendorLiveSmokeTests`; broader solution tests should pass with the smoke gate missing.
- Keep documentation link validation local and offline with `docs\documentation-test\Validate-ReadmeLinks.ps1`.
