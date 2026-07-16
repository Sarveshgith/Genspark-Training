// ============================================================================
// Azure Container Registry (ACR)
// ----------------------------------------------------------------------------
// Purpose:
//   Stores Docker images for the Restaurant Management System.
//
// Why Basic SKU?
//   - Sufficient for a capstone/demo project.
//   - Lowest cost.
//   - Supports private image repositories.
//
// Why adminUserEnabled?
//   Ideally AKS should authenticate using its Managed Identity with the AcrPull
//   role assignment.
//
//   However, creating role assignments requires the
//   Microsoft.Authorization/roleAssignments/write permission, which a
//   Contributor does not have.
//
//   Therefore we temporarily enable the admin account and create a Kubernetes
//   imagePullSecret during bootstrap.
//
//   If the subscription later grants User Access Administrator, disable the
//   admin account and switch to Managed Identity + AcrPull.
// ============================================================================

@description('Globally unique Azure Container Registry name.')
param name string

@description('Azure region.')
param location string

@allowed([
  'Basic'
  'Standard'
  'Premium'
])
@description('Azure Container Registry SKU.')
param sku string = 'Basic'

@description('Enable the ACR admin account. Only required when AcrPull role assignments cannot be created.')
param enableAdminUser bool = true

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: name
  location: location

  sku: {
    name: sku
  }

  properties: {

    // Enables docker login using username/password.
    // Recommended ONLY when Managed Identity cannot be used.
    adminUserEnabled: enableAdminUser

    // Public network access enabled for simplicity.
    // In production this could be disabled and accessed through Private Link.
    publicNetworkAccess: 'Enabled'

    // Retention policy applies only to Premium SKU.
    // Leaving it empty keeps compatibility across SKUs.
  }
}

output id string = acr.id
output name string = acr.name
output loginServer string = acr.properties.loginServer
output location string = acr.location
output skuName string = sku
output adminUserEnabled bool = enableAdminUser