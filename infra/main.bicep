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

@description('SQL Server administrator login.')
param sqlAdminLogin string = 'staffhradmin'

@description('SQL Server administrator password. Stored only inside the Key Vault connection-string secret.')
@secure()
param sqlAdminPassword string

@description('Entra ID tenant (directory) id that issues tokens for the API.')
param entraTenantId string = subscription().tenantId

@description('Application ID URI of the Staff/HR API app registration.')
param entraAudience string = 'api://cmes-staff-hr'

@description('Application (client) id of the Staff/HR API app registration.')
param entraClientId string

@description('App Service plan SKU.')
param appServiceSku string = 'B1'

var suffix = uniqueString(resourceGroup().id)
var webAppName = '${namePrefix}-api-${suffix}'
var keyVaultName = take('kv${replace(namePrefix, '-', '')}${suffix}', 24)
var sqlServerName = '${namePrefix}-sql-${suffix}'
var serviceBusName = '${namePrefix}-sb-${suffix}'
var databaseName = 'CmesStaffHr'

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
  sku: {
    name: 'S0'
    tier: 'Standard'
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

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
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

resource staffEvents 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
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
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
}

// ---- App Service ------------------------------------------------------------------------------

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${namePrefix}-plan-${suffix}'
  location: location
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
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      healthCheckPath: '/health'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'KeyVault__Uri', value: keyVault.properties.vaultUri }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'Messaging__ServiceBus__FullyQualifiedNamespace', value: '${serviceBus.name}.servicebus.windows.net' }
        { name: 'Messaging__ServiceBus__TopicName', value: staffEvents.name }
        { name: 'Authentication__Mode', value: 'EntraId' }
        { name: 'Authentication__EntraId__TenantId', value: entraTenantId }
        { name: 'Authentication__EntraId__Audience', value: entraAudience }
        { name: 'Authentication__EntraId__ClientId', value: entraClientId }
      ]
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

resource webAppSendsEvents 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
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
output serviceBusNamespace string = '${serviceBus.name}.servicebus.windows.net'
