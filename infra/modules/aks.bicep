// ============================================================================
// Azure Kubernetes Service (AKS)
// ----------------------------------------------------------------------------
// Purpose
// -------
// Creates the Kubernetes cluster that hosts the Restaurant Management System.
//
// This cluster is configured for:
//
// • Azure Workload Identity
// • OIDC Issuer
// • Azure CNI Networking
// • Managed Identity
// • Log Analytics Integration
// • Future Horizontal Scaling
//
// NOTES
// -----
// • This module creates ONLY the AKS cluster.
//
// • Kubernetes resources such as:
//
//      Deployment
//      Service
//      ConfigMap
//      Ingress
//      SecretProviderClass
//
// are applied later using kubectl.
//
// • Ingress Controller and Cert Manager are installed using Helm during the
// bootstrap process.
// ============================================================================

@description('AKS Cluster Name')
param name string

@description('Azure Region')
param location string

@description('Log Analytics Workspace Resource ID')
param logAnalyticsWorkspaceId string

@description('Node VM Size')
param vmSize string = 'Standard_B2s_v2'

@description('Initial Node Count')
@minValue(1)
param nodeCount int = 1

@description('Kubernetes Version')
param kubernetesVersion string = ''

resource aks 'Microsoft.ContainerService/managedClusters@2024-01-01' = {
  name: name
  location: location

  identity: {
    type: 'SystemAssigned'
  }

  sku: {
    name: 'Base'
    tier: 'Free'
  }

  properties: {

    kubernetesVersion: empty(kubernetesVersion)
      ? null
      : kubernetesVersion

    dnsPrefix: '${name}-dns'

    // Azure Workload Identity
    oidcIssuerProfile: {
      enabled: true
    }

    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
    }

    // Node Pool

    agentPoolProfiles: [
      {
        name: 'system'
        mode: 'System'
        count: nodeCount
        vmSize: vmSize
        osDiskSizeGB: 64
        osType: 'Linux'
        enableAutoScaling: false
        maxPods: 30
      }

    ]

    // Networking
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'azure'
      loadBalancerSku: 'standard'
      outboundType: 'loadBalancer'
    }

    // Monitoring
    addonProfiles: {
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceId
        }
      }
    }

    // API Server
    apiServerAccessProfile: {

      enablePrivateCluster: false

    }

    // Local accounts enabled.
    // GitHub Actions will use kubeconfig.
    disableLocalAccounts: false
  }

}

output id string = aks.id
output name string = aks.name
output nodeResourceGroup string = aks.properties.nodeResourceGroup
output kubeletIdentityObjectId string = aks.identity.principalId
output oidcIssuerUrl string = aks.properties.oidcIssuerProfile.issuerURL