// ============================================================================
// Azure Static Web App
// ----------------------------------------------------------------------------
// Hosts the Angular frontend.
//
// NOTE
// ----
// This module only provisions the Static Web App.
//
// GitHub Actions will later deploy the Angular build using the deployment token.
//
// ============================================================================

@description('Static Web App Name')
param name string

@description('Azure region where SWA is available.')
param location string

@allowed([
  'Free'
  'Standard'
])
param sku string = 'Free'

resource swa 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location

  sku: {
    name: sku
    tier: sku
  }

  properties: {
    publicNetworkAccess: 'Enabled'
  }
}

output id string = swa.id

output name string = swa.name

output defaultHostname string = swa.properties.defaultHostname