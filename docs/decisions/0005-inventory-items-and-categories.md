# ADR 0005 — Inventory items and categories

- Status: Inventory item and category CRUD, including a manually maintained current quantity, is implemented; stock movements remain undecided
- Date: 2026-11-19

## Context

The existing inventory entities contain item name, category, unit price and active state, plus category name, description and active state. StockMovement exists, but its use and issue policies have not been approved. Soft delete is already provided by the shared interceptor.

## Decisions

1. Inventory items have a non-negative integer `Quantity` interpreted as a manually maintained current count in pieces. Existing rows start at zero. No SKU, separate unit field, thresholds, barcode, stock issue, or movement workflow is included.
2. Use typed asynchronous service results, permission guards, Arabic-normalized duplicate checks, tracked full-replacement updates, and soft delete/restore. Record create/update/status/restore audit rows in the same unit of work, including before/after price and quantity; the soft-delete interceptor owns delete audit rows.
3. Block category deletion while any non-deleted item references it. Deactivate such a category instead.
4. Grant Inventory.Export to Admin and Manager and Inventory.Restore to Admin through idempotent seed V6, adding only missing mappings.
5. EF warning 10622 reports required filtered navigations from InventoryItem to InvoiceLine and StockMovement (and other entity pairs). Inventory screens and services must query from InventoryItem/InventoryCategory roots. Do not load InventoryItem through StockMovement/InvoiceLine until relationship/filter semantics are decided; a required filtered related entity can cause its parent row to disappear from query results. Do not globally suppress the warning or change FK nullability without a migration decision.

## Stock movement questions for the owner

- Is quantity tracked at all, and what units are valid?
- Are movement quantities signed, or positive with direction carried by type?
- Is negative stock allowed?
- What is the correction policy and is a reason mandatory?
- Which recipient and purpose fields are needed when issuing items?
- Are low-stock thresholds needed? This would require a new field and migration.
- Is an issue-document PDF required?

The current count is not derived from StockMovement. If a movement workflow is later approved, decide how it becomes the source of truth and how it relates to this manually maintained count. A possible traceable design is to derive on-hand quantity from movements and correct mistakes with new Adjustment movements rather than overwriting history.

## Consequences

Price and quantity changes are captured in before/after audit summaries. Migration `AddInventoryItemQuantity` adds the quantity column with default zero and a non-negative database constraint. The WinForms screen displays and edits the current count; visual acceptance checks remain outstanding.
