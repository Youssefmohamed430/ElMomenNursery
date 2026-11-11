# ADR 0001: Solution boundaries and development baseline

- **Status:** Accepted for the initial foundation; open items are listed below.
- **Date:** 2026-11-11

## Context

The nursery needs a simple Windows desktop application. The UI and backend must be separate projects in one .NET solution. Each branch will use one PC with its own local-only database; the app must run offline. Data fields and hardware models remain owner-defined/TBD.

## Decisions

1. Keep the existing Windows Forms desktop project as the UI for the initial implementation.
2. Keep UI, Application, Domain, and Infrastructure in the same solution.
3. Keep Domain framework-independent. Application owns use cases and abstractions. Infrastructure implements persistence, export, documents, backup, and device adapters. The UI calls Application and wires Infrastructure at startup.
4. Current target: .NET 9 (`net9.0` for non-UI projects and `net9.0-windows` for the Windows Forms UI), as requested to match the installed Visual Studio 2022 17.14 and .NET SDK 9.0.306.
5. Use shared nullable reference types, implicit usings, deterministic output, and latest analysis level through `Directory.Build.props`; use `.editorconfig` for formatting.
6. Keep generated build/IDE output, user-specific settings, local databases, exports, logs, and backups out of source control.
7. Keep developer/sample data separate under an ignored `dev-data/` directory. Never use real nursery records as sample data.
8. Place the real logo in `ElMomenNursery/Resources/Branding/AlMoumenNurseryLogo.jpeg`; preserve the original file and use it as the identity reference.

## Dependency rules

- Desktop UI → Application.
- Desktop UI → Infrastructure only at the startup/composition boundary.
- Application → Domain.
- Infrastructure → Application and Domain.
- Domain → no other project.

Do not put database queries or business rules in Windows Forms event handlers. Hardware, database-provider, export, and printer libraries stay in Infrastructure behind narrow abstractions.

## Open decisions

- **Support lifecycle:** .NET 9 reached end of support on November 10, 2026. This target is retained for current Visual Studio compatibility as requested; plan a coordinated upgrade of Visual Studio and the target framework.
- Supported Windows versions and actual workstation display characteristics need confirmation.
- Local database provider, migration library, PDF/XLSX libraries, and installer technology are not selected yet.
- The Windows account model determines whether the eventual data directory should be per-user or machine-wide; choose a protected location outside the install directory before implementing persistence.
- Child/employee fields, fee policies, printer/scanner/biometric models, and related vendor dependencies remain TBD.
- Logo color sampling and the final accessible palette remain part of visual design work.

## Consequences

The solution has clear dependency boundaries without introducing a server or cross-branch features. The current Visual Studio/SDK can target .NET 9. No build was run as part of the original foundation change.

