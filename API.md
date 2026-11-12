# CMES Staff/HR API (v1)

Contract for review by the other CMES groups. The live, always-current version is the OpenAPI document at
`/openapi/v1.json`, browsable at `/swagger` when the service runs in Development.

## Conventions

- **JSON** uses camelCase. Enums are strings (`"Active"`, not `0`).
- **Dates** (`dateOfBirth`, `hireDate`, `startDate`, `issueDate`, …) are `yyyy-MM-dd`.
- **Times** (`shiftStart`, `shiftEnd`, `createdAtUtc`, `at`, `from`, `to`) are ISO 8601 and **UTC**. Responses
  always end in `Z`. A request value without an offset is read as UTC; a value with an offset is converted.
- **Staff number** `STF-0000000` is the public key for an employee. Store it as your foreign key. It never changes
  (not even on a department transfer) and is never reused.
- **Department code** (`EMS`, `SEC`, `PTR`, `MNT`, `BUD`, `RES`, `HRS`) is the public key for a department and is
  case-insensitive. **`serviceId`** (an integer, from `GET /departments/{code}/services`) identifies a service.
- **PATCH** bodies are partial: omit a field (or send `null`) to leave it unchanged.
- **Salary is never returned** by any endpoint.

### Enums

| Enum | Values |
| --- | --- |
| EmploymentStatus | `Active`, `Probation`, `OnLeave`, `Suspended`, `Terminated` |
| EmploymentType | `FullTime`, `PartTime`, `Contract`, `Seasonal` |
| ShiftType | `Day`, `Evening`, `Night`, `OnCall` |
| RosterStatus | `Scheduled`, `Confirmed`, `Completed`, `Cancelled` |
| RequisitionType | `Hire`, `Termination` |
| RequisitionStatus | `Received`, `HRReview`, `AwaitingBudget`, `Approved`, `Rejected`, `Filling`, `Completed`, `Closed` |
| BudgetDecision | `Pending`, `Approved`, `Rejected` |
| CredentialStatus | `Valid`, `Expiring`, `Expired`, `Suspended` |

### Errors (RFC 7807)

Every error is `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Validation failed",
  "status": 422,
  "detail": "One or more fields are invalid. See 'errors' for details.",
  "errors": { "email": ["Must be a valid email address."] },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

| Status | Meaning | `errors` |
| --- | --- | --- |
| 400 | Malformed: unreadable JSON, wrong value type (incl. unknown enum value), empty body, missing `Idempotency-Key` | Present when the problem is in a field (`$.hireDate`, `pageSize`) |
| 404 | The resource in the URL doesn't exist | — |
| 409 | Conflict: duplicate work email, `Idempotency-Key` reused with a different body, overlapping shift | — |
| 422 | Well-formed but breaks a rule; one entry per invalid field | Always |

## Shared shapes

```text
DepartmentRef   { code, name }
PagedResult<T>  { items: T[], page, pageSize, total }

StaffSummary    { staffNumber, displayName, firstName, lastName, email, phone?, employmentStatus,
                  employmentType, homeDepartment: DepartmentRef, currentRole?, currentService?,
                  credentialState? }
StaffProfile    { staffNumber, displayName, firstName, lastName, preferredName?, email, phone?,
                  dateOfBirth, citizenId?, hireDate, employmentStatus, employmentType,
                  homeDepartment: DepartmentRef, credentialState?,
                  assignments: Assignment[], credentials: Credential[] }
Assignment      { assignmentId, staffNumber, serviceId, serviceName, departmentCode, roleTitle,
                  startDate, endDate?, isCurrent }
RosterEntry     { rosterEntryId, staffNumber, staffName, serviceId, serviceName,
                  shiftStart, shiftEnd, shiftType, status }
Requisition     { requisitionNumber, type, department: DepartmentRef, roleTitle, positions,
                  positionsFilled, status, reasonCode?, budgetDecision?, staffNumber?, createdAtUtc }
