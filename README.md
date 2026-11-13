# CMES Staff/HR Service

Backend and React frontend for the Staff/HR service of the City Management Enterprise System (CSCN73030 Project V, Group 6).
It is the single source of truth for municipal employees: departments, services, staff, assignments,
duty rosters, hire and termination requisitions, credentials, duty eligibility, security flags and payroll.
It publishes `Staff.Onboarded` / `Staff.Terminated` events, keeps an audit log, and is secured with Entra ID roles.

- **API contract for other groups:** [API.md](API.md)
- **Swagger UI (Development):** `https://localhost:7038/swagger` (or `http://localhost:5058/swagger`)
- **Raw OpenAPI document:** `/openapi/v1.json`
- **Architecture and rules:** [docs/BACKEND.md](docs/BACKEND.md) · **Azure deployment:** [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)
- **React client (the HR screens):** [cscn73030-26f-sec1-projectv-group-6.client/README.md](cscn73030-26f-sec1-projectv-group-6.client/README.md)

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | 10.0.x | `dotnet --list-sdks` |
| Node.js | 20.19+ or 22.12+ | Building the server also restores the React client (`.esproj` reference). |
| SQL Server | LocalDB **or** Docker | LocalDB ships with Visual Studio on Windows. Everyone else: use Docker (below). |

## Run it from a clean clone

All commands run from the repository root.

### 1. Restore

```bash
dotnet tool restore
```

```bash
dotnet restore CSCN73030-26F-Sec1-ProjectV-Group-6.Server
```

`dotnet tool restore` installs the pinned `dotnet-ef` tool from `.config/dotnet-tools.json`.

### 2. Pick a database

**Option A: LocalDB (Windows + Visual Studio).** Nothing to do. `appsettings.json` already points at
`(localdb)\MSSQLLocalDB`, database `CmesStaffHr`.

**Option B: SQL Server in Docker (any OS).** Start the container:

```bash
docker compose up -d
```

Then tell the API to use it, in the same terminal you run the next steps from.

bash / zsh:

```bash
export ConnectionStrings__StaffHr='Server=localhost,1433;Database=CmesStaffHr;User Id=sa;Password=LocalDev_Passw0rd!;TrustServerCertificate=True;MultipleActiveResultSets=true'
```

PowerShell:

```powershell
$env:ConnectionStrings__StaffHr = 'Server=localhost,1433;Database=CmesStaffHr;User Id=sa;Password=LocalDev_Passw0rd!;TrustServerCertificate=True;MultipleActiveResultSets=true'
```

The password is only for that throwaway local container (see `docker-compose.yml`).

### 3. Migrate

```bash
dotnet ef database update --project CSCN73030-26F-Sec1-ProjectV-Group-6.Server
```

This creates the database, all 12 tables and the two number sequences from the single `InitialCreate` migration.

### 4. Seed

```bash
dotnet run --project CSCN73030-26F-Sec1-ProjectV-Group-6.Server -- --seed
```

