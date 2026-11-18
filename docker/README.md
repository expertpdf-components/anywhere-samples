# ExpertPdf Anywhere - Docker samples

Container builds of the ExpertPdf Anywhere samples, for Linux and Windows containers. Each Dockerfile builds
one of the existing sample projects, so the container runs exactly what the non-container sample runs.

| Folder | App | Container | Runtime image |
|---|---|---|---|
| [`linux/web`](linux/web) | Razor Pages web catalog | Linux | `aspnet:10.0-noble` |
| [`linux/console`](linux/console) | command-line HTML to PDF converter (run-once) | Linux | `runtime:10.0-noble` |
| [`windows/web`](windows/web) | Razor Pages web catalog | Windows | `aspnet:10.0-windowsservercore-ltsc2022` |
| [`windows/console`](windows/console) | command-line HTML to PDF converter (run-once) | Windows | `runtime:10.0-windowsservercore-ltsc2022` |
| [`remote-engine`](remote-engine) | an Alpine app converting through the `expertpdf/engine` container (Docker Compose) | Linux | `aspnet:10.0-alpine` + `expertpdf/engine` |

Each folder's `README.md` has the build and run commands. All of them run from the samples folder - the
one that holds `windows/`, `linux/`, `mac/` and `docker/` - which is the build context.

## How the images are built

- Multi-stage: an SDK stage restores and publishes the sample project, a runtime stage carries the
  published app. The conversion engines come with the platform package (`ExpertPdf.Anywhere.Linux`,
  `ExpertPdf.Anywhere.Windows`) and are published with the app.
- Packages are restored from nuget.org. To build against `.nupkg` files you have locally, put them in a
  `nupkg` folder next to this `docker` folder; the build adds it as an extra package source.
- **Linux**: no `apt-get`. The Linux engine package carries the libraries the engine needs, and
  `ExpertPdf.Anywhere.Fonts` the fonts, so the app runs on the plain .NET runtime image, with a plain
  `docker run`.
- **Windows**: Server Core images ship almost no fonts, and the Chromium engine needs the standard Windows
  families, so the Dockerfiles copy them from Microsoft's full Windows Server image and register them.
  Nano Server is not supported.

The documentation's Docker topic has base images, a minimal Dockerfile for your own app, non-root
containers and ARM64: https://docs.html-to-pdf.net/anywhere-library/html/Deployment-Docker.htm
