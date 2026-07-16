#!/usr/bin/env bash

# =============================================================================
# Restaurant Management System
#
# 04-deploy-backnd.sh
#
# Responsibilities
#
# • Run EF Core database migrations
# • Build and push Docker image to ACR
# • Connect to AKS
# • Create imagePullSecret
# • Deploy Kubernetes resources
# • Wait for deployment rollout
#
# =============================================================================

set -euo pipefail

# Navigate to the repository root directory
cd "$(dirname "${BASH_SOURCE[0]}")/.."

echo ""
echo "==========================================="
echo " Deploy Restaurant Management System"
echo "==========================================="
echo ""

# -----------------------------------------------------------------------------
# Validate configuration
# -----------------------------------------------------------------------------

if [ ! -f deployment.env ]; then
    echo "deployment.env not found."
    exit 1
fi

source deployment.env

# -----------------------------------------------------------------------------
# Run EF Core Migrations
# -----------------------------------------------------------------------------

CONNECTION_STRING=$(az keyvault secret show \
    --vault-name "$KEYVAULT_NAME" \
    --name "ConnectionStrings--DefaultConnection" \
    --query value -o tsv)

echo "Running database migrations..."

dotnet ef database update \
    --project OrderNKitchenMS-Engine/OrderNKitchenMS-API \
    --connection "$CONNECTION_STRING"

echo "Migrations applied."
echo ""

# -----------------------------------------------------------------------------
# Build and Push Docker Image to ACR
# -----------------------------------------------------------------------------

echo "Building and pushing Docker image to ACR..."

az acr login --name "$ACR_NAME"

docker build --platform linux/amd64 \
    -t "$ACR_LOGIN_SERVER/restaurant-api:latest" \
    -f OrderNKitchenMS-Engine/Dockerfile \
    OrderNKitchenMS-Engine/

docker push "$ACR_LOGIN_SERVER/restaurant-api:latest"

echo "Image pushed."
echo ""

# -----------------------------------------------------------------------------
# Connect to AKS
# -----------------------------------------------------------------------------

echo "Connecting to AKS..."

az aks get-credentials \
    --resource-group "$RESOURCE_GROUP" \
    --name "$AKS_NAME" \
    --overwrite-existing

echo "Connected."
echo ""

# -----------------------------------------------------------------------------
# Obtain ACR Credentials
# -----------------------------------------------------------------------------

echo "Retrieving ACR credentials..."

ACR_USERNAME=$(az acr credential show \
    --name "$ACR_NAME" \
    --query username \
    -o tsv)

ACR_PASSWORD=$(az acr credential show \
    --name "$ACR_NAME" \
    --query "passwords[0].value" \
    -o tsv)

echo "Done."
echo ""

# -----------------------------------------------------------------------------
# Apply Namespace
# -----------------------------------------------------------------------------

kubectl apply -f generated/namespace.yaml

# -----------------------------------------------------------------------------
# Create Image Pull Secret
# -----------------------------------------------------------------------------

echo "Creating imagePullSecret..."

kubectl create secret docker-registry acr-pull \
    --namespace "$KUBERNETES_NAMESPACE" \
    --docker-server="$ACR_LOGIN_SERVER" \
    --docker-username="$ACR_USERNAME" \
    --docker-password="$ACR_PASSWORD" \
    --dry-run=client \
    -o yaml | kubectl apply -f -

echo "Done."
echo ""

# -----------------------------------------------------------------------------
# Deploy Application Resources
# -----------------------------------------------------------------------------

kubectl apply -f generated/serviceaccount.yaml

kubectl apply -f generated/configmap.yaml

kubectl apply -f generated/secretproviderclass.yaml

kubectl apply -f generated/deployment.yaml

kubectl apply -f generated/service.yaml

kubectl apply -f generated/ingress.yaml

echo ""

# -----------------------------------------------------------------------------
# Wait for Rollout
# -----------------------------------------------------------------------------

echo "Waiting for Deployment..."

kubectl rollout status deployment/restaurant-api \
    -n "$KUBERNETES_NAMESPACE"

echo ""

# -----------------------------------------------------------------------------
# Show Resources
# -----------------------------------------------------------------------------

echo "Pods"

kubectl get pods -n "$KUBERNETES_NAMESPACE"

echo ""

echo "Services"

kubectl get svc -n "$KUBERNETES_NAMESPACE"

echo ""

echo "Ingress"

kubectl get ingress -n "$KUBERNETES_NAMESPACE"

echo ""

echo "Retrieving Ingress External IP..."
EXTERNAL_IP=""
for i in {1..12}; do
    EXTERNAL_IP=$(kubectl get svc ingress-nginx-controller -n ingress-nginx -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
    if [ -n "$EXTERNAL_IP" ]; then
        break
    fi
    echo "Waiting for LoadBalancer IP..."
    sleep 5
done

if [ -z "$EXTERNAL_IP" ]; then
    echo "Failed to retrieve Ingress External IP."
else
    echo "Ingress External IP: $EXTERNAL_IP"
    API_HOSTNAME="restaurant-api-srvsh0502.${LOCATION}.cloudapp.azure.com"

    echo ""
    echo "Checking ingress..."
    curl -H "Host: ${API_HOSTNAME}" \
         "http://${EXTERNAL_IP}/health/live"
    echo ""
fi

echo "==========================================="
echo " Deployment Completed"
echo "==========================================="