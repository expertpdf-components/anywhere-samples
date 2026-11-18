# ExpertPdf Anywhere - HTML to PDF in an AWS Lambda function (container image).
#
# Run from the samples folder (the folder that holds cloud/, docker/, linux/, windows/):
#
#   docker build --platform linux/amd64 --provenance=false --sbom=false \
#       -f cloud/aws-lambda/Dockerfile -t expertpdf-lambda .
#
# For an arm64 (Graviton) function, build with --platform linux/arm64 --build-arg RID=linux-arm64.
# --provenance=false --sbom=false: Lambda accepts only a plain image manifest.
#
# Packages are restored from nuget.org. To build against packages you have locally, put the
# .nupkg files in a "nupkg" folder next to the cloud folder; they are used as an extra source.

# ---- build ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG RID=linux-x64
WORKDIR /src
COPY cloud/aws-lambda/README.md nupk[g] ./local-packages/
RUN dotnet nuget add source /src/local-packages --name local-packages
COPY cloud/aws-lambda/csharp ./app
WORKDIR /src/app
RUN dotnet publish -c Release -r $RID --self-contained false -o /out

# ---- runtime -------------------------------------------------------------------------------
# The AWS Lambda base image for .NET 10. Nothing else is installed: the Linux engine package carries
# the libraries the Chromium engine uses, and ExpertPdf.Anywhere.Fonts the fonts.
FROM public.ecr.aws/lambda/dotnet:10
COPY --from=build /out/ ${LAMBDA_TASK_ROOT}/
CMD ["ExpertPdf.Anywhere.Samples.Lambda::ExpertPdf.Anywhere.Samples.Lambda.Function::Handler"]
