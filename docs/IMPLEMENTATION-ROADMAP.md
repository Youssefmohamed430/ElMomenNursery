# Implementation Roadmap — Al-Moumen Nursery Management System

This roadmap breaks the approved product scope into an ordered, end-to-end task plan. It is a planning document; unchecked items are not claims that implementation has happened.

**Product rule:** deliver comprehensive day-to-day functionality through an extremely simple Arabic RTL experience for non-technical nursery staff.

**Project baseline:** one Windows PC and one independent local database per branch; Windows Forms desktop UI with Domain, Application, and Infrastructure projects in one .NET solution; currently targeting .NET 9 to match the installed Visual Studio 2022 17.14 and .NET SDK 9.0.306. .NET 9 reached end of support on November 10, 2026. No central database/server, cross-branch synchronization, Reports page, or Branch Management page.

**Source of truth:** [Project specification](../SPECIFICATION.md), [AI agent instructions](../AGENT.md), and [solution architecture](ARCHITECTURE.md).

## How to use this roadmap

- Complete phases in order unless a task is explicitly independent.
- Treat child/employee fields, fee policy, and hardware models as owner decisions. Keep them marked **TBD** until confirmed; do not invent them to unblock unrelated work.
- Each phase has a completion gate. Do not start a dependent phase until its gate is met.
- Device adapters are optional extensions. Manual attendance, local data, and PDF printing must work without devices.
- Tests and acceptance checks are planned deliverables. This file does not mean they have already been run.

## Phase 0 — Confirm scope and operating decisions

- [ ] Review the specification with the nursery owner and resolve conflicting or missing requirements.
- [ ] Confirm the target Windows versions and whether the installed .NET runtime will be bundled or self-contained in the installer.
- [ ] Confirm the supported display size/resolution and the staff workstation usage pattern.
- [x] Copy the approved logo into `ElMomenNursery/Resources/Branding/AlMoumenNurseryLogo.jpeg` and preserve it as the identity reference.
- [ ] Sample logo colors and define a restrained accessible palette; verify contrast and grayscale printing.
- [ ] Obtain and record nursery-approved Arabic terms for the main navigation, payment states, attendance states, and invoice wording.
- [ ] Obtain the child and employee field lists from the nursery owner; do not implement placeholder business fields as final requirements.
- [ ] Confirm subscription/fee rules, payment methods, invoice numbering, correction/refund policy, and any required legal/tax wording.
- [ ] Confirm which local staff accounts/roles are needed and what each should be permitted to view or do.
- [ ] Keep barcode model/card issuance, biometric model/SDK, and receipt printer model/format listed as **TBD** until selected.

**Gate:** scope, user terminology, data ownership, and remaining TBD decisions are documented. Explicit exclusions remain intact.

## Phase 1 — Solution and developer foundation

- [x] Keep the existing desktop UI project and the Domain, Application, and Infrastructure projects in the same solution.
- [x] Confirm project reference direction: Desktop UI → Application; Desktop UI composition root → Infrastructure; Application → Domain; Infrastructure → Application/Domain; Domain has no project dependencies.
- [x] Add shared build conventions and document supported SDK/runtime versions. (The installed .NET SDK 9.0.306 matches the selected target.)
- [x] Add repository ignore rules for build output, IDE state, local database files, exports, backups, secrets, and user-specific settings.
- [x] Establish naming, formatting, nullable-reference, exception, logging, and configuration conventions.
- [x] Record initial architecture decisions and explicitly list unresolved database, migration, document-library, and installer choices.
- [x] Define a separate ignored `dev-data/` location for synthetic development data; real nursery records must never be used as sample data.
- [x] Define the location policy: user data stays outside program binaries; exact per-user vs machine-wide path remains open pending Windows account model.

**Gate:** project responsibilities and dependency rules are documented and configured. Current target is .NET 9, matching the installed SDK and Visual Studio 2022 17.14. .NET 9 is out of support; revisit the target when Visual Studio is upgraded. No nursery feature logic is embedded in the UI project.

## Phase 2 — UI shell and brand system

- [x] Replace the starter form with the application shell and a clear Arabic RTL navigation structure.
- [x] Add reusable page/section and form-field layouts, Arabic information/confirmation dialogs, and status/message patterns.
- [x] Add the real logo and a restrained palette sampled/derived from its green (`#80B040`), red (`#E00000`), and blue (`#0040E0`), with a darker green for readable primary actions.
- [x] Set Segoe UI as the Arabic-capable UI font and configure RTL layout, keyboard-accessible buttons, spacing, and readable control sizes.
- [ ] Verify Arabic shaping, mixed Arabic/Latin text, numerals, dates, and focus order on the supported workstation before closing the phase.
- [x] Add reusable loading, empty, validation-hint, success, warning, error, and device-unavailable message patterns in Arabic.
- [ ] Wire navigation visibility to the signed-in account's permissions in the authentication/authorization phase. The Admin-only settings entry is hidden from the shell until that work exists.
- [ ] Select and verify a consistent icon set with text labels after the supported Windows versions are confirmed; current navigation uses clear text labels to avoid unverified glyphs.
- [x] Ensure the home screen is a task launcher, not a reports/dashboard page.

