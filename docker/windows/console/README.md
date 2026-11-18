# ExpertPdf Anywhere - Windows console container

Builds the console sample in `windows/Console/csharp/HtmlToPdfConverter/` into a Windows container image.
The container is run-once: it converts a web page or an HTML string to a PDF and exits.

## Prerequisites

- Docker on a Windows host, switched to Windows containers.

## Build and run

From the samples folder (the one that holds `windows/`, `linux/`, `mac/` and `docker/`):

```powershell
docker build -f docker/windows/console/Dockerfile -t expertpdf-anywhere:console-windows .

# default: converts https://www.html-to-pdf.net to out\output.pdf
docker run --rm -v "${PWD}\out:C:\out" expertpdf-anywhere:console-windows

# or pass your own arguments (they replace the default ones)
docker run --rm -v "${PWD}\out:C:\out" expertpdf-anywhere:console-windows --html "<h1>Hello</h1>" -o C:\out\hello.pdf
docker run --rm expertpdf-anywhere:console-windows --help
```

The PDF lands in the `out` folder of the directory you run from.

Or with Docker Compose, from this folder:

```powershell
docker compose run --build --rm expertpdf-console
```

## Notes

- Windows Server Core images ship almost no fonts; the Dockerfile copies the standard Windows
  families from Microsoft's full Windows Server image and registers them. Nano Server is not
  supported.
- Packages are restored from nuget.org. To build against `.nupkg` files you have locally, put them
  in a `nupkg` folder next to the `docker` folder; the build adds it as an extra package source.
- Base images: `mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022` to build, `mcr.microsoft.com/dotnet/runtime:10.0-windowsservercore-ltsc2022` to run.

See the Docker topic of the documentation:
https://docs.html-to-pdf.net/anywhere-library/html/Deployment-Docker.htm
