# ExpertPdf Anywhere - the HtmlToPdfConverter console sample in a Linux container.
#
# Builds the sample project in linux/Console/csharp/HtmlToPdfConverter/ - the same code as the
# non-container sample. The conversion engines come with the ExpertPdf.Anywhere.Linux package and are
# published with the app; nothing is copied by hand.
#
# Run from the samples folder (the folder that holds windows/, linux/, mac/ and docker/):
#
#   docker build -f docker/linux/console/Dockerfile -t expertpdf-anywhere:console-linux .
#   docker run --rm -v "$PWD/out:/out" expertpdf-anywhere:console-linux
#   # the PDF is written to ./out/output.pdf; pass your own arguments to convert something else
#
# Packages are restored from nuget.org. To build against packages you have locally, put the
# .nupkg files in a "nupkg" folder next to the docker folder; they are used as an extra source.

# ---- build ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# The optional nupkg folder (see above). docker/README.md is always present, so the copy never
# fails when there is no nupkg folder.
COPY docker/README.md nupk[g] ./local-packages/
RUN dotnet nuget add source /src/local-packages --name local-packages
COPY linux/Console/csharp/HtmlToPdfConverter ./app
WORKDIR /src/app
RUN dotnet publish -c Release -f net10.0 -o /out

# ---- runtime -------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble AS runtime
# No system packages are needed: the Linux engine package carries the libraries the Chromium engine
# uses, ExpertPdf.Anywhere.Fonts the fonts, and the engine renders without a display.
WORKDIR /app
COPY --from=build /out ./
# Output PDFs are written under /out - mount a host folder there to collect them.
VOLUME /out
ENTRYPOINT ["dotnet", "HtmlToPdfConverter.dll"]
# Default: convert the ExpertPdf home page. Override with your own arguments, e.g.
# --html "<h1>Hi</h1>" -o /out/x.pdf
CMD ["--url", "https://www.html-to-pdf.net", "-o", "/out/output.pdf"]
