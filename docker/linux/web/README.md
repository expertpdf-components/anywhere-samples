# ExpertPdf Anywhere - Linux web container

Builds the web sample in `linux/Web/csharp/ExpertPdf.Anywhere.Samples/` into a Linux container image.
The web app is the same Razor Pages catalog as the non-container sample.

## Prerequisites

- Docker with Linux containers.

## Build and run

From the samples folder (the one that holds `windows/`, `linux/`, `mac/` and `docker/`):

```bash
docker build -f docker/linux/web/Dockerfile -t expertpdf-anywhere:web-linux .
docker run --rm -p 8080:8080 expertpdf-anywhere:web-linux
```

Then open http://localhost:8080.

Or with Docker Compose, from this folder:

```bash
docker compose up --build
```

## Notes

- No `apt-get`: the Linux engine package carries the libraries the engine needs, and
  `ExpertPdf.Anywhere.Fonts` the fonts. The image runs with a plain `docker run` - no extra
  capabilities, no `--no-sandbox`, no display.
- Keep the same port inside and outside the container (`-p 8080:8080`): the pages that convert one of
  the site's own pages call the site at the address it was opened with.
- Packages are restored from nuget.org. To build against `.nupkg` files you have locally, put them
  in a `nupkg` folder next to the `docker` folder; the build adds it as an extra package source.
- Base images: `mcr.microsoft.com/dotnet/sdk:10.0` to build, `mcr.microsoft.com/dotnet/aspnet:10.0-noble` to run.

See the Docker topic of the documentation:
https://docs.html-to-pdf.net/anywhere-library/html/Deployment-Docker.htm