Credential      { credentialId, staffNumber, staffName, departmentCode, credentialTypeId,
                  credentialType, isDutyBlocking, number, issueDate, expiryDate, status,
                  daysUntilExpiry }
```

`?` = may be `null`. `displayName` is the preferred name (or first name) plus last name.
`credentialState` on staff is their most urgent credential status (Expired > Suspended > Expiring > Valid),
or `null` if they hold none. `daysUntilExpiry` is negative once expired.

---

## Health

### `GET /health`

`200` when the database is reachable, `503` when it isn't.

```json
{ "status": "Healthy", "checks": { "database": "Healthy" } }
```

## Staff

### `POST /staff`: onboard an employee

Issues the next staff number. The optional `initialAssignment` is created in the same transaction.

```json
{
  "firstName": "Avery",              // required
  "lastName": "Quillfeather",        // required
  "preferredName": "Ave",
  "email": "avery.quillfeather@cmes.example",  // required, unique city-wide (case-insensitive)
  "dateOfBirth": "1997-04-12",       // required, in the past
  "phone": "555-0199",
  "citizenId": "CIT-FAKE-000123",
  "hireDate": "2026-11-02",          // required, at least 16 years after dateOfBirth
  "employmentType": "FullTime",      // required
  "employmentStatus": "Probation",   // Active or Probation; default Probation
  "homeDepartmentCode": "EMS",       // required
  "initialAssignment": { "serviceId": 1, "roleTitle": "Primary Care Paramedic",
                         "startDate": "2026-11-02", "endDate": null }  // startDate defaults to hireDate
}
```

`201` → `StaffProfile`, `Location: /staff/STF-0000021` · `400` · `409` duplicate email · `422`

### `GET /staff/{staffNumber}`: profile

`200` → `StaffProfile` (assignments current-first, credentials soonest-expiry first) · `404`

### `PATCH /staff/{staffNumber}`: partial update

Any subset of: `firstName`, `lastName`, `preferredName`, `email`, `dateOfBirth`, `phone`, `citizenId`,
`hireDate`, `employmentType`, `employmentStatus`, `homeDepartmentCode`. Send `""` to clear `preferredName`,
`phone` or `citizenId`.

- `staffNumber` may be sent but must equal the current value; anything else is `422`.
- `employmentStatus: "Terminated"` is `422`: termination goes through a termination requisition.
- Changing `homeDepartmentCode` (a transfer) leaves the staff number unchanged.

`200` → `StaffProfile` · `400` · `404` · `409` email taken · `422`

### `GET /staff`: directory search

| Query | Meaning |
| --- | --- |
| `q` | Matches staff number, first/last/preferred name, full name, email |
| `department` | Home department code |
| `service` | serviceId; staff with a current assignment there |
| `role` | Text in the role title of a current assignment |
| `status` | EmploymentStatus |
| `credentialState` | Staff holding at least one credential in this CredentialStatus |
| `page`, `pageSize` | Default 1 and 20; pageSize 1–100 |

Sorted by last name, first name. `200` → `PagedResult<StaffSummary>` · `400` bad enum/number · `422` bad paging or unknown department

## Departments & services

### `GET /departments`

`200` → `[{ "code": "EMS", "name": "Emergency Services", "serviceCount": 3 }, …]`

### `GET /departments/{code}/services`

`200` → `[{ "serviceId": 1, "name": "Paramedic Service", "departmentCode": "EMS" }, …]` · `404`

### `GET /departments/headcount`

`200` → `[{ "code": "EMS", "name": "Emergency Services", "active": 7, "onDuty": 2, "vacancies": 2 }, …]`

- `active`: home-department staff with status Active or Probation.
- `onDuty`: distinct staff on a non-cancelled shift right now in the department's services.
- `vacancies`: `positions − positionsFilled` summed over Hire requisitions in Approved or Filling.

## Assignments

### `POST /staff/{staffNumber}/assignments`

```json
{ "serviceId": 19, "roleTitle": "Recruiter", "startDate": "2026-11-10", "endDate": null }
```

`serviceId` and `roleTitle` required; `startDate` defaults to today; `endDate` must be after `startDate`.
Terminated staff can't be assigned.
`201` → `Assignment` · `400` · `404` staff · `422`

### `GET /staff/{staffNumber}/assignments`

All assignments, newest first. `200` → `Assignment[]` · `404`

### `PATCH /assignments/{id}`: close an assignment

```json
{ "endDate": "2026-12-31" }
```

`endDate` required and after `startDate`. `200` → `Assignment` · `400` · `404` · `422`

## Rosters

### `GET /rosters?service=&from=&to=`

Shifts overlapping `[from, to)`. `from` defaults to midnight UTC today, `to` to `from` + 7 days, max 62 days.
`service` (serviceId) is optional. Sorted by `shiftStart`.
`200` → `RosterEntry[]` · `400` · `422` (`to` ≤ `from`, unknown service)

### `POST /rosters/entries`

```json
{
  "staffNumber": "STF-0000021",
  "serviceId": 1,
  "shiftStart": "2026-11-12T07:00:00Z",
  "shiftEnd": "2026-11-12T19:00:00Z",
  "shiftType": "Day",
  "status": "Scheduled"            // optional, default Scheduled
}
```

All fields except `status` required. `shiftEnd` must be after `shiftStart`, and a shift is at most 24 h.
Terminated staff can't be rostered. A shift that overlaps one of the person's non-cancelled shifts is `409`.
Back-to-back shifts (one ends exactly when the next starts) are allowed.
`201` → `RosterEntry` · `400` · `409` overlap · `422`

### `DELETE /rosters/entries/{id}`

`204` · `404`

### `GET /rosters/headcount?service=`

One row per service (all, or only `service`):
`200` → `[{ "serviceId": 1, "serviceName": "Paramedic Service", "departmentCode": "EMS", "active": 6, "onDuty": 2 }]` · `422`

- `active`: staff with a current assignment in the service and status Active or Probation.
- `onDuty`: distinct staff on a non-cancelled shift there right now.

### `GET /rosters/on-duty?service=&at=`

Non-cancelled shifts covering instant `at` (default now). `200` → `RosterEntry[]` · `400` · `422`

## Requisitions (Hire and Termination)

### `POST /requisitions`

Header **`Idempotency-Key: <unique value, e.g. a GUID>`** is required (max 100 characters).

Hire:

```json
{
  "type": "Hire",                   // optional, default Hire
  "departmentCode": "EMS",          // required
  "roleTitle": "Emergency Dispatcher",  // required
  "positions": 2,                   // required, 1–100
  "reasonCode": "GROWTH"
}
```

Termination:

```json
{
  "type": "Termination",
  "staffNumber": "STF-0000021",     // required; must not already be Terminated
  "reasonCode": "RESIGNATION",      // required, e.g. RESIGNATION, RETIREMENT, DISMISSAL, END_OF_CONTRACT
  "departmentCode": "EMS",          // optional, defaults to the employee's home department
  "roleTitle": "Paramedic"          // optional, defaults to the employee's current role
}
```

A person can have only one open termination at a time (a second one is `409`). New requisitions start as
`Received` with `budgetDecision: null` and get a number `REQ-0000000`.

| Case | Response |
| --- | --- |
| New key | `201` → `Requisition`, `Location: /requisitions/REQ-0000007` |
| Same key, same body (a retry) | `200` → the **original** `Requisition`, header `Idempotent-Replayed: true` |
| Same key, different body | `409` |
| Missing / blank key | `400` |
| Invalid body | `422` |

### `GET /requisitions?type=&status=&department=`

Newest first. `200` → `Requisition[]` · `400` bad enum · `422` unknown department

### `GET /requisitions/{number}`

`200` → `Requisition` · `404`

### `PATCH /requisitions/{number}`: record status / budget decision

```json
{ "status": "Approved", "budgetDecision": "Approved", "positionsFilled": 0 }
```

At least one field. Status moves allowed:

```text
Hire:        Received → HRReview → AwaitingBudget → Approved → Filling → Completed → Closed
             Received / HRReview / AwaitingBudget → Rejected
