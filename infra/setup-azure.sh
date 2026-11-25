#!/usr/bin/env bash
# One-time Azure setup for the "Deploy to Azure" GitHub workflow. Run it in Azure Cloud Shell (Bash), where
# you are already signed in:  bash setup-azure.sh <region>   e.g.  bash setup-azure.sh canadacentral
#
# It creates the resource group, registers the resource providers the template uses, and creates the identity
# GitHub Actions signs in with: a user-assigned managed identity with a federated credential, so no password is
# stored. A managed identity is an ordinary Azure resource, so this works in school directories where students
# may not create app registrations.
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
for provider in Microsoft.ManagedIdentity Microsoft.Web Microsoft.Sql Microsoft.KeyVault Microsoft.Insights Microsoft.OperationalInsights Microsoft.ServiceBus; do
  az provider register --namespace "$provider" --wait --output none
done

echo "3/4 Deployment identity $IDENTITY_NAME (managed identity in $RESOURCE_GROUP)"
az identity create --resource-group "$RESOURCE_GROUP" --name "$IDENTITY_NAME" --location "$LOCATION" --output none
CLIENT_ID=$(az identity show --resource-group "$RESOURCE_GROUP" --name "$IDENTITY_NAME" --query clientId -o tsv)
PRINCIPAL_ID=$(az identity show --resource-group "$RESOURCE_GROUP" --name "$IDENTITY_NAME" --query principalId -o tsv)
# Trust tokens GitHub issues to jobs in this repository's "production" environment (the workflow uses it).
# GitHub writes the token subject in one of two forms, depending on the repository's OIDC settings:
#   repo:owner/name:environment:production                      (classic)
#   repo:owner@<owner id>/name@<repository id>:environment:production   (with immutable ids)
# so register both. The ids come from the public GitHub API.
trust() {   # trust <credential name> <subject>
  if az identity federated-credential show --resource-group "$RESOURCE_GROUP" --identity-name "$IDENTITY_NAME" \
       --name "$1" --output none 2>/dev/null; then verb=update; else verb=create; fi
  az identity federated-credential "$verb" --resource-group "$RESOURCE_GROUP" --identity-name "$IDENTITY_NAME" \
    --name "$1" --issuer https://token.actions.githubusercontent.com --subject "$2" \
    --audiences api://AzureADTokenExchange --output none
  echo "    trusts $2"
}
trust github-production "repo:${REPOSITORY}:environment:production"
REPO_JSON=$(curl -fsS "https://api.github.com/repos/${REPOSITORY}" 2>/dev/null || true)
REPO_ID=$(echo "$REPO_JSON" | sed -n 's/^  "id": \([0-9]*\),$/\1/p' | head -1)
OWNER_ID=$(echo "$REPO_JSON" | sed -n 's/^    "id": \([0-9]*\),$/\1/p' | head -1)
if [ -n "$REPO_ID" ] && [ -n "$OWNER_ID" ]; then
  trust github-production-ids "repo:${REPOSITORY%%/*}@${OWNER_ID}/${REPOSITORY#*/}@${REPO_ID}:environment:production"
else
  echo "    (could not read the repository ids from GitHub; if sign-in fails with AADSTS700213, see docs/DEPLOYMENT.md)"
fi

echo "4/4 Permissions on the resource group only"
SCOPE=$(az group show --name "$RESOURCE_GROUP" --query id -o tsv)
# Contributor creates the resources; User Access Administrator lets the template give the web app's
# managed identity read access to Key Vault. The object id is used directly, so no directory lookup is needed.
for role in "Contributor" "User Access Administrator"; do
  for attempt in 1 2 3 4 5 6; do   # a brand-new identity can take a minute to be usable
    if az role assignment create --assignee-object-id "$PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
         --role "$role" --scope "$SCOPE" --output none 2>/dev/null; then
      echo "    $role: granted"; break
    fi
    [ "$attempt" = 6 ] && { echo "Could not grant $role. You need Owner on the subscription; check Access control (IAM)." >&2; exit 1; }
    sleep 15
  done
done

cat <<EOF

Done. Add these as repository secrets on GitHub (Settings > Secrets and variables > Actions > New repository secret):

  AZURE_CLIENT_ID        $CLIENT_ID
  AZURE_TENANT_ID        $(az account show --query tenantId -o tsv)
  AZURE_SUBSCRIPTION_ID  $(az account show --query id -o tsv)
  SQL_ADMIN_PASSWORD     a password you make up: 12+ characters with upper- and lower-case letters and digits
                         (avoid ; ' " characters, which would break the connection string)

Then run Actions > "Deploy to Azure" with resource group $RESOURCE_GROUP.
EOF
