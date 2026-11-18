# ExpertPdf Anywhere - an application that converts through the expertpdf/engine container.
# Built and started together with the engine by compose.yaml in this folder.
#
# The application is on Alpine Linux (musl): it references only the managed ExpertPdf.Anywhere
# package and sends every conversion to the engine.
#
# Run from the samples folder (the folder that holds windows/, linux/, mac/ and docker/), through
# compose.yaml - see README.md.
#
# Packages are restored from nuget.org. To build against packages you have locally, put the
# .nupkg files in a "nupkg" folder next to the docker folder; they are used as an extra source.

# ---- build ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY docker/README.md nupk[g] ./local-packages/
RUN dotnet nuget add source /src/local-packages --name local-packages
COPY docker/remote-engine/csharp ./app
WORKDIR /src/app
RUN dotnet publish -c Release --os linux-musl -a $TARGETARCH --self-contained false -o /out

# ---- runtime -------------------------------------------------------------------------------
# Nothing is installed: the application carries no engine and needs no fonts of its own (the engine
# has them). The image's non-root "app" user runs the service.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /out ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpertPdf.Anywhere.Samples.RemoteEngine.dll"]
