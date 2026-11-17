# ADR 0004 — نطاق الحذف الناعم وإدارة الفصول غير المعتمدة

- Status: Accepted
- Date: 2026-11-17

## Context

The execution plan requires soft deletion for approved records, but asks the nursery owner to decide whether financial records should use `IsDeleted` alongside their void/status workflows. Classroom services and the Classes page also contained placeholders and sample rows despite the instruction not to invent class fields.

## Decisions

1. Payments, expenses, and invoices keep their existing void/status workflows. They will not receive `IsDeleted` unless the owner makes a later decision.
2. Classroom management remains unavailable until the owner approves classroom fields and rules. No sample classroom rows or speculative DTO fields are presented.
3. Existing Manager and Accountant role mappings remain unchanged pending a separate owner decision about `Payments.Create` for Manager and `Subscriptions.Manage` for Accountant.
4. Generated receipt files remain in the local application data directory; no automatic cleanup is performed, preserving reprint availability.

## Consequences

- The Classes entry displays an Arabic explanation rather than sample data.
- Payment, expense, and invoice history continues to be controlled by existing status/void workflows.
- The later soft-delete migration must exclude those financial entities unless the owner revises this decision.
