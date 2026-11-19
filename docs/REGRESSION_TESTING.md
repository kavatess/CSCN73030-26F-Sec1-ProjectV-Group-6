# Regression testing

The Staff/HR service has two automated test suites. Run both before every release.

| Suite | Tooling | Location |
| --- | --- | --- |
| Server (ASP.NET) | xUnit + `WebApplicationFactory` + Testcontainers (SQL Server) | `CSCN73030-26F-Sec1-ProjectV-Group-6.Server.Tests/` |
| Client (React) | Vitest + Testing Library | `cscn73030-26f-sec1-projectv-group-6.client/src/*.test.jsx` |

The server suite has 532 test cases: 22 for the original template sample and 510 for the Staff/HR backend
(116 unit cases and 394 API cases).

## Prerequisites

- .NET 10 SDK
- Node.js 20.19+ or 22.12+
- For the backend API tests, a SQL Server they can create a database on. Either:
  - **Docker** running (Docker Desktop on Windows/macOS). The tests start a SQL Server 2022 container
    themselves with Testcontainers and remove it afterwards. The first run downloads the image (about 1.5 GB).
  - **Or** an existing server named in the `STAFFHR_TEST_SQL` environment variable, for example LocalDB.
    The tests create a uniquely named database there and delete it at the end.

    PowerShell, for LocalDB:

    ```
    $env:STAFFHR_TEST_SQL = 'Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True'
    ```

## Running the tests

Server tests, from the repository root:

```
dotnet test CSCN73030-26F-Sec1-ProjectV-Group-6.Server.Tests --logger "console;verbosity=normal"
```

Only the unit tests (no database needed):

```
dotnet test CSCN73030-26F-Sec1-ProjectV-Group-6.Server.Tests --filter "FullyQualifiedName~.Unit."
```

### How the backend API tests work

`Api/StaffHrApiFactory.cs` boots the real application in memory against a fresh SQL Server database, applies the
`InitialCreate` migration and loads the normal demo seed data. The clock is pinned to **5 November 2026, 12:00 UTC**,
so seeded credential states, rosters and headcounts are the same on every run. All API test classes share that one
database and run one after another. Tests that write create their own records in the HRS department and never change
seeded ones, so the seeded figures other tests check (EMS, SEC, PTR, …) stay true. The seeded people the tests rely on
are listed in the `Seed` class in `Api/ApiTestBase.cs`.

Client tests:

```
cd cscn73030-26f-sec1-projectv-group-6.client
npm install
npm test
```

`npm run test:watch` re-runs the client tests as files change. In Visual Studio, the server tests also
appear in **Test > Test Explorer**.

Both commands list every test with its result, which is the evidence the Release Note asks for:
screenshot or copy the output into the "Record of Regression Testing" box.

CI runs the same commands (plus `npm run lint`) on every push: see `.github/workflows/regression-tests.yml`
and the repository's **Actions** tab.

## Test cases

Fill in the **Issue** column with the GitHub issue number when a test fails, so the Release Note can
trace each failure to a task.

### Server: template sample

| ID | Test | Protects | Issue |
| --- | --- | --- | --- |
| SRV-01 | `WeatherForecastTests.TemperatureF_AtFreezingPoint_Is32` | 0 °C converts to 32 °F | |
| SRV-02 | `WeatherForecastTests.TemperatureF_IsWithinOneDegreeOfExactConversion` (8 cases) | Celsius to Fahrenheit conversion stays accurate | |
| SRV-03 | `WeatherForecastTests.TemperatureF_RisesWithTemperatureC` | Fahrenheit never decreases as Celsius rises | |
| SRV-04 | `WeatherForecastTests.NewForecast_HasNoSummary` | `Summary` is optional | |
| SRV-05 | `WeatherForecastControllerTests.Get_ReturnsFiveForecasts` | The endpoint returns 5 forecasts | |
| SRV-06 | `WeatherForecastControllerTests.Get_ReturnsOneForecastPerDayStartingTomorrow` | Forecast dates are consecutive days from tomorrow | |
| SRV-07 | `WeatherForecastControllerTests.Get_TemperaturesStayWithinSupportedRange` | Temperatures stay between -20 and 54 °C | |
| SRV-08 | `WeatherForecastControllerTests.Get_EveryForecastHasASummary` | Every forecast has a summary | |
| SRV-09 | `WeatherForecastApiTests.Get_ReturnsOkWithJson` (2 cases) | `GET /WeatherForecast` and `/weatherforecast` return 200 JSON | |
| SRV-10 | `WeatherForecastApiTests.Get_ReturnsFiveForecastsWithTheFieldsTheClientReads` | JSON field names the React client depends on | |
| SRV-11 | `WeatherForecastApiTests.Get_ReturnsUniqueIsoDates` | Dates are unique `yyyy-MM-dd` strings (used as React row keys) | |
| SRV-12 | `WeatherForecastApiTests.Post_IsNotAllowed` | The endpoint is read-only (405 on POST) | |
| SRV-13 | `OpenApiDocumentTests.OpenApiDocument_IsServedInDevelopment` | `/openapi/v1.json` is available in Development | |
| SRV-14 | `OpenApiDocumentTests.OpenApiDocument_IsNotServedInProduction` | `/openapi/v1.json` is not exposed in Production | |

### Server: Staff/HR backend

Each row is one test method; **Cases** counts its data rows when it is a theory. IDs are permanent: a new test gets the next free ID, and a renamed or removed test retires its ID instead of passing it on, so IDs quoted in release notes stay valid.

Retired IDs (the test was renamed or replaced when Termination requisitions and payroll were added):

| Retired ID | Old test | Replaced by |
| --- | --- | --- |
| API-105 | `RequisitionsApiTests.Create_Termination_Returns422` | API-158 `RequisitionsApiTests.Create_TerminationWithoutStaffNumberOrReason_Returns422` |
| API-153 | `OpenApiContractTests.Document_NoSchemaExposesSalary` | API-161 `OpenApiContractTests.Document_SalaryOnlyInPayrollSchemas` |
| UNT-012 | `RequisitionWorkflowTests.CanMove_AllowsTheDocumentedMoves` | UNT-035 `RequisitionWorkflowTests.CanMove_Hire_AllowsTheDocumentedMoves` |
| UNT-014 | `RequisitionWorkflowTests.NextStatuses_IsEmptyForFinalStatuses` | UNT-038 `RequisitionWorkflowTests.FinalStatuses_HaveNoNextStatus` |
| UNT-016 | `RequisitionWorkflowTests.NextStatuses_IsDefinedForEveryStatus` | UNT-039 `RequisitionWorkflowTests.NextStatuses_IsDefinedForEveryStatusAndType` |

Where each rule and feature is tested:

| Rule / feature | Tests |
| --- | --- |
| 1. Staff number: STF-0000000, one sequence, immutable, never reused | UNT-001–004, API-008, API-009, API-028–030, API-037 |
| 2. Work email unique city-wide (409) | API-012, API-013, API-031, API-032, API-259 |
| 3. Idempotency-Key on POST /requisitions | API-096–104, API-108, API-204 |
| 4. No salary in staff/search DTOs; payroll role only | API-023, API-048, API-161, API-230, payroll rows of API-240 |
| 5. Credential status derived on read; Suspended overrides | UNT-005–011, API-127–129, API-138, API-145 |
| 6. Date ordering; no overlapping shifts | API-054, API-063, API-084–089, API-140, API-146 |
| Duty eligibility | DutyEligibilityRulesTests (UNT-040–UNT-049), EligibilityApiTests (API-191–API-202) |
| Termination requisitions | TerminationApiTests (API-203–API-221), workflow UNT-013–UNT-039 |
| Security flag sync | SecurityFlagsApiTests (API-178–API-190) |
| Audit log | AuditApiTests (API-162–API-170) |
| Integration events (outbox, Service Bus) | IntegrationEventsApiTests (API-171–API-177), ServiceBusMessageTests (UNT-050–UNT-052) |
| Authentication and roles | AuthorizationApiTests (API-237–API-243), JwtAuthenticationTests (API-244–API-250), AuthSetupTests (UNT-053–UNT-056) |
| Department scope (EF query filters) | DepartmentScopeApiTests (API-251–API-259) |
| Health, metrics, configuration | HealthApiTests (API-001–API-157), MetricsApiTests (API-260–API-262), ObservabilitySetupTests (UNT-057–UNT-059) |

