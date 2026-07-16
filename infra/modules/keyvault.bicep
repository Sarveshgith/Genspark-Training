// ============================================================================
// Azure Key Vault
// ----------------------------------------------------------------------------
// Purpose
// -------
// Stores all sensitive configuration required by the Restaurant Management
// System.
//
// Examples:
//   • Database Connection String
//   • JWT Secret
//   • SMTP Credentials
//   • Stripe Secret Key
//   • Resend API Key
//
// SECURITY MODEL
// --------------
// This project deliberately uses ACCESS POLICIES instead of Azure RBAC.
//
// Why?
//
// Creating Azure RBAC role assignments requires
//
//     Microsoft.Authorization/roleAssignments/write
//
// which the current Azure subscription does not grant.
//
// Therefore:
//
//   • The deployer receives Secret Management permissions.
//   • The AKS Managed Identity receives read-only permissions.
//
// If the subscription later grants
//
//     User Access Administrator
//
// this module can be migrated to Azure RBAC.
// ============================================================================

@description('Azure Key Vault name. Must be globally unique.')
param name string

@description('Azure region.')
param location string

@description('Principal ID of the AKS User Assigned Managed Identity.')
param workloadIdentityPrincipalId string

@description('Object ID of the user deploying the infrastructure.')
param deployerObjectId string

@description('Enable purge protection.')
param enablePurgeProtection bool = true

@description('Soft delete retention period.')
param softDeleteRetentionDays int = 90

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location

  properties: {

    tenantId: subscription().tenantId

    sku: {
      family: 'A'
      name: 'standard'
    }

    // ---------------------------------------------------------------------
    // Azure RBAC is intentionally disabled.
    //
    // Access Policies are used because Contributor cannot create
    // role assignments.
    // ---------------------------------------------------------------------
    enableRbacAuthorization: false

    enabledForDeployment: false

    enabledForDiskEncryption: false

    enabledForTemplateDeployment: false

    enablePurgeProtection: enablePurgeProtection

    enableSoftDelete: true

    softDeleteRetentionInDays: softDeleteRetentionDays

    publicNetworkAccess: 'Enabled'

    networkAcls: {

      defaultAction: 'Allow'

      bypass: 'AzureServices'
    }

    accessPolicies: [

      // -------------------------------------------------------------------
      // AKS Managed Identity
      //
      // Read-only access.
      //
      // Pods authenticate using Azure Workload Identity.
      // They only need to read secrets.
      // -------------------------------------------------------------------
      {
        tenantId: subscription().tenantId

        objectId: workloadIdentityPrincipalId

        permissions: {

          secrets: [
            'Get'
            'List'
          ]
        }
      }

      // -------------------------------------------------------------------
      // Infrastructure Deployer
      //
      // Used by bootstrap scripts to populate secrets.
      //
      // Does NOT manage keys or certificates.
      // -------------------------------------------------------------------
      {
        tenantId: subscription().tenantId

        objectId: deployerObjectId

        permissions: {

          secrets: [
            'Get'
            'List'
            'Set'
            'Delete'
            'Recover'
            'Backup'
            'Restore'
          ]
        }
      }
    ]
  }
}

output id string = keyVault.id

output name string = keyVault.name

output vaultUri string = keyVault.properties.vaultUri