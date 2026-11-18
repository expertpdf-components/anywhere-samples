# ExpertPdf Anywhere - Linux console container

Builds the console sample in `linux/Console/csharp/HtmlToPdfConverter/` into a Linux container image.
The container is run-once: it converts a web page or an HTML string to a PDF and exits.

## Prerequisites

- Docker with Linux containers.

## Build and run

From the samples folder (the one that holds `windows/`, `linux/`, `mac/` and `docker/`):

```bash
docker build -f docker/linux/console/Dockerfile -t expertpdf-anywhere:console-linux .

# default: converts https://www.html-to-pdf.net to out/output.pdf
docker run --rm -v "$PWD/out:/out" expertpdf-anywhere:console-linux

# or pass your own arguments (they replace the default ones)
docker run --rm -v "$PWD/out:/out" expertpdf-anywhere:console-linux --html "<h1>Hello</h1>" -o /out/hello.pdf
docker run --rm expertpdf-anywhere:console-linux --help
```

The PDF lands in the `out` folder of the directory you run from.

Or with Docker Compose, from this folder:

```bash
docker compose run --build --rm expertpdf-console
```

## Notes

- No `apt-get`: the Linux engine package carries the libraries the engine needs, and
  `ExpertPdf.Anywhere.Fonts` the fonts. The image runs with a plain `docker run` - no extra
  capabilities, no `--no-sandbox`, no display.
- Packages are restored from nuget.org. To build against `.nupkg` files you have locally, put them
  in a `nupkg` folder next to the `docker` folder; the build adds it as an extra package source.
- Base images: `mcr.microsoft.com/dotnet/sdk:10.0` to build, `mcr.microsoft.com/dotnet/runtime:10.0-noble` to run.

See the Docker topic of the documentation:
https://docs.html-to-pdf.net/anywhere-library/html/Deployment-Docker.htm
