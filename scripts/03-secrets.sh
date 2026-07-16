#!/usr/bin/env bash

# =============================================================================
# Restaurant Management System
#
# 03-secrets.sh
#
# Responsibilities
#
# • Read deployment outputs
# • Read local secrets
# • Build PostgreSQL connection string
# • Upload secrets to Azure Key Vault
#
# This script NEVER creates infrastructure.
# It ONLY populates Azure Key Vault.
#
# =============================================================================

set -euo pipefail

# Navigate to the repository root directory
cd "$(dirname "${BASH_SOURCE[0]}")/.."

echo ""
echo "==========================================="
echo " Azure Key Vault Configuration"
echo "==========================================="
echo ""

# -----------------------------------------------------------------------------
# Validate configuration files
# -----------------------------------------------------------------------------

if [ ! -f deployment.env ]; then
    echo "deployment.env not found."
    echo "Run 01-provision.sh first."
    exit 1
fi

SECRETS_FILE=""
if [ -f secrets.env ]; then
    SECRETS_FILE="secrets.env"
elif [ -f scripts/secrets.env ]; then
    SECRETS_FILE="scripts/secrets.env"
elif [ -f local.secrets.env ]; then
    SECRETS_FILE="local.secrets.env"
elif [ -f scripts/local.secrets.env ]; then
    SECRETS_FILE="scripts/local.secrets.env"
else
    echo "Secrets file (secrets.env or local.secrets.env) not found."
    exit 1
fi

source deployment.env
source "$SECRETS_FILE"

POSTGRES_ADMIN="restaurantadmin"

# -----------------------------------------------------------------------------
# Validate required secrets
# -----------------------------------------------------------------------------

required=(
    POSTGRES_ADMIN_PASSWORD
    JWT_KEY
    JWT_REFRESH_KEY
    JWT_ISSUER
    JWT_AUDIENCE
    GEMINI_API_KEY
)

for var in "${required[@]}"; do

    if [ -z "${!var:-}" ]; then
        echo "$var is empty."
        exit 1
    fi

done

# -----------------------------------------------------------------------------
# Build PostgreSQL Connection String
# -----------------------------------------------------------------------------

echo "Building PostgreSQL Connection String..."

CONNECTION_STRING="Host=${POSTGRES_FQDN};\
Port=5432;\
Database=${POSTGRES_DATABASE};\
Username=${POSTGRES_ADMIN};\
Password=${POSTGRES_ADMIN_PASSWORD};\
SSL Mode=Require;\
Trust Server Certificate=true"

echo "Done."
echo ""

# -----------------------------------------------------------------------------
# Upload Connection String
# -----------------------------------------------------------------------------

echo "Uploading Database Connection String..."

az keyvault secret set \
    --vault-name "$KEYVAULT_NAME" \
    --name "ConnectionStrings--DefaultConnection" \
    --value "$CONNECTION_STRING" \
    --output none

# -----------------------------------------------------------------------------
# Upload JWT
# -----------------------------------------------------------------------------

echo "Uploading JWT Configuration..."

az keyvault secret set \
    --vault-name "$KEYVAULT_NAME" \
    --name "JwtSettings--SecretKey" \
    --value "$JWT_KEY" \
    --output none

az keyvault secret set \
    --vault-name "$KEYVAULT_NAME" \
    --name "JwtSettings--RefreshKey" \
    --value "$JWT_REFRESH_KEY" \
    --output none

az keyvault secret set \
    --vault-name "$KEYVAULT_NAME" \
    --name "JwtSettings--Issuer" \
    --value "$JWT_ISSUER" \
    --output none

az keyvault secret set \
    --vault-name "$KEYVAULT_NAME" \
    --name "JwtSettings--Audience" \
    --value "$JWT_AUDIENCE" \
    --output none

# -----------------------------------------------------------------------------
# Upload Gemini API Key
# -----------------------------------------------------------------------------

echo "Uploading Gemini API Key..."

az keyvault secret set \
    --vault-name "$KEYVAULT_NAME" \
    --name "Gemini--ApiKey" \
    --value "$GEMINI_API_KEY" \
    --output none

# -----------------------------------------------------------------------------
# Upload SignalR Connection String
# -----------------------------------------------------------------------------

echo "Retrieving SignalR connection string..."

SIGNALR_CONNECTION=$(az signalr key list \
    --name "$SIGNALR_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query primaryConnectionString \
    -o tsv)

echo "Uploading SignalR Connection String..."

az keyvault secret set \
    --vault-name "$KEYVAULT_NAME" \
    --name "SignalR--ConnectionString" \
    --value "$SIGNALR_CONNECTION" \
    --output none

echo ""
echo "==========================================="
echo " Azure Key Vault Configured"
echo "==========================================="
echo ""

echo "Stored Secrets"

echo "✔ ConnectionStrings--DefaultConnection"
echo "✔ JwtSettings--SecretKey"
echo "✔ JwtSettings--RefreshKey"
echo "✔ JwtSettings--Issuer"
echo "✔ JwtSettings--Audience"
echo "✔ Gemini--ApiKey"
echo "✔ SignalR--ConnectionString"

echo ""
echo "Next Step:"
echo ""
echo "./scripts/04-deploy.sh"
echo ""