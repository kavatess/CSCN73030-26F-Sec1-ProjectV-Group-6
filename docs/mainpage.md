# Staff/HR Service {#mainpage}

This is the design documentation for the **Staff & Human Resources (Staff/HR)** service of the
City Management Enterprise System (CMES), built by CSCN73030 Group 6.

## Role in the city system

CMES is split into seven services, each built by its own team. Staff/HR is the central workforce
registry for the city: employee IDs, departmental rosters, licences, and hiring or firing requests.
The other services call Staff/HR over web APIs, for example when a department hires new staff, which
must be validated through Staff/HR and Budget & Treasury.

\dot
digraph cmes {
    rankdir=LR;
    node [shape=box, style=rounded, fontname=Helvetica, fontsize=10];
    hr [label="Staff / HR\n(this service)", style="rounded,filled", fillcolor="#cfe2ff"];
    sec [label="Security (Police)"];
    emg [label="Emergency Services"];
    mnt [label="Maintenance"];
    trn [label="Public Transport"];
    res [label="Residence"];
    bud [label="Budget & Treasury"];
    sec -> hr; emg -> hr; mnt -> hr; trn -> hr; res -> hr; bud -> hr;
    hr -> bud [label="funding requests,\npayroll expenses", fontsize=9];
}
\enddot

## Architecture

The service is one Visual Studio solution with a Web API and a single-page client.

| Component | Technology | Location |
| --- | --- | --- |
| Server | ASP.NET Core (.NET 10) Web API | `CSCN73030-26F-Sec1-ProjectV-Group-6.Server` |
| Client | React 19 + Vite | `cscn73030-26f-sec1-projectv-group-6.client` |
| Server tests | xUnit + `WebApplicationFactory` | `CSCN73030-26F-Sec1-ProjectV-Group-6.Server.Tests` |

\dot
digraph arch {
    rankdir=LR;
    node [shape=box, style=rounded, fontname=Helvetica, fontsize=10];
    browser [label="Browser"];
    client [label="React client"];
    ctrl [label="Controllers\n+ Contracts (DTOs)"];
    svc [label="Application services\n(rules, validation)"];
    repo [label="Repositories\n(EF Core)"];
    db [label="SQL Server"];
    browser -> client [label="loads"];
    client -> ctrl [label="fetch(JSON)"];
    ctrl -> svc [label="calls"];
    svc -> repo [label="reads / writes"];
    repo -> db [label="SQL"];
}
\enddot

The server's layers, folders and business rules are described in @ref backend "Backend features and structure".

During development the server starts the Vite dev server through SpaProxy. The client calls the API
with a relative URL, so the same code works behind the Vite proxy and when served by ASP.NET.

## Current state (v0.3.0, Sprint 3)

- **Backend:** staff, departments and services, assignments, duty rosters, hire and termination requisitions,
  credentials, duty eligibility, security-flag sync, payroll, an audit log, `Staff.Onboarded` / `Staff.Terminated`
  events through a transactional outbox, Entra ID roles with department-scoped data, telemetry. See
  @ref backend "Backend features and structure".
- **Frontend:** the seven HR screens run on the live API. See @ref frontend "Frontend: the HR screens".
- **Budget and Treasury:** hires are funded through Budget's Finance API, Budget's decisions are read back, department
  budgets are shown, and monthly payroll cost is recorded in Budget. See the Budget section of @ref backend "Backend features and structure".
- **Deployment:** Azure App Service and Azure SQL through GitHub Actions. See @ref deployment "Deploying to Azure".
- **Contract and tools:** `API.md` is the contract for other groups (38 endpoints); `postman/` holds a Postman
  collection of every request. Every feature has regression tests: see @ref REGRESSION_TESTING.md "Regression testing".

## Requirements

The service requirements are maintained in the collaborative SRS. As requirements are implemented,
document the code that satisfies each one with a Doxygen comment that names it, for example
`@see` or `@par Requirement`, so the generated pages trace back to the SRS.

## Regenerating this documentation

From the repository root run `doxygen Doxyfile`, then open `docs/doxygen/html/index.html`.