**Gate:** needs visual review on the supported workstation for Arabic shaping, actual font/spacing, contrast, and keyboard flow. The target now matches SDK 9.0.306; the UI still needs to be launched and visually reviewed on the supported workstation.
## Phase 3 — Local persistence and application startup

- [x] Choose and document the local database engine and its Windows deployment/licensing requirements. SQLite with EF Core 9.0.10 is configured.
- [x] Define a versioned schema/migration strategy before domain data is added. EF Core migrations are stored in Infrastructure; the local `dotnet-ef` tool is pinned to 9.0.10.
- [x] Define the database path under an appropriate Windows application-data location, outside replaceable program binaries. The app and design-time factory use `%LOCALAPPDATA%\AlMoumenNursery\nursery.db`.
- [x] Implement Infrastructure persistence behind Application interfaces; keep SQL/provider details out of UI and Domain. Persistence and EF mappings are in Infrastructure.
- [ ] Add startup initialization, schema migration, safe transaction handling, and clear recovery messages. Startup migration is implemented; transaction handling and clear recovery UI remain incomplete.
- [x] Add constraints and stable identifiers as entities are introduced; plan indexes for common child search and list filtering. The implemented foundational model includes stable IDs, constraints, and initial indexes.
- [x] Ensure normal application operation has no internet dependency and creates no remote connection. SQLite is configured as a local file database.
- [ ] Define local logging that supports troubleshooting without logging unnecessary child, employee, credential, or payment details.

**Gate:** partially met. EF Core migrations were applied successfully to `%LOCALAPPDATA%\AlMoumenNursery\nursery.db`; the startup migration and error-recovery UX still need a full app-launch review.

## Phase 4 — Sign-in, accounts, roles, and authorization

- [ ] Define the initial administrator setup flow for first launch.
- [ ] Implement local account storage and safe password hashing/credential handling.
- [ ] Implement sign-in, sign-out, session/current-user context, and basic account lifecycle.
- [ ] Define granular permissions for page visibility and actions (view, add, edit, delete, attendance, payment, invoice, export, inventory, user/settings management).
- [ ] Build the Admin-only permissions page to assign what each account can see and do.
- [ ] Enforce permissions in Application use cases as well as hiding inaccessible UI elements.
- [ ] Prevent removal/disablement of the last usable administrator.
- [ ] Record actor and time for sensitive actions; include reasons for consequential corrections where appropriate.
- [ ] Verify staff cannot bypass restrictions by invoking a hidden action or direct use case.

**Gate:** admin and restricted staff flows are distinct, understandable, and protected below the UI layer.

## Phase 5 — Classes and classroom management

- [ ] Define the minimal class entity and rules with the nursery owner (class-specific fields remain configurable as needed).
- [ ] Implement class create, view, edit, availability/archive, and list use cases.
- [ ] Build the Classes page with simple search/list behavior and clear status.
- [ ] Implement assigning and moving children between classes with safeguards and an understandable confirmation.
- [ ] Show the current children roster from each class.
- [ ] Add current-view print/export actions to the class roster.

**Gate:** staff can manage classes and rosters; class records are not confused with branches.

## Phase 6 — Child records and fast student lookup

- [ ] Implement child domain/application records using only owner-approved fields.
- [ ] Build create, details, edit, archive/active-state, and list experiences.
- [ ] Implement fast, forgiving search using only approved/configured identifiers and fields.
- [ ] Show enrollment/class state and a concise payment status entry point from the child context.
- [ ] Add validation and duplicate-warning behavior for nursery-approved unique identifiers.
- [ ] Add permissions to view/create/edit/archive and export child data.
- [ ] Prepare child list current-view print/export actions without exposing unauthorized/hidden fields.

**Gate:** staff can find the correct child quickly and manage approved child data without guessed fields.

## Phase 7 — Employee records

- [ ] Implement employee records using only owner-approved fields.
- [ ] Build employee create, details, edit, active/archive, and list experiences.
- [ ] Add approved identifiers/card association only when the nursery decides to use them.
- [ ] Add employee list search/filter and current-view print/export.
- [ ] Apply employee-data permissions and protect sensitive details.

