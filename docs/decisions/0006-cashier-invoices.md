# Decision 0006: Cashier invoices

**Status:** Confirmed by nursery owner, 2026-11-19

The subscriptions and payments screen also provides cashier sales. Inventory management remains available in its existing section.

- A cashier invoice can contain multiple active stock items and manually described items that are not in the inventory catalog.
- Stock item names and prices are saved as invoice-line snapshots. Manual lines have no inventory foreign key and do not change stock.
- Posting a stock-item line reduces available quantity and adds a sale-out stock movement in the same transaction as the posted invoice.
- Issuing an invoice records a full immediate payment method (cash, bank transfer, or other); partial payment and unpaid invoices are not part of this workflow yet.
- Customer name is required; payment reference is optional. Invoice number is unique and sequential by year.
- The invoice can be opened as a PDF after it is saved.
