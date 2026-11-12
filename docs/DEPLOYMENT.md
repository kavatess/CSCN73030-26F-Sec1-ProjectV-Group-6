# Deploying to Azure {#deployment}

The Staff/HR service runs on **Azure App Service (Linux, .NET 10)** with **Azure SQL Database**, an
**Azure Service Bus** topic for integration events, **Key Vault** for secrets and **Application Insights** for
telemetry. `infra/main.bicep` creates all of it; `.github/workflows/deploy.yml` provisions, migrates and deploys.

```
GitHub Actions ──OIDC──▶ Azure
   │ Bicep                 ├─ App Service (managed identity) ──▶ Key Vault (ConnectionStrings--StaffHr)
   │ EF migration bundle   │                                 ──▶ Service Bus topic staff-events (Data Sender)
   └ zip deploy            │                                 ──▶ Azure SQL Database
                           └─ Application Insights ◀── OpenTelemetry
```

The app's managed identity can only read secrets and send to Service Bus. Nothing secret is stored in app settings.

## One-time setup

1. **Resource group**

   ```bash
   az group create -n cmes-staffhr-rg -l canadacentral
   ```

2. **Entra ID app registration for the API** (Microsoft Entra admin center → App registrations → New):
   - Name `CMES Staff/HR API`; **Expose an API** → Application ID URI `api://cmes-staff-hr`.
   - **App roles**: add the seven roles in `infra/entra-app-roles.json` (or paste its `appRoles` into the manifest).
   - Assign roles: HR staff → `StaffHr.Admin`; each peer service's app registration → the roles it needs
     (e.g. Security → `StaffHr.SecurityFlags.Write`, Budget → `StaffHr.Payroll`). To limit a peer service to one
     department, add an optional claim or claims-mapping policy that emits `department` (e.g. `PTR`).
   - Note the **Application (client) ID**.

3. **Deployment identity for GitHub** (a second app registration or a user-assigned identity):
   - Federated credential: GitHub Actions, this repository, environment `production`.
   - Grant it **Contributor** and **User Access Administrator** on the resource group (the template creates role
     assignments), and **Key Vault Secrets User** on the vault after the first run (to read the connection string
     for migrations).

4. **Repository secrets** (Settings → Secrets and variables → Actions):

   | Secret | Value |
   | --- | --- |
   | `AZURE_CLIENT_ID` | Client id of the deployment identity |
   | `AZURE_TENANT_ID` | Tenant id |
   | `AZURE_SUBSCRIPTION_ID` | Subscription id |
   | `SQL_ADMIN_PASSWORD` | A strong password for the SQL administrator |
   | `STAFFHR_API_CLIENT_ID` | Client id of the Staff/HR API app registration (step 2) |

## Deploying

Actions → **Deploy to Azure** → Run workflow (resource group `cmes-staffhr-rg`). The workflow:

1. deploys `infra/main.bicep` (idempotent: re-running updates in place);
2. publishes the app and builds an EF Core migration bundle;
3. opens a temporary SQL firewall rule for the runner, applies the migrations, removes the rule;
4. zip-deploys the app and waits for `GET /health` to return 200.

The production database is **not** seeded with demo data. To load the fictional demo data into a test
environment, run the app once with `-- --seed` against that database.

## Configuration reference

| Setting (App Service) | Source | Purpose |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Bicep | `Production` (Swagger off, Entra ID auth enforced) |
| `KeyVault__Uri` | Bicep | Loads `ConnectionStrings--StaffHr` from Key Vault |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Bicep | OpenTelemetry export to Application Insights |
| `Messaging__ServiceBus__FullyQualifiedNamespace` | Bicep | Publish events with the managed identity |
| `Authentication__EntraId__TenantId` / `Audience` / `ClientId` | Bicep | Token validation |

## Checking a deployment

- `GET https://<app>.azurewebsites.net/health` → `{"status":"Healthy","checks":{"database":"Healthy","outbox":"Healthy"}}`.
- Application Insights → Metrics → custom metrics `staffhr.*` (onboardings, terminations, events published).
- Service Bus → topic `staff-events` → incoming messages after an onboarding.

`infra/main.bicep` is compiled on every change by `.github/workflows/infra-validate.yml`.
