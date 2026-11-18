# ExpertPdf Anywhere with the remote engine on Azure Container Apps

Two container apps in one environment: the engine, with **internal** ingress, and the application,
with external ingress. Apps in the environment reach the engine as `http://expertpdf-engine`; nothing
outside the environment can.

```
internet --> expertpdf-app (external, HTTPS) --> http://expertpdf-engine (internal) --> the pages it converts
```

## Step by step

You need an Azure subscription, the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
and Docker. The registry name must be globally unique; change `expertpdfregistry` below.

**1. Sign in and prepare the subscription** (once):

```
az login
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
az provider register --namespace Microsoft.OperationalInsights --wait
```

**2. Create a resource group, a container registry and the environment:**

```
RG=expertpdf-rg
LOCATION=westeurope
ACR=expertpdfregistry
az group create --name $RG --location $LOCATION
az acr create --resource-group $RG --name $ACR --sku Basic --admin-enabled true
az containerapp env create --name expertpdf-env --resource-group $RG --location $LOCATION
```

The environment can take several minutes to provision.

**3. Build and push the application image** - from the samples folder (the one that holds
`cloud/` and `docker/`):

```
az acr login --name $ACR
docker build --platform linux/amd64 -f docker/remote-engine/Dockerfile -t $ACR.azurecr.io/expertpdf-remote-app:1 .
docker push $ACR.azurecr.io/expertpdf-remote-app:1
```

**4. Create the engine** with internal ingress. Its name, `expertpdf-engine`, is the address the
application uses:

```
TOKEN=$(openssl rand -hex 32)
az containerapp create --name expertpdf-engine --resource-group $RG --environment expertpdf-env \
  --image ghcr.io/expertpdf/engine:22.0 \
  --ingress internal --target-port 8080 --transport http \
  --cpu 2 --memory 4Gi --min-replicas 1 --max-replicas 3 \
  --secrets "engine-token=$TOKEN" \
  --env-vars EXPERTPDF_ENGINE_TOKEN=secretref:engine-token
```

- **`--min-replicas 1`** - an engine scaled to zero starts Chromium on the first request after it.
- **`--cpu 2 --memory 4Gi`** - what one engine replica needs for real pages. Container Apps adds
  replicas, up to `--max-replicas`, when the requests in flight grow.

**5. Create the application:**

```
az containerapp create --name expertpdf-app --resource-group $RG --environment expertpdf-env \
  --image $ACR.azurecr.io/expertpdf-remote-app:1 --registry-server $ACR.azurecr.io \
  --ingress external --target-port 8080 \
  --cpu 0.5 --memory 1Gi --min-replicas 0 --max-replicas 3 \
  --secrets "engine-token=$TOKEN" \
  --env-vars EXPERTPDF_RENDERING_ENGINE=ChromiumRemote \
             EXPERTPDF_ENGINE_ENDPOINT=http://expertpdf-engine \
             EXPERTPDF_ENGINE_TOKEN=secretref:engine-token
```

The application carries no browser, so it is small: half a vCPU is enough, and it can scale to zero.

**6. Convert:**

```
APP=$(az containerapp show --name expertpdf-app --resource-group $RG --query properties.configuration.ingress.fqdn -o tsv)
curl -o document.pdf "https://$APP/convert?url=https://www.html-to-pdf.net"
curl "https://$APP/text?url=https://www.html-to-pdf.net"
```

**7. Delete everything** when you are done:

```
az group delete --name $RG --yes
```

## Notes

- **The token** is stored as a Container Apps secret in both apps. To rotate it without downtime, give
  the engine both tokens separated by a comma, move the application to the new one, then drop the old.
- **One engine, several applications**: any app in the environment can use the engine with the token.
  When they are different callers, add `EXPERTPDF_ENGINE_HTTP_CACHE=clear` to the engine's
  `--env-vars`, so no conversion is served a response another one fetched.
- **Local files**: the engine cannot see the application's files - a `file://` url is refused. Convert
  the html as a string, or serve it over http.

The documentation's Remote Engine topic has the engine's settings: https://docs.html-to-pdf.net/anywhere-library/html/Remote-Engine.htm
