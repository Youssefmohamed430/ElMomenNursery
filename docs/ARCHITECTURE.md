# Solution architecture

This solution is organized into four projects. The application is still a Windows Forms desktop app; this setup only establishes clean project boundaries and does not implement nursery features.

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

## Initial technology target

All projects target .NET 10; the desktop project targets `net10.0-windows` and keeps Windows Forms. .NET 10 is the current LTS target. Keep application and business logic in the non-UI projects so a future UI change would not require rewriting those layers.

## Boundaries to keep

- Do not place database queries or business rules in form event handlers.
- Keep provider-specific persistence, printer APIs, barcode integration, and biometric SDK code in Infrastructure behind Application interfaces.
- Preserve manual attendance and PDF printing as the usable baseline while device models remain TBD.
- Detailed child and employee fields remain nursery-defined; do not add guessed fields to Domain.
- Add a module folder under `Application/Features` and a corresponding UI form only when that feature is ready to be implemented.
- The structure intentionally avoids separate microservices, a central database, a reports module, and branch management.
