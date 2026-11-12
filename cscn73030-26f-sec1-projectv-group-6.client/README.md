# CMES Staff & HR frontend

React/Vite implementation of the seven screens in the Staff & HR design:
workforce overview, staff directory, duty roster, requisitions, credentials,
employee profile, and onboarding.

## Run

```bash
npm ci
npm run dev
```

Open the URL printed by Vite. On Windows, Visual Studio can still launch the
client and ASP.NET server together. Run `npm test` for the frontend interaction
tests, `npm run lint` for static checks, or `npm run build` for a production build.

## Current scope

This is a frontend prototype with a March 2026 sample dataset in
`src/demoData.js`. Navigation, filters, search, roster assignments, CSV
exports, and the onboarding form operate in the browser session. The UI does
not write to the ASP.NET server, publish Azure Service Bus events, or process
real HR records. Connect the proposed Staff, Roster, Requisition, and
Credential endpoints before using it with live data.

The employee form deliberately starts with an example duplicate work email
to demonstrate the design's 409 validation state. Change it to another
example address to try the local create flow.

The `vite.config.js` keeps the Visual Studio HTTPS development proxy on
Windows while allowing Linux/macOS standalone builds without a .NET
development certificate.
