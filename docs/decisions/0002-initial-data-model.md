# Data model implementation decisions — 2026-11-12

This log records implementation choices needed to begin the confirmed core model. It does not close the business decisions that remain TBD in `../DATA_MODEL_SPECIFICATION.md`.

## Decided for the initial persistence slice

- Use SQLite through EF Core 9.0.10, with schema migrations stored in Infrastructure. The app applies pending migrations at startup.
- Store the database at `%LOCALAPPDATA%\AlMoumenNursery\nursery.db` by default, outside program binaries. `LocalData:DirectoryPath` can override this for deployment; the selected directory is created on startup.
- Persist identifiers as canonical GUID strings; dates as ISO `yyyy-MM-dd` strings; timestamps as UTC Unix milliseconds; money as decimal with two fractional digits; enums as stable strings.
- Implement Child, Employee, UserAccount, Role, InventoryCategory, InventoryItem, SubscriptionPlan, Subscription, Classroom, ChildClassEnrollment, ClassTeacherAssignment and child FileAttachment.
- Keep `MaritalStatus` and subscription `Status` as strings pending owner-approved values. Preserve the confirmed ChildGender values as an enum persisted by string.
- Username uniqueness is enforced on an application-populated invariant uppercase `UsernameNormalized` column, using SQLite's unique index. Callers must set both username fields consistently until a dedicated account creation use case owns normalization.
- Employee national ID, role name, class name, plan name, category name, and optional employee-account link are unique. Foreign keys restrict deletion of referenced records.
- Store attachment metadata and app-managed relative path references in the database; file storage, allowed types/size, backup, and version policy remain to be decided. The child relationship is explicit and referentially valid.
- Enforce only unambiguous integrity rules in the database: child date of birth no later than enrollment, non-negative salary/fees/prices/attachment size, valid assignment and subscription date ranges, and exact single-role FK for each account.

## Still open

The owner must still decide field lengths/requiredness and normalization policy; marital status and job designation lists; authorization/permission representation and bootstrap administrator workflow; subscription month semantics and duplicate-per-period policy; assignment overlap rules and simultaneous class membership; account owner exception; attachment lifecycle; attendance, payments, inventory movements/sales, invoices, expenses/payroll, audit policy, backup process, device mapping, and regional locale/currency rules. Attendance and financial support entities are deliberately not introduced in this foundational slice because their event, allocation, correction, and status semantics remain unresolved.