#### Unit tests (no database)

**StaffNumberTests**: Staff numbers are STF- plus 7 zero-padded digits.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-001 | `StaffNumberTests.Format_PadsSequenceValueToSevenDigits` | 4 | Format: pads sequence value to seven digits | |
| UNT-002 | `StaffNumberTests.Format_AlwaysProducesElevenCharacters` | 1 | Format: always produces eleven characters | |
| UNT-003 | `StaffNumberTests.IsWellFormed_AcceptsTheStandardFormat` | 2 | IsWellFormed: accepts the standard format | |
| UNT-004 | `StaffNumberTests.IsWellFormed_RejectsAnythingElse` | 9 | IsWellFormed: rejects anything else | |

**CredentialStatusRulesTests**: Expired if past, Expiring within 30 days, otherwise Valid; Suspended overrides.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-005 | `CredentialStatusRulesTests.Derive_UsesExpiryDateRelativeToToday` | 7 | Derive: uses expiry date relative to today | |
| UNT-006 | `CredentialStatusRulesTests.Derive_SuspendedOverridesTheDate` | 3 | Derive: suspended overrides the date | |
| UNT-007 | `CredentialStatusRulesTests.Derive_IgnoresAStaleStoredStatus` | 3 | Derive: ignores a stale stored status | |
| UNT-008 | `CredentialStatusRulesTests.Derive_ForACredential_MatchesTheValueOverload` | 1 | Derive: for a credential → matches the value overload | |
| UNT-009 | `CredentialStatusRulesTests.IsInState_AgreesWithDerive_ForEveryStateAndBoundary` | 1 | IsInState: agrees with derive → for every state and boundary | |
| UNT-010 | `CredentialStatusRulesTests.Worst_PicksTheMostUrgentStatus` | 1 | Worst: picks the most urgent status | |
| UNT-011 | `CredentialStatusRulesTests.Worst_ReturnsNullWhenNoCredentialsAreHeld` | 1 | Worst: returns null when no credentials are held | |

**RequisitionWorkflowTests**: Hire: Received → HRReview → AwaitingBudget → Approved → Filling → Completed → Closed. Termination: Received → HRReview → Approved → Completed → Closed. Both have Rejected and Closed exits.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-013 | `RequisitionWorkflowTests.CanMove_RejectsEveryOtherMove` | 2 | CanMove: rejects every other move | |
| UNT-015 | `RequisitionWorkflowTests.NextStatuses_ListsTheAllowedMovesFromReceived` | 1 | NextStatuses: lists the allowed moves from received | |
| UNT-035 | `RequisitionWorkflowTests.CanMove_Hire_AllowsTheDocumentedMoves` | 14 | CanMove: hire → allows the documented moves | |
| UNT-036 | `RequisitionWorkflowTests.CanMove_Termination_AllowsTheDocumentedMoves` | 9 | CanMove: termination → allows the documented moves | |
| UNT-037 | `RequisitionWorkflowTests.Termination_SkipsTheBudgetStep` | 1 | Termination: skips the budget step | |
| UNT-038 | `RequisitionWorkflowTests.FinalStatuses_HaveNoNextStatus` | 2 | FinalStatuses: have no next status | |
| UNT-039 | `RequisitionWorkflowTests.NextStatuses_IsDefinedForEveryStatusAndType` | 1 | NextStatuses: is defined for every status and type | |

**ValidationErrorsTests**: ValidationErrors collects every field problem, then throws one 422.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-017 | `ValidationErrorsTests.ThrowIfAny_DoesNothingWhenThereAreNoErrors` | 1 | ThrowIfAny: does nothing when there are no errors | |
| UNT-018 | `ValidationErrorsTests.ThrowIfAny_ThrowsOne422WithEveryField` | 1 | ThrowIfAny: throws one 422 with every field | |
| UNT-019 | `ValidationErrorsTests.Required_RejectsMissingOrBlankText` | 3 | Required: rejects missing or blank text | |
| UNT-020 | `ValidationErrorsTests.Required_RejectsAMissingValueType` | 1 | Required: rejects a missing value type | |
| UNT-021 | `ValidationErrorsTests.Required_ChecksMaxLength` | 1 | Required: checks max length | |
| UNT-022 | `ValidationErrorsTests.MaxLength_IgnoresSurroundingWhitespaceAndNull` | 1 | MaxLength: ignores surrounding whitespace and null | |
| UNT-023 | `ValidationErrorsTests.NotBlankIfPresent_AllowsNullButNotBlank` | 1 | NotBlankIfPresent: allows null but not blank | |
| UNT-024 | `ValidationErrorsTests.Email_AcceptsAPlainAddress` | 2 | Email: accepts a plain address | |
| UNT-025 | `ValidationErrorsTests.Email_RejectsAnythingButOneAddress` | 3 | Email: rejects anything but one address | |
| UNT-026 | `ValidationErrorsTests.AddIf_OnlyAddsWhenTheConditionHolds` | 1 | AddIf: only adds when the condition holds | |
| UNT-027 | `ValidationErrorsTests.Text_TrimToNullAndNormaliseEmail` | 1 | Text: trim to null and normalise email | |

**DomainTests**: Clock, Staff.DisplayName, StaffAssignment.IsCurrentOn and the API exception types.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-028 | `DomainTests.Clock_ReadsUtcNowAndTodayFromTheTimeProvider` | 1 | Clock: reads UTC now and today from the time provider | |
| UNT-029 | `DomainTests.Clock_AsUtc_TreatsATimeWithoutOffsetAsUtc` | 1 | Clock: as UTC → treats a time without offset as UTC | |
| UNT-030 | `DomainTests.Clock_AsUtc_ConvertsLocalTimeAndKeepsUtc` | 1 | Clock: as UTC → converts local time and keeps UTC | |
| UNT-031 | `DomainTests.Staff_DisplayName_PrefersThePreferredName` | 2 | Staff: display name → prefers the preferred name | |
| UNT-032 | `DomainTests.StaffAssignment_IsCurrentOn_IsInclusiveAtBothEnds` | 5 | StaffAssignment: is current on → is inclusive at both ends | |
| UNT-033 | `DomainTests.ApiExceptions_CarryTheirStatusCodes` | 1 | ApiExceptions: carry their status codes | |
| UNT-034 | `DomainTests.NotFoundException_NamesTheResourceAndKey` | 1 | NotFoundException: names the resource and key | |

