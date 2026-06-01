# DragonCAD Vendor And Fabrication Partnership Outreach Playbook

Verified date: 2026-06-01

Purpose: identify the right public channels, partner-program entry points, and target roles for starting DragonCAD partnership conversations with component vendors, maker suppliers, and PCB fabrication providers.

This document intentionally avoids naming random individual employees. Start with official developer, sales, support, partner, distributor, or business-development channels, then ask those teams to route DragonCAD to the right product/API/partnership owner.

## Partnership Goals

DragonCAD should ask each organization for:

- Authorized catalog ingestion route: official API, feed, CSV, affiliate catalog, marketplace feed, or allowed scraping policy.
- Datasheet rights: permission to deep-link, cache metadata, or cache PDFs where allowed.
- Pricing and stock refresh terms: rate limits, quote freshness, attribution, and cache duration.
- Product identity rules: MPN, SKU, package, lifecycle, substitution, and do-not-substitute metadata.
- Order handoff terms: cart handoff, quote export, affiliate links, or future in-app checkout.
- Branding rules: logo usage, attribution text, trademark terms, and partner listing approval.
- Test/sandbox access: non-production keys, fixtures, mock catalogs, or development accounts.

## Priority Contact Matrix

| Organization | Best first channel | Target internal owner | Why this matters to DragonCAD | Initial ask |
| --- | --- | --- | --- | --- |
| Digi-Key | Developer/API support and Marketplace/sales contact routes | API partnerships, eProcurement, marketplace integrations, developer relations | Large catalog, pricing, stock, datasheets, BOM sourcing, cart/order handoff | API access, data-use terms, BOM/cart handoff, datasheet metadata rights |
| Mouser | Search API / web service support and sales/contact route | API/web services, eProcurement integrations, digital commerce partnerships | Large catalog, pricing, stock, datasheets, BOM sourcing | API access, data-use terms, BOM/cart handoff, sandbox/test keys |
| Jameco | Official contact/sales/support route | Sales operations, web commerce, catalog/data owner | Useful maker/prototype catalog and pricing data | Ask whether API/feed exists; if not, request approved ingestion method and rate limits |
| SparkFun | Official contact/support and business inquiry route | Business development, wholesale, catalog/content owner, open-source hardware team | Strong open hardware library/catalog source, education/maker audience | Catalog/feed permission, Eagle/KiCad library use, datasheet/deep-linking permission |
| Adafruit | Official support/contact and distributor/wholesale route | Wholesale/distribution, catalog/content owner, open-source hardware team | Strong maker catalog, libraries, guides, board docs | Catalog/feed permission, datasheet/guide deep links, distributor/affiliate terms |
| OSH Park | Official support/contact route | Founder/operator, support, order workflow/API owner | Prototype board ordering and fabrication package handoff | Handoff workflow, upload/API options, design-rule package requirements, branding approval |
| PCBCart | Official contact/sales/quote route | Sales, production quote workflow owner, API/e-commerce owner | Production boards and assembly handoff | Quote/package handoff, production/BOM/PNP requirements, upload/API options |

## Public Contact Targets

Use these as first-pass outreach targets. These are public official or organization-published contacts found during research on 2026-06-01. Do not use guessed address patterns for individual employees.

