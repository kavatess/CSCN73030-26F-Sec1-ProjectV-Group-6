# Deploying to Azure {#deployment}

The Staff/HR service (the API and the React screens, served as one site) runs on **Azure App Service
(Linux, .NET 10)** with **Azure SQL Database**, an optional **Azure Service Bus** topic for integration events, **Key Vault** for secrets and **Application Insights** for
telemetry. `infra/main.bicep` creates all of it; `.github/workflows/deploy.yml` provisions, migrates and deploys.

```
GitHub Actions ──OIDC──▶ Azure
   │ Bicep                 ├─ App Service (managed identity) ──▶ Key Vault (ConnectionStrings--StaffHr)
   │ EF migration bundle   │                                 ──▶ Service Bus topic staff-events (Data Sender)
   └ zip deploy            │                                 ──▶ Azure SQL Database
                           └─ Application Insights ◀── OpenTelemetry
```

The app's managed identity can only read secrets and send to Service Bus. Nothing secret is stored in app settings.

## Student (free) setup

The parameters in `infra/main.parameters.json` are set for an **Azure for Students** subscription:

| Piece | Setting | Cost |
| --- | --- | --- |
| App Service | `F1` (free; 60 CPU minutes a day, sleeps when idle) | Free. Pick `B1` in the workflow (about 13 USD a month) if F1 is too slow. |
| Azure SQL Database | Free offer: serverless, 100,000 vCore-seconds and 32 GB a month, pauses itself when the allowance runs out | Free. Untick *sql_free_offer* for the Basic tier (about 5 USD a month). |
| Service Bus | Off (`enableServiceBus: false`): `Staff.Onboarded` / `Staff.Terminated` go to the app log | Free |
| Key Vault, Application Insights | On | A few cents a month |

The free database pauses after an hour without queries and takes up to a minute to resume, so the first page
load after a quiet spell is slow. For the same reason the template turns off Always On and the App Service
health probe when the free offer is used: both would keep the database awake and use up the month's allowance
in about two days.

**Demo mode.** Until the CMES login service issues Entra ID tokens, the React screens have no way to sign in.
The workflow's *demo_mode* option (on by default) runs the app in the Development environment with the
no-token sign-in: every visitor gets every role, and Swagger is available at `/swagger`. Use it only with the
fictional seed data. When the login service is ready, run the workflow with *demo_mode* unticked and set up the
API app registration (step 4 below).

## One-time setup

