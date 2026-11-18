# ExpertPdf Anywhere on Google Cloud Run

A small HTTP service that converts a web page or an HTML string to PDF, deployed as a container to
Google Cloud Run.

```
GET  https://<service-url>/convert?url=https://www.html-to-pdf.net    converts a web page
POST https://<service-url>/convert   (body: html)                      converts an HTML string
```

The same image runs unchanged on any container platform - Azure Container Apps, AWS App Runner or
ECS, Kubernetes - see also `../azure-container-apps`.

## What is in the image

- the service (`csharp/Program.cs`, ASP.NET Core minimal API, .NET 10) on the stock
  `mcr.microsoft.com/dotnet/aspnet:10.0` image, running as that image's non-root `app` user;
- `ExpertPdf.Anywhere.Linux` - the library with the Chromium engine and every library it needs
  (Cloud Run runs x86-64 containers);
- `ExpertPdf.Anywhere.Fonts` - fonts for every language. The image has no fonts installed; without this
  package only Latin text renders, and Arabic, Chinese, Japanese, Korean, Hindi, Thai and other scripts
  print as empty boxes;
- `csharp/fonts/` - your own fonts, if your HTML uses fonts by name (see the README in that folder).

Nothing is installed in the image: no `apt-get`, no `chmod`, no special flags.

## Step by step

**1. Set up the project** (once):

1. Sign in to the [Google Cloud console](https://console.cloud.google.com) and create a project
   (project picker > New project). Note its project ID.
2. Link a billing account to it (Billing > Link a billing account). New accounts get free trial credit.
3. Install the [gcloud CLI](https://cloud.google.com/sdk/docs/install), then:

```
gcloud auth login
gcloud config set project YOUR_PROJECT_ID
gcloud config set run/region europe-north1
gcloud services enable run.googleapis.com artifactregistry.googleapis.com cloudbuild.googleapis.com
gcloud artifacts repositories create expertpdf --repository-format=docker --location=europe-north1
gcloud auth configure-docker europe-north1-docker.pkg.dev
```

**2. Build and push the image** - from the samples folder (the one that holds `cloud/`):

```
docker build --platform linux/amd64 -f cloud/google-cloud-run/Dockerfile -t europe-north1-docker.pkg.dev/YOUR_PROJECT_ID/expertpdf/expertpdf-cloudrun .
docker push europe-north1-docker.pkg.dev/YOUR_PROJECT_ID/expertpdf/expertpdf-cloudrun
```

**3. Deploy:**

```
gcloud run deploy expertpdf-cloudrun --image europe-north1-docker.pkg.dev/YOUR_PROJECT_ID/expertpdf/expertpdf-cloudrun --cpu 2 --memory 4Gi --timeout 300 --concurrency 8 --no-allow-unauthenticated
```

- **`--cpu 2 --memory 4Gi`** - Chromium needs both. With 2 GiB a heavy page can run the instance out
  of memory (Cloud Run then restarts it and the request fails with 503).
- **`--concurrency 8`** - matches the 8 conversions ExpertPdf Anywhere runs at once by default
  (`PdfConverter.MaximumConcurrentConversions`); more requests per instance would only queue.
- **`--no-allow-unauthenticated`** - callers need an identity token. Use `--allow-unauthenticated` only
  for a public service.
- `--execution-environment gen2` is optional: the library runs in the default (first generation,
  gVisor) environment and in the second generation alike.

**4. Call it:**

```
curl -H "Authorization: Bearer $(gcloud auth print-identity-token)" "https://SERVICE_URL/convert?url=https://www.html-to-pdf.net" -o document.pdf
```

## What to expect

With 2 vCPU:

- live web sites convert, in both execution environments;
- the first conversion in a new instance also starts the engine. An idle
  instance is kept for a while, so occasional traffic often finds it warm; `--min-instances 1` keeps one
  ready at all times;
- a response can be up to 32 MB, so large PDFs are returned directly.

## Cloud Run functions

The same code also runs as a Cloud Run function (`gcloud functions deploy --gen2 --runtime dotnet8`
with the `Google.Cloud.Functions.Hosting` package and an `IHttpFunction` class). Google then builds the
image from source: the ExpertPdf Anywhere packages are restored and published like any other, and the
function runs on Ubuntu as a non-root user. Use `--memory 2Gi` (or more) and `--cpu 2`.

## Testing locally

```
docker build -f cloud/google-cloud-run/Dockerfile -t expertpdf-cloudrun .
docker run --rm -p 8080:8080 expertpdf-cloudrun
```

Then open http://localhost:8080/convert?url=https://www.html-to-pdf.net.

See the Google Cloud Run topic of the documentation for details:
https://docs.html-to-pdf.net/anywhere-library/html/Google-Cloud-Run.htm
