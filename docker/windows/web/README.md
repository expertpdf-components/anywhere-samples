# ExpertPdf Anywhere - Windows web container

Builds the web sample in `windows/Web/csharp/ExpertPdf.Anywhere.Samples/` into a Windows container image.
The web app is the same Razor Pages catalog as the non-container sample.

## Prerequisites

- Docker on a Windows host, switched to Windows containers.

## Build and run

From the samples folder (the one that holds `windows/`, `linux/`, `mac/` and `docker/`):

```powershell
docker build -f docker/windows/web/Dockerfile -t expertpdf-anywhere:web-windows .
docker run --rm -p 8080:8080 expertpdf-anywhere:web-windows
```

Then open http://localhost:8080.

Or with Docker Compose, from this folder:

```powershell
docker compose up --build
```

## Notes

- Windows Server Core images ship almost no fonts; the Dockerfile copies the standard Windows
  families from Microsoft's full Windows Server image and registers them. Nano Server is not
  supported.
- Keep the same port inside and outside the container (`-p 8080:8080`): the pages that convert one of
  the site's own pages call the site at the address it was opened with.
- Packages are restored from nuget.org. To build against `.nupkg` files you have locally, put them
  in a `nupkg` folder next to the `docker` folder; the build adds it as an extra package source.
- Base images: `mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022` to build, `mcr.microsoft.com/dotnet/aspnet:10.0-windowsservercore-ltsc2022` to run.

See the Docker topic of the documentation:
https://docs.html-to-pdf.net/anywhere-library/html/Deployment-Docker.htm
