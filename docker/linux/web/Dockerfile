# ExpertPdf Anywhere - ASP.NET Razor Pages web sample in a Linux container.
#
# Builds the sample project in linux/Web/csharp/ExpertPdf.Anywhere.Samples/ - the same code as the
# non-container sample. The conversion engines come with the ExpertPdf.Anywhere.Linux package and are
# published with the app; nothing is copied by hand.
#
# Run from the samples folder (the folder that holds windows/, linux/, mac/ and docker/):
#
#   docker build -f docker/linux/web/Dockerfile -t expertpdf-anywhere:web-linux .
#   docker run --rm -p 8080:8080 expertpdf-anywhere:web-linux
#   # then open http://localhost:8080
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
COPY linux/Web/csharp/ExpertPdf.Anywhere.Samples ./app
WORKDIR /src/app
RUN dotnet publish -c Release -f net10.0 -o /out

# ---- runtime -------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
# No system packages are needed: the Linux engine package carries the libraries the Chromium engine
# uses, ExpertPdf.Anywhere.Fonts the fonts, and the engine renders without a display.
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpertPdf.Anywhere.Samples.dll"]
