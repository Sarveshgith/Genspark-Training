#!/usr/bin/env bash

# =============================================================================
# Restaurant Management System
#
# 01-provision.sh
#
# Responsibilities
#
# 1. Verify required tools
# 2. Verify Azure login
# 3. Prompt for PostgreSQL password
# 4. Create Resource Group
# 5. Deploy Bicep Infrastructure
# 6. Capture deployment outputs
# 7. Generate deployment.env
#
# This script is executed ONLY ONCE.
# =============================================================================

set -euo pipefail

# Navigate to the repository root directory
cd "$(dirname "${BASH_SOURCE[0]}")/.."

# -----------------------------------------------------------------------------
# Configuration
# -----------------------------------------------------------------------------

RESOURCE_GROUP="restaurant-rg"
LOCATION="southindia"

echo ""
echo "==========================================="
echo " Restaurant Management Infrastructure"
echo "==========================================="
echo ""

# -----------------------------------------------------------------------------
# Check required tools
# -----------------------------------------------------------------------------

for cmd in az jq; do
    if ! command -v "$cmd" >/dev/null; then
        echo "ERROR: $cmd is not installed."
        exit 1
    fi
done

# -----------------------------------------------------------------------------
# Verify Azure Login
# -----------------------------------------------------------------------------

echo "Checking Azure login..."

az account show >/dev/null || {
    echo ""
    echo "Please login first."
    az login
}

echo "Azure login verified."
echo ""

# -----------------------------------------------------------------------------
# Resolve PostgreSQL Password
# -----------------------------------------------------------------------------

SECRETS_FILE=""

if [ -f secrets.env ]; then
    SECRETS_FILE="secrets.env"
elif [ -f scripts/secrets.env ]; then
    SECRETS_FILE="scripts/secrets.env"
elif [ -f local.secrets.env ]; then
    SECRETS_FILE="local.secrets.env"
elif [ -f scripts/local.secrets.env ]; then
    SECRETS_FILE="scripts/local.secrets.env"
fi

if [ -n "$SECRETS_FILE" ]; then
    source "$SECRETS_FILE"
fi

if [ -z "${POSTGRES_ADMIN_PASSWORD:-}" ]; then
    echo "Enter PostgreSQL Administrator Password:"
    read -s POSTGRES_PASSWORD
    echo ""
    if [ -z "$POSTGRES_PASSWORD" ]; then
        echo "Password cannot be empty."
        exit 1
    fi
else
    POSTGRES_PASSWORD="$POSTGRES_ADMIN_PASSWORD"
    echo "PostgreSQL Password loaded from $SECRETS_FILE."
    echo ""
fi

# -----------------------------------------------------------------------------
# Get current user's Object ID
# -----------------------------------------------------------------------------

echo "Getting current Azure User Object ID..."

DEPLOYER_OBJECT_ID=$(az ad signed-in-user show \
    --query id \
    -o tsv)

echo "Object ID Retrieved."
echo ""

# -----------------------------------------------------------------------------
# Create or Reuse Resource Group
# -----------------------------------------------------------------------------

echo "Checking if Resource Group '$RESOURCE_GROUP' exists..."

if az group exists --name "$RESOURCE_GROUP" -o tsv 2>/dev/null | grep -q "true"; then
    echo "Resource Group '$RESOURCE_GROUP' already exists. Reusing it."
else
    echo "Resource Group '$RESOURCE_GROUP' does not exist. Creating in '$LOCATION'..."
    az group create \
        --name "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --output none
    echo "Resource Group Created."
fi
echo ""

# -----------------------------------------------------------------------------
# Deploy Bicep
# -----------------------------------------------------------------------------

echo "Deploying infrastructure..."

if az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --name restaurant-deployment \
    --template-file infra/main.bicep \
    --parameters \
        postgresAdminPassword="$POSTGRES_PASSWORD" \
        deployerObjectId="$DEPLOYER_OBJECT_ID"
then
    echo "Infrastructure deployed successfully."
else
    echo "Infrastructure deployment failed."
    echo "Run the following to inspect the detailed deployment operations:"
    echo "az deployment operation group list --resource-group $RESOURCE_GROUP --name restaurant-deployment -o table"
    exit 1
fi