1. **Find a region your subscription may use.** Student subscriptions only allow a few regions. In the
   [Azure portal](https://portal.azure.com) open **Cloud Shell** (the `>_` icon, choose Bash) and run:

   ```bash
   az policy assignment list --disable-scope-strict-match --query "[].parameters.listOfAllowedLocations.value" -o tsv
   ```

   If it prints nothing, any region works (for example `canadacentral`).

2. **Create the resource group and the GitHub deployment identity.** Still in Cloud Shell:

   ```bash
   curl -sO https://raw.githubusercontent.com/kavatess/CSCN73030-26F-Sec1-ProjectV-Group-6/production/infra/setup-azure.sh
   ```

   ```bash
   bash setup-azure.sh canadacentral
   ```

   Use your region from step 1. The script (`infra/setup-azure.sh`) creates resource group
   `cmes-staffhr-rg`, registers the resource providers, and creates the identity GitHub Actions signs in with:
   a user-assigned managed identity `cmes-staffhr-github-deploy` with a federated credential (no password),
   given Contributor and User Access Administrator on that resource group only. A managed identity is an
   Azure resource, so this works even where the school directory doesn't let students create app
   registrations. Running the script again is safe.

3. **Repository secrets** (GitHub → Settings → Secrets and variables → Actions → New repository secret).
   The script prints the first three:

   | Secret | Value |
   | --- | --- |
   | `AZURE_CLIENT_ID` | Client id of the deployment managed identity |
   | `AZURE_TENANT_ID` | Tenant id |
   | `AZURE_SUBSCRIPTION_ID` | Subscription id |
   | `SQL_ADMIN_PASSWORD` | A password you make up for the SQL administrator: 12+ characters with upper- and lower-case letters and digits, without `;` `'` `"` |
   | `STAFFHR_API_CLIENT_ID` | Optional until demo mode is turned off: client id of the API app registration (step 4) |

4. **Later, for real sign-in: the API app registration** (Microsoft Entra admin center → App registrations → New):
   - Name `CMES Staff/HR API`; **Expose an API** → Application ID URI `api://cmes-staff-hr`.
   - **App roles**: add the seven roles in `infra/entra-app-roles.json` (or paste its `appRoles` into the manifest).
   - Assign roles: HR staff → `StaffHr.Admin`; each peer service's app registration → the roles it needs
     (e.g. Security → `StaffHr.SecurityFlags.Write`, Budget → `StaffHr.Payroll`). To limit a peer service to one
     department, add an optional claim or claims-mapping policy that emits `department` (e.g. `PTR`).
   - Put its **Application (client) ID** in the `STAFFHR_API_CLIENT_ID` secret.

## Deploying

GitHub only offers **Run workflow** for workflows that are on the repository's default branch (`master`), so
merge `production` into `master` first. Then: Actions → **Deploy to Azure** → Run workflow, choose the
`production` branch and the options:

| Option | Default | Meaning |
| --- | --- | --- |
| resource_group | `cmes-staffhr-rg` | Where to deploy |
| app_service_sku | `F1` | `F1` free, `B1` paid |
| sql_free_offer | on | Free serverless database; off for Basic |
| demo_mode | on | No sign-in, every role (fictional data only) |
| seed_demo_data | on | Load the fictional demo data if the database is empty |

The workflow:

1. deploys `infra/main.bicep` (idempotent: re-running updates in place);
2. publishes the app (the React client is built into `wwwroot`) and builds an EF Core migration bundle;
3. opens a temporary SQL firewall rule for the runner, applies the migrations, optionally seeds the demo data,
   and removes the rule;
4. zip-deploys the app, waits for `GET /health` to return 200 and prints the site URL in the run summary.

The first run takes about 10 minutes. Open the URL from the run summary: the HR screens are at `/`, Swagger at
`/swagger` (demo mode only), health at `/health`. Re-run the workflow to deploy new code; seeding never
overwrites existing data.

### If something fails

| Error | Fix |
| --- | --- |
| `RequestDisallowedByPolicy` / location not allowed | Use a region from step 1 (delete and recreate the resource group there). |
| `AADSTS700213` / `AADSTS70021`: no matching federated identity record | The error shows the subject GitHub sent. Re-run `setup-azure.sh` (it registers both the classic `repo:owner/name:…` and the immutable-id `repo:owner@id/name@id:…` forms), or add that exact subject to the identity's federated credentials. The workflow must run in the `production` environment. |
| Free offer not available for this subscription | Re-run with *sql_free_offer* unticked (Basic tier). |
| SQL password rejected | It needs 3 of: upper case, lower case, digits, symbols, and at least 8 characters. |
| `SubscriptionIsOverQuotaForSku` … Current Limit (F1 App Service instances): 0 | The subscription has no free-plan quota in that region (common on student subscriptions). Re-run with *app_service_sku* `B1`, or request an F1 quota of 1 on the portal's Quotas page (Provider: App Service). |
| Smoke test times out on F1 | The free plan is slow to start; re-run, or choose `B1`. |

## Configuration reference

| Setting (App Service) | Source | Purpose |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Bicep | `Production` (Swagger off, Entra ID auth enforced); `Development` in demo mode |
| `Authentication__Mode` | Bicep | `EntraId`; `Development` (no-token sign-in) in demo mode |
| `KeyVault__Uri` | Bicep | Loads `ConnectionStrings--StaffHr` from Key Vault |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Bicep | OpenTelemetry export to Application Insights |
| `Messaging__ServiceBus__FullyQualifiedNamespace` | Bicep | Publish events with the managed identity (only when `enableServiceBus` is true) |
| `Authentication__EntraId__TenantId` / `Audience` / `ClientId` | Bicep | Token validation |

## Checking a deployment

- `GET https://<app>.azurewebsites.net/health` → `{"status":"Healthy","checks":{"database":"Healthy","outbox":"Healthy"}}`.
- Application Insights → Metrics → custom metrics `staffhr.*` (onboardings, terminations, events published).
- Service Bus (when `enableServiceBus` is on) → topic `staff-events` → incoming messages after an onboarding.

`infra/main.bicep` is compiled on every change by `.github/workflows/infra-validate.yml`.
