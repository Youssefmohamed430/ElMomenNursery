# Development conventions

## Toolchain

- Use the installed .NET 9 SDK (currently 9.0.306) to build or run the solution.
- The app targets `net9.0-windows`; the non-UI projects target `net9.0`.
- The selected target matches Visual Studio 2022 17.14 and the installed .NET 9 SDK. .NET 9 is out of support as of November 10, 2026; plan to update Visual Studio and the target framework together.
- Do not add a `global.json` SDK pin unless the team explicitly needs to lock builds to a particular installed SDK.

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



- Persistence uses EF Core SQLite 9.0.10. Add schema changes as EF Core migrations in ElMomenNursery.Infrastructure/Persistence/Migrations; keep provider mappings and constraints in entity configuration classes.

## Database migrations

Run these commands from the `ElMomenNursery` solution directory. The repository pins the local `dotnet-ef` tool to EF Core 9.0.10:

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> --project ElMomenNursery.Infrastructure --startup-project ElMomenNursery
dotnet ef database update --project ElMomenNursery.Infrastructure --startup-project ElMomenNursery
```

## Application logging

The desktop composition root configures Serilog and exposes it through `Microsoft.Extensions.Logging`, so Application and Infrastructure services can receive `ILogger<T>`. Rolling log files are written under `<application install directory>\Logs` (for example, `Program Files (x86)\AlMoumenNursery\Logs`) with daily rollover and 31 retained files. The installer must grant the standard Windows Users group modify access to this `Logs` subdirectory; do not grant write access to the full application directory. Avoid logging passwords, secrets, or unnecessary child, employee, and payment details.

## Application results and errors

Use `ElMomenNursery.Domain.Abstractions.Result` / `Result<TValue>` for expected business outcomes from Application use cases. Return typed `Error` values with stable codes and Arabic descriptions; group feature errors in a feature-specific static class. Exceptions remain for infrastructure failures and programming errors, and should be logged at the application boundary. Read `Result<TValue>.Value` only after checking `IsSuccess`.
