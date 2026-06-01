# DragonCAD Provider Ingestion Status

Updated: 2026-06-01

This document tracks the current provider ingestion push. The goal is to move from seeded/offline marketplace rows to safe, provider-specific ingestion paths that can later feed the marketplace, BOM planning, datasheet intake, and trusted-library review pipeline.

## Current Execution Round

| Lane | Provider / Area | Owner | Write scope | Target outcome | Status |
| --- | --- | --- | --- | --- | --- |
| 1 | Adafruit public catalog | Worker 1 | `src/DragonCAD.Sourcing/Catalog/Adafruit/**`, `tests/DragonCAD.Sourcing.Tests/Catalog/Adafruit/**` | Fixture parser/client with provenance and diagnostics, no live fetch | Complete |
| 2 | SparkFun source libraries | Worker 2 | `src/DragonCAD.Sourcing/Catalog/SparkFun/**`, `tests/DragonCAD.Sourcing.Tests/Catalog/SparkFun/**` | Source manifest parser/validator for local/cache repository metadata | Complete |
| 3 | Jameco manual feed | Worker 3 | `src/DragonCAD.Sourcing/Catalog/Jameco/**`, `tests/DragonCAD.Sourcing.Tests/Catalog/Jameco/**` | CSV/manual feed parser with deterministic diagnostics | Complete |
| 4 | Secure credentials | Worker 4 | `src/DragonCAD.Sourcing/Credentials/**`, `tests/DragonCAD.Sourcing.Tests/Credentials/**` | Credential metadata/planning without storing secrets in project files | Complete |

## Provider Reality

| Provider | Pull-down path | Current policy |
| --- | --- | --- |
| Adafruit | Public catalog/API-style fixture parser first | No credentials; no live fetch until parser and rate/cache policy are verified |
| SparkFun | Source repository/library manifest sync | Prefer local/cache source packages and open hardware library assets |
| Jameco | Manual CSV/feed import | No scraping without written permission |
| Digi-Key | Credentialed official API | Requires secure credential boundary before client implementation |
| Mouser | Credentialed official API | Requires secure credential boundary before client implementation |

## Integration Rule

Provider-specific parsers create catalog candidates only. No imported provider row becomes a trusted/placeable component until it passes candidate linking, review, and trusted-library promotion gates.

## Completed Provider Pull-Down Foundation

- Adafruit: local JSON fixture parser maps public-catalog style rows into normalized listings with price, stock, datasheet/product URLs, retrieved timestamps, and deterministic diagnostics.
- SparkFun: source manifest parser and validator tracks local/cache repository metadata for open hardware/library sources, including stale-source and duplicate-source diagnostics.
- Jameco: manual CSV feed parser maps SKU, manufacturer part number, stock, unit price, product URL, and datasheet URL into catalog candidates while marking the provider as scrape-restricted.
- Digi-Key and Mouser: secure credential planning now describes required keys, storage location, missing-key diagnostics, and redacted log summaries before any API client is allowed to run.

## Credentialed API Clients

- Digi-Key: added a client-credentials OAuth token client for `https://api.digikey.com/v1/oauth2/token`, plus a Product Information V4 keyword-search client for `https://api.digikey.com/products/v4/search/keyword`.
- Digi-Key search maps vendor SKU, manufacturer part number, manufacturer, description, stock, standard price breaks, product URL, datasheet URL, package type, MOQ, status, category, and series into normalized catalog listings.
- Mouser: added Search API clients for part-number and keyword queries using the configured API key against `https://api.mouser.com/api/v2/search/partnumber` and `https://api.mouser.com/api/v2/search/keyword`.
- Mouser search maps vendor SKU, manufacturer part number, manufacturer, description, availability, price breaks, product detail URL, datasheet URL, category, packaging, lifecycle, RoHS, lead time, MOQ/multiple, and image path into normalized catalog listings.
- Client options can be loaded from `DRAGONCAD_DIGIKEY_CLIENT_ID`, `DRAGONCAD_DIGIKEY_CLIENT_SECRET`, and `DRAGONCAD_MOUSER_API_KEY`.
- API errors and missing credentials return diagnostics without logging client IDs, client secrets, API keys, or OAuth tokens.

## Executable Sync Surface

