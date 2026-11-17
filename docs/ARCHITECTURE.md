# Solution architecture

This solution is organized into four product projects plus an xUnit test project. The desktop app uses Windows Forms and keeps local persistence and use cases in the same offline solution.

```text
ElMomenNursery.sln
Directory.Build.props
ElMomenNursery/                         # Desktop UI (Windows Forms)
  Forms/                                # Screens/forms
  Controls/                             # Reusable UI controls
  Resources/Branding/                   # Nursery logo and UI assets
ElMomenNursery.Domain/                  # Business concepts and rules; framework-independent
  Entities/
  ValueObjects/
  Enums/
ElMomenNursery.Application/             # Use cases and ports/interfaces
  Abstractions/
  Common/
  Features/                             # Organized by business feature as it is added
ElMomenNursery.Infrastructure/          # Local database, files, exports, devices, backup
  Persistence/
  Documents/
  Exports/
  Devices/
  Backup/
  Security/
ElMomenNursery.Tests/                   # SQLite in-memory application/integration tests
docs/                                   # Project and architecture documentation
```

## Dependency direction

```text
Desktop UI  ->  Application  ->  Domain
      |                ^
      +-> Infrastructure -> Domain
```

- **Domain** owns business concepts and rules. It has no dependency on UI, database, or device libraries.
- **Application** coordinates user-facing use cases and defines interfaces (ports) for persistence, documents, device input, and other external work.
- **Infrastructure** implements those interfaces for the local database, PDF/Excel output, backup, and hardware adapters.
- **Desktop UI** displays the workflows and calls Application use cases. It acts as the composition root that wires Infrastructure implementations into Application interfaces.
- The database is local to this one PC/branch. No network server or cross-branch sync is part of this structure.
- `ApplicationServiceCollectionExtensions.AddNurseryApplication()` registers Application use cases. Attendance pages resolve services from a fresh asynchronous DI scope for each load/save, so each operation gets its own scoped `DbContext` and unit of work.

## Initial technology target

All projects currently target .NET 9: the desktop project uses `net9.0-windows` and the non-UI projects use `net9.0`. This target was restored to match the available Visual Studio 2022 17.14 and .NET SDK 9 development environment. .NET 9 reached end of support on November 10, 2026; this is a compatibility choice requested for the current workstation, and the project should move to a supported target together with a compatible Visual Studio update.
## Boundaries to keep

- Do not place database queries or business rules in form event handlers.
- Keep provider-specific persistence, printer APIs, barcode integration, and biometric SDK code in Infrastructure behind Application interfaces.
- Preserve manual attendance and PDF printing as the usable baseline while device models remain TBD.
- Detailed child and employee fields remain nursery-defined; do not add guessed fields to Domain.
- Add a module folder under `Application/Features` and a corresponding UI form only when that feature is ready to be implemented.
- The structure intentionally avoids separate microservices, a central database, a reports module, and branch management.

## Desktop shell and brand foundation

- `ElMomenNursery/Forms/MainForm.cs` is the current application shell. It provides a brand header, right-side Arabic navigation, a home task launcher, and neutral empty states for sections whose business features are not implemented yet.
- The Classes entry currently displays a neutral Arabic unavailable state. Classroom management remains disabled until the owner approves its fields and rules; sample rows are not used.
- `ElMomenNursery/Styling/BrandPalette.cs` centralizes colors sampled/derived from the supplied logo (green `#80B040`, red `#E00000`, and blue `#0040E0`). The logo's bright green is an accent; a darker derived green (`#376927`) is used for primary actions and readable text. Its white-text contrast is approximately 6.54:1. The bright logo green is reserved for decorative accents. Red and blue tones are reserved for status feedback.
- The actual logo is kept at `ElMomenNursery/Resources/Branding/AlMoumenNurseryLogo.jpeg` and copied to the output directory for the app header.
- `ElMomenNursery/Controls/` contains reusable navigation buttons, loading/empty/message states, validation hints, form sections/field rows, and Arabic confirmation/information dialogs.
- The Admin-only settings entry is intentionally not shown until sign-in and permission enforcement are implemented. Role-based filtering remains a later task.
- Attendance is the first operational section and reads/writes through scoped Application services. Other main-shell sections remain placeholders or existing partial workflows.




The foundational model uses EF Core 9 with SQLite. `ElMomenNursery.Infrastructure/Persistence/NurseryDbContext.cs` owns provider mappings and entity configurations; versioned migrations live beside it. Desktop startup applies pending migrations and stores the local database under `%LOCALAPPDATA%\AlMoumenNursery` by default. The existing `ChildAttendances` and `EmployeeAttendances` tables retain their unique person/date indexes; Phase 8 adds no schema changes. `Forms/AttendancePage` hosts the child and employee daily views, and the Application attendance services enforce permissions and return `Result<T>` with Arabic errors. Attendance corrections write before/after values, reason, and session user name to `AuditLog.ChangeSummary`. Current-view Excel export is available on operational tables through `Forms/GridExcelExport.cs` and `Infrastructure/Spreadsheet/ExcelExportService.cs`; export-specific permissions gate each action. Subscription and payment workflows use `SubscriptionService`, a unique optional receipt number, transactional allocations, and an Infrastructure QuestPDF renderer with an embedded OFL Arabic font. See Phase 8/9 change notes and ADRs 0002/0003 for decisions and remaining owner questions.