| Organization | Contact name / role | Email or route | Source confidence | Best use |
| --- | --- | --- | --- | --- |
| Digi-Key | Digi-Key API Team | `api.contact@digikey.com` | Official API agreement / developer support | Request approval for API data display, enhanced feed/API access, API partnership routing |
| Digi-Key | Customer support / general order support | `orders@digikey.com` | Official Digi-Key site footer/support | Backup route; ask to route to API partnerships or digital solutions |
| Digi-Key | Media inquiries team | Digi-Key Newsroom media inquiry form | Official newsroom | Backup route for corporate partnership routing if API channel stalls |
| Mouser | Kelly DeGarmo, Corporate Relations and Events | `MediaRelationsHQ@mouser.com` | Official Mouser corporate/media relations page | Corporate partnership routing, ask for API/eProcurement/digital commerce owner |
| Mouser | Mouser sales/customer service | `sales@mouser.com` | Official Mouser contact page | API/business integration routing and sales-side partnership request |
| Mouser | Mouser Search API team / API request path | Search API request form / My Mouser API key flow | Official Mouser Search API page | Get Search API access for catalog, stock, pricing, datasheet URLs |
| Mouser | Ceres Wang, APAC Media Relations | `MediaRelationsAPAC@mouser.com` | Official Mouser corporate/media relations page | APAC routing only if regional contact is relevant |
| SparkFun | Sales team | `sales@sparkfun.com` | Official SparkFun contact page | Best first route for catalog/feed/default-library partnership |
| SparkFun | Customer support | `support@sparkfun.com` | Official SparkFun contact page | Backup route; ask to route to business development/catalog owner |
| SparkFun | Education team | `education@sparkfun.com` | Official SparkFun Learn contact page | Education/workshop angle only, not primary vendor integration |
| SparkFun | Accounting/W9 team | `AR@sparkfun.com` | Official SparkFun contact page | Not for partnership; only if procurement paperwork is needed |
| Adafruit | Customer support team | `support@adafruit.com` | Official Adafruit support/community docs | Best email route when no partnership email is listed; ask to route to distributor/catalog/content owner |
| Adafruit | Distributor/reseller application team | Distributor signup form | Official Adafruit distributor page | Use if positioning DragonCAD as channel/marketplace/distributor-style partner |
| Adafruit | Limor Fried, Founder / CEO | Public named company leader; use support/distributor route for routing, do not guess a direct email | Official Adafruit press page names Limor; no direct email listed | Mention as executive owner only if asking support to route internally |
| OSH Park | Support team | `support@oshpark.com` | Official OSH Park docs | Best route for upload/API/fabrication handoff discussion |
| OSH Park | Drew Fustini | `drew@oshpark.com` | Public OSH Park presentation PDF, older/community-facing source | Secondary/low-confidence route; prefer support first and ask for API/integration owner |
| PCBCart | Sales team | `sales@pcbcart.com` | Public directory listings and PCBCart-related materials | Best first route for production quote/package handoff |
| PCBCart | Shen Yi, General Manager | `sales@generalcircuits.com` | Public Assembly directory listing | Secondary named route for management escalation; verify before use |

### Recommended First Emails By Organization

Use these as the `To:` line for the first outreach:

- Digi-Key: `api.contact@digikey.com`
- Mouser: `sales@mouser.com`; CC `MediaRelationsHQ@mouser.com` only if asking for corporate routing.
- SparkFun: `sales@sparkfun.com`
- Adafruit: `support@adafruit.com` plus distributor signup form if partnership resembles a reseller/channel relationship.
- Jameco: `Sales@Jameco.com` from Jameco catalog/order materials, or use the official contact page to route to management/catalog data.
- OSH Park: `support@oshpark.com`
- PCBCart: `sales@pcbcart.com`

### Who To Ask For

When writing to generic inboxes, ask to be routed to:

- Developer platform / API partnerships owner.
- Catalog data licensing owner.
- eProcurement or BOM integration owner.
- Business development or channel partnership owner.
- Legal/brand approvals for data, logos, attribution, and cached datasheet metadata.
- For fabrication providers: upload/API owner or production quote workflow owner.

## Organization Notes

### Digi-Key

Official routes to start:

- Digi-Key developer/API portal and support.
- Digi-Key Marketplace/supplier or sales contact channels.
- General contact/support if API support does not route partnership requests.

Target roles to ask for:

- API partnerships or developer platform owner.
- eProcurement/BOM integration owner.
- Marketplace/catalog data partnership owner.
- Legal/commercial contact for data-use and branding terms.

DragonCAD-specific talking points:

