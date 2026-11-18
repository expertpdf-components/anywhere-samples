# ExpertPdf Anywhere with the remote engine on Google Cloud Run

Two Cloud Run services: the engine and the application. The application calls the engine at its
`https://...run.app` address, so the token always travels over HTTPS.

```
client --> expertpdf-app --> https://expertpdf-engine-...run.app --> the pages it converts
```

## Step by step

**1. Set up the project** (once). You need a Google Cloud project with billing, the
[gcloud CLI](https://cloud.google.com/sdk/docs/install) and Docker:

```
gcloud auth login
PROJECT=YOUR_PROJECT_ID
REGION=europe-north1
gcloud config set project $PROJECT
gcloud config set run/region $REGION
gcloud services enable run.googleapis.com artifactregistry.googleapis.com secretmanager.googleapis.com
gcloud artifacts repositories create expertpdf --repository-format=docker --location=$REGION
gcloud auth configure-docker $REGION-docker.pkg.dev
```

**2. Build and push the application image** - from the samples folder (the one that holds
`cloud/` and `docker/`):

```
IMAGE=$REGION-docker.pkg.dev/$PROJECT/expertpdf/expertpdf-remote-app:1
docker build --platform linux/amd64 -f docker/remote-engine/Dockerfile -t $IMAGE .
docker push $IMAGE
```

**3. Store the token in Secret Manager** and let the services read it (they run as the project's default
compute service account):

```
printf %s "$(openssl rand -hex 32)" | gcloud secrets create expertpdf-engine-token --data-file=-
SA=$(gcloud projects describe $PROJECT --format='value(projectNumber)')-compute@developer.gserviceaccount.com
gcloud secrets add-iam-policy-binding expertpdf-engine-token \
  --member=serviceAccount:$SA --role=roles/secretmanager.secretAccessor
```

**4. Deploy the engine:**

```
gcloud run deploy expertpdf-engine --image ghcr.io/expertpdf-components/engine:22.0 \
  --port 8080 --cpu 2 --memory 4Gi --concurrency 8 --timeout 600 \
  --execution-environment gen2 --min-instances 1 \
  --no-invoker-iam-check \
  --set-secrets EXPERTPDF_ENGINE_TOKEN=expertpdf-engine-token:latest
```

- **`--no-invoker-iam-check` is required.** Without it Cloud Run reads the `Authorization: Bearer`
  header of a request from another Cloud Run service as a Google identity token, and refuses the
  engine's token before it reaches the engine. The engine still checks its own token on every request.
- **`--min-instances 1`** keeps one engine started; an engine scaled to zero starts Chromium on the first
  request after it.
- Cloud Run pulls the public image from ghcr.io directly. Google recommends Artifact Registry, which can
  mirror it through a remote repository.

**5. Deploy the application** with the engine's address:

```
ENGINE=$(gcloud run services describe expertpdf-engine --format='value(status.url)')
gcloud run deploy expertpdf-app --image $IMAGE \
  --cpu 1 --memory 512Mi --concurrency 40 \
  --no-allow-unauthenticated \
  --set-env-vars EXPERTPDF_RENDERING_ENGINE=ChromiumRemote,EXPERTPDF_ENGINE_ENDPOINT=$ENGINE \
  --set-secrets EXPERTPDF_ENGINE_TOKEN=expertpdf-engine-token:latest
```

The application carries no browser, so it is small. `--no-allow-unauthenticated` keeps it private: only
callers with the Cloud Run Invoker role can use it.

**6. Convert:**

```
APP=$(gcloud run services describe expertpdf-app --format='value(status.url)')
curl -H "Authorization: Bearer $(gcloud auth print-identity-token)" -o document.pdf "$APP/convert?url=https://www.html-to-pdf.net"
curl -H "Authorization: Bearer $(gcloud auth print-identity-token)" "$APP/text?url=https://www.html-to-pdf.net"
```

**7. Delete everything** when you are done:

```
gcloud run services delete expertpdf-app --quiet
gcloud run services delete expertpdf-engine --quiet
gcloud secrets delete expertpdf-engine-token --quiet
gcloud artifacts repositories delete expertpdf --location=$REGION --quiet
```

## Notes

- **The engine's address is reachable from the internet**, as a Cloud Run address is by default; the
  token is what protects it, so keep the token secret and rotate it if it leaks (the engine accepts
  several tokens separated by commas, so the application can move to a new one first).
- **The engine's health from outside** is at `$ENGINE/health`. Cloud Run serves no path ending in `z`
  from outside, so the older `/healthz` answers 404 there; Cloud Run's own startup probe reaches the
  container directly either way.
- **One engine, several applications**: any service with the token can use the engine. When they are
  different callers, add `--set-env-vars EXPERTPDF_ENGINE_HTTP_CACHE=clear` to the engine, so no
  conversion is served a response another one fetched.
- **Local files**: the engine cannot see the application's files - a `file://` url is refused. Convert
  the html as a string, or serve it over http.

The documentation's Remote Engine topic has the engine's settings: https://docs.html-to-pdf.net/anywhere-library/html/Remote-Engine.htm
