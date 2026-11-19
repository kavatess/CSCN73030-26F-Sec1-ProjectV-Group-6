# CMES Staff & HR frontend

React 19 + Vite client for the Staff/HR service. It implements the seven screens of the Staff & HR
design (workforce overview, staff directory, duty roster, requisitions, credentials, employee profile
and onboarding) and reads and writes everything through the Staff/HR API described in
[API.md](../API.md). There is no sample data in the client: what you see is what is in the database.

## Run it

Start the API first (see the [root README](../README.md): restore, migrate, seed, run). Then:

```bash
npm ci
```

```bash
npm run dev
```

Open `http://localhost:54552` (or `https://` on Windows). Vite forwards the API paths (`/staff`,
`/departments`, `/rosters`, `/requisitions`, `/credentials`, `/security-flags`, `/headcounts`, `/audit`,
`/health`, `/openapi`) to the ASP.NET server, so the browser only ever talks to one origin.

Where the proxy sends requests:

| How you start it | API target |
| --- | --- |
| Visual Studio F5, or `dotnet run --launch-profile https` (SpaProxy starts Vite) | the server that launched it |
| `npm run dev` on its own | `https://localhost:7038`, or the first URL in `ASPNETCORE_URLS` |

On Linux or macOS without a trusted dev certificate, run the API on HTTP and point Vite at it:

```bash
ASPNETCORE_URLS=http://localhost:5058 npm run dev
```

In the Development environment the API signs every request in as `dev-user` with all roles, so no
login is needed. The sidebar shows whether the API and its database are reachable (`GET /health`).

## What each screen uses

| Screen | Reads | Writes |
| --- | --- | --- |
| Overview | `GET /departments/headcount`, `/headcounts/payroll`, `/requisitions`, `/credentials/expiring`, `/credentials?state=Expired`, `/security-flags`, `/audit` | — |
| Staff directory | `GET /staff` (search, filters, paging), `/departments`, `/departments/{code}/services`, `/rosters/on-duty` | — |
| Employee profile (`#profile/STF-…`) | `GET /staff/{n}`, `/eligibility`, `/security-flags`, `/compensation`, `/rosters`, `/audit` | `PATCH /staff/{n}`, `POST /staff/{n}/assignments`, `PATCH /assignments/{id}`, `POST /staff/{n}/credentials`, `POST /requisitions` (termination) |
| Onboard employee | `GET /departments`, services, `/credentials/types`, `/requisitions?type=Hire` | `POST /staff`, `POST /staff/{n}/credentials`, `PUT /staff/{n}/compensation`, `PATCH /requisitions/{number}` |
| Duty roster | `GET /rosters`, `/staff?service=`, `/staff/{n}/eligibility?service=` | `POST /rosters/entries` (after an eligibility check), `DELETE /rosters/entries/{id}` |
| Requisitions | `GET /requisitions` | `POST /requisitions` (with `Idempotency-Key`), `PATCH /requisitions/{number}` |
| Credentials | `GET /credentials`, `/credentials/types`, `/credentials/expiring` | `PATCH /credentials/{id}` (renew, suspend, reinstate) |

Role-restricted data degrades rather than failing: without the Payroll role the payroll column and the
compensation tab say so, and without `Audit.Read` the audit panels say so.

## Code layout

```
src/
  api.js          fetch wrapper for every endpoint, ProblemDetails → ApiError, label/date helpers
  useApi.js       hook that loads data for a screen and reloads on demand
  ui.jsx          shared pieces: Pill, Button, Panel, Select, Field, Dialog, Header, ApiNotice
  screens.js      screen titles, header routes, CSV export
  App.jsx         sidebar, hash routing (#overview, #profile/STF-0000002, ...), toasts
  pages/          one module per screen
  test/fakeApi.js fake Staff/HR API for the tests (a slice of the fictional seed data)
```

Errors from the API are RFC 7807 ProblemDetails. `api.js` throws them as `ApiError`; forms show the
`errors` of a 400/422 under the matching field, and a 409 (for example a work email already in use)
next to the field it concerns.

## Checks

```bash
npm test
```

```bash
npm run lint
```

```bash
npm run build
```

The tests render the whole app against `src/test/fakeApi.js`, a stand-in for `fetch`, so they need
neither the server nor a database.
