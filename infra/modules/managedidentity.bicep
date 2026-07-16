// ============================================================================
// Azure User Assigned Managed Identity
// ----------------------------------------------------------------------------
// Purpose:
//   Creates a User Assigned Managed Identity that will be used by AKS Pods
//   through Azure Workload Identity.
//
// Why?
//   Instead of storing Azure credentials inside Kubernetes Secrets,
//   the pods authenticate to Azure using their Kubernetes Service Account.
//   Azure validates the Service Account through OIDC and issues an access
//   token for this Managed Identity.
//
// Authentication Flow:
//
//   Pod
//      │
//      ▼
//   Kubernetes Service Account
//      │
//      ▼
//   Azure Workload Identity (OIDC)
//      │
//      ▼
//   User Assigned Managed Identity
//      │
//      ▼
//   Azure Key Vault
//
// Reference:
// https://learn.microsoft.com/azure/aks/workload-identity-overview
// ============================================================================

@description('Managed Identity name')
param name string

@description('Azure Region')
param location string

@description('AKS OIDC Issuer URL')
param oidcIssuerUrl string

@description('Kubernetes Namespace')
param kubernetesNamespace string = 'restaurant-ms'

@description('Kubernetes Service Account Name')
param serviceAccountName string = 'restaurant-api-sa'

//
// User Assigned Managed Identity
//

resource workloadIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
}

//
// Federated Credential
//
// This tells Azure:
//
// "If a pod running with the specified Kubernetes Service Account
// presents an OIDC token issued by this AKS cluster,
// trust it as this Managed Identity."
//

resource federatedCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: workloadIdentity

  name: 'aks-workload-identity'

  properties: {
    issuer: oidcIssuerUrl

    subject: 'system:serviceaccount:${kubernetesNamespace}:${serviceAccountName}'

    audiences: [
      'api://AzureADTokenExchange'
    ]
  }
}

//
// Outputs
//

@description('Managed Identity Resource ID')
output id string = workloadIdentity.id

@description('Managed Identity Name')
output name string = workloadIdentity.name

@description('Managed Identity Principal ID')
output principalId string = workloadIdentity.properties.principalId

@description('Managed Identity Client ID')
output clientId string = workloadIdentity.properties.clientId