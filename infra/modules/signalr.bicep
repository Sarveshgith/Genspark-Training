// ============================================================================
// Azure SignalR Service
// ----------------------------------------------------------------------------
// Purpose
// -------
// Provides a managed SignalR backplane for real-time communication.
//
// Used for:
//
// • Kitchen Display System updates
// • Live Order Status
// • Table Status
// • Dashboard Notifications
//
// ============================================================================

@description('SignalR Service Name')
param name string

@description('Azure Region')
param location string

@allowed([
  'Free_F1'
  'Standard_S1'
])
param skuName string = 'Free_F1'

resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: name
  location: location
  sku: {
    name: skuName
    capacity: 1
  }
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
      }
    ]
    publicNetworkAccess: 'Enabled'
  }
}
output id string = signalr.id
output name string = signalr.name
output hostname string = signalr.properties.hostName