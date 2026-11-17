# ADR 0003 — Subscription payments and receipts

- Status: Implemented with owner confirmations pending
- Date: 2026-11-17

## Context

Subscriptions and payments need a stable local receipt identifier, a readable Arabic PDF, an explicit policy for overlapping periods, and a way to preserve audit history. The application remains offline and uses one SQLite database per workstation.

## Decisions

1. **Receipt numbering — assumption for owner review:** `R-{yyyy}-{000001}`. The per-year sequence lives in `AppSettings` under `Receipts.Sequence.{year}` and increments inside the same database transaction that records payment and allocation. The database enforces uniqueness on `Payments.ReceiptNumber`.
2. **PDF renderer and font:** QuestPDF provides RTL/bidirectional layout support. The application bundles Noto Naskh Arabic under SIL Open Font License 1.1. QuestPDF Community is selected in code; production use depends on the nursery meeting its current eligibility criteria (including the annual revenue limit and exclusion categories). Confirm eligibility before operational deployment.
3. **Overlapping periods:** default behavior is a warning returned by the service. The Arabic editor asks the user whether to continue; confirmation allows the save. Overlap is not a database constraint.
4. **Cancellation and deletion:** cancellation changes status and records the reason/audit. Deletion is permitted only when the subscription has no payment or allocation records; paid history is preserved.
5. **Local receipt identity:** receipt content uses the supplied logo and nursery name. Optional address/phone values are read from `AppSettings` keys `Nursery.Address` and `Nursery.Phone` and omitted when absent. The accepted configuration key names and wording remain owner-reviewable.
6. **Initial payment:** initial payment, allocation, subscription, receipt sequence, and audit entry are persisted together. `Payments.Create` is required in addition to `Subscriptions.Manage` when an initial payment is included.

## Open owner decisions

- Confirm the receipt-number pattern and the annual sequence reset.
- Confirm QuestPDF Community eligibility based on the nursery's legal form and revenue; choose a paid tier if ineligible.
- Confirm whether accountants should receive `Subscriptions.Manage`. Current role seed gives accountants payment creation/view and subscription viewing, but does not grant subscription management.
- Confirm whether managers should receive `Payments.Create`. Managers can manage subscriptions, but do not currently have permission to record the optional initial payment. The editor now hides those fields when the permission is absent.
- Confirm whether a payment amount should also be printed in words.
- Confirm overlap policy; the current implementation warns and asks before continuing.
- Confirm naming and values for optional nursery identity settings.