**DutyEligibilityRulesTests**: Status, current assignment, duty-blocking credentials and security flags.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-040 | `DutyEligibilityRulesTests.ActiveStaffWithCurrentAssignmentAndValidCredentials_IsEligible` | 1 | ActiveStaffWithCurrentAssignmentAndValidCredentials: is eligible | |
| UNT-041 | `DutyEligibilityRulesTests.EmploymentStatus_OnlyActiveAndProbationMayWork` | 5 | EmploymentStatus: only active and probation may work | |
| UNT-042 | `DutyEligibilityRulesTests.NoAssignmentCoveringTheDay_IsIneligible` | 1 | NoAssignmentCoveringTheDay: is ineligible | |
| UNT-043 | `DutyEligibilityRulesTests.AssignmentInAnotherService_IsIneligibleForThatService` | 1 | AssignmentInAnotherService: is ineligible for that service | |
| UNT-044 | `DutyEligibilityRulesTests.ExpiredDutyBlockingCredential_IsIneligible` | 1 | ExpiredDutyBlockingCredential: is ineligible | |
| UNT-045 | `DutyEligibilityRulesTests.SuspendedDutyBlockingCredential_IsIneligible` | 1 | SuspendedDutyBlockingCredential: is ineligible | |
| UNT-046 | `DutyEligibilityRulesTests.ExpiringDutyBlockingCredential_IsOnlyAWarning` | 1 | ExpiringDutyBlockingCredential: is only a warning | |
| UNT-047 | `DutyEligibilityRulesTests.ExpiredCredentialThatDoesNotBlockDuty_IsIgnored` | 1 | ExpiredCredentialThatDoesNotBlockDuty: is ignored | |
| UNT-048 | `DutyEligibilityRulesTests.ActiveSecurityFlag_IsIneligible_ClearedIsNot` | 1 | ActiveSecurityFlag: is ineligible → cleared is not | |
| UNT-049 | `DutyEligibilityRulesTests.EveryProblemIsReported` | 1 | Every problem is reported | |

**ServiceBusMessageTests**: Message id, subject, body and properties of a published event.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-050 | `ServiceBusMessageTests.ToMessage_UsesTheEventIdForDuplicateDetection` | 1 | ToMessage: uses the event id for duplicate detection | |
| UNT-051 | `ServiceBusMessageTests.ToMessage_CarriesTypeVersionAndPayload` | 1 | ToMessage: carries type version and payload | |
| UNT-052 | `ServiceBusMessageTests.MessagingOptions_AreConfiguredByConnectionStringOrNamespace` | 1 | MessagingOptions: are configured by connection string or namespace | |

**AuthSetupTests**: Development mode must never be usable outside the Development environment.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-053 | `AuthSetupTests.DevelopmentMode_OutsideDevelopment_RefusesToStart` | 2 | DevelopmentMode: outside development → refuses to start | |
| UNT-054 | `AuthSetupTests.DevelopmentMode_InDevelopment_IsAllowed` | 1 | DevelopmentMode: in development → is allowed | |
| UNT-055 | `AuthSetupTests.EntraIdMode_IsAllowedEverywhere` | 1 | EntraIdMode: is allowed everywhere | |
| UNT-056 | `AuthSetupTests.DefaultMode_IsEntraId` | 1 | DefaultMode: is entra id | |

**ObservabilitySetupTests**: Local runs need no Azure resources.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| UNT-057 | `ObservabilitySetupTests.KeyVault_IsSkippedWithoutAUri` | 1 | KeyVault: is skipped without a uri | |
| UNT-058 | `ObservabilitySetupTests.Telemetry_IsLocalOnlyWithoutAConnectionString` | 1 | Telemetry: is local only without a connection string | |
| UNT-059 | `ObservabilitySetupTests.Telemetry_ExportsToAzureMonitorWhenConfigured` | 1 | Telemetry: exports to azure monitor when configured | |

#### API tests (real app + SQL Server)

**HealthApiTests**: GET /health reports the service, its database and the integration-event backlog.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-001 | `HealthApiTests.Health_WithDatabaseUp_Returns200Healthy` | 1 | Health: with database up → returns 200 healthy | |
| API-157 | `HealthApiTests.Health_WithEventsStuckInTheOutbox_IsDegradedButStill200` | 1 | Health: with events stuck in the outbox → is degraded but still 200 | |

**DepartmentsApiTests**: Departments, their services and per-department headcount.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-002 | `DepartmentsApiTests.List_ReturnsTheSevenDepartmentsSortedByCode` | 1 | List: returns the seven departments sorted by code | |
| API-003 | `DepartmentsApiTests.Services_ReturnsTheDepartmentsServicesSortedByName` | 1 | Services: returns the departments services sorted by name | |
| API-004 | `DepartmentsApiTests.Services_DepartmentCodeIsCaseInsensitive` | 1 | Services: department code is case insensitive | |
| API-005 | `DepartmentsApiTests.Services_UnknownDepartment_Returns404Problem` | 1 | Services: unknown department → returns 404 problem | |
| API-006 | `DepartmentsApiTests.Headcount_CountsActiveOnDutyAndVacanciesFromSeedData` | 1 | Headcount: counts active on duty and vacancies from seed data | |

**StaffApiTests**: Onboarding, profile and partial update of employees.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-007 | `StaffApiTests.Create_ValidRequest_Returns201WithLocationAndProfile` | 1 | Create: valid request → returns 201 with location and profile | |
| API-008 | `StaffApiTests.Create_IssuesAWellFormedStaffNumber` | 1 | Create: issues a well formed staff number | |
| API-009 | `StaffApiTests.Create_StaffNumbersComeFromOneIncreasingSequence` | 1 | Create: staff numbers come from one increasing sequence | |
| API-010 | `StaffApiTests.Create_WithInitialAssignment_CreatesItInTheSameRequest` | 1 | Create: with initial assignment → creates it in the same request | |
| API-011 | `StaffApiTests.Create_AcceptsActiveAsTheInitialStatus` | 1 | Create: accepts active as the initial status | |
| API-012 | `StaffApiTests.Create_DuplicateEmailInAnyCase_Returns409` | 1 | Create: duplicate email in any case → returns 409 | |
| API-013 | `StaffApiTests.Create_SeededEmail_Returns409` | 1 | Create: seeded email → returns 409 | |
| API-014 | `StaffApiTests.Create_MissingRequiredFields_Returns422ListingEachField` | 1 | Create: missing required fields → returns 422 listing each field | |
| API-015 | `StaffApiTests.Create_InvalidValues_Returns422ForEachField` | 1 | Create: invalid values → returns 422 for each field | |
| API-016 | `StaffApiTests.Create_HireDateLessThan16YearsAfterBirth_Returns422` | 1 | Create: hire date less than 16 years after birth → returns 422 | |
| API-017 | `StaffApiTests.Create_InvalidInitialAssignment_Returns422WithNestedFieldNames` | 1 | Create: invalid initial assignment → returns 422 with nested field names | |
| API-018 | `StaffApiTests.Create_RejectedRequest_DoesNotCreateAnyone` | 1 | Create: rejected request → does not create anyone | |
| API-019 | `StaffApiTests.Create_MalformedBody_Returns400` | 4 | Create: malformed body → returns 400 | |
| API-020 | `StaffApiTests.Create_EmptyBody_Returns400` | 1 | Create: empty body → returns 400 | |
| API-021 | `StaffApiTests.Create_WrongTypeInBody_NamesTheJsonPath` | 1 | Create: wrong type in body → names the JSON path | |
| API-022 | `StaffApiTests.Get_SeededEmployee_ReturnsFullProfile` | 1 | Get: seeded employee → returns full profile | |
| API-023 | `StaffApiTests.Get_NeverReturnsSalary` | 1 | Get: never returns salary | |
| API-024 | `StaffApiTests.Get_UsesCamelCaseAndStringEnums` | 1 | Get: uses camel case and string enums | |
| API-025 | `StaffApiTests.Get_UnknownStaffNumber_Returns404` | 1 | Get: unknown staff number → returns 404 | |
| API-026 | `StaffApiTests.Patch_ChangesOnlyTheFieldsSent` | 1 | Patch: changes only the fields sent | |
| API-027 | `StaffApiTests.Patch_EmptyStringClearsOptionalFields` | 1 | Patch: empty string clears optional fields | |
| API-028 | `StaffApiTests.Patch_DifferentStaffNumber_Returns422AndKeepsTheNumber` | 1 | Patch: different staff number → returns 422 and keeps the number | |
| API-029 | `StaffApiTests.Patch_SameStaffNumber_IsAccepted` | 1 | Patch: same staff number → is accepted | |
| API-030 | `StaffApiTests.Patch_TransferToAnotherDepartment_KeepsTheStaffNumber` | 1 | Patch: transfer to another department → keeps the staff number | |
| API-031 | `StaffApiTests.Patch_EmailAlreadyUsed_Returns409` | 1 | Patch: email already used → returns 409 | |
| API-032 | `StaffApiTests.Patch_OwnEmailInAnotherCase_IsAccepted` | 1 | Patch: own email in another case → is accepted | |
| API-033 | `StaffApiTests.Patch_ToTerminated_Returns422` | 1 | Patch: to terminated → returns 422 | |
| API-034 | `StaffApiTests.Patch_InvalidValues_Returns422` | 1 | Patch: invalid values → returns 422 | |
| API-035 | `StaffApiTests.Patch_UnknownStaffNumber_Returns404` | 1 | Patch: unknown staff number → returns 404 | |
| API-036 | `StaffApiTests.Patch_MalformedBody_Returns400` | 1 | Patch: malformed body → returns 400 | |
| API-037 | `StaffApiTests.StaffNumber_CannotBeChangedEvenThroughEfCore` | 1 | StaffNumber: cannot be changed even through EF core | |

