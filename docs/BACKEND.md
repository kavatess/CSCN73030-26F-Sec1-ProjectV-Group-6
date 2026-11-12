# Backend features and structure {#backend}

The Staff/HR backend serves the seven HR screens (Workforce overview, Staff directory, Employee profile,
Onboard employee, Duty roster, Requisitions, Credentials) through 24 REST endpoints on a 12-table
SQL Server database. Request and response shapes for every endpoint are in `API.md`.

## Layers

A request flows **Controller → Service → Repository → DbContext → SQL Server**, and a DTO comes back up.

| Layer | Folder | Responsibility | Main classes |
| --- | --- | --- | --- |
| Controllers | `Controllers/` | Routing and HTTP status codes only; each action calls one service method | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::StaffController "StaffController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::DepartmentsController "DepartmentsController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::AssignmentsController "AssignmentsController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::RostersController "RostersController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::RequisitionsController "RequisitionsController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::CredentialsController "CredentialsController", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Controllers::HealthController "HealthController" |
| Contracts | `Contracts/` | Request and response JSON shapes (DTOs) | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Contracts::StaffProfileDto "StaffProfileDto", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Contracts::CreateStaffRequest "CreateStaffRequest", PagedResult, … |
| Application services | `Application/Services/` | Business rules, validation (422), entity-to-DTO mapping | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::StaffService "StaffService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::DepartmentService "DepartmentService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::AssignmentService "AssignmentService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::RosterService "RosterService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::RequisitionService "RequisitionService", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::CredentialService "CredentialService" |
| Validation and errors | `Application/Validation/`, `Application/Errors/` | Collect field errors; exceptions for 400/404/409/422 | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Validation::ValidationErrors "ValidationErrors", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::ApiException "ApiException" and subclasses |
| Repositories | `Data/Repositories/` | EF Core queries, one per area | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::StaffRepository "StaffRepository", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::DepartmentRepository "DepartmentRepository", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::AssignmentRepository "AssignmentRepository", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::RosterRepository "RosterRepository", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::RequisitionRepository "RequisitionRepository", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::CredentialRepository "CredentialRepository" |
| Database | `Data/` | DbContext, table mappings, the InitialCreate migration, demo seeder | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::CmesDbContext "CmesDbContext", Data/Configurations, @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Seeding::DatabaseSeeder "DatabaseSeeder" |
| Domain | `Domain/` | Entities, enums and pure rules shared by services and repositories | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::Staff "Staff", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::Credential "Credential", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::StaffNumber "StaffNumber", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::CredentialStatusRules "CredentialStatusRules", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::RequisitionWorkflow "RequisitionWorkflow" |
| Infrastructure | `Infrastructure/` | RFC 7807 error handling, OpenAPI examples | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::ApiExceptionHandler "ApiExceptionHandler", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::ProblemDetailsResult "ProblemDetailsResult", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::ProblemExamplesTransformer "ProblemExamplesTransformer" |

Services throw @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::NotFoundException "NotFoundException", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::ConflictException "ConflictException", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::ValidationFailedException "ValidationFailedException" or @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Errors::BadRequestException "BadRequestException";
@ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::ApiExceptionHandler "ApiExceptionHandler" turns them into `application/problem+json` responses, so controllers hold no error handling.
Model-binding failures (unreadable JSON, wrong types) are answered with 400 by @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::ProblemDetailsResult "ProblemDetailsResult".

## Features

| Area | Endpoints | Screen |
| --- | --- | --- |
| Health | `GET /health` | — |
| Staff | `POST /staff`, `GET /staff/{staffNumber}`, `PATCH /staff/{staffNumber}`, `GET /staff` | Onboard employee, Employee profile, Staff directory |
| Departments | `GET /departments`, `GET /departments/{code}/services`, `GET /departments/headcount` | Workforce overview |
| Assignments | `POST /staff/{staffNumber}/assignments`, `GET /staff/{staffNumber}/assignments`, `PATCH /assignments/{id}` | Employee profile |
| Rosters | `GET /rosters`, `POST /rosters/entries`, `DELETE /rosters/entries/{id}`, `GET /rosters/headcount`, `GET /rosters/on-duty` | Duty roster |
| Requisitions (hire) | `POST /requisitions`, `GET /requisitions`, `GET /requisitions/{number}`, `PATCH /requisitions/{number}` | Requisitions |
| Credentials | `GET /credentials`, `GET /credentials/expiring`, `POST /staff/{staffNumber}/credentials`, `PATCH /credentials/{id}` | Credentials, Workforce overview |

## Business rules

Each rule is tagged in the code with a `Business rule N` paragraph, so it shows on the class and member pages.

| Rule | Enforced by |
| --- | --- |
| 1. Staff number `STF-0000000` from one sequence, in the same transaction as the Staff row, immutable, never reused | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::StaffNumber "StaffNumber", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::StaffRepository::NextStaffNumberAsync "StaffRepository::NextStaffNumberAsync", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::StaffService::CreateAsync "StaffService::CreateAsync" / UpdateAsync, @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Configurations::StaffConfiguration "StaffConfiguration" |
| 2. Work email unique city-wide → 409 | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::StaffService "StaffService", unique index in @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Configurations::StaffConfiguration "StaffConfiguration", @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Infrastructure::ApiExceptionHandler "ApiExceptionHandler" |
| 3. `Idempotency-Key` on `POST /requisitions`; replay returns the original | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Application::Services::RequisitionService::CreateAsync "RequisitionService::CreateAsync", unique index in @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Configurations::RequisitionConfiguration "RequisitionConfiguration" |
| 4. No salary in any DTO | Contracts (no salary fields); Compensation is never read |
| 5. Credential status derived from ExpiryDate on read; Suspended overrides | @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::CredentialStatusRules "CredentialStatusRules" |
| 6. StartDate < EndDate, IssueDate < ExpiryDate, ShiftStart < ShiftEnd; no overlapping shifts | Service validation, check constraints, @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Data::Repositories::RosterRepository::FindOverlapAsync "RosterRepository::FindOverlapAsync" |

## Requisition workflow

\dot
digraph workflow {
    rankdir=LR;
    node [shape=box, style=rounded, fontname=Helvetica, fontsize=10];
    Received -> HRReview -> AwaitingBudget -> Approved -> Filling -> Completed -> Closed;
    Received -> Rejected; HRReview -> Rejected; AwaitingBudget -> Rejected;
    Received -> Closed; HRReview -> Closed; AwaitingBudget -> Closed; Approved -> Closed; Filling -> Closed;
    Rejected [style="rounded,filled", fillcolor="#eeeeee"];
    Closed [style="rounded,filled", fillcolor="#eeeeee"];
}
\enddot

Approved requires BudgetDecision Approved; Completed requires every position filled; Rejected and Closed are final
(see @ref CSCN73030_26F_Sec1_ProjectV_Group_6::Server::Domain::RequisitionWorkflow "RequisitionWorkflow").

## Database

All 12 tables are created by the single `InitialCreate` migration. Department, Service, Staff, StaffAssignment,
RosterEntry, Requisition, CredentialType and Credential are used now; Compensation, SecurityFlag, AuditLog and
OutboxEvent are schema only, ready for the next sprint. Two SQL sequences issue staff and requisition numbers.
Enums are stored as strings and every DateTime is UTC.

## Out of scope in this release

Eligibility, termination requisitions, Security-flag sync, outbox / Service Bus events, payroll headcount,
authentication and authorization, audit logging and Azure deployment.
