// Azure infrastructure for the CMES Staff/HR service.
// Deploy into an existing resource group:
//   az deployment group create -g <rg> -f infra/main.bicep -p infra/main.parameters.json -p sqlAdminPassword=<secret>
// See docs/DEPLOYMENT.md for the full procedure.

@description('Prefix for resource names, e.g. cmes-staffhr.')
@minLength(3)
@maxLength(16)
param namePrefix string = 'cmes-staffhr'

@description('Azure region. Defaults to the resource group region.')
param location string = resourceGroup().location

@description('''Region for the App Service plan and web app only. App Service quota is per region, and student
subscriptions often have none in some regions; use infra/find-app-region.sh to find one. Defaults to location.''')
param appServiceLocation string = location

@description('SQL Server administrator login.')
param sqlAdminLogin string = 'staffhradmin'

@description('SQL Server administrator password. Stored only inside the Key Vault connection-string secret.')
@secure()
param sqlAdminPassword string

@description('Entra ID tenant (directory) id that issues tokens for the API.')
param entraTenantId string = subscription().tenantId

@description('Application ID URI of the Staff/HR API app registration.')
param entraAudience string = 'api://cmes-staff-hr'

@description('Application (client) id of the Staff/HR API app registration. Leave empty until it exists.')
param entraClientId string = ''

@description('App Service plan SKU: F1 (free), B1 (basic) or larger.')
param appServiceSku string = 'B1'

@description('''Use the Azure SQL Database free offer (serverless, 100,000 vCore-seconds and 32 GB a month, auto-pauses
when the allowance runs out instead of billing). false = Basic tier (about 5 USD a month).''')
param sqlFreeOffer bool = false

@description('Create the Service Bus namespace and staff-events topic. Without it, events are written to the app log.')
param enableServiceBus bool = true

@description('''Demo mode: no sign-in and Swagger is on. For fictional demo data
only, until the CMES login service issues tokens. Every request gets every role. Never use it with real employee records.''')
param demoMode bool = false

var suffix = uniqueString(resourceGroup().id)
var webAppName = '${namePrefix}-api-${suffix}'
var keyVaultName = take('kv${replace(namePrefix, '-', '')}${suffix}', 24)
var sqlServerName = '${namePrefix}-sql-${suffix}'
var serviceBusName = '${namePrefix}-sb-${suffix}'
var databaseName = 'CmesStaffHr'
// Always On and the health probe keep the app (and so the database) busy. Off for F1 (not supported)
// and for the free SQL offer (they would use up its allowance).
var keepWarm = !sqlFreeOffer && !contains(['F1', 'D1'], appServiceSku)

// Built-in role definition ids.
var keyVaultSecretsUserRole = '4633458b-17de-408a-b874-0445c86b69e6'
var serviceBusDataSenderRole = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'

// ---- Monitoring -------------------------------------------------------------------------------

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-logs-${suffix}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-ai-${suffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

// ---- Database ---------------------------------------------------------------------------------

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  // The free offer needs a serverless General Purpose database. It must be allowed to pause when idle,
  // otherwise the monthly allowance lasts about two days.
  sku: sqlFreeOffer
    ? { name: 'GP_S_Gen5', tier: 'GeneralPurpose', family: 'Gen5', capacity: 2 }
    : { name: 'Basic', tier: 'Basic' }
  properties: sqlFreeOffer
    ? {
        useFreeLimit: true
        freeLimitExhaustionBehavior: 'AutoPause'
        autoPauseDelay: 60
        minCapacity: json('0.5')
        maxSizeBytes: 34359738368 // 32 GB, the free-offer limit
      }
    : {
        maxSizeBytes: 2147483648 // 2 GB, the Basic limit
      }
}

// 0.0.0.0 - 0.0.0.0 is Azure's convention for "allow Azure services", which lets the App Service connect.
resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---- Messaging --------------------------------------------------------------------------------

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = if (enableServiceBus) {
  name: serviceBusName
  location: location
  sku: {
    name: 'Standard' // topics need Standard or Premium
    tier: 'Standard'
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource staffEvents 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = if (enableServiceBus) {
  parent: serviceBus
  name: 'staff-events'
  properties: {
    requiresDuplicateDetection: true // MessageId = event id, so retried publishes are dropped
    duplicateDetectionHistoryTimeWindow: 'PT1H'
    defaultMessageTimeToLive: 'P14D'
  }
}

// ---- Secrets ----------------------------------------------------------------------------------

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// Read by the app through the Key Vault configuration provider ("--" stands for ":").
resource connectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--StaffHr'
  properties: {
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'
  }
}

// ---- App Service ------------------------------------------------------------------------------

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${namePrefix}-plan-${suffix}'
  location: appServiceLocation
  kind: 'linux'
  sku: {
    name: appServiceSku
  }
  properties: {
    reserved: true // Linux
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: appServiceLocation
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: keepWarm
      // The health probe queries the database every minute, which would keep a free-offer database awake.
      healthCheckPath: keepWarm ? '/health' : null
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: concat([
        // The app only accepts the no-token sign-in in the Development environment.
        { name: 'ASPNETCORE_ENVIRONMENT', value: demoMode ? 'Development' : 'Production' }
        { name: 'KeyVault__Uri', value: keyVault.properties.vaultUri }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'Authentication__Mode', value: demoMode ? 'Development' : 'EntraId' }
        { name: 'Authentication__EntraId__TenantId', value: entraTenantId }
        { name: 'Authentication__EntraId__Audience', value: entraAudience }
        { name: 'Authentication__EntraId__ClientId', value: entraClientId }
      ], enableServiceBus ? [
        { name: 'Messaging__ServiceBus__FullyQualifiedNamespace', value: '${serviceBusName}.servicebus.windows.net' }
        { name: 'Messaging__ServiceBus__TopicName', value: 'staff-events' }
      ] : [])
    }
  }
}

// The app's managed identity may read secrets and send to Service Bus; nothing else.
resource webAppReadsSecrets 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webApp.id, keyVaultSecretsUserRole)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRole)
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource webAppSendsEvents 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableServiceBus) {
  name: guid(serviceBus.id, webApp.id, serviceBusDataSenderRole)
  scope: serviceBus
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusDataSenderRole)
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output keyVaultName string = keyVault.name
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = databaseName
output serviceBusNamespace string = enableServiceBus ? '${serviceBusName}.servicebus.windows.net' : ''
