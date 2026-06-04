# Marketplace-Safe Placement

Marketplace-safe placement keeps sourced parts from entering a project without provenance and lifecycle review. The goal is to use vendor data as a local design aid while preserving a trusted component path.

## Safe placement checks

- Search and inspect vendor rows in the marketplace before adding parts to the cart.
- Confirm the selected component has usable manufacturer, manufacturer part number, lifecycle, and availability details.
- Use `PrepareMarketplaceBomCsvCommand` for local BOM review before purchasing.
- Use `CreateMarketplaceOrderDraftCommand` only after the cart and project identity are reviewed.
- Prefer trusted-library promotion before repeated schematic placement.

| Risk | Local check |
| --- | --- |
| Wrong package | Compare the marketplace package against the local component footprint. |
| Weak provenance | Keep source, vendor, and part-number fields visible in review. |
| Untrusted reuse | Promote reviewed parts through the trusted-library workflow before reuse. |

Return to [Schematic placement](../schematic-editing/placing-symbols.md) when the component is ready to place.