**StaffSearchApiTests**: Directory search: filters, paging and validation. Asserts on EMS/SEC/PTR, which no test writes to.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-038 | `StaffSearchApiTests.Search_ReturnsAPagedEnvelopeSortedByLastName` | 1 | Search: returns a paged envelope sorted by last name | |
| API-039 | `StaffSearchApiTests.Search_DefaultsToPage1Of20` | 1 | Search: defaults to page 1 of 20 | |
| API-040 | `StaffSearchApiTests.Search_TextMatchesNumberNamesAndEmail` | 5 | Search: text matches number names and email | |
| API-041 | `StaffSearchApiTests.Search_ByStatus` | 1 | Search: by status | |
| API-042 | `StaffSearchApiTests.Search_ByServiceMatchesCurrentAssignmentsOnly` | 1 | Search: by service matches current assignments only | |
| API-043 | `StaffSearchApiTests.Search_ByRoleMatchesCurrentRoleTitles` | 1 | Search: by role matches current role titles | |
| API-044 | `StaffSearchApiTests.Search_ByRole_IgnoresEndedAssignments` | 1 | Search: by role → ignores ended assignments | |
| API-045 | `StaffSearchApiTests.Search_ByCredentialState` | 1 | Search: by credential state | |
| API-046 | `StaffSearchApiTests.Search_FiltersCombine` | 1 | Search: filters combine | |
| API-047 | `StaffSearchApiTests.Search_SummaryRowShowsCurrentRoleAndWorstCredential` | 1 | Search: summary row shows current role and worst credential | |
| API-048 | `StaffSearchApiTests.Search_NeverReturnsSalary` | 1 | Search: never returns salary | |
| API-049 | `StaffSearchApiTests.Search_InvalidFilter_Returns422` | 4 | Search: invalid filter → returns 422 | |
| API-050 | `StaffSearchApiTests.Search_UnreadableQueryValue_Returns400WithCamelCaseField` | 3 | Search: unreadable query value → returns 400 with camel case field | |

**AssignmentsApiTests**: POST/GET /staff/{staffNumber}/assignments and PATCH /assignments/{id}.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-051 | `AssignmentsApiTests.Create_ValidRequest_Returns201` | 1 | Create: valid request → returns 201 | |
| API-052 | `AssignmentsApiTests.Create_WithoutStartDate_StartsToday` | 1 | Create: without start date → starts today | |
| API-053 | `AssignmentsApiTests.Create_FutureAssignment_IsNotCurrent` | 1 | Create: future assignment → is not current | |
| API-054 | `AssignmentsApiTests.Create_EndDateNotAfterStartDate_Returns422` | 2 | Create: end date not after start date → returns 422 | |
| API-055 | `AssignmentsApiTests.Create_MissingFields_Returns422` | 1 | Create: missing fields → returns 422 | |
| API-056 | `AssignmentsApiTests.Create_UnknownService_Returns422` | 1 | Create: unknown service → returns 422 | |
| API-057 | `AssignmentsApiTests.Create_ForTerminatedEmployee_Returns422` | 1 | Create: for terminated employee → returns 422 | |
| API-058 | `AssignmentsApiTests.Create_UnknownStaff_Returns404` | 1 | Create: unknown staff → returns 404 | |
| API-059 | `AssignmentsApiTests.Create_MalformedBody_Returns400` | 1 | Create: malformed body → returns 400 | |
| API-060 | `AssignmentsApiTests.List_ReturnsAllAssignmentsNewestFirst` | 1 | List: returns all assignments newest first | |
| API-061 | `AssignmentsApiTests.List_UnknownStaff_Returns404` | 1 | List: unknown staff → returns 404 | |
| API-062 | `AssignmentsApiTests.Patch_SetsEndDateToCloseTheAssignment` | 1 | Patch: sets end date to close the assignment | |
| API-063 | `AssignmentsApiTests.Patch_EndDateNotAfterStartDate_Returns422` | 1 | Patch: end date not after start date → returns 422 | |
| API-064 | `AssignmentsApiTests.Patch_WithoutEndDate_Returns422` | 1 | Patch: without end date → returns 422 | |
| API-065 | `AssignmentsApiTests.Patch_UnknownAssignment_Returns404` | 1 | Patch: unknown assignment → returns 404 | |