# -----------------------------------------------------------------------------
# Capture Outputs
# -----------------------------------------------------------------------------

echo "Reading Deployment Outputs..."

OUTPUTS=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name restaurant-deployment \
    --query properties.outputs)

# -----------------------------------------------------------------------------
# Create deployment.env
# -----------------------------------------------------------------------------

echo "Generating deployment.env..."

SWA_NAME=$(echo "$OUTPUTS" | jq -r '.frontendName.value')

cat > deployment.env <<EOF
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
SWA_NAME=$SWA_NAME

ACR_NAME=$(echo "$OUTPUTS" | jq -r '.acrName.value')
ACR_LOGIN_SERVER=$(echo "$OUTPUTS" | jq -r '.acrLoginServer.value')

AKS_NAME=$(echo "$OUTPUTS" | jq -r '.aksName.value')
AKS_NODE_RESOURCE_GROUP=$(echo "$OUTPUTS" | jq -r '.aksNodeResourceGroup.value')

KEYVAULT_NAME=$(echo "$OUTPUTS" | jq -r '.keyVaultName.value')
KEYVAULT_URI=$(echo "$OUTPUTS" | jq -r '.keyVaultUri.value')

POSTGRES_NAME=$(echo "$OUTPUTS" | jq -r '.postgresName.value')
POSTGRES_FQDN=$(echo "$OUTPUTS" | jq -r '.postgresFqdn.value')
POSTGRES_DATABASE=$(echo "$OUTPUTS" | jq -r '.postgresDatabase.value')

WORKLOAD_IDENTITY_CLIENT_ID=$(echo "$OUTPUTS" | jq -r '.workloadIdentityClientId.value')
WORKLOAD_IDENTITY_PRINCIPAL_ID=$(echo "$OUTPUTS" | jq -r '.workloadIdentityPrincipalId.value')

KUBERNETES_NAMESPACE=$(echo "$OUTPUTS" | jq -r '.kubernetesNamespace.value')
SERVICE_ACCOUNT_NAME=$(echo "$OUTPUTS" | jq -r '.serviceAccountName.value')

SIGNALR_NAME=$(echo "$OUTPUTS" | jq -r '.signalRName.value')
SIGNALR_HOSTNAME=$(echo "$OUTPUTS" | jq -r '.signalRHostname.value')

FRONTEND_HOSTNAME=$(echo "$OUTPUTS" | jq -r '.frontendHostname.value')

TENANT_ID=$(az account show --query tenantId -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
EOF

echo ""
echo "deployment.env created."
echo ""

# -----------------------------------------------------------------------------
# Retrieve SWA Deployment Token and store it in secrets.env
# -----------------------------------------------------------------------------

echo "Retrieving Static Web App Deployment Token..."
SWA_SECRET_JSON=$(az staticwebapp secrets list \
    --name "$SWA_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    -o json 2>/dev/null || echo "{}")

SWA_DEPLOYMENT_TOKEN=$(echo "$SWA_SECRET_JSON" | jq -r '
    .properties.apiKey //
    .properties.deploymentToken //
    .deploymentToken //
    empty
')

if [ -n "$SWA_DEPLOYMENT_TOKEN" ]; then
    if [ -z "${SECRETS_FILE:-}" ]; then
        SECRETS_FILE="secrets.env"
    fi
    if [ -f "$SECRETS_FILE" ]; then
        grep -v "^SWA_DEPLOYMENT_TOKEN=" "$SECRETS_FILE" > "${SECRETS_FILE}.tmp" || true
        mv "${SECRETS_FILE}.tmp" "$SECRETS_FILE"
    fi
    echo "SWA_DEPLOYMENT_TOKEN=\"$SWA_DEPLOYMENT_TOKEN\"" >> "$SECRETS_FILE"
    echo "SWA Deployment Token saved to $SECRETS_FILE."
else
    echo "Warning: Could not retrieve SWA Deployment Token."
fi

echo ""

# -----------------------------------------------------------------------------
# Summary
# -----------------------------------------------------------------------------

echo "==========================================="
echo " Provisioning Complete"
echo "==========================================="
echo ""

cat deployment.env

echo ""
echo "Next Step:"
echo ""
echo "./scripts/02-bootstrap.sh"
echo ""