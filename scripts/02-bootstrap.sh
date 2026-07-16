#!/usr/bin/env bash

# =============================================================================
# Restaurant Management System
#
# 02-bootstrap.sh
#
# Responsibilities
#
# 1. Read deployment.env
# 2. Configure kubectl
# 3. Install NGINX Ingress Controller
# 4. Install Cert Manager
# 5. Install Azure Key Vault CSI Driver
# 6. Generate Kubernetes manifests
# 7. Apply ClusterIssuer
#
# =============================================================================

set -euo pipefail

# Navigate to the repository root directory
cd "$(dirname "${BASH_SOURCE[0]}")/.."

echo ""
echo "==========================================="
echo " AKS Bootstrap"
echo "==========================================="
echo ""

# -----------------------------------------------------------------------------
# Load deployment variables
# -----------------------------------------------------------------------------

if [ ! -f deployment.env ]; then
    echo "deployment.env not found."
    echo "Run 01-provision.sh first."
    exit 1
fi

source deployment.env

# Discover and load secrets file
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

source "$SECRETS_FILE"

# -----------------------------------------------------------------------------
# Check required tools
# -----------------------------------------------------------------------------

for cmd in az kubectl helm envsubst; do
    if ! command -v "$cmd" >/dev/null; then
        echo "$cmd is not installed."
        exit 1
    fi
done

# -----------------------------------------------------------------------------
# Get AKS Credentials
# -----------------------------------------------------------------------------

echo "Connecting to AKS..."

az aks get-credentials \
    --resource-group "$RESOURCE_GROUP" \
    --name "$AKS_NAME" \
    --overwrite-existing

echo "Connected."
echo ""

# -----------------------------------------------------------------------------
# Allow AKS Outbound IP to access PostgreSQL
# -----------------------------------------------------------------------------

echo "Resolving AKS outbound public IP..."

OUTBOUND_IP_ID=$(az aks show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$AKS_NAME" \
    --query "networkProfile.loadBalancerProfile.effectiveOutboundIPs[0].id" \
    -o tsv)

OUTBOUND_IP=$(az network public-ip show \
    --ids "$OUTBOUND_IP_ID" \
    --query ipAddress \
    -o tsv)

echo "AKS Outbound IP: $OUTBOUND_IP"

EXISTING_RULE=$(az postgres flexible-server firewall-rule show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$POSTGRES_NAME" \
    --rule-name "allow-aks-outbound" \
    --query startIpAddress -o tsv 2>/dev/null || true)

if [ "$EXISTING_RULE" = "$OUTBOUND_IP" ]; then
    echo "PostgreSQL firewall rule for AKS already exists. Skipping..."
else
    echo "Adding PostgreSQL firewall rule for AKS..."
    az postgres flexible-server firewall-rule create \
        --resource-group "$RESOURCE_GROUP" \
        --server-name "$POSTGRES_NAME" \
        --name "allow-aks-outbound" \
        --start-ip-address "$OUTBOUND_IP" \
        --end-ip-address "$OUTBOUND_IP" \
        --output none
    echo "Firewall rule created."
fi

echo ""

# -----------------------------------------------------------------------------
# Install NGINX Ingress
# -----------------------------------------------------------------------------

echo "Updating Helm Repositories..."
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo add csi-secrets-store-provider-azure https://azure.github.io/secrets-store-csi-driver-provider-azure/charts
helm repo update

DNS_LABEL="restaurant-api-srvsh0502"

helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
    --namespace ingress-nginx \
    --create-namespace \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/azure\-dns\-label\-name"="$DNS_LABEL" \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/port_80_health-probe_protocol"="tcp" \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/port_443_health-probe_protocol"="tcp" \
    --wait

echo "NGINX Ingress ready."
echo "Using DNS Label: $DNS_LABEL"
echo ""

# -----------------------------------------------------------------------------
# Install Azure Key Vault CSI Driver
# -----------------------------------------------------------------------------

if helm status csi -n kube-system >/dev/null 2>&1; then
    echo "Azure Key Vault CSI Driver already installed. Skipping..."
else
    echo "Installing Azure Key Vault CSI Driver..."
    helm install csi csi-secrets-store-provider-azure/csi-secrets-store-provider-azure \
        --namespace kube-system \
        --wait
    echo "CSI Driver Installed."
fi
echo ""

# -----------------------------------------------------------------------------
# Generate Kubernetes manifests
# -----------------------------------------------------------------------------

echo "Generating Kubernetes manifests..."

mkdir -p generated

export KEYVAULT_NAME
export WORKLOAD_IDENTITY_CLIENT_ID
export TENANT_ID
export ACR_LOGIN_SERVER
export KUBERNETES_NAMESPACE
export SERVICE_ACCOUNT_NAME

# ------------------------------------------------------------------
# API Hostname
#
# This will be updated after the LoadBalancer receives
# a public IP.
# ------------------------------------------------------------------

export API_HOSTNAME="${DNS_LABEL}.${LOCATION}.cloudapp.azure.com"

if [ -n "$FRONTEND_HOSTNAME" ]; then
    export ALLOWED_ORIGINS="http://localhost:4200,https://${FRONTEND_HOSTNAME}"
    export FRONTEND_URL="https://${FRONTEND_HOSTNAME}"
else
    export ALLOWED_ORIGINS="http://localhost:4200"
    export FRONTEND_URL="http://localhost:4200"
fi

for file in k8_configs/*.yaml
do
    envsubst < "$file" > "generated/$(basename "$file")"
done

echo "Generated manifests."
echo ""

# Add local developer IP to PostgreSQL firewall so local dotnet ef runs work
echo "Adding local developer IP to PostgreSQL firewall..."
MY_IP=$(curl -4 -s https://api.ipify.org || echo "")
if [ -n "$MY_IP" ]; then
    az postgres flexible-server firewall-rule create \
        --resource-group "$RESOURCE_GROUP" \
        --server-name "$POSTGRES_NAME" \
        --name "allow-my-ip" \
        --start-ip-address "$MY_IP" \
        --end-ip-address "$MY_IP" \
        --output none 2>/dev/null || true
    echo "Local IP $MY_IP added."
else
    echo "Warning: Could not determine local IP. Skipping allow-my-ip."
fi

echo ""

echo "==========================================="
echo " Bootstrap Complete"
echo "==========================================="

echo ""
echo "Next Step:"
echo ""
echo "./scripts/03-secrets.sh"
echo ""