**RostersApiTests**: Duty roster. The seed rosters the Paramedic Service from 4 to 10 November 2026 in 12-hour shifts (07:00 and 19:00 UTC); at the test instant (5 Nov, 12:00) Desmond and Priya are on duty. Write tests roster new HRS staff in December so seeded counts never change.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-066 | `RostersApiTests.List_DefaultsToTheSevenDaysFromMidnightToday` | 1 | List: defaults to the seven days from midnight today | |
| API-067 | `RostersApiTests.List_ExplicitWindowReturnsOverlappingShifts` | 1 | List: explicit window returns overlapping shifts | |
| API-068 | `RostersApiTests.List_SeedShowsCompletedConfirmedScheduledAndCancelledShifts` | 1 | List: seed shows completed confirmed scheduled and cancelled shifts | |
| API-069 | `RostersApiTests.List_ToNotAfterFrom_Returns422` | 1 | List: to not after from → returns 422 | |
| API-070 | `RostersApiTests.List_WindowLongerThan62Days_Returns422` | 1 | List: window longer than 62 days → returns 422 | |
| API-071 | `RostersApiTests.List_UnknownService_Returns422` | 1 | List: unknown service → returns 422 | |
| API-072 | `RostersApiTests.List_UnreadableDate_Returns400` | 1 | List: unreadable date → returns 400 | |
| API-073 | `RostersApiTests.OnDuty_DefaultsToNow` | 1 | OnDuty: defaults to now | |
| API-074 | `RostersApiTests.OnDuty_AtAGivenInstant` | 1 | OnDuty: at a given instant | |
| API-075 | `RostersApiTests.OnDuty_ShiftEndIsExclusive` | 1 | OnDuty: shift end is exclusive | |
| API-076 | `RostersApiTests.OnDuty_ExcludesCancelledShifts` | 1 | OnDuty: excludes cancelled shifts | |
| API-077 | `RostersApiTests.OnDuty_UnknownService_Returns422` | 1 | OnDuty: unknown service → returns 422 | |
| API-078 | `RostersApiTests.Headcount_ForOneService` | 1 | Headcount: for one service | |
| API-079 | `RostersApiTests.Headcount_ForEveryService` | 1 | Headcount: for every service | |
| API-080 | `RostersApiTests.Headcount_UnknownService_Returns422` | 1 | Headcount: unknown service → returns 422 | |
| API-081 | `RostersApiTests.Create_ValidShift_Returns201AsScheduled` | 1 | Create: valid shift → returns 201 as scheduled | |
| API-082 | `RostersApiTests.Create_TimeWithOffset_IsStoredAsUtc` | 1 | Create: time with offset → is stored as UTC | |
| API-083 | `RostersApiTests.Create_TimeWithoutOffset_IsReadAsUtc` | 1 | Create: time without offset → is read as UTC | |
| API-084 | `RostersApiTests.Create_OverlappingShift_Returns409AndAddsNothing` | 1 | Create: overlapping shift → returns 409 and adds nothing | |
| API-085 | `RostersApiTests.Create_AnyKindOfOverlap_Returns409` | 4 | Create: any kind of overlap → returns 409 | |
| API-086 | `RostersApiTests.Create_BackToBackShifts_AreAllowed` | 1 | Create: back to back shifts → are allowed | |
| API-087 | `RostersApiTests.Create_CancelledShiftDoesNotBlockANewOne` | 1 | Create: cancelled shift does not block a new one | |
| API-088 | `RostersApiTests.Create_OverlapForADifferentPerson_IsAllowed` | 1 | Create: overlap for a different person → is allowed | |
| API-089 | `RostersApiTests.Create_EndNotAfterStart_Returns422` | 2 | Create: end not after start → returns 422 | |
| API-090 | `RostersApiTests.Create_ShiftLongerThan24Hours_Returns422` | 1 | Create: shift longer than 24 hours → returns 422 | |
| API-091 | `RostersApiTests.Create_MissingFields_Returns422` | 1 | Create: missing fields → returns 422 | |
| API-092 | `RostersApiTests.Create_UnknownStaffOrService_Returns422` | 1 | Create: unknown staff or service → returns 422 | |
| API-093 | `RostersApiTests.Create_TerminatedEmployee_Returns422` | 1 | Create: terminated employee → returns 422 | |
| API-094 | `RostersApiTests.Create_UnknownShiftType_Returns400` | 1 | Create: unknown shift type → returns 400 | |
| API-095 | `RostersApiTests.Delete_RemovesTheShift_ThenReturns404` | 1 | Delete: removes the shift → then returns 404 | |

**RequisitionsApiTests**: Hire requisitions. Write tests raise their own HRS requisitions with fresh keys.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-096 | `RequisitionsApiTests.Create_NewKey_Returns201AsReceived` | 1 | Create: new key → returns 201 as received | |
| API-097 | `RequisitionsApiTests.Create_ReplayedKeyWithSameBody_Returns200WithTheOriginal` | 1 | Create: replayed key with same body → returns 200 with the original | |
| API-098 | `RequisitionsApiTests.Create_ReplayIgnoresCaseOfTheDepartmentCode` | 1 | Create: replay ignores case of the department code | |
| API-099 | `RequisitionsApiTests.Create_ReplayedKey_DoesNotCreateASecondRequisition` | 1 | Create: replayed key → does not create a second requisition | |
| API-100 | `RequisitionsApiTests.Create_SameKeyAtTheSameTime_CreatesOnlyOne` | 1 | Create: same key at the same time → creates only one | |
| API-101 | `RequisitionsApiTests.Create_ReplayedKeyWithDifferentBody_Returns409` | 1 | Create: replayed key with different body → returns 409 | |
| API-102 | `RequisitionsApiTests.Create_SeededKeyIsAlsoProtected` | 1 | Create: seeded key is also protected | |
| API-103 | `RequisitionsApiTests.Create_MissingOrBlankIdempotencyKey_Returns400` | 3 | Create: missing or blank idempotency key → returns 400 | |
| API-104 | `RequisitionsApiTests.Create_IdempotencyKeyOver100Characters_Returns400` | 1 | Create: idempotency key over 100 characters → returns 400 | |
| API-106 | `RequisitionsApiTests.Create_PositionsOutOfRange_Returns422` | 2 | Create: positions out of range → returns 422 | |
| API-107 | `RequisitionsApiTests.Create_MissingFieldsOrUnknownDepartment_Returns422` | 1 | Create: missing fields or unknown department → returns 422 | |
| API-108 | `RequisitionsApiTests.Create_InvalidRequest_DoesNotUseUpTheKey` | 1 | Create: invalid request → does not use up the key | |
| API-109 | `RequisitionsApiTests.List_FiltersByTypeStatusAndDepartment` | 1 | List: filters by type status and department | |
| API-110 | `RequisitionsApiTests.List_SeedHasOneRequisitionInEachOfSixStatuses` | 1 | List: seed has one requisition in each of six statuses | |
| API-111 | `RequisitionsApiTests.List_IsNewestFirst` | 1 | List: is newest first | |
| API-112 | `RequisitionsApiTests.List_UnknownDepartment_Returns422` | 1 | List: unknown department → returns 422 | |
| API-113 | `RequisitionsApiTests.List_UnknownStatus_Returns400` | 1 | List: unknown status → returns 400 | |
| API-114 | `RequisitionsApiTests.Get_ByNumberInAnyCase` | 1 | Get: by number in any case | |
| API-115 | `RequisitionsApiTests.Get_UnknownNumber_Returns404` | 1 | Get: unknown number → returns 404 | |
| API-116 | `RequisitionsApiTests.Patch_FollowsTheWholeWorkflowToClosed` | 1 | Patch: follows the whole workflow to closed | |
| API-117 | `RequisitionsApiTests.Patch_SkippingAStep_Returns422ListingAllowedMoves` | 1 | Patch: skipping a step → returns 422 listing allowed moves | |
| API-118 | `RequisitionsApiTests.Patch_ApprovedWithoutBudgetApproval_Returns422` | 1 | Patch: approved without budget approval → returns 422 | |
| API-119 | `RequisitionsApiTests.Patch_CompletedBeforeEveryPositionIsFilled_Returns422` | 1 | Patch: completed before every position is filled → returns 422 | |
| API-120 | `RequisitionsApiTests.Patch_FinalStatus_CannotChange` | 2 | Patch: final status → cannot change | |
| API-121 | `RequisitionsApiTests.Patch_SameStatusAgain_IsAccepted` | 1 | Patch: same status again → is accepted | |
| API-122 | `RequisitionsApiTests.Patch_PositionsFilledOutOfRange_Returns422` | 2 | Patch: positions filled out of range → returns 422 | |
| API-123 | `RequisitionsApiTests.Patch_EmptyBody_Returns422` | 1 | Patch: empty body → returns 422 | |
| API-124 | `RequisitionsApiTests.Patch_UnknownNumber_Returns404` | 1 | Patch: unknown number → returns 404 | |
| API-125 | `RequisitionsApiTests.Patch_UnknownStatusValue_Returns400` | 1 | Patch: unknown status value → returns 400 | |
| API-126 | `RequisitionsApiTests.Vacancies_CountApprovedAndFillingRequisitionsOnly` | 1 | Vacancies: count approved and filling requisitions only | |
| API-158 | `RequisitionsApiTests.Create_TerminationWithoutStaffNumberOrReason_Returns422` | 1 | Create: termination without staff number or reason → returns 422 | |