`--seed` applies any pending migrations, loads the demo data, then exits (it doesn't start the web server).
It does nothing if the database already has departments, so it is safe to run twice. To start over, drop the
database and seed again:

```bash
dotnet ef database drop --force --project CSCN73030-26F-Sec1-ProjectV-Group-6.Server
```

### 5. Run

```bash
dotnet run --project CSCN73030-26F-Sec1-ProjectV-Group-6.Server --launch-profile https
```

Open `https://localhost:7038/swagger`. Check `https://localhost:7038/health` reports the database as `Healthy`.
The launch profile also starts the React client through SpaProxy at `https://localhost:54552`: the seven
Staff & HR screens, reading and writing this API. In Visual Studio, pressing F5 on the solution does the same.

If your machine has no trusted HTTPS dev certificate, use `--launch-profile http` and `http://localhost:5058/swagger`,
then start the client yourself from `cscn73030-26f-sec1-projectv-group-6.client`:

```bash
ASPNETCORE_URLS=http://localhost:5058 npm run dev
```

and open `http://localhost:54552`.

**No token needed locally.** In the Development environment every request is signed in as `dev-user` with all roles,
so Swagger and the React client just work. To try a restricted caller, send headers such as
`X-Dev-Roles: StaffHr.Read` or `X-Dev-Department: EMS` (see API.md, "Authentication and roles").
Integration events are written to the console log unless a Service Bus is configured.

## Configuration

Everything below is optional locally and set by the Bicep template in Azure (see docs/DEPLOYMENT.md).

| Setting | Default | Purpose |
| --- | --- | --- |
| `ConnectionStrings:StaffHr` | LocalDB | SQL Server database |
| `Authentication:Mode` | `EntraId` (`Development` in appsettings.Development.json) | Token validation, or the no-token development sign-in |
| `Authentication:EntraId:TenantId` / `Audience` / `ClientId` | empty / `api://cmes-staff-hr` / empty | Entra ID app registration |
| `Messaging:ServiceBus:FullyQualifiedNamespace` or `ConnectionString` | empty (events go to the log) | Publish integration events to the `staff-events` topic |
| `KeyVault:Uri` | empty | Load secrets (e.g. `ConnectionStrings--StaffHr`) from Azure Key Vault |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | empty | Export traces, metrics and logs to Application Insights |

## Tests and documentation

Regression tests (532 cases) live in `CSCN73030-26F-Sec1-ProjectV-Group-6.Server.Tests`. The backend API tests need
Docker running, or a SQL Server named in the `STAFFHR_TEST_SQL` environment variable (for example LocalDB). Details,
and a table of every test case, are in [docs/REGRESSION_TESTING.md](docs/REGRESSION_TESTING.md).

```bash
dotnet test CSCN73030-26F-Sec1-ProjectV-Group-6.Server.Tests
```

The React client has its own tests (Vitest and Testing Library, against a fake API, so no server is needed). From
`cscn73030-26f-sec1-projectv-group-6.client`:

```bash
npm test
```

Code documentation is generated with Doxygen (install Doxygen and Graphviz first). The backend overview page is
[docs/BACKEND.md](docs/BACKEND.md).

```bash
doxygen Doxyfile
```

Then open `docs/doxygen/html/index.html`.

## Seed data

Every person in the seed data is **fictional**: invented names, `@cmes.example` emails (a reserved domain),
`555-01xx` phone numbers and `CIT-FAKE-…` citizen ids. Dates are relative to the day you seed, so the data
always looks current.

| What | Count | Notes |
| --- | --- | --- |
| Departments | 7 | EMS, SEC, PTR, MNT, BUD, RES, HRS |
| Services | 21 | 3 per department. Paramedic Service is `serviceId` 1. |
| Staff | 20 | Mostly Active; some Probation, OnLeave, Suspended and one Terminated |
| Assignments | 22 | One current role each, plus a past role (Desmond) and a second current role (Ezra) |
| Roster entries | 28 | Paramedic Service, yesterday to 5 days ahead, 12-hour day and night shifts, so someone is always on duty |
| Requisitions | 6 | Hire, one each in Received, HRReview, AwaitingBudget, Approved, Filling, Rejected |
| Credential types | 12 | Paramedic licences, CPR, driver's licence, etc. |
| Credentials | 27 | 17 Valid, 5 Expiring, 4 Expired, 1 Suspended at seed time |
| Compensation | 21 | A fictional rate for every employee; Desmond also has his pre-promotion rate |
| Security flags | 2 | Noor: Active `UnderInvestigation`; Anders: Cleared `BackgroundCheckRenewal` |

## Project layout

```
cscn73030-26f-sec1-projectv-group-6.client/
  src/api.js        API client used by every screen.
  src/pages/        One React module per screen (overview, directory, profile, onboarding, roster, ...).
CSCN73030-26F-Sec1-ProjectV-Group-6.Server/
  Controllers/      HTTP endpoints and their role policies. Thin: call one service method, return the result.
  Contracts/        Request and response DTOs (the JSON shapes in API.md).
  Application/
    Services/       Business rules and validation, one service per area.
    Events/         Transactional outbox: OutboxWriter queues events, OutboxDispatcher publishes them.
    Validation/     ValidationErrors helper that collects 422 field errors.
    Errors/         Exceptions that map to 400 / 403 / 404 / 409 / 422.
    Mapping.cs      Entity → DTO conversion.
  Domain/           Entities, enums and pure rules (staff number, credential status, requisition workflow, eligibility).
  Data/
    CmesDbContext.cs  DbContext, including the department-scope query filters.
    Auditing/       AuditInterceptor: an audit row for every change.
    Configurations/ EF Core table mappings (keys, indexes, check constraints).
    Repositories/   EF Core queries, one repository per area.
    Migrations/     InitialCreate and SecurityFlagPerType.
    Seeding/        DatabaseSeeder (demo data).
  Infrastructure/
    Auth/           Entra ID / development authentication, roles and policies, 401/403 ProblemDetails.
    Messaging/      Service Bus (or log) publisher and the background outbox loop.
    Observability/  Key Vault, OpenTelemetry / Application Insights, metrics, outbox health check.
    *.cs            ProblemDetails exception handler and OpenAPI transformers.
infra/              Bicep for Azure, Entra ID app roles.
```

A request flows **Controller → Service → Repository → DbContext**. Services throw `NotFoundException`,
`ConflictException`, `ValidationFailedException`, `ForbiddenException` or `BadRequestException`, and
`ApiExceptionHandler` turns them into RFC 7807 responses, so controllers contain no error handling.

## Where the business rules live

| Rule | Code |
| --- | --- |
| 1. Staff number `STF-0000000` from one sequence, in the same transaction, immutable, never reused | `Domain/StaffNumber.cs`, `StaffRepository.NextStaffNumberAsync`, `StaffService.CreateAsync` / `UpdateAsync`, `StaffConfiguration` (EF throws if it is modified) |
| 2. Unique work email → 409 | Unique index in `StaffConfiguration`, check in `StaffService`, fallback in `ApiExceptionHandler` |
| 3. `Idempotency-Key` on `POST /requisitions` | `RequisitionService.CreateAsync`, unique index in `RequisitionConfiguration` |
| 4. No salary in any staff or search DTO | Staff/search contracts have no salary fields; salary is returned only by `PayrollService` / `PayrollController` |
| 5. Credential status derived on read; Suspended overrides | `Domain/CredentialStatusRules.cs` |
| 6. Date ordering and no overlapping shifts | Service validation plus check constraints; overlap in `RosterRepository.FindOverlapAsync` |

## Adding a migration

```bash
dotnet ef migrations add <Name> --project CSCN73030-26F-Sec1-ProjectV-Group-6.Server --output-dir Data/Migrations
```

## Integration with other CMES services

| Service | What it uses |
| --- | --- |
| Emergency Services, Security, Transport | `GET /staff/{staffNumber}/eligibility` before rostering or dispatching; staff and roster reads |
| Security | `PUT /staff/{staffNumber}/security-flags/{flagType}` to sync flags |
| Budget & Treasury | `GET /headcounts/payroll` (Payroll role); records budget decisions on requisitions via HR |
| Every department | `POST /requisitions` (hire or termination); subscribe to `Staff.Onboarded` / `Staff.Terminated` |
