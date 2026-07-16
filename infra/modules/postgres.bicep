// ============================================================================
// Azure PostgreSQL Flexible Server
// ----------------------------------------------------------------------------
// Purpose:
//   Managed PostgreSQL instance used by the Restaurant Management System.
//
// Notes
// -----
// • This module creates ONLY the PostgreSQL server.
//
// • Database firewall rules are NOT created here because the AKS outbound IP
//   does not exist until after the cluster is provisioned.
//
// • The bootstrap script will:
//
//      1. Read the server FQDN
//      2. Determine the AKS outbound IP
//      3. Create the firewall rule
//      4. Store the connection string in Azure Key Vault
//
// • SSL Mode is required by Azure PostgreSQL.
// ============================================================================

@description('Azure PostgreSQL Flexible Server name. Must be globally unique.')
param name string

@description('Azure region.')
param location string

@description('Administrator username.')
param administratorLogin string = 'restaurantadmin'

@secure()
@description('Administrator password.')
param administratorPassword string

@description('Application database name.')
param databaseName string = 'RestaurantManagementDB'

@allowed([
  'Standard_B1ms'
  'Standard_B2s'
  'Standard_D2s_v3'
])
@description('Compute SKU.')
param skuName string = 'Standard_B1ms'

@description('PostgreSQL major version.')
param postgresVersion string = '16'

@allowed([
  32
  64
  128
])
@description('Storage size in GB.')
param storageSizeGB int = 32

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: name
  location: location

  sku: {
    name: skuName
    tier: 'Burstable'
  }

  properties: {

    administratorLogin: administratorLogin

    administratorLoginPassword: administratorPassword

    version: postgresVersion

    storage: {
      storageSizeGB: storageSizeGB

      autoGrow: 'Enabled'
    }

    backup: {

      backupRetentionDays: 7

      geoRedundantBackup: 'Disabled'
    }

    network: {

      publicNetworkAccess: 'Enabled'
    }

    highAvailability: {

      mode: 'Disabled'
    }

    maintenanceWindow: {

      customWindow: 'Disabled'
    }

    createMode: 'Create'
  }
}

// --------------------------------------------------------------------------
// Creates the application database.
//
// The server exists independently of the database.
// Additional databases can be added later if required.
// --------------------------------------------------------------------------

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: postgres

  name: databaseName

  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

output id string = postgres.id

output name string = postgres.name

output fqdn string = postgres.properties.fullyQualifiedDomainName

output administratorLogin string = administratorLogin

output databaseName string = database.name

output port int = 5432