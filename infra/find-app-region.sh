#!/usr/bin/env bash
# Finds regions where this subscription may create an App Service plan. Run it in Azure Cloud Shell (Bash):
#   bash find-app-region.sh            checks F1 and B1 in every region the subscription is allowed to use
#   bash find-app-region.sh B1         checks one plan size
#
# App Service quota is per region, and student subscriptions often have a limit of 0 in some regions.
# Each check is an ARM *validation* of a one-plan template (the same preflight check that rejected the
# deployment), so nothing is created and nothing is charged.
set -uo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-cmes-staffhr-rg}"
SKUS=("${@:-F1 B1}")
read -r -a SKUS <<< "${SKUS[*]}"

TEMPLATE=$(mktemp --suffix .json)
trap 'rm -f "$TEMPLATE"' EXIT
cat > "$TEMPLATE" <<'JSON'
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": { "region": { "type": "string" }, "sku": { "type": "string" } },
  "resources": [{
    "type": "Microsoft.Web/serverfarms", "apiVersion": "2023-12-01",
    "name": "[concat('quota-check-', parameters('sku'), '-', uniqueString(resourceGroup().id, parameters('region')))]",
    "location": "[parameters('region')]", "kind": "linux",
    "sku": { "name": "[parameters('sku')]" }, "properties": { "reserved": true }
  }]
}
JSON

# Regions the subscription's policy allows; if there is no such policy, a list of common regions.
REGIONS=$(az policy assignment list --disable-scope-strict-match \
  --query "[].parameters.listOfAllowedLocations.value[]" -o tsv 2>/dev/null | sort -u)
if [ -z "$REGIONS" ]; then
  REGIONS="canadacentral canadaeast eastus eastus2 centralus westus2 westus3 northcentralus southcentralus northeurope westeurope uksouth"
fi

echo "Subscription: $(az account show --query name -o tsv)"
echo "Checking App Service plans ${SKUS[*]} in: $(echo $REGIONS | tr '\n' ' ')"
echo
found=""
for region in $REGIONS; do
  for sku in "${SKUS[@]}"; do
    if result=$(az deployment group validate --resource-group "$RESOURCE_GROUP" --template-file "$TEMPLATE" \
         --parameters region="$region" sku="$sku" --output none 2>&1); then
      printf '  %-16s %-3s OK\n' "$region" "$sku"
      found="${found}${region} ${sku}\n"
    elif echo "$result" | grep -q SubscriptionIsOverQuotaForSku; then
      printf '  %-16s %-3s no quota\n' "$region" "$sku"
    else
      printf '  %-16s %-3s not allowed (%s)\n' "$region" "$sku" \
        "$(echo "$result" | grep -o '"code": *"[A-Za-z]*"' | tail -1 | sed 's/.*"\([A-Za-z]*\)"$/\1/')"
    fi
  done
done

echo
if [ -n "$found" ]; then
  first=$(printf "$found" | head -1)
  echo "Use one of the OK rows. In GitHub: Actions > Deploy to Azure > Run workflow, set"
  echo "  app_location     = ${first% *}"
  echo "  app_service_sku  = ${first#* }"
else
  echo "No region has App Service quota for this subscription. Request a quota of 1 on the Quotas page"
  echo "(https://aka.ms/azure-quota, Provider: App Service), or ask about another way to host it."
fi