**Gate:** employee administration works with nursery-defined fields and does not depend on an attendance device.

## Phase 8 — Manual attendance

- [ ] Confirm attendance states and correction policy with the nursery.
- [x] Implement shared child and employee attendance rules, date/time validation, and typed Arabic errors.
- [x] Build daily child and employee attendance tabs with date selection and quick manual actions.
- [x] Show current status and prevent duplicate daily marks with a pre-check and the existing unique indexes.
- [x] Provide permission-guarded corrections with actor, timestamp, and required reason in the audit summary.
- [x] Add Arabic-normalized name search, child classroom/status filters, and daily status counts.
- [x] Keep manual attendance usable without a configured device; device adapters remain unimplemented.
- [ ] Add current-view print/export (planned in Phase 11).
- [ ] Verify the full flow visually on the nursery workstation at 1024×700.

**Gate:** staff can record and correct attendance manually for children and employees offline.

## Phase 9 — Subscriptions, fees, payments, and invoices

- [ ] Confirm fee/subscription rules, payment methods, invoice numbering, and correction/refund policy with the nursery.
- [ ] Define domain entities and invariants for charges, payments, balances, and payment history.
- [x] Implement transactional payment recording so a partial failure cannot leave inconsistent balances. (Subscription + initial payment + allocation + audit are committed together.)
- [x] Build a prominent child search-to-payment flow with minimal navigation. (Subscriptions page supports child search and payment actions.)
- [x] Support paid, partially paid, and unpaid states with due, paid, and remaining amounts shown clearly.
- [ ] Preserve payment history; implement only the owner-approved correction/refund/void workflow with authorization and audit trail.
- [x] Define an invoice/receipt document model and Arabic PDF template.
- [x] Generate and save/print PDF receipts for subscription payments, including logo and locally configured nursery identity fields.
- [x] Make invoice reprint/history access appropriately permission-controlled. (`Payments.View`)
- [ ] Keep physical receipt-printer support behind an adapter and defer model-specific work until selected.

**Gate:** full and partial payments remain consistent and traceable; staff can print a readable PDF without special hardware.

## Phase 10 — Inventory and stock issue

- [ ] Confirm inventory tracking rules, units, adjustment policy, and item-issue fields with the nursery.
- [ ] Implement inventory items and stock movement rules; do not silently overwrite movement history.
- [ ] Build inventory list, item details, receive/adjust workflows, and clear on-hand quantity display.
- [ ] Implement item issue recording with recipient/purpose only as approved/configured.
- [ ] Add low/insufficient-stock feedback if the nursery chooses quantity tracking.
- [ ] Generate a printable PDF document for inventory items issued.
- [ ] Add permissions, action traceability, and current-view inventory print/export.

**Gate:** stock changes are understandable and auditable; issue documents print without receipt-printer hardware.

## Phase 11 — Excel-compatible exports and ordinary printing

- [ ] Define one export/print request model that captures the current visible list view (search, filters, sort, selected/visible columns).
- [ ] Implement `.xlsx` export behind an Infrastructure service and ensure only authorized visible data is included.
- [ ] Support operational lists for classes, children, employees, attendance, inventory, and other approved list pages.
- [ ] Format Arabic headings, right-to-left column order, dates, numbers, widths, and print setup for common office software/printers.
- [ ] Offer a clear distinction between exporting the current view and all permitted records if both behaviors exist.
- [ ] Add preview/print flows where practical and verify readable grayscale output.
- [ ] Check Arabic shaping and workbook/PDF behavior on the actual Windows machine.

**Gate:** staff can print or export the rows they are looking at, and the output respects permissions and filters.

## Phase 12 — Barcode cards and scanning (hardware-dependent)

- [ ] Confirm scanner model/interface and card/barcode format with the nursery.
- [ ] Define a replaceable barcode input adapter; support keyboard-wedge behavior only if confirmed.
- [ ] Implement safe identifier assignment and duplicate detection.
- [ ] Implement scan-to-record lookup with a visible identity confirmation before any consequential action.
- [ ] If approved, allow scan-to-attendance through the same attendance rules/use cases as manual marking.
- [ ] Handle unknown, inactive, duplicate, and malformed codes with plain-language messages.
- [ ] Test using the actual scanner; do not claim hardware support based only on a simulated scan.

**Gate:** selected scanner/card works as documented; manual lookup and attendance still work when disconnected.

## Phase 13 — Biometric attendance integration (hardware-dependent)

