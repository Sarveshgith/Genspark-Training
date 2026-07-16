targetScope = 'resourceGroup'

// ============================================================================
// Restaurant Management System Infrastructure
//
// Creates:
//
// • Azure Kubernetes Service
// • Azure Container Registry
// • Azure PostgreSQL Flexible Server
// • Azure Key Vault
// • Log Analytics
// • User Assigned Managed Identity
// • (Optional) Static Web App
//
// This file orchestrates the infrastructure.
//
// Individual Azure resources are implemented inside modules.
// ============================================================================

param location string = 'southindia'
param prefix string = 'restaurant'

@secure()
param postgresAdminPassword string

param deployerObjectId string

var suffix = uniqueString(resourceGroup().id)
var shortSuffix = take(suffix, 8)

var acrName = '${prefix}acr${suffix}'
var kvName = '${prefix}-kv-${shortSuffix}04'
var aksName = '${prefix}-aks'
var postgresName = '${prefix}-pg-${suffix}'
var logAnalyticsName = '${prefix}-law'
var identityName = '${prefix}-identity'
var kubernetesNamespace = 'restaurant-ms'
var serviceAccountName = 'restaurant-api-sa'

// --------------------------------------------------------------------------
// Log Analytics
// --------------------------------------------------------------------------

module logAnalytics 'modules/loganalytics.bicep' = {
  name: 'loganalytics'

  params: {
    name: logAnalyticsName
    location: location
  }
}

// --------------------------------------------------------------------------
// Managed Identity
// --------------------------------------------------------------------------

module identity 'modules/managedidentity.bicep' = {
  name: 'identity'

  params: {
    name: identityName
    location: location
    oidcIssuerUrl: aks.outputs.oidcIssuerUrl
    kubernetesNamespace: kubernetesNamespace
    serviceAccountName: serviceAccountName
  }
}

// --------------------------------------------------------------------------
// Azure Kubernetes Service
// --------------------------------------------------------------------------

module aks 'modules/aks.bicep' = {
  name: 'aks'

  params: {
    name: aksName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// --------------------------------------------------------------------------
// Azure Container Registry
// --------------------------------------------------------------------------

module acr 'modules/acr.bicep' = {
  name: 'acr'

  params: {
    name: acrName
    location: location
  }
}

// --------------------------------------------------------------------------
// Azure Key Vault
// --------------------------------------------------------------------------

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'

  params: {
    name: kvName
    location: location
    workloadIdentityPrincipalId: identity.outputs.principalId
    deployerObjectId: deployerObjectId
  }
}

// --------------------------------------------------------------------------
// PostgreSQL
// --------------------------------------------------------------------------

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'

  params: {
    name: postgresName
    location: location
    administratorPassword: postgresAdminPassword
  }
}

// --------------------------------------------------------------------------
// Static Web App
// --------------------------------------------------------------------------

module frontend 'modules/staticwebapp.bicep' = {
  name: 'frontend'

  params: {
      name: '${prefix}-frontend-${shortSuffix}'
      location: 'eastasia'
  }
}

// --------------------------------------------------------------------------
// SignalR
// --------------------------------------------------------------------------

module signalr 'modules/signalr.bicep' = {
  name: 'signalr'

  params: {
    name: '${prefix}-signalr-${shortSuffix}'
    location: location
  }
}

// ============================================================================
// Outputs
// ============================================================================

output resourceGroupName string = resourceGroup().name

output acrName string = acr.outputs.name
output acrLoginServer string = acr.outputs.loginServer

output aksName string = aks.outputs.name
output aksNodeResourceGroup string = aks.outputs.nodeResourceGroup
output oidcIssuerUrl string = aks.outputs.oidcIssuerUrl

output keyVaultName string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.vaultUri

output postgresName string = postgres.outputs.name
output postgresFqdn string = postgres.outputs.fqdn
output postgresDatabase string = postgres.outputs.databaseName

output workloadIdentityClientId string = identity.outputs.clientId
output workloadIdentityPrincipalId string = identity.outputs.principalId

output logAnalyticsWorkspaceId string = logAnalytics.outputs.id

output kubernetesNamespace string = kubernetesNamespace
output serviceAccountName string = serviceAccountName

output signalRName string = signalr.outputs.name
output signalRHostname string = signalr.outputs.hostname

output frontendHostname string = frontend.outputs.defaultHostname
output frontendName string = frontend.outputs.name