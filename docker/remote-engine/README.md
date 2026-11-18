# ExpertPdf Anywhere with the remote engine (Docker Compose)

An application and the ExpertPdf engine as two containers. The application references only the managed
`ExpertPdf.Anywhere` package and runs on **Alpine Linux** (musl), where every conversion runs in the
`expertpdf/engine` container:

```
GET  http://localhost:8080/convert?url=https://www.html-to-pdf.net    converts a web page
POST http://localhost:8080/convert   (body: html)                      converts an HTML string
GET  http://localhost:8080/text?url=https://www.html-to-pdf.net       converts a page and returns its text
```

## Run it

From this folder:

```
export EXPERTPDF_ENGINE_TOKEN=$(openssl rand -base64 32)
docker compose up --build
```

PowerShell:

```
$b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
$env:EXPERTPDF_ENGINE_TOKEN = [Convert]::ToBase64String($b)
docker compose up --build
```

Or put `EXPERTPDF_ENGINE_TOKEN=...` in a `.env` file next to `compose.yaml`. A token must be at least 32
characters, or the engine does not start.

Compose starts the engine first and the application once the engine reports healthy. Then open
http://localhost:8080/convert?url=https://www.html-to-pdf.net.

## What makes the application use the engine

Configuration, not code - the conversion code is the same as for a local engine. In `compose.yaml`:

| setting | value |
|---|---|
| `EXPERTPDF_RENDERING_ENGINE` | `ChromiumRemote` |
| `EXPERTPDF_ENGINE_TOKEN` | one of the tokens the engine was started with |
| `EXPERTPDF_ENGINE_ENDPOINT` | not set: the default `http://expertpdf-engine:8080` is the engine service's name |

The same settings exist in code: `GlobalSettings.RenderingEngine`, `GlobalSettings.EngineEndpoint`
and `GlobalSettings.EngineAccessToken`.

## Files

- `compose.yaml` - the two services. The engine is not published on the host, only the application is.
- `Dockerfile` - builds `csharp/` and runs it on `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`. Run it from
  the samples folder (compose does).
- `csharp/` - the application: an ASP.NET Core minimal API that awaits each conversion, so a client that
  goes away cancels its conversion in the engine.

## Notes

- **The engine image**: `ghcr.io/expertpdf/engine:22.0` (Docker Hub mirror: `expertpdf/engine:22.0`), for
  x64 and arm64. Set `EXPERTPDF_ENGINE_IMAGE` to use another tag.
- **A shared engine**: when one engine serves different callers, uncomment
  `EXPERTPDF_ENGINE_HTTP_CACHE: clear` so no conversion is served a response another one fetched.
- **The application's own fonts** (a `fonts` folder in the project) are sent to the engine with each
  conversion; the engine has fonts for every language already.
- **Local files**: the engine cannot see the application's files - a `file://` url is refused. Convert the
  html as a string, or serve it over http.
- To build against `.nupkg` files you have locally, put them in a `nupkg` folder next to the `docker`
  folder; the build adds it as an extra package source.

The documentation's Remote Engine topic has the engine's settings, security and sizing:
https://docs.html-to-pdf.net/anywhere-library/html/Remote-Engine.htm
