# Regression testing

The Staff/HR service has two automated test suites. Run both before every release.

| Suite | Tooling | Location |
| --- | --- | --- |
| Server (ASP.NET) | xUnit + `WebApplicationFactory` | `CSCN73030-26F-Sec1-ProjectV-Group-6.Server.Tests/` |
| Client (React) | Vitest + Testing Library | `cscn73030-26f-sec1-projectv-group-6.client/src/*.test.jsx` |

## Prerequisites

- .NET 10 SDK
- Node.js 20.19+ or 22.12+

## Running the tests

Server tests, from the repository root:

```
dotnet test CSCN73030-26F-Sec1-ProjectV-Group-6.Server.Tests --logger "console;verbosity=normal"
```

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

### Server

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

The current tests cover the template's weather-forecast sample. As Staff/HR features replace it, add tests
alongside each change and delete the ones for removed code:

- **New endpoint or business rule:** add an xUnit test in the server test project. Use
  `WebApplicationFactory<Program>` (see `WeatherForecastApiTests`) to test through HTTP.
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
