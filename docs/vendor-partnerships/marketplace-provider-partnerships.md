# Marketplace Provider Partnership Artifact

Status: product planning artifact

Owner: DragonCAD Marketplace and Sourcing

Purpose: define the approved partnership, API, and contact approach for provider integrations used by DragonCAD's component marketplace, sourcing, and fabrication flows.

## Provider Partnership Matrix

| Provider | Integration Role | Partnership / API / Contact Approach | Official URL | Credential Model | Initial Product Decision |
| --- | --- | --- | --- | --- | --- |
| Digi-Key | Authorized distributor catalog, price, availability, quoting, and ordering candidate | Register and manage applications through the Digi-Key developer portal. Use the portal support path for API onboarding or production-access questions. | https://developer.digikey.com and https://developer.digikey.com/support | OAuth 2.0 client credentials and tokens. Store client ID, client secret, access tokens, and refresh tokens only in the DragonCAD secret store. | Preferred credentialed API provider for catalog search and future quote/order workflows. |
| Mouser | Authorized distributor catalog, price, availability, cart, and ordering candidate | Use Mouser's API hub for Search API onboarding and contact Mouser through its API pages for enablement questions. | https://www.mouser.com/api-hub and https://www.mouser.com/api-search | API key. Store keys only in the DragonCAD secret store and redact them from logs, exports, telemetry, and screenshots. | Preferred credentialed API provider for catalog search and future cart workflows. |
| SparkFun | Maker-marketplace product metadata, open-hardware collateral, and educational component references | Prefer documented SparkFun product documentation and public repositories. Contact SparkFun through official site channels before using any automated commercial feed not already documented for public reuse. | https://docs.sparkfun.com and https://www.sparkfun.com/contact_us | No shared secret assumed for public documentation references. Add credentials only if SparkFun provides a partner API agreement. | Public documentation and repository sync only until a formal partner feed is approved. |
| Adafruit | Maker-marketplace product metadata, guides, and Adafruit IO-compatible ecosystem references | Use Adafruit IO API documentation only for IO-related integrations. For store, guide, or product-catalog partnership questions, use official Adafruit support/contact paths. | https://io.adafruit.com/api/docs and https://www.adafruit.com/support | Adafruit IO key or token where applicable. Treat it as a user or workspace secret and never commit it. | Use public product links and guide metadata unless Adafruit approves a catalog partnership. |
| Jameco | Distributor catalog coverage and manual sourcing fallback | No first-party public catalog API is assumed. Use Jameco's official contact page for partnership, feed, or data-license requests before automation beyond manual catalog entry. | https://www.jameco.com/contact | No application credential assumed. If a feed is approved, store access credentials as partner secrets and record license scope. | Manual catalog feed fallback until Jameco approves a structured data feed. |
| OSH Park | PCB fabrication quote/order handoff candidate | Use the OSH Park developer documentation for uploads, projects, and orders. Confirm production integration terms before enabling order placement. | https://oshpark.com/developer | Short-lived session token. Store active tokens only in the secret store or encrypted runtime cache and expire them aggressively. | API-backed fabrication handoff candidate after sandbox validation. |
| PCB Cart | PCB fabrication and assembly quote/order handoff candidate | No first-party public API is assumed. Use PCBCart's official support and FAQ paths for quote, file, assembly, and potential integration discussions. | https://www.pcbcart.com/support/faq and https://www.pcbcart.com/contact-us | No application credential assumed. If PCBCart approves account, upload, or quote automation, store account/API credentials as partner secrets. | Manual or semi-assisted quote package export until PCBCart grants integration terms. |

## Security Requirements

- Use HTTPS-only endpoints and validate TLS certificates for every provider call.
- Keep partner credentials out of source control, markdown examples, test fixtures, build artifacts, screenshots, crash dumps, and telemetry payloads.
- Store OAuth clients, API keys, session tokens, and partner feed credentials in the DragonCAD secret store with per-workspace scoping.
- Redact authorization headers, query-string API keys, cookies, refresh tokens, and session identifiers before logging.
- Give each provider integration a named credential reference, rotation date, owner, allowed scopes, and revocation procedure.
- Limit provider data retention to the approved product use case. Preserve provenance, source URL, retrieval time, and license/terms notes with imported records.
- Gate live ordering, quote submission, file upload, and cart mutation behind explicit user confirmation and an audit record.

## OAuth And API Key Handling

- Digi-Key: use the official developer portal flow. Keep client secrets server-side or in the local encrypted credential vault. Refresh tokens must be encrypted at rest and never shown in UI diagnostics.
- Mouser: pass API keys only through the approved request planner/runtime credential bag. Do not place keys in generated URLs displayed to users.
- Adafruit: treat Adafruit IO keys and tokens as user-owned secrets. Do not reuse an IO token for store or product-catalog workflows.
- OSH Park: treat session tokens as short-lived runtime material. Clear tokens on logout, provider disconnect, workspace close, or failed authorization refresh.
- SparkFun, Jameco, and PCB Cart: do not invent credentials or undocumented endpoints. Add credential handling only after an official agreement or documented developer program supplies the required mechanism.

## Scraping Policy Warning

DragonCAD must not scrape provider websites, bypass access controls, simulate user browsing at scale, or collect catalog data from pages where the provider has not granted permission for automated reuse. Use official APIs, documented public repositories, approved feeds, or explicit written partner terms. If a provider lacks a public API, keep the integration in manual export/import mode until partnership approval is documented.

## Product Follow-Up Checklist

- Confirm partner terms for displaying price, availability, images, product descriptions, and datasheet links inside DragonCAD.
- Record each provider's rate limits, attribution requirements, cache duration, and prohibited use cases before enabling scheduled sync.
- Add provider-specific kill switches so DragonCAD can disable a partner integration without removing local library data.
- Review every provider integration with security before enabling live credentials in production builds.
