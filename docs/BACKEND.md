# Backend features and structure {#backend}

The Staff/HR backend serves the seven HR screens (Workforce overview, Staff directory, Employee profile,
Onboard employee, Duty roster, Requisitions, Credentials) and the other CMES services through 33 REST endpoints,
Azure Service Bus events and a 12-table SQL Server database. Request and response shapes are in `API.md`;
deployment is in @ref deployment "Deploying to Azure".

## Layers

A request flows **Controller → Service → Repository → DbContext → SQL Server**, and a DTO comes back up.

| Layer | Folder | Responsibility | Main classes |
| --- | --- | --- | --- |
| Controllers | `Controllers/` | Routing, HTTP status codes and the role policy for each endpoint | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::StaffController "StaffController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::RequisitionsController "RequisitionsController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::EligibilityController "EligibilityController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::PayrollController "PayrollController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::SecurityFlagsController "SecurityFlagsController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::AuditController "AuditController", … |
| Contracts | `Contracts/` | Request and response JSON shapes (DTOs) | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Contracts::StaffProfileDto "StaffProfileDto", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Contracts::CreateStaffRequest "CreateStaffRequest", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Contracts::DutyEligibilityDto "DutyEligibilityDto", … |
| Application services | `Application/Services/` | Business rules, validation (422), entity-to-DTO mapping | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::StaffService "StaffService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::RequisitionService "RequisitionService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::TerminationService "TerminationService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::EligibilityService "EligibilityService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::PayrollService "PayrollService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::SecurityFlagService "SecurityFlagService" |
| Integration events | `Application/Events/` | Transactional outbox: queue events with the change, publish them in order | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Events::OutboxWriter "OutboxWriter", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Events::OutboxDispatcher "OutboxDispatcher" |
| Validation and errors | `Application/Validation/`, `Application/Errors/` | Collect field errors; exceptions for 400/403/404/409/422 | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Validation::ValidationErrors "ValidationErrors", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::ApiException "ApiException" and subclasses |
| Repositories | `Data/Repositories/` | EF Core queries, one per area | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::StaffRepository "StaffRepository", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::RosterRepository "RosterRepository", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::CompensationRepository "CompensationRepository", … |
| Database | `Data/` | DbContext with department-scope query filters, mappings, migrations, audit interceptor, seeder | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::CmesDbContext "CmesDbContext", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Auditing::AuditInterceptor "AuditInterceptor", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Seeding::DatabaseSeeder "DatabaseSeeder" |
| Domain | `Domain/` | Entities, enums and pure rules | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::StaffNumber "StaffNumber", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::CredentialStatusRules "CredentialStatusRules", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::RequisitionWorkflow "RequisitionWorkflow", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::DutyEligibilityRules "DutyEligibilityRules" |
| Infrastructure | `Infrastructure/` | Authentication and roles, ProblemDetails, OpenAPI, Service Bus, telemetry | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::Auth::AuthSetup "AuthSetup", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::ApiExceptionHandler "ApiExceptionHandler", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::Messaging::ServiceBusEventPublisher "ServiceBusEventPublisher", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::Observability::StaffHrMetrics "StaffHrMetrics" |

Services throw @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::NotFoundException "NotFoundException", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::ConflictException "ConflictException", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::ValidationFailedException "ValidationFailedException", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::ForbiddenException "ForbiddenException" or
@ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::BadRequestException "BadRequestException"; @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::ApiExceptionHandler "ApiExceptionHandler" turns them into `application/problem+json`, so controllers hold no error
handling. 401 and 403 from the authorization layer use the same format.

## Features

| Area | Endpoints | Used by |
| --- | --- | --- |
| Health | `GET /health` (database and outbox checks; anonymous) | Azure App Service probe, monitoring |
| Staff | `POST /staff`, `GET /staff/{staffNumber}`, `PATCH /staff/{staffNumber}`, `GET /staff` | Onboard employee, Employee profile, Staff directory |
| Departments | `GET /departments`, `GET /departments/{code}/services`, `GET /departments/headcount` | Workforce overview |
| Assignments | `POST /staff/{staffNumber}/assignments`, `GET /staff/{staffNumber}/assignments`, `PATCH /assignments/{id}` | Employee profile |
| Rosters | `GET /rosters`, `POST /rosters/entries`, `DELETE /rosters/entries/{id}`, `GET /rosters/headcount`, `GET /rosters/on-duty` | Duty roster |
| Requisitions | `POST /requisitions` (Hire or Termination), `GET /requisitions`, `GET /requisitions/{number}`, `PATCH /requisitions/{number}` | Requisitions, departments |
| Credentials | `GET /credentials`, `GET /credentials/types`, `GET /credentials/expiring`, `POST /staff/{staffNumber}/credentials`, `PATCH /credentials/{id}` | Credentials, Workforce overview |
| Duty eligibility | `GET /staff/{staffNumber}/eligibility` | Emergency Services, Security, Transport |
| Security flags | `PUT /staff/{staffNumber}/security-flags/{flagType}`, `GET /staff/{staffNumber}/security-flags`, `GET /security-flags` | Security service |
| Payroll | `PUT /staff/{staffNumber}/compensation`, `GET /staff/{staffNumber}/compensation`, `GET /headcounts/payroll` | Payroll officers, Budget service |
| Audit | `GET /audit` | HR, auditors |
| Events | `Staff.Onboarded`, `Staff.Terminated` on the Service Bus topic `staff-events` | Every peer service |

