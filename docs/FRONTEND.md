# Frontend: the HR screens {#frontend}

The React 19 + Vite client (`cscn73030-26f-sec1-projectv-group-6.client`) implements the seven screens of the Staff
& HR design and reads and writes everything through the Staff/HR API. In production the ASP.NET server serves the
built client from `wwwroot`, so the screens and the API share one address; in development Vite forwards the API
paths to the server (`vite.config.js`).

## Modules

| File | Responsibility |
| --- | --- |
| `src/api.js` | `fetch` wrapper for every endpoint; RFC 7807 errors become `ApiError` (status, message, field errors); `Idempotency-Key`; label, date and money helpers |
| `src/useApi.js` | `useApi(load, deps)`: loads data for a screen, keeps the previous result while reloading, `reload()` |
| `src/ui.jsx` | Shared pieces: `Pill`, `Button`, `Stat`, `Panel`, `Select`, `Field`, `Header`, `Avatar`, `Dialog`, `ApiNotice`, `Empty` |
| `src/screens.js` | Screen titles, the API route shown above each title, CSV export |
| `src/App.jsx` | Sidebar (live badges, API status from `GET /health`), hash routing, toasts |
| `src/pages/*.jsx` | One module per screen (below) |
| `src/test/fakeApi.js` | Fake Staff/HR API used by the interaction tests |

## Screens

| Screen | Route | Reads | Writes |
| --- | --- | --- | --- |
| Workforce overview | `#overview` | headcount, payroll, requisitions, expiring and expired credentials, security flags, audit | payroll statements to Budget |
| Staff directory | `#directory` | `GET /staff` (search, filters, paging), departments, services, on duty | — |
| Employee profile | `#profile/STF-0000000` | profile, eligibility, security flags, compensation, shifts, audit | profile, assignments, credentials, termination requisition |
| Onboard employee | `#onboard` | departments, services, credential types, hire requisitions | `POST /staff`, credentials, compensation, requisition filled |
| Duty roster | `#roster` | rosters, staff of the service, eligibility per person | shifts (after an eligibility check) |
| Requisitions | `#requisitions` | requisitions, department budget from Budget | raise, workflow moves, send to Budget, check Budget's decision |
| Credentials | `#credentials` | credentials, types, expiring | renew, suspend, reinstate |

Role-restricted data degrades instead of failing: without the Payroll role the payroll column, compensation tab
and "Send payroll to Budget" say so or are hidden; without `Audit.Read` the audit panels say so.

## Tests

`npm test` renders the whole app against `src/test/fakeApi.js` (30 interaction tests: every screen, error states,
the Budget flows); `npm run lint` and `npm run build` are the other checks.