- DragonCAD wants review-first catalog and BOM sourcing inside a desktop ECAD app.
- DragonCAD will not place live orders without explicit user confirmation and approved integration terms.
- Ask for official MPN/SKU/package/lifecycle/stock/pricing/datasheet metadata access.
- Ask whether cart handoff, saved BOM, or quote handoff is preferred over in-app checkout.

### Mouser

Official routes to start:

- Mouser Search API / web service channel.
- Mouser sales/contact support route.

Target roles to ask for:

- API/web services owner.
- eProcurement or BOM integration owner.
- Digital commerce partnership owner.
- Catalog data and branding/legal reviewer.

DragonCAD-specific talking points:

- DragonCAD wants BOM line matching, pricing/stock snapshots, datasheet links, and quote/order handoff.
- Ask for rate limits, attribution requirements, cache rules, and sandbox keys.
- Ask if they prefer API-only access and whether scraping is prohibited.

### Jameco

Official routes to start:

- Jameco official contact/support/sales page.
- If no API program is listed, ask sales/support to route to the web commerce/catalog data owner.

Target roles to ask for:

- Sales operations manager.
- E-commerce/catalog data owner.
- Business development or partnerships contact.

DragonCAD-specific talking points:

- Ask whether Jameco has a product catalog API, CSV feed, affiliate feed, or approved integration method.
- If no API/feed exists, ask for written permission and rules before any scraper/provider is built.
- Ask for SKU, MPN, package, datasheet URL, stock, price break, and lifecycle fields.

### SparkFun

Official routes to start:

- SparkFun contact/support route.
- Wholesale/reseller/business inquiry route if available.
- Public GitHub/library repositories for technical reference, but not as the commercial permission channel.

Target roles to ask for:

- Business development/wholesale contact.
- Catalog/content owner.
- Open-source hardware library maintainer or engineering content owner.

DragonCAD-specific talking points:

- DragonCAD wants to include SparkFun components as default searchable starter content.
- Ask for approved use of SparkFun Eagle/KiCad libraries, product metadata, images, and datasheet/guide links.
- Ask whether a public catalog feed or preferred affiliate/source route exists.
- Clarify whether embedded offline component assets can ship with DragonCAD installers.

### Adafruit

Official routes to start:

- Adafruit support/contact route.
- Distributor/wholesale route if partnership is commercial.
- Public GitHub/library repositories for technical reference, but not as the commercial permission channel.

Target roles to ask for:

- Wholesale/distributor contact.
- Catalog/content owner.
- Open-source hardware library maintainer or engineering content owner.

DragonCAD-specific talking points:

- DragonCAD wants Adafruit components discoverable in a component marketplace/library surface.
- Ask for catalog/feed terms, permitted use of libraries, datasheet/guide links, images, and attribution.
- Ask whether DragonCAD should deep-link to product pages instead of caching product media.

### OSH Park

Official routes to start:

- OSH Park support/contact route.
- Ask support to route to whoever owns upload/order workflow or API/business integrations.

Target roles to ask for:

- Fabrication workflow/API owner.
- Support/operations owner.
- Business development/contact for app integrations.

DragonCAD-specific talking points:

- DragonCAD wants a prototype-board handoff workflow from reviewed Gerber/drill/outline artifacts.
- Ask whether upload API, cart handoff, or manual handoff is preferred.
- Ask for package requirements, accepted file formats, board-size/layer constraints, and branding/link rules.

### PCBCart

Official routes to start:

- PCBCart contact/sales/quote route.
- Ask sales to route to API/e-commerce or production quote workflow owner.

Target roles to ask for:

- Sales/quote workflow owner.
- Production engineering/package intake owner.
- API/e-commerce owner if one exists.

DragonCAD-specific talking points:

- DragonCAD wants production board and assembly package review before external handoff.
- Ask for BOM, pick-and-place, stackup, fabrication notes, drill/Gerber requirements, and quote handoff method.
- Ask whether they support upload/API integration or prefer generated quote packages/manual upload.

## Scraping Fallback Policy To Discuss

For any organization without an API or feed, ask explicitly:

1. Do you allow product-page ingestion by automated tools?
2. What rate limits and user-agent/contact identification do you require?
3. Can DragonCAD cache product metadata, prices, stock, images, datasheet URLs, or PDFs?
4. What attribution and linking text is required?
5. Are there endpoints, feeds, or affiliate programs preferred over scraping?
6. Are there restricted pages, robots.txt rules, or terms we must not cross?

DragonCAD should not bypass authentication, CAPTCHA, anti-bot systems, paywalls, or terms restrictions.

## If They Do Not Have An API

Use this request when an organization says there is no public API, feed, or formal developer program.

### Request

DragonCAD would like written approval for a controlled catalog ingestion path that does not bypass access controls and does not create unreasonable traffic.

We can support one of these options, in preferred order:

1. A periodic CSV/JSON/XML catalog export provided by your team.
2. A static product feed or affiliate feed.
3. A limited endpoint specifically for product metadata, pricing snapshots, stock snapshots, and datasheet links.
4. A documented permission to crawl specific public product pages under agreed limits.

If page crawling is the only available option, DragonCAD requests written guidance for:

- Allowed URL patterns.
- Disallowed URL patterns.
- Maximum request rate.
- Time-of-day restrictions, if any.
- Required user-agent string.
- Required contact email in the user agent or request header.
- Cache duration for product pages, stock, pricing, images, and datasheet links.
- Whether prices and stock can be displayed as snapshots.
- Whether product images can be cached, hot-linked, or must be avoided.
- Whether datasheet PDFs may be cached, or only linked.
- Required attribution and product-page links.
- Any affiliate or referral tagging rules.
- Any required opt-out or disable mechanism.

### Proposed DragonCAD Scraper User Agent

`DragonCAD-CatalogIngest/0.1 (+https://dragoncad.example; contact: partnerships@dragoncad.example)`

Replace the URL and email before sending.

### Proposed Technical Restrictions

DragonCAD can commit to:

- User-triggered fetches only; no hidden background crawling by default.
- Provider disabled by default until the user accepts provider terms.
- Per-provider rate limits.
- Local cache with configurable retention.
- Respect for `robots.txt` and written vendor restrictions.
- No CAPTCHA bypass, login scraping, paywall bypass, or anti-bot circumvention.
- No live ordering through scraped pages.
- Clear source/provenance on every imported catalog candidate.
- Human review before any scraped row can link to or promote a trusted component.

### No-API Email Addendum

If you do not currently offer an API or catalog feed, would you be open to approving one of the following integration paths for DragonCAD?

- A periodic CSV/JSON/XML catalog export.
- A static product feed or affiliate feed.
- A limited metadata endpoint.
- Written permission to crawl specific public product pages under agreed rate limits and cache rules.

DragonCAD can identify itself with a dedicated user agent, respect `robots.txt`, cache aggressively, avoid login/CAPTCHA/protected pages, and keep all imported rows as review-only catalog candidates. We would not place orders or represent scraped data as verified components without explicit user review.

Could you provide the allowed URL patterns, rate limits, cache rules, attribution requirements, and any prohibited use cases?

## If They Do Have An API

Use this path when an organization has a public API, partner API, feed, eProcurement interface, quote API, or developer program.

### Secure API Usage Request

DragonCAD would like to use the official API/feed in a way that protects user credentials, respects rate limits, and keeps supplier data clearly attributed.

Please provide or confirm:

- API documentation and versioning policy.
- Sandbox/test environment.
- Authentication mechanism.
- Whether OAuth, API keys, signed requests, or partner tokens are required.
- Required redirect URIs if OAuth is used.
- Scopes/permissions needed for catalog search, pricing, stock, datasheets, carts, quotes, or orders.
- Rate limits and burst limits.
- Cache policy for catalog, pricing, stock, images, and datasheets.
- Required attribution and product-page links.
- Error handling and retry guidance.
- Webhook support, if any.
- Data retention restrictions.
- Production approval process.
- Security review or app registration process.

### DragonCAD Security Position

DragonCAD should implement official APIs with these constraints:

- Credentials are never stored in project files.
- API keys/tokens are stored only in the OS secure credential store when available.
- Secrets are redacted from logs, diagnostics, screenshots, project exports, and support bundles.
- Provider credentials are scoped per local user profile, not globally embedded in a design.
- API calls are user-triggered or explicitly scheduled by the user.
- All live-order, quote, cart, upload, or checkout actions require a confirmation screen.
- Catalog/pricing/stock data records source, retrieval timestamp, provider, API version, and cache expiry.
- Tests use fixtures or sandbox responses, never production secrets.
- Provider integrations fail closed when credentials are missing, expired, or insufficiently scoped.
- Users can revoke provider credentials from DragonCAD.

### Preferred Credential Models

Preferred order:

1. OAuth 2.0 with least-privilege scopes and refresh-token revocation.
2. Partner-issued API key stored in OS credential vault.
3. User-provided API key stored in OS credential vault.
4. Manually imported offline feed with no secret storage.

Avoid:

- Hardcoded shared keys.
- Project-file secrets.
- Secrets in environment variables as the default desktop-app path.
- Scraping authenticated user sessions.
- Browser automation that depends on personal cookies.

### API Integration Email Addendum

If an official API or feed is available, DragonCAD would like to integrate through that path rather than scraping public pages.

Could you provide the current API documentation, sandbox access, authentication requirements, rate limits, allowed scopes, cache rules, attribution requirements, and production approval process?

DragonCAD will store credentials outside project files using the local OS credential store where available, redact secrets from logs, keep pricing/stock as timestamped snapshots, and require explicit user confirmation before any cart, quote, upload, checkout, or order action.

## Initial Outreach Email

Subject: DragonCAD component library and sourcing integration partnership

Hello,

I am building DragonCAD, a local-first hardware IDE for schematic capture, PCB layout, component library management, BOM planning, and fabrication handoff.

We would like to include your organization as an approved component/catalog or fabrication partner inside DragonCAD. The goal is to help engineers find verified parts, review datasheets, plan BOM cost and availability, and hand off orders or fabrication packages through approved workflows.

Could you route me to the person or team responsible for developer APIs, catalog data partnerships, eProcurement/BOM integrations, or app partnerships?

The initial topics are:

- Official catalog/API/feed access, if available.
- Terms for pricing, stock, datasheet, image, and product metadata use.
- Preferred handoff model for carts, quotes, BOMs, or fabrication packages.
- Branding and attribution requirements.
- Sandbox or test access for development.
- Whether automated product-page ingestion is permitted if no API/feed exists.

DragonCAD will keep imported/generated component data review-first and will not place live orders or mutate trusted component libraries without explicit user review and approved integration terms.

Thank you,

[Name]
[Company/Project]
[Email]
[Website/GitHub]

## LinkedIn / Short Form Message

Hello, I am building DragonCAD, a local-first hardware IDE for ECAD, component libraries, BOM planning, and fabrication handoff. I am looking for the right person to discuss API/catalog or fabrication partnership integration with [Organization]. Could you point me to the developer platform, catalog data, eProcurement, or business development owner?

## Discovery Call Agenda

1. DragonCAD overview and user workflow.
2. What data/workflows the organization wants exposed inside third-party engineering tools.
3. Official API/feed availability and rate limits.
4. Catalog metadata, datasheet, image, pricing, stock, lifecycle, and SKU permissions.
5. Branding and attribution rules.
6. Commercial model: free API, affiliate, reseller, marketplace, quote handoff, or paid partnership.
7. Sandbox/test data.
8. Security and privacy expectations.
9. Written approval path and next technical contact.

## Internal Follow-Up Checklist

- Record official contact route used.
- Record contact person/team once routed.
- Save terms/API docs links.
- Save permission notes and restrictions.
- Create provider story only after terms are understood.
- Keep provider disabled by default until credentials/config and tests exist.
- Add fixture tests before any live fetch.
- Never mark scraped/catalog-only rows as verified components.