- Added a provider-neutral `VendorCatalogSyncRunner` that executes configured catalog search providers and returns normalized listings plus diagnostics.
- Added Digi-Key and Mouser search-provider adapters so the runner can call credentialed clients through one common interface.
- Unknown providers and blank search queries return blocked sync results with deterministic diagnostics.
- Added an app-side `VendorCatalogSyncResultViewModel` that maps sync results into display rows for SKU, MPN, stock/price, package, datasheet, product URL, and diagnostic messages.
- The Marketplace Vendor Sync panel now includes an API Sync Results section bound to `VendorCatalogSyncResult.ResultRows` and `VendorCatalogSyncResult.Diagnostics`.
- Added a UI command path with provider selection, part/keyword entry, a `Run API Sync` command, running/status state, and result replacement after the sync service returns.
- The default app service is environment-backed and can call Digi-Key or Mouser when the required credentials are configured.
- Digi-Key OAuth tokens are cached in memory and refreshed when they are inside the configured expiry skew, so normal searches do not request a new OAuth token every time.
- Credential loading now checks process environment first and then user-scoped environment variables, matching the local setup flow for Digi-Key credentials.
- Digi-Key and Mouser search calls now retry transient provider responses such as rate limits and service outages with bounded backoff.
- The Marketplace panel now automatically builds an in-use vendor refresh queue from sourced schematic parts and offers a `Run In-Use Sync` command that fans out to Digi-Key and Mouser for the placed parts' manufacturer part numbers.
- In-use vendor sync now records fresh sync state per component/provider/query and skips repeated requests inside the freshness window, while showing the last sync status in the queue.
- In-use vendor sync state is now saved as deterministic local JSON under the app artifact path so refreshed parts remain fresh across app sessions.
- A separate `Force Refresh` path lets the user intentionally re-query all runnable in-use Digi-Key/Mouser requests even when the current state is still fresh.
- In-use vendor sync now uses provider-specific freshness windows instead of one fixed TTL, currently Digi-Key 12 hours and Mouser 24 hours.
- Freshness windows are editable in the Marketplace panel and saved as deterministic local JSON under the app artifact path.
- The Marketplace panel includes a reset action that restores Digi-Key/Mouser freshness windows to the default policy and persists that reset.
- Freshness-hour edits now show local validation feedback and reject non-positive or non-numeric values without saving them.
- The Marketplace panel includes a `Clear Sync State` action that wipes persisted in-use vendor freshness state, returning placed parts to `Never synced` without changing freshness-window settings.
- Component marketplace deduplication now groups provider listings into canonical candidates using normalized manufacturer part numbers, aliases, manufacturer/package/value signals, and merge warnings for disagreements.
- BOM cost rollup now combines component quantities with normalized vendor listings, selects deterministic provider offers and price breaks, totals estimated cost, and emits missing-source diagnostics.
- Trusted-library vendor match promotion now produces deterministic non-mutating promotion plans/records for reviewed provider matches before any core library write path exists.
- Vendor live-smoke coverage is opt-in behind `DRAGONCAD_VENDOR_LIVE_SMOKE`; the default test path stays offline and does not create HTTP clients.

Verification evidence:
- `dotnet build src\DragonCAD.Sourcing\DragonCAD.Sourcing.csproj -v:minimal` passed.
- `dotnet test tests\DragonCAD.Sourcing.Tests\DragonCAD.Sourcing.Tests.csproj --filter "FullyQualifiedName~AdafruitCatalogFixtureParserTests|FullyQualifiedName~JamecoCsvManualFeedParserTests|FullyQualifiedName~ProviderCredentialPlannerTests|FullyQualifiedName~SparkFunSourceManifestTests" --logger "console;verbosity=minimal"` passed 19 tests.
- Full solution verification passed with `dotnet build DragonCAD.slnx --no-restore -v:minimal` and `dotnet test DragonCAD.slnx --no-build --logger "console;verbosity=minimal"`.
- Credentialed API client focused tests passed with 23 tests.
- Full solution verification after the client implementation passed with `dotnet build DragonCAD.slnx --no-restore -v:minimal` and `dotnet test DragonCAD.slnx --no-build --logger "console;verbosity=minimal"`.
- Sync runner and app result binding focused tests passed with 5 tests.
- Marketplace API sync command focused tests passed with 3 tests.
- Digi-Key OAuth token cache focused tests passed with 3 tests.
- Credential fallback, provider retry, full bundled-library preload, in-use vendor sync, fresh-state skip, persisted sync-state, force-refresh, provider-specific freshness, editable freshness-policy, reset-defaults, validation, clear-state, BOM rollup, deduplication, trusted-library promotion, partnership document, fabrication ordering, and opt-in live-smoke focused tests passed.

## Next Integration Steps

1. Add app UI surfaces for BOM cost rollups, canonical deduplication candidates, and trusted-library promotion queues.
2. Add a manual, opt-in live-account smoke command that sets `DRAGONCAD_VENDOR_LIVE_SMOKE=1` only for local verification.
3. Promote reviewed vendor matches into a staged DragonCAD library candidate, still gated by audit records.