Termination: Received → HRReview → Approved → Completed → Closed
             Received / HRReview → Rejected
Both:        any status except Rejected / Closed → Closed
```

Hire:
- `Approved` requires `budgetDecision` `Approved` (already stored, or in the same request).
- `Completed` requires `positionsFilled == positions`.
- `positionsFilled` must be 0 … `positions`.

Termination (no `budgetDecision` or `positionsFilled`; sending either is `422`). Moving to **Completed** carries out
the termination in one transaction:
- employment status becomes `Terminated` (the staff number is kept and never reused);
- open assignments end today; assignments that had not started are removed;
- shifts that have not finished are `Cancelled`, so the person is no longer eligible for duty;
- a `Staff.Terminated` event is published.

`200` → `Requisition` · `400` · `404` · `422`

## Credentials

Status is derived on every read from `expiryDate` (UTC today): **Expired** if `expiryDate` is before today,
**Expiring** if within the next 30 days, otherwise **Valid**. A credential marked **Suspended** stays
Suspended whatever its date.

### `GET /credentials?state=&type=&department=&page=&pageSize=`

`state` CredentialStatus, `type` credentialTypeId, `department` holder's home department code.
Sorted by expiry date. `200` → `PagedResult<Credential>` · `400` · `422`

### `GET /credentials/types`

Every kind of credential, sorted by name. Use `credentialTypeId` when recording a credential or filtering
`GET /credentials?type=`.

`200` → `[{ "credentialTypeId": 1, "name": "Primary Care Paramedic Licence", "isDutyBlocking": true }, …]`

### `GET /credentials/expiring?withinDays=30`

Not-yet-expired, non-suspended credentials expiring within `withinDays` (0–365, default 30), soonest first.
`200` → `Credential[]` · `422`

### `POST /staff/{staffNumber}/credentials`

```json
{ "credentialTypeId": 1, "number": "FAKE-9001", "issueDate": "2026-11-01", "expiryDate": "2028-11-01" }
```

All required; `expiryDate` must be after `issueDate`. `201` → `Credential` · `400` · `404` staff · `422`

### `PATCH /credentials/{id}`: record a renewal

```json
{ "number": "FAKE-9002", "issueDate": "2026-11-15", "expiryDate": "2028-11-15", "suspended": false }
```

At least one field. `suspended: true` suspends; `false` lifts the suspension.
`200` → `Credential` · `400` · `404` · `422`

## Duty eligibility

### `GET /staff/{staffNumber}/eligibility?service=&at=`

Whether a staff member may work a shift. Emergency Services, Security and Transport call this before rostering
or dispatching someone. `service` (a serviceId) narrows the assignment check to that service; `at` (UTC) defaults to
now and its date is used for assignments and credentials.

```json
{
  "staffNumber": "STF-0000003", "eligible": false, "at": "2026-11-05T12:00:00Z", "serviceId": null,
  "reasons": [ { "code": "CREDENTIAL_EXPIRED", "message": "CPR & First Aid Certificate FAKE-1005 expired on 2026-10-31." } ],
  "warnings": []
}
```

| Reason code | Meaning |
| --- | --- |
| `EMPLOYMENT_STATUS` | Status is OnLeave, Suspended or Terminated |
| `NO_CURRENT_ASSIGNMENT` | No assignment covers the date (in `service`, if given) |
| `CREDENTIAL_EXPIRED` | A duty-blocking credential (see `GET /credentials/types`) has expired |
| `CREDENTIAL_SUSPENDED` | A duty-blocking credential is suspended |
| `SECURITY_FLAG_ACTIVE` | The Security service has an Active flag on the person |

Warning code `CREDENTIAL_EXPIRING`: a duty-blocking credential expires within 30 days. It does not block duty.

`200` · `400` unreadable `at` · `404` unknown staff · `422` unknown service

## Security flags (sync from the Security service)

The Security service pushes each flag change to Staff/HR. A flag is identified by staff member and flag type
(e.g. `UnderInvestigation`); an **Active** flag makes the person ineligible for duty.

### `PUT /staff/{staffNumber}/security-flags/{flagType}`

```json
{ "status": "Active", "updatedAt": "2026-11-04T09:00:00Z" }
```

`status` (`Active` | `Cleared`) is required. `updatedAt` is when Security changed the flag; it defaults to now.
`flagType` is 1–50 letters, digits, `.`, `-` or `_`.

| Case | Response | `Sync-Result` header |
| --- | --- | --- |
| New flag | `201` → `SecurityFlag` | `created` |
| Newer (or equal) `updatedAt` | `200` → `SecurityFlag` | `updated` |
| Older `updatedAt` than stored (a late message) | `200` → the stored flag, unchanged | `ignored-stale` |

Errors: `400` unknown status · `404` unknown staff · `422` invalid flag type or missing status.

`SecurityFlag` = `{ flagId, staffNumber, flagType, status, updatedAt }`

### `GET /staff/{staffNumber}/security-flags`

`200` → `SecurityFlag[]` · `404`

### `GET /security-flags?status=&flagType=`

Most recently changed first, e.g. `?status=Active` to reconcile with the Security service. `200` → `SecurityFlag[]` · `400`

## Audit

Every create, update and delete of a staff member, assignment, roster entry, requisition, credential,
security flag or compensation record writes an audit row in the same transaction. A rejected request writes none.
Seeded records show actor `system`.

### `GET /audit?entityType=&entityId=&actorId=&page=&pageSize=`

Newest first. `entityId` is the public id: a staff number, a requisition number or the numeric id.
`pageSize` 1–100, default 50.

`200` → `PagedResult<{ auditId, entityType, entityId, action, actorId, timestampUtc }>` · `400` · `422`

`action` is `Created`, `Updated` or `Deleted`.

## Integration events (Azure Service Bus)

Staff/HR publishes events to the Service Bus topic **`staff-events`**. Each message is:

| Message field | Value |
| --- | --- |
| `MessageId` | The event id (a GUID). Enable duplicate detection on your subscription: delivery is at-least-once. |
| `Subject` | The event type, e.g. `Staff.Onboarded`. Filter your subscription on it. |
| `ContentType` | `application/json`; the body is the payload below. |
| Application properties | `eventType`, `schemaVersion` (currently 1), `occurredAtUtc`, `source` = `staff-hr` |

Events are written to an outbox table in the same transaction as the change, then published in the order they
happened. An event therefore exists only if its change was committed.

### `Staff.Onboarded`

Sent when `POST /staff` succeeds. Contains no date of birth, citizen id or salary.

```json
{
  "staffNumber": "STF-0000021", "firstName": "Avery", "lastName": "Quillfeather", "displayName": "Ave Quillfeather",
  "email": "avery.quillfeather@cmes.example", "departmentCode": "EMS", "employmentType": "FullTime",
  "employmentStatus": "Probation", "hireDate": "2026-11-02", "serviceId": 1, "roleTitle": "Primary Care Paramedic"
}
```

`serviceId` and `roleTitle` are `null` when no initial assignment was given.

### `Staff.Terminated`

Sent when a termination requisition is completed. Remove the person from your active lists and rosters.

```json
{ "staffNumber": "STF-0000021", "departmentCode": "EMS", "effectiveDate": "2026-11-05",
  "requisitionNumber": "REQ-0000012", "cancelledShifts": 2 }
```

Configuration (section `Messaging`): set `ServiceBus:FullyQualifiedNamespace` (managed identity) or
`ServiceBus:ConnectionString`. With neither set, events are written to the application log instead.
