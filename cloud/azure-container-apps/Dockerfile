# ExpertPdf Anywhere - HTML to PDF as an HTTP service on Azure Container Apps.
#
# Run from the samples folder (the folder that holds cloud/, docker/, linux/, windows/):
#
#   docker build -f cloud/azure-container-apps/Dockerfile -t expertpdf-containerapps .
#   docker run --rm -p 8080:8080 expertpdf-containerapps
#   # then open http://localhost:8080/convert?url=https://www.html-to-pdf.net
#
# Packages are restored from nuget.org. To build against packages you have locally, put the
# .nupkg files in a "nupkg" folder next to the cloud folder; they are used as an extra source.

# ---- build ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY cloud/azure-container-apps/README.md nupk[g] ./local-packages/
RUN dotnet nuget add source /src/local-packages --name local-packages
COPY cloud/azure-container-apps/csharp ./app
WORKDIR /src/app
RUN dotnet publish -c Release -o /out

# ---- runtime -------------------------------------------------------------------------------
# Nothing is installed: the Linux engine package carries the libraries the Chromium engine uses, and
# ExpertPdf.Anywhere.Fonts the fonts. The image's non-root "app" user runs the service.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpertPdf.Anywhere.Samples.ContainerApps.dll"]
