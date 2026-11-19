#!/usr/bin/env bash
# One-time Azure setup for the "Deploy to Azure" GitHub workflow. Run it in Azure Cloud Shell (Bash), where
# you are already signed in:  bash setup-azure.sh <region>   e.g.  bash setup-azure.sh canadacentral
#
# It creates the resource group, registers the resource providers the template uses, and creates the identity
# GitHub Actions signs in with (an app registration with a federated credential, so no password is stored).
# Safe to run again. At the end it prints the values for the repository secrets.
set -euo pipefail

LOCATION="${1:?Usage: bash setup-azure.sh <region>   (see docs/DEPLOYMENT.md for how to find an allowed region)}"
RESOURCE_GROUP="${RESOURCE_GROUP:-cmes-staffhr-rg}"
REPOSITORY="${REPOSITORY:-kavatess/CSCN73030-26F-Sec1-ProjectV-Group-6}"   # owner/name, exactly as on GitHub
IDENTITY_NAME="${IDENTITY_NAME:-cmes-staffhr-github-deploy}"

echo "Subscription: $(az account show --query name -o tsv)"

echo "1/4 Resource group $RESOURCE_GROUP in $LOCATION"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

echo "2/4 Registering resource providers (first time only; can take a few minutes)"
for provider in Microsoft.Web Microsoft.Sql Microsoft.KeyVault Microsoft.Insights Microsoft.OperationalInsights Microsoft.ServiceBus; do
  az provider register --namespace "$provider" --wait --output none
done

echo "3/4 Deployment identity $IDENTITY_NAME"
APP_ID=$(az ad app list --display-name "$IDENTITY_NAME" --query "[0].appId" -o tsv)
if [ -z "$APP_ID" ]; then
  APP_ID=$(az ad app create --display-name "$IDENTITY_NAME" --query appId -o tsv)
fi
az ad sp show --id "$APP_ID" --output none 2>/dev/null || az ad sp create --id "$APP_ID" --output none
# Trust tokens GitHub issues to jobs in this repository's "production" environment (the workflow uses it).
if ! az ad app federated-credential list --id "$APP_ID" --query "[?name=='github-production']" -o tsv | grep -q .; then
  az ad app federated-credential create --id "$APP_ID" --parameters "{
    \"name\": \"github-production\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${REPOSITORY}:environment:production\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" --output none
fi

echo "4/4 Permissions on the resource group only"
SCOPE=$(az group show --name "$RESOURCE_GROUP" --query id -o tsv)
# Contributor creates the resources; User Access Administrator lets the template give the web app's
# managed identity read access to Key Vault.
for role in "Contributor" "User Access Administrator"; do
  for attempt in 1 2 3 4 5; do   # a brand-new service principal can take a minute to appear
    az role assignment create --assignee "$APP_ID" --role "$role" --scope "$SCOPE" --output none 2>/dev/null && break
    sleep 15
  done
done

cat <<EOF

Done. Add these as repository secrets on GitHub (Settings > Secrets and variables > Actions > New repository secret):

  AZURE_CLIENT_ID        $APP_ID
  AZURE_TENANT_ID        $(az account show --query tenantId -o tsv)
  AZURE_SUBSCRIPTION_ID  $(az account show --query id -o tsv)
  SQL_ADMIN_PASSWORD     a password you make up: 12+ characters with upper- and lower-case letters and digits
                         (avoid ; ' " characters, which would break the connection string)

Then run Actions > "Deploy to Azure" with resource group $RESOURCE_GROUP.
EOF
