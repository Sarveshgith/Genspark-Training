// ============================================================================
// Azure Log Analytics Workspace
// ----------------------------------------------------------------------------
// Purpose:
//   Creates a Log Analytics Workspace used by Azure Monitor.
//
// Why do we need it?
//   - Collect AKS container logs
//   - Collect Kubernetes events
//   - Monitor CPU/Memory usage
//   - Enable Azure Monitor Container Insights
//
// This workspace will later be attached to the AKS cluster.
//
// Reference:
// https://learn.microsoft.com/azure/azure-monitor/logs/log-analytics-workspace-overview
// ============================================================================

@description('Name of the Log Analytics Workspace')
param name string

@description('Azure region')
param location string

@allowed([
  'PerGB2018'
])
@description('Workspace pricing tier')
param sku string = 'PerGB2018'

@minValue(30)
@maxValue(730)
@description('Retention period (days)')
param retentionInDays int = 30

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location

  properties: {
    retentionInDays: retentionInDays
    sku: {
      name: sku
    }
  }
}

//
// Outputs
//

@description('Workspace Resource ID')
output id string = workspace.id

@description('Workspace Name')
output name string = workspace.name

@description('Workspace Customer ID (Workspace ID)')
output customerId string = workspace.properties.customerId