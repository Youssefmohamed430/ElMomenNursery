# Development conventions

## Toolchain

- Install the .NET 10 SDK (latest available 10.0.x servicing release) before building or running the solution.
- The app targets `net10.0-windows`; the non-UI projects target `net10.0`.
- Use a supported Windows development machine and Visual Studio/.NET tooling that supports .NET 10.
- Do not add a `global.json` SDK pin until the required SDK is installed on the development machine.

## Code and project boundaries

- Use clear English names for code symbols and projects; Arabic is the primary language for staff-facing text.
- Use nullable reference types, implicit usings, deterministic builds, and the shared `.editorconfig` formatting rules.
- Keep UI event handlers thin. Put workflows in Application and business invariants in Domain.
- Keep database/provider, file system, Excel/PDF, printer, barcode, and biometric implementation details in Infrastructure.
- Use interfaces in Application to isolate replaceable Infrastructure implementations.
- Avoid adding packages until a real use case needs them; record licenses and runtime requirements for selected packages.

## Errors and logging

- Validate user input near the workflow and return clear Arabic messages suitable for nursery staff.
- Do not show raw exceptions or stack traces in the UI. Preserve technical context in local logs when useful.
- Do not log passwords, secrets, biometric data, or unnecessary child/employee/payment details.
- Keep financial and stock corrections traceable; do not silently overwrite transaction history.

## Configuration and secrets

- Keep safe defaults in the application; keep machine-specific paths and device settings in local configuration outside source control.
- Never commit passwords, API keys, signing credentials, real connection details, or nursery records. This application is local-only and should not need service credentials.
- Validate configuration at startup and explain fixable issues in plain Arabic; do not write secrets into logs.
## Local and sample data

- Keep development-only fixtures under the ignored `dev-data/` directory at the solution root.
- Use synthetic records only. Never put real nursery records, credentials, exports, backups, or local database files in source control.
- The eventual application database must live in a protected Windows application-data location outside the installation folder. The exact per-user vs machine-wide location is TBD pending the nursery's Windows sign-in model.

## Change workflow

- Read `../SPECIFICATION.md`, `../AGENT.md`, `docs/ARCHITECTURE.md`, and the relevant ADR before changing scope or boundaries.
- Preserve offline, one-PC-per-branch local operation.
- Do not add a central database/server, cross-branch sync, Reports page, or Branch Management page.
- Do not invent child/employee fields or hardware behavior while those decisions are TBD.
- Keep Arabic RTL, short workflows, and usability for non-technical staff as acceptance requirements.

