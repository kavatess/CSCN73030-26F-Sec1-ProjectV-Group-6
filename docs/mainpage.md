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
    client [label="React client\n(App.jsx)"];
    api [label="ASP.NET Core API\n(Program.cs)"];
    ctrl [label="Controllers"];
    model [label="Models"];
    browser -> client [label="loads"];
    client -> api [label="fetch(JSON)"];
    api -> ctrl [label="routes"];
    ctrl -> model [label="returns"];
}
\enddot

During development the server starts the Vite dev server through SpaProxy. The client calls the API
with a relative URL, so the same code works behind the Vite proxy and when served by ASP.NET.

## Current state (release v0.1.0-sprint1)

The application still contains the project template's weather-forecast sample:
WeatherForecastController, WeatherForecast and App. It shows the controller and client patterns the
Staff/HR features will follow and is covered by regression tests. No Staff/HR features are
implemented yet; see @ref REGRESSION_TESTING.md "Regression testing" for the test cases.

## Requirements

The service requirements are maintained in the collaborative SRS. As requirements are implemented,
document the code that satisfies each one with a Doxygen comment that names it, for example
`@see` or `@par Requirement`, so the generated pages trace back to the SRS.

## Regenerating this documentation

From the repository root run `doxygen Doxyfile`, then open `docs/doxygen/html/index.html`.
