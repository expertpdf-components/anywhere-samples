# ExpertPdf Anywhere on Azure Container Apps

A small HTTP service that converts a web page or an HTML string to PDF, deployed as a container to
Azure Container Apps.

```
GET  https://<app-url>/convert?url=https://www.html-to-pdf.net    converts a web page
POST https://<app-url>/convert   (body: html)                      converts an HTML string
```

## What is in the image

- the service (`csharp/Program.cs`, ASP.NET Core minimal API, .NET 10) on the stock
  `mcr.microsoft.com/dotnet/aspnet:10.0` image, running as that image's non-root `app` user;
- `ExpertPdf.Anywhere.Linux` - the library with the Chromium engine and every library it needs;
- `ExpertPdf.Anywhere.Fonts` - fonts for every language. The image has no fonts installed; without this
  package only Latin text renders, and Arabic, Chinese, Japanese, Korean, Hindi, Thai and other scripts
  print as empty boxes;
- `csharp/fonts/` - your own fonts, if your HTML uses fonts by name (see the README in that folder).

Nothing is installed in the image: no `apt-get`, no `chmod`, no special flags.

## Step by step

You need an Azure subscription, the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
and Docker. The examples use the `westeurope` region; the registry name must be globally unique.

**1. Sign in and prepare the subscription** (once):

```
az login
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
az provider register --namespace Microsoft.OperationalInsights --wait
```

**2. Create a resource group and a container registry:**

```
az group create --name expertpdf-rg --location westeurope
az acr create --resource-group expertpdf-rg --name expertpdfregistry --sku Basic --admin-enabled true
az acr login --name expertpdfregistry
```

**3. Build and push the image** - from the samples folder (the one that holds `cloud/`):

```
docker build --platform linux/amd64 -f cloud/azure-container-apps/Dockerfile -t expertpdfregistry.azurecr.io/expertpdf-containerapps:1 .
docker push expertpdfregistry.azurecr.io/expertpdf-containerapps:1
```

**4. Create the environment.** It can take several minutes to provision; wait until
`az containerapp env show --name expertpdf-env --resource-group expertpdf-rg --query properties.provisioningState`
reports `Succeeded` before the next step:

```
az containerapp env create --name expertpdf-env --resource-group expertpdf-rg --location westeurope
```

**5. Create the app:**

```
az containerapp create --name expertpdf-app --resource-group expertpdf-rg --environment expertpdf-env --image expertpdfregistry.azurecr.io/expertpdf-containerapps:1 --registry-server expertpdfregistry.azurecr.io --target-port 8080 --ingress external --cpu 2 --memory 4Gi --min-replicas 0 --max-replicas 3 --scale-rule-name http --scale-rule-type http --scale-rule-http-concurrency 8
```

- **`--cpu 2 --memory 4Gi`** - the largest consumption size; Chromium needs both.
- **`--scale-rule-http-concurrency 8`** - a new replica is started when one handles 8 requests, the
  number of conversions ExpertPdf Anywhere runs at once by default.
- **`--min-replicas 0`** scales to zero when idle; the first request then waits for a replica to start.
  Use `--min-replicas 1` to keep one ready.
- `--ingress external` makes the app reachable from the internet; use `internal` for a service only
  other apps in the environment call.

The command prints the app's address. Open `https://<address>/convert?url=https://www.html-to-pdf.net`.

**6. Delete everything** when you are done:

```
az group delete --name expertpdf-rg --yes
```

## What to expect

On the consumption plan with 2 vCPU / 4 GiB, live web sites convert; the first conversion in a new
replica also starts the engine, and later ones reuse it.

## Testing locally

```
docker build -f cloud/azure-container-apps/Dockerfile -t expertpdf-containerapps .
docker run --rm -p 8080:8080 expertpdf-containerapps
```

Then open http://localhost:8080/convert?url=https://www.html-to-pdf.net.

See the Azure Container Apps topic of the documentation for details:
https://docs.html-to-pdf.net/anywhere-library/html/Azure-Container-Apps.htm
