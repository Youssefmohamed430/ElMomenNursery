# Offline authentication and authorization

The desktop application uses local SQLite accounts and `PasswordHasher<UserAccount>` hashes. No token, server, or network identity provider is involved. `SessionManager` is a process-wide singleton; the application always starts at the login dialog and does not restore sessions after restart.

## First launch

Startup applies EF Core migrations and idempotently seeds the Admin, Manager, Teacher, Accountant, and Receptionist roles plus permissions matching the implemented nursery modules. The initial role-permission map is written once; later startup does not replace administrator edits. When there is no account with a current password hash, the login dialog switches to administrator setup. This also provides recovery for databases whose old placeholder accounts stored plaintext: create a new administrator, then reset those accounts' passwords from **إدارة المستخدمين**. Enter a username, full name, and a password with at least 12 characters, including a letter and a digit. The app then returns to sign-in; the password is never stored in plaintext.

## Use

- Sign in with the username and password.
- In **إدارة المستخدمين**, an administrator can create/deactivate accounts, reset account passwords, assign one or more roles, and change role permissions.
- To switch users, select **تسجيل الخروج** and sign in again. Closing the dashboard also clears the in-memory session.
- Password changes are available through `IAuthService.ChangePasswordAsync` for application workflows; a dedicated password-change screen is not yet part of the current shell.

Login failures use a generic message. The default lockout is five consecutive failures for 15 minutes; both values are configurable under `Authentication` in `appsettings.json`. Successful authentication resets the counter. Authentication actions are recorded in `AuthenticationAuditLogs` without passwords.

## Permissions and current feature boundaries

The session stores a snapshot of role names and permission codes. UI navigation/actions use that snapshot, and `IPermissionService` provides business-operation guards for services. Role edits refresh the currently logged-in process session before the next operation. Attendance services enforce the child permissions `Attendance.View/Record/Edit` and employee permissions `EmployeeAttendance.View/Record/Edit`; UI visibility is only a usability aid.

Role defaults are deliberately limited to implemented modules: Admin receives all seeded codes; Manager manages students, classes, child and employee attendance, employees, subscriptions, and inventory; Teacher views students/classes and records child attendance; Accountant manages payments/expenses and views subscriptions; Receptionist views/registers students and views classes/subscriptions. Inventory export is granted to Admin and Manager, while inventory restore is Admin-only. Administrators can edit these mappings. `Authentication.RolePermissionsSeeded.V6` adds the inventory export and restore permissions only when missing; it does not replace existing role-permission edits.

## Database and security limits

Migration `20261112162402_AddOfflineAuthentication` adds lockout/profile fields, permission and join tables, authentication audit rows, and preserves each existing account's previous single-role link in `UserRoles`. Existing accounts whose `PasswordHash` column contains a legacy plaintext password cannot authenticate with the new hasher; an administrator must select the account and set a new password in **إدارة المستخدمين**. The app does not compare or retain legacy plaintext values. Back up the local SQLite database before applying migrations. Restrict access to the Windows account and the local database directory: offline RBAC cannot protect files or binaries from someone with unrestricted device access.

Multiple application processes can each hold an in-memory session. A permission change refreshes the session in the process that made the change; another running process sees new permissions on its next login, not through cross-process notification. Run one application instance per local database for immediate revocation.

## Verification

The repository currently has no test project or test framework. The implementation was verified with a full solution build and EF Core migration script generation. Add automated auth, session, authorization, and seed-idempotency tests with the first test project.