**CredentialsApiTests**: Credentials. Status is derived from the expiry date on every read (test day: 5 Nov 2026). Assertions on seeded counts filter by EMS or SEC; write tests use new HRS staff.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-127 | `CredentialsApiTests.Search_ByDerivedState_InEms` | 4 | Search: by derived state → in EMS | |
| API-128 | `CredentialsApiTests.Search_Suspended_FindsNoorsCertificate` | 1 | Search: suspended → finds noors certificate | |
| API-129 | `CredentialsApiTests.Search_EveryStatusMatchesItsExpiryDate` | 1 | Search: every status matches its expiry date | |
| API-130 | `CredentialsApiTests.Search_IsPagedAndSortedBySoonestExpiry` | 1 | Search: is paged and sorted by soonest expiry | |
| API-131 | `CredentialsApiTests.Search_ByType` | 1 | Search: by type | |
| API-132 | `CredentialsApiTests.Search_InvalidFilter_Returns422` | 3 | Search: invalid filter → returns 422 | |
| API-133 | `CredentialsApiTests.Search_UnknownState_Returns400` | 1 | Search: unknown state → returns 400 | |
| API-134 | `CredentialsApiTests.Expiring_DefaultsTo30DaysSoonestFirst` | 1 | Expiring: defaults to 30 days soonest first | |
| API-135 | `CredentialsApiTests.Expiring_WithinDaysNarrowsTheWindow` | 1 | Expiring: within days narrows the window | |
| API-136 | `CredentialsApiTests.Expiring_ExcludesExpiredAndSuspended` | 1 | Expiring: excludes expired and suspended | |
| API-137 | `CredentialsApiTests.Expiring_WithinDaysOutOfRange_Returns422` | 2 | Expiring: within days out of range → returns 422 | |
| API-138 | `CredentialsApiTests.Create_StatusIsDerivedFromTheExpiryDate` | 3 | Create: status is derived from the expiry date | |
| API-139 | `CredentialsApiTests.Create_ShowsOnTheProfileAndSetsTheWorstState` | 1 | Create: shows on the profile and sets the worst state | |
| API-140 | `CredentialsApiTests.Create_ExpiryNotAfterIssue_Returns422` | 2 | Create: expiry not after issue → returns 422 | |
| API-141 | `CredentialsApiTests.Create_MissingFieldsOrUnknownType_Returns422` | 1 | Create: missing fields or unknown type → returns 422 | |
| API-142 | `CredentialsApiTests.Create_UnknownStaff_Returns404` | 1 | Create: unknown staff → returns 404 | |
| API-143 | `CredentialsApiTests.Create_MalformedDate_Returns400` | 1 | Create: malformed date → returns 400 | |
| API-144 | `CredentialsApiTests.Patch_Renewal_UpdatesNumberDatesAndStatus` | 1 | Patch: renewal → updates number dates and status | |
| API-145 | `CredentialsApiTests.Patch_Suspend_OverridesTheDate_AndLiftingRestoresIt` | 1 | Patch: suspend → overrides the date → and lifting restores it | |
| API-146 | `CredentialsApiTests.Patch_ExpiryBeforeIssue_Returns422` | 1 | Patch: expiry before issue → returns 422 | |
| API-147 | `CredentialsApiTests.Patch_EmptyBodyOrBlankNumber_Returns422` | 1 | Patch: empty body or blank number → returns 422 | |
| API-148 | `CredentialsApiTests.Patch_UnknownCredential_Returns404` | 1 | Patch: unknown credential → returns 404 | |
| API-159 | `CredentialsApiTests.Types_ListsTheTwelveSeededTypesByName` | 1 | Types: lists the twelve seeded types by name | |
| API-160 | `CredentialsApiTests.Types_IdsWorkForCreatingACredential` | 1 | Types: ids work for creating a credential | |

**OpenApiContractTests**: The OpenAPI document other groups review, and the Swagger UI over it.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-149 | `OpenApiContractTests.Document_ListsEveryEndpoint` | 33 | Document: lists every endpoint | |
| API-150 | `OpenApiContractTests.Document_ErrorResponsesAreProblemJsonWithAnExample` | 33 | Document: error responses are problem JSON with an example | |
| API-151 | `OpenApiContractTests.Document_DescribesTheKeyErrorCases` | 8 | Document: describes the key error cases | |
| API-152 | `OpenApiContractTests.Document_IdempotencyKeyHeaderIsRequired` | 1 | Document: idempotency key header is required | |
| API-154 | `OpenApiContractTests.Document_RequestSchemasListOnlyTrulyRequiredFields` | 1 | Document: request schemas list only truly required fields | |
| API-155 | `OpenApiContractTests.Document_EnumsAreStrings` | 1 | Document: enums are strings | |
| API-156 | `OpenApiContractTests.SwaggerUi_IsServedInDevelopment` | 1 | SwaggerUi: is served in development | |
| API-161 | `OpenApiContractTests.Document_SalaryOnlyInPayrollSchemas` | 1 | Document: salary only in payroll schemas | |

**AuditApiTests**: Every create, update and delete leaves an AuditLog row; failed requests leave none.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-162 | `AuditApiTests.CreatingStaff_IsAuditedWithTheStaffNumber` | 1 | CreatingStaff: is audited with the staff number | |
| API-163 | `AuditApiTests.UpdatingStaff_AddsAnUpdatedRow_NewestFirst` | 1 | UpdatingStaff: adds an updated row → newest first | |
| API-164 | `AuditApiTests.InitialAssignment_IsAuditedWithItsNewId` | 1 | InitialAssignment: is audited with its new id | |
| API-165 | `AuditApiTests.DeletingAShift_IsAudited` | 1 | DeletingAShift: is audited | |
| API-166 | `AuditApiTests.Requisitions_AreAuditedByNumber` | 1 | Requisitions: are audited by number | |
| API-167 | `AuditApiTests.RejectedRequest_WritesNoAuditRow` | 1 | RejectedRequest: writes no audit row | |
| API-168 | `AuditApiTests.SeededRecords_AreAuditedAsCreatedBySystem` | 1 | SeededRecords: are audited as created by system | |
| API-169 | `AuditApiTests.Search_IsPaged` | 1 | Search: is paged | |
| API-170 | `AuditApiTests.Search_InvalidPaging_Returns422` | 2 | Search: invalid paging → returns 422 | |

**IntegrationEventsApiTests**: Onboarding writes a Staff.Onboarded event in the same transaction; the dispatcher publishes it once, in order.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-171 | `IntegrationEventsApiTests.Onboarding_WritesOneUnpublishedStaffOnboardedEvent` | 1 | Onboarding: writes one unpublished staff onboarded event | |
| API-172 | `IntegrationEventsApiTests.OnboardedPayload_LeavesOutPersonalAndPayData` | 1 | OnboardedPayload: leaves out personal and pay data | |
| API-173 | `IntegrationEventsApiTests.FailedOnboarding_WritesNoEvent` | 1 | FailedOnboarding: writes no event | |
| API-174 | `IntegrationEventsApiTests.Dispatcher_PublishesPendingEventsAndMarksThem` | 1 | Dispatcher: publishes pending events and marks them | |
| API-175 | `IntegrationEventsApiTests.Dispatcher_DoesNotPublishTheSameEventTwice` | 1 | Dispatcher: does not publish the same event twice | |
| API-176 | `IntegrationEventsApiTests.Dispatcher_WhenBrokerIsDown_KeepsEventsAndRetriesLater` | 1 | Dispatcher: when broker is down → keeps events and retries later | |
| API-177 | `IntegrationEventsApiTests.Dispatcher_PublishesInTheOrderEventsHappened` | 1 | Dispatcher: publishes in the order events happened | |