## Business rules

Each rule is tagged in the code with a `Business rule N` paragraph, so it shows on the class and member pages.

| Rule | Enforced by |
| --- | --- |
| 1. Staff number `STF-0000000` from one sequence, in the same transaction as the Staff row, immutable, never reused | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::StaffNumber "StaffNumber", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::StaffRepository::NextStaffNumberAsync "StaffRepository::NextStaffNumberAsync", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::StaffService::CreateAsync "StaffService::CreateAsync" / UpdateAsync, @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Configurations::StaffConfiguration "StaffConfiguration" |
| 2. Work email unique city-wide → 409 (even for department-scoped callers) | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::StaffService "StaffService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::StaffRepository::EmailInUseAsync "StaffRepository::EmailInUseAsync", unique index |
| 3. `Idempotency-Key` on `POST /requisitions`; replay returns the original | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::RequisitionService::CreateAsync "RequisitionService::CreateAsync", unique index |
| 4. No salary in any staff or search DTO; salary only on payroll endpoints, payroll role only | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::PayrollService "PayrollService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::PayrollController "PayrollController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::Auth::StaffHrPolicies::Payroll "StaffHrPolicies::Payroll" |
| 5. Credential status derived from ExpiryDate on read; Suspended overrides | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::CredentialStatusRules "CredentialStatusRules" |
| 6. StartDate &lt; EndDate, IssueDate &lt; ExpiryDate, ShiftStart &lt; ShiftEnd; no overlapping shifts | Service validation, check constraints, @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::RosterRepository::FindOverlapAsync "RosterRepository::FindOverlapAsync" |
| Duty eligibility: Active/Probation, current assignment, no expired or suspended duty-blocking credential, no Active security flag | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::DutyEligibilityRules "DutyEligibilityRules" |
| Completing a termination terminates the employee, ends assignments, cancels unfinished shifts and publishes Staff.Terminated, all in one save | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::TerminationService "TerminationService" |

## Requisition workflow

\dot
digraph workflow {
    rankdir=LR;
    node [shape=box, style=rounded, fontname=Helvetica, fontsize=10];
    subgraph cluster_hire { label="Hire"; fontname=Helvetica; fontsize=10;
        hReceived [label="Received"]; hReview [label="HRReview"]; hBudget [label="AwaitingBudget"];
        hApproved [label="Approved"]; hFilling [label="Filling"]; hCompleted [label="Completed"];
        hReceived -> hReview -> hBudget -> hApproved -> hFilling -> hCompleted; }
    subgraph cluster_term { label="Termination"; fontname=Helvetica; fontsize=10;
        tReceived [label="Received"]; tReview [label="HRReview"]; tApproved [label="Approved"];
        tCompleted [label="Completed\n(terminates)"];
        tReceived -> tReview -> tApproved -> tCompleted; }
}
\enddot

Both types may move to Rejected from the early steps and to Closed from any non-final step; Rejected and Closed are
final (see @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::RequisitionWorkflow "RequisitionWorkflow"). A Hire is Approved only once Budget approved it, and Completed only when every
position is filled.

## Security

- **Authentication.** Entra ID access tokens are validated: issuer, audience, signature and lifetime. In the
  Development environment a development scheme signs everyone in with all roles, so no token is needed locally.
- **Authorization.** Seven app roles map to policies (@ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::Auth::StaffHrRoles "StaffHrRoles", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::Auth::StaffHrPolicies "StaffHrPolicies"). Salary needs the Payroll role;
  HR admins don't have it.
- **Row-level security.** A caller whose token carries a `department` claim sees only that department's people and
  records. This is done by global query filters in @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::CmesDbContext "CmesDbContext".
- **Audit.** Every change is recorded with the signed-in user (@ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Auditing::AuditInterceptor "AuditInterceptor").

## Integration events

Changes that peer services care about are written to the `OutboxEvent` table in the same transaction as the
change (@ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Events::OutboxWriter "OutboxWriter"). @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::Messaging::OutboxPublisherService "OutboxPublisherService" runs @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Events::OutboxDispatcher "OutboxDispatcher" every few seconds, which publishes them oldest
first to the Service Bus topic `staff-events` (@ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::Messaging::ServiceBusEventPublisher "ServiceBusEventPublisher"). The event id is used as the Service Bus
`MessageId`, so retried events are removed as duplicates. Without Service Bus configured, events are written to the
log. `/health` reports `outbox: Degraded` when an event waits more than 5 minutes.

## Database

Two migrations: `InitialCreate` (all 12 tables, two number sequences, check constraints) and
`SecurityFlagPerType` (one flag per type per person). Enums are stored as strings and every DateTime is UTC.
The seeder loads fictional demo data, including pay rates and security flags.
