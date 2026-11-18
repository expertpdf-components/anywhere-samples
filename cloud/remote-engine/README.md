# ExpertPdf Anywhere with the remote engine, in the cloud

An application and the ExpertPdf engine as two separate services on a cloud platform. The application
references only the managed `ExpertPdf.Anywhere` package, runs on **Alpine Linux** and sends every
conversion to the `expertpdf/engine` container over HTTP:

```
GET  <app-url>/convert?url=https://www.html-to-pdf.net    converts a web page
POST <app-url>/convert   (body: html)                      converts an HTML string
GET  <app-url>/text?url=https://www.html-to-pdf.net       converts a page and returns its text
```

| folder | platform | how the application reaches the engine |
|---|---|---|
| `azure-container-apps` | Azure Container Apps | an app with internal ingress, `http://expertpdf-engine` |
| `aws-ecs` | Amazon ECS on Fargate | a service with ECS Service Connect, the default `http://expertpdf-engine:8080` |
| `google-cloud-run` | Google Cloud Run | a service, at its `https://...run.app` address |
| `kubernetes` | any Kubernetes cluster (AKS, EKS, GKE, ...) | a Service named `expertpdf-engine`, the default `http://expertpdf-engine:8080` |

Each folder has a step-by-step README. The application is the one in `../../docker/remote-engine`, built
with its Dockerfile from the samples folder (the folder that holds `cloud/` and `docker/`), so the
conversion code is the same everywhere; only the configuration changes.

## What makes the application use the engine

Three settings, as environment variables (or `GlobalSettings` in code):

| setting | value |
|---|---|
| `EXPERTPDF_RENDERING_ENGINE` | `ChromiumRemote` |
| `EXPERTPDF_ENGINE_TOKEN` | one of the tokens the engine was started with |
| `EXPERTPDF_ENGINE_ENDPOINT` | the engine's address; not needed when it is `http://expertpdf-engine:8080` |

The library sends the token over plain HTTP only to a local address - a name without a dot,
`localhost`, a `.svc.cluster.local` name or a private IP address - and over HTTPS anywhere else. Each
platform's route is chosen so the engine is either private to the application's network or served over
HTTPS.

## The engine image

`ghcr.io/expertpdf-components/engine:22.0` (Docker Hub: `expertpdf/engine:22.0`), for x64 and arm64. It runs as a
non-root user with a read-only root file system and needs 2 vCPU and 4 GB of memory per replica for
real pages. The engine needs no license; the license key is set in the application.

The documentation's Remote Engine topic has its settings, security and sizing:
https://docs.html-to-pdf.net/anywhere-library/html/Remote-Engine.htm

## The token

Every route below keeps the token in the platform's secret store. A token must be at least 32
characters; generate one with:

```
openssl rand -hex 32
```

The commands in these READMEs are for bash (Linux, macOS or WSL) with Docker, which builds the
application image, and the platform's command-line tool.