**SecurityFlagsApiTests**: PUT upserts a flag per staff member and type; stale updates are ignored.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-178 | `SecurityFlagsApiTests.Put_NewFlag_Returns201` | 1 | Put: new flag → returns 201 | |
| API-179 | `SecurityFlagsApiTests.Put_ExistingFlag_UpdatesInPlace` | 1 | Put: existing flag → updates in place | |
| API-180 | `SecurityFlagsApiTests.Put_OlderUpdate_IsIgnored` | 1 | Put: older update → is ignored | |
| API-181 | `SecurityFlagsApiTests.Put_SameUpdateTwice_IsHarmless` | 1 | Put: same update twice → is harmless | |
| API-182 | `SecurityFlagsApiTests.Put_IsAudited` | 1 | Put: is audited | |
| API-183 | `SecurityFlagsApiTests.Put_InvalidFlagType_Returns422` | 2 | Put: invalid flag type → returns 422 | |
| API-184 | `SecurityFlagsApiTests.Put_FlagTypeOver50Characters_Returns422` | 1 | Put: flag type over 50 characters → returns 422 | |
| API-185 | `SecurityFlagsApiTests.Put_MissingStatus_Returns422` | 1 | Put: missing status → returns 422 | |
| API-186 | `SecurityFlagsApiTests.Put_UnknownStatus_Returns400` | 1 | Put: unknown status → returns 400 | |
| API-187 | `SecurityFlagsApiTests.Put_UnknownStaff_Returns404` | 1 | Put: unknown staff → returns 404 | |
| API-188 | `SecurityFlagsApiTests.List_ReturnsSeededFlags` | 1 | List: returns seeded flags | |
| API-189 | `SecurityFlagsApiTests.List_UnknownStaff_Returns404` | 1 | List: unknown staff → returns 404 | |
| API-190 | `SecurityFlagsApiTests.Search_FiltersByStatusAndType` | 1 | Search: filters by status and type | |

**EligibilityApiTests**: Eligibility of seeded people on the test day (5 Nov 2026), whose situations cover every rule.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-191 | `EligibilityApiTests.ValidParamedic_IsEligibleWithoutWarnings` | 1 | ValidParamedic: is eligible without warnings | |
| API-192 | `EligibilityApiTests.ExpiringCpr_IsEligibleWithAWarning` | 1 | ExpiringCpr: is eligible with a warning | |
| API-193 | `EligibilityApiTests.SingleProblem_IsIneligibleForThatReason` | 3 | SingleProblem: is ineligible for that reason | |
| API-194 | `EligibilityApiTests.SuspendedFlaggedOfficer_ListsEveryReason` | 1 | SuspendedFlaggedOfficer: lists every reason | |
| API-195 | `EligibilityApiTests.TerminatedEmployee_HasNoCurrentAssignment` | 1 | TerminatedEmployee: has no current assignment | |
| API-196 | `EligibilityApiTests.ForAService_RequiresAnAssignmentInThatService` | 1 | ForAService: requires an assignment in that service | |
| API-197 | `EligibilityApiTests.AtAFutureDate_UsesThatDatesCredentialStatus` | 1 | AtAFutureDate: uses that dates credential status | |
| API-198 | `EligibilityApiTests.ActiveFlag_BlocksDuty_UntilCleared` | 1 | ActiveFlag: blocks duty → until cleared | |
| API-199 | `EligibilityApiTests.NewHireWithoutAssignment_IsIneligible` | 1 | NewHireWithoutAssignment: is ineligible | |
| API-200 | `EligibilityApiTests.UnknownStaff_Returns404` | 1 | UnknownStaff: returns 404 | |
| API-201 | `EligibilityApiTests.UnknownService_Returns422` | 1 | UnknownService: returns 422 | |
| API-202 | `EligibilityApiTests.UnreadableInstant_Returns400` | 1 | UnreadableInstant: returns 400 | |

**TerminationApiTests**: Termination requisitions: Received → HRReview → Approved → Completed. Completing one terminates the employee, ends their assignments, cancels unfinished shifts and publishes Staff.Terminated.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-203 | `TerminationApiTests.Create_ReturnsATerminationForTheEmployee` | 1 | Create: returns a termination for the employee | |
| API-204 | `TerminationApiTests.Create_ReplayWithSameKey_ReturnsTheOriginal_DifferentEmployee_Is409` | 1 | Create: replay with same key → returns the original → different employee → is 409 | |
| API-205 | `TerminationApiTests.Create_SecondOpenTerminationForTheSamePerson_Returns409` | 1 | Create: second open termination for the same person → returns 409 | |
| API-206 | `TerminationApiTests.Create_AfterARejectedTermination_IsAllowed` | 1 | Create: after a rejected termination → is allowed | |
| API-207 | `TerminationApiTests.Create_ForAnAlreadyTerminatedEmployee_Returns422` | 1 | Create: for an already terminated employee → returns 422 | |
| API-208 | `TerminationApiTests.Create_InvalidTermination_Returns422PerField` | 1 | Create: invalid termination → returns 422 per field | |
| API-209 | `TerminationApiTests.Create_HireNamingAnEmployee_Returns422` | 1 | Create: hire naming an employee → returns 422 | |
| API-210 | `TerminationApiTests.Workflow_SkippingHRReview_Returns422` | 1 | Workflow: skipping hr review → returns 422 | |
| API-211 | `TerminationApiTests.Workflow_HasNoBudgetStepOrPositionsToFill` | 1 | Workflow: has no budget step or positions to fill | |
| API-212 | `TerminationApiTests.Workflow_ApprovedIsNotYetATermination` | 1 | Workflow: approved is not yet a termination | |
| API-213 | `TerminationApiTests.Workflow_RejectedLeavesTheEmployeeUntouched` | 1 | Workflow: rejected leaves the employee untouched | |
| API-214 | `TerminationApiTests.Complete_TerminatesTheEmployeeAndKeepsTheStaffNumber` | 1 | Complete: terminates the employee and keeps the staff number | |
| API-215 | `TerminationApiTests.Complete_EndsCurrentAssignmentsTodayAndRemovesFutureOnes` | 1 | Complete: ends current assignments today and removes future ones | |
| API-216 | `TerminationApiTests.Complete_CancelsUnfinishedShiftsOnly` | 1 | Complete: cancels unfinished shifts only | |
| API-217 | `TerminationApiTests.Complete_RemovesDutyEligibility` | 1 | Complete: removes duty eligibility | |
| API-218 | `TerminationApiTests.Complete_PublishesStaffTerminated` | 1 | Complete: publishes staff terminated | |
| API-219 | `TerminationApiTests.Complete_IsAudited` | 1 | Complete: is audited | |
| API-220 | `TerminationApiTests.Complete_ThenClosed_IsAllowed_ButNotReopened` | 1 | Complete: then closed → is allowed → but not reopened | |
| API-221 | `TerminationApiTests.Terminations_AreNotVacancies_AndFilterByType` | 1 | Terminations: are not vacancies → and filter by type | |

