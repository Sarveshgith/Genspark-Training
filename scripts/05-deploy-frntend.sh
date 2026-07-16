#!/usr/bin/env bash

set -euo pipefail

# Navigate to the repository root directory
cd "$(dirname "${BASH_SOURCE[0]}")/.."

source deployment.env
source ./scripts/secrets.env

echo "Building Angular..."

cd OrderNKitchenMS

npm ci

npm run build -- --configuration production

echo "Deploying..."

npx -y @azure/static-web-apps-cli deploy \
    dist/OrderNKitchenMS/browser \
    --deployment-token "$SWA_DEPLOYMENT_TOKEN"

echo "Done."