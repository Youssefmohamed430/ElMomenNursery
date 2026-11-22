# ADR 0004 — نطاق الحذف الناعم وإدارة الفصول غير المعتمدة

- Status: Accepted, updated 2026-11-22
- Date: 2026-11-17

## Context

The execution plan requires soft deletion for approved records, but asks the nursery owner to decide whether financial records should use `IsDeleted` alongside their void/status workflows. Classroom services and the Classes page also contained placeholders and sample rows despite the instruction not to invent class fields.

## Decisions

1. Payments, expenses, and invoices keep their existing void/status workflows. They will not receive `IsDeleted` unless the owner makes a later decision.
2. Owner approval on 2026-11-22 enables classroom management with only a classroom name and its assigned teacher. Staff can assign children from the Classes screen. Child enrollment in a classroom remains optional and can be set or changed in the child editor. Existing classroom, teacher-assignment, and child-enrollment entities are used; no extra classroom fields are introduced.
3. Existing Manager and Accountant role mappings remain unchanged pending a separate owner decision about `Payments.Create` for Manager and `Subscriptions.Manage` for Accountant.
4. Generated receipt files remain in the local application data directory; no automatic cleanup is performed, preserving reprint availability.

## Consequences

- The Classes entry manages classrooms, current teachers, and child assignments using the approved workflow.
- Payment, expense, and invoice history continues to be controlled by existing status/void workflows.
- The later soft-delete migration must exclude those financial entities unless the owner revises this decision.