- [ ] Confirm biometric model, SDK/protocol, driver/runtime/licensing requirements, event flow, and enrollment responsibility.
- [ ] Define an adapter interface and event model independent of vendor SDK types.
- [ ] Implement a simulator for development and failure/reconnection scenarios.
- [ ] Implement the vendor adapter only after the device is selected and documentation is available.
- [ ] Map device identities to approved child/employee identifiers without storing biometric templates unless explicitly approved after review.
- [ ] Handle duplicate/out-of-order events safely and record device-assisted event provenance.
- [ ] Confirm device failure never disables manual attendance or application startup.
- [ ] Test on the nursery's actual device and PC, including restart/disconnect behavior.

**Gate:** actual selected hardware is validated; unsupported or disconnected hardware degrades gracefully to manual attendance.

## Phase 14 — Backup, restore, and data recovery

- [ ] Define backup contents, database-safe snapshot method, destination options, retention expectation, and responsible staff role.
- [ ] Implement authorized on-demand backup with integrity verification and clear completion location/time.
- [ ] Implement guided restore with explicit warning and pre-restore safety backup where possible.
- [ ] Validate restored schema, records, and related local assets before reopening normal operation.
- [ ] Explain failures and recovery steps in Arabic without technical jargon.
- [ ] Verify backup/restore fully offline and keep all data local.
- [ ] Document what an ordinary application uninstall/update does to database and backup files.

**Gate:** a staff member can create and restore a verified local backup without command-line steps or internet access.

## Phase 15 — Integration, usability, and quality checks

- [ ] Add unit tests for important Domain rules and Application use cases.
- [ ] Add persistence/migration integration checks and transaction/failure-path checks.
- [ ] Add export/PDF checks for Arabic RTL, correct filtering, document values, page layout, and permission-safe fields.
- [ ] Walk through end-to-end tasks with a non-technical staff member and simplify any confusing labels or extra steps.
- [ ] Verify permissions for every page and consequential action.
- [ ] Verify offline startup and normal operation with network disabled.
- [ ] Verify device-absent, invalid input, duplicate scan, printer unavailable, low disk, and interrupted database operation states.
- [ ] Review logs for unnecessary sensitive information.
- [ ] Verify logo proportions, accessible contrast, and grayscale print output.
- [ ] Record unresolved defects and hardware limitations; do not label untested integrations as supported.

**Gate:** acceptance scenarios in the specification pass on the supported Windows setup, with known limitations recorded.

## Phase 16 — Installer, upgrade, and operational handoff

- [ ] Choose installer technology and package the application for a normal nursery Windows PC.
- [ ] Include or bootstrap the supported .NET runtime and required dependencies; document third-party redistribution/licensing.
- [ ] Create a first-run Arabic setup flow for admin, local data, and backup destination.
- [ ] Keep user database/backups separate from binaries and preserve them during normal upgrades.
- [ ] Add safe schema upgrade and recovery behavior; test upgrade from the prior release using a copied sample database.
- [ ] Verify install, launch, update, repair/uninstall behavior on a clean supported Windows PC while preserving data.
- [ ] Prepare concise Arabic staff guides for daily use, payments/invoices, attendance, exports/printing, backup/restore, and device setup.
- [ ] Deliver the installer, version notes, support/troubleshooting instructions, and documented device prerequisites.

**Gate:** the application can be installed and operated by nursery staff without command-line setup, internet dependency, or data loss during upgrade.

## Cross-cutting completion checklist

- [ ] Every workflow remains simple, clear, Arabic-first, and right-to-left.
- [ ] No Central Database, central server, cloud backend, cross-branch sync, Branch Management page, or Reports page has been introduced.
- [ ] Each branch installation remains one PC with one independent local database.
- [ ] No child/employee fields or hardware requirements have been invented while marked TBD.
- [ ] UI, Application, Domain, and Infrastructure responsibilities remain separate.
- [ ] Permissions are enforced in both the UI and Application use cases.
- [ ] Financial and inventory history is traceable and transaction-safe.
- [ ] Manual attendance and PDF generation work without optional devices.
- [ ] List exports reflect the visible view and do not leak hidden/unauthorized data.
- [ ] Backup/restore is local, verified, and understandable to staff.
- [ ] Installation and upgrades preserve nursery data.

## Major dependency order

```text
Scope decisions
  -> Solution foundation
  -> UI shell + local persistence
  -> Accounts and permissions
  -> Classes
  -> Children and employees
  -> Manual attendance
  -> Payments/invoices and inventory
  -> Excel/PDF/printing refinement
  -> Optional barcode/biometric hardware adapters
  -> Backup/restore + full acceptance
  -> Installer and handoff
```

Some work can proceed in parallel after its prerequisites, but device-specific implementation must wait for actual hardware decisions. The app's manual workflows, local storage, permissions, exports, and PDF documents do not need to wait for that hardware.