**PayrollApiTests**: Pay history (non-overlapping rates) and per-department payroll totals.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-222 | `PayrollApiTests.SetPay_FirstRate_IsCurrent` | 1 | SetPay: first rate → is current | |
| API-223 | `PayrollApiTests.SetPay_NewRate_ClosesThePreviousOneOnItsStart` | 1 | SetPay: new rate → closes the previous one on its start | |
| API-224 | `PayrollApiTests.SetPay_SameStartDate_CorrectsTheCurrentRate` | 1 | SetPay: same start date → corrects the current rate | |
| API-225 | `PayrollApiTests.SetPay_StartingBeforeTheCurrentRate_Returns422` | 1 | SetPay: starting before the current rate → returns 422 | |
| API-226 | `PayrollApiTests.SetPay_SalaryOutOfRange_Returns422` | 3 | SetPay: salary out of range → returns 422 | |
| API-227 | `PayrollApiTests.SetPay_MissingFieldsOrBeforeHire_Returns422` | 1 | SetPay: missing fields or before hire → returns 422 | |
| API-228 | `PayrollApiTests.SetPay_ForTerminatedEmployee_Returns422` | 1 | SetPay: for terminated employee → returns 422 | |
| API-229 | `PayrollApiTests.SetPay_UnknownStaff_Returns404` | 1 | SetPay: unknown staff → returns 404 | |
| API-230 | `PayrollApiTests.SetPay_IsAudited_ButNeverShownOnTheProfile` | 1 | SetPay: is audited → but never shown on the profile | |
| API-231 | `PayrollApiTests.History_ShowsSeededPromotion` | 1 | History: shows seeded promotion | |
| API-232 | `PayrollApiTests.History_UnknownStaff_Returns404` | 1 | History: unknown staff → returns 404 | |
| API-233 | `PayrollApiTests.Headcount_TotalsEachDepartmentFromSeedData` | 1 | Headcount: totals each department from seed data | |
| API-234 | `PayrollApiTests.Headcount_AsOfAnEarlierDate_UsesTheRatesInForceThen` | 1 | Headcount: as of an earlier date → uses the rates in force then | |
| API-235 | `PayrollApiTests.Headcount_CountsEmployeesWithoutARate` | 1 | Headcount: counts employees without a rate | |
| API-236 | `PayrollApiTests.Headcount_UnreadableDate_Returns400` | 1 | Headcount: unreadable date → returns 400 | |

**AuthorizationApiTests**: Each policy lets the right roles through and answers 401 / 403 as ProblemDetails otherwise.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-237 | `AuthorizationApiTests.NotSignedIn_Returns401Problem` | 5 | NotSignedIn: returns 401 problem | |
| API-238 | `AuthorizationApiTests.Health_IsOpenToEveryone` | 1 | Health: is open to everyone | |
| API-239 | `AuthorizationApiTests.SignedInWithoutAnyRole_Returns403` | 1 | SignedInWithoutAnyRole: returns 403 | |
| API-240 | `AuthorizationApiTests.Roles_AreEnforced` | 23 | Roles: are enforced | |
| API-241 | `AuthorizationApiTests.RosterScheduler_CanAddAShift` | 1 | RosterScheduler: can add a shift | |
| API-242 | `AuthorizationApiTests.Audit_RecordsTheSignedInUser` | 1 | Audit: records the signed in user | |
| API-243 | `AuthorizationApiTests.OpenApi_DocumentsBearerAuthAnd401403` | 1 | OpenApi: documents bearer auth and 401403 | |

**JwtAuthenticationTests**: Runs the app in EntraId mode. Instead of downloading Entra ID's signing keys, the test trusts one local key and issuer, so it can mint valid, expired and wrongly addressed tokens.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-244 | `JwtAuthenticationTests.ValidTokenWithRole_IsAccepted` | 1 | ValidTokenWithRole: is accepted | |
| API-245 | `JwtAuthenticationTests.NoToken_Returns401WithBearerChallenge` | 1 | NoToken: returns 401 with bearer challenge | |
| API-246 | `JwtAuthenticationTests.ExpiredToken_Returns401` | 1 | ExpiredToken: returns 401 | |
| API-247 | `JwtAuthenticationTests.TokenForAnotherApi_Returns401` | 1 | TokenForAnotherApi: returns 401 | |
| API-248 | `JwtAuthenticationTests.TokenSignedWithAnotherKey_Returns401` | 1 | TokenSignedWithAnotherKey: returns 401 | |
| API-249 | `JwtAuthenticationTests.ValidTokenWithoutTheRole_Returns403` | 1 | ValidTokenWithoutTheRole: returns 403 | |
| API-250 | `JwtAuthenticationTests.DevelopmentHeaders_AreIgnoredInEntraIdMode` | 1 | DevelopmentHeaders: are ignored in entra id mode | |

**DepartmentScopeApiTests**: A caller with a department claim sees only its own department's people and records.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-251 | `DepartmentScopeApiTests.Search_ShowsOnlyTheCallersDepartment` | 1 | Search: shows only the callers department | |
| API-252 | `DepartmentScopeApiTests.OtherDepartmentsStaff_AreNotFound` | 1 | OtherDepartmentsStaff: are not found | |
| API-253 | `DepartmentScopeApiTests.Credentials_AreFilteredToo` | 1 | Credentials: are filtered too | |
| API-254 | `DepartmentScopeApiTests.Requisitions_ShowOnlyThoseRaisedByTheDepartment` | 1 | Requisitions: show only those raised by the department | |
| API-255 | `DepartmentScopeApiTests.Headcount_CountsOnlyTheCallersDepartment` | 1 | Headcount: counts only the callers department | |
| API-256 | `DepartmentScopeApiTests.RosterWriter_CannotRosterAnotherDepartmentsStaff` | 1 | RosterWriter: cannot roster another departments staff | |
| API-257 | `DepartmentScopeApiTests.Submitter_CanOnlyRaiseRequisitionsForItsDepartment` | 1 | Submitter: can only raise requisitions for its department | |
| API-258 | `DepartmentScopeApiTests.Admin_IsNeverScoped` | 1 | Admin: is never scoped | |
| API-259 | `DepartmentScopeApiTests.EmailUniqueness_StaysCityWide` | 1 | EmailUniqueness: stays city wide | |

**MetricsApiTests**: Onboarding and event publishing increment their counters, tagged by department / event type.

| ID | Test | Cases | Checks | Issue |
| --- | --- | --- | --- | --- |
| API-260 | `MetricsApiTests.Onboarding_CountsStaffOnboardedForTheDepartment` | 1 | Onboarding: counts staff onboarded for the department | |
| API-261 | `MetricsApiTests.Publishing_CountsEventsPublished` | 1 | Publishing: counts events published | |
| API-262 | `MetricsApiTests.FailedPublish_CountsAFailure` | 1 | FailedPublish: counts a failure | |

### Client

| ID | Test (`src/App.test.jsx`) | Protects | Issue |
| --- | --- | --- | --- |
| CLI-01 | shows the page heading | Page renders its heading | |
| CLI-02 | shows the loading message until the forecasts arrive | Loading state before data arrives | |
| CLI-03 | requests the forecasts from the weatherforecast endpoint | Client calls the endpoint the server and Vite proxy expose | |
| CLI-04 | renders one table row per forecast with every field | Row and cell content match the API data | |
| CLI-05 | renders the table column headers | Table headers | |
| CLI-06 | keeps showing the loading message when the server responds with an error | An HTTP error doesn't crash the page | |

## Adding tests

Add tests alongside each change and delete the ones for removed code:

- **New business rule with no database:** add a unit test under `Unit/`.
- **New endpoint or rule that needs the database:** add an API test under `Api/` that derives from `ApiTestBase`
  (it joins the shared database automatically). Create your own records with `CreateStaffAsync()` and friends rather
  than changing seeded ones, and check errors with `AssertProblemAsync`.
- **New React component:** add a `Component.test.jsx` next to it. Mock `fetch` with `vi.stubGlobal`
  (see `App.test.jsx`).
- Add a row to the tables above for each new test.

## Tagging a release

Tag after the sprint branch is merged into `master` and both suites pass on that commit.

```
git checkout master
git pull
git tag -a v0.1.0-sprint1 -m "Sprint 1 release"
git push origin v0.1.0-sprint1
```

Put the tag name in the Release Note's "Github Branch Repo TAG" field. Check it exists with
`git ls-remote --tags origin`.
