#!/bin/sh
# Builds the two Elastic Beanstalk source bundles of the ExpertPdf Anywhere web sample:
#   out/expertpdf-eb-windows.zip   for the "Windows Server ... running IIS" platform
#   out/expertpdf-eb-linux.zip     for the "Amazon Linux 2023 ... running .NET 10" platform
# Run from this folder: sh build-bundles.sh   (needs the .NET SDK and zip)
set -e
here=$(cd "$(dirname "$0")" && pwd)
samples=$(cd "$here/../.." && pwd)
out="$here/out"
rm -rf "$out"; mkdir -p "$out/windows/bundle"

# Windows / IIS: self-contained, installed as the IIS site by the deployment manifest.
dotnet publish "$samples/windows/Web/csharp/ExpertPdf.Anywhere.Samples" -c Release -f net10.0 -r win-x64 --self-contained true -o "$out/windows/site"
# A <rewrite> section in web.config matters only for a sub-application and needs a module the IIS of
# the Beanstalk platform may not have (IIS then answers 500.19), so it is removed if present.
sed -i '/<rewrite>/,/<\/rewrite>/d' "$out/windows/site/web.config"
# Self-contained, IIS starts ExpertPdf.Anywhere.Samples.exe itself; it takes no dll argument.
sed -i 's#arguments="[^"]*"#arguments=""#' "$out/windows/site/web.config"
(cd "$out/windows/site" && zip -qr "$out/windows/bundle/site.zip" .)
cp "$here/windows/aws-windows-deployment-manifest.json" "$out/windows/bundle/"
(cd "$out/windows/bundle" && zip -qr "$out/expertpdf-eb-windows.zip" .)

# Amazon Linux: the published app plus a Procfile; Beanstalk runs it behind nginx on port 5000.
dotnet publish "$samples/linux/Web/csharp/ExpertPdf.Anywhere.Samples" -c Release -f net10.0 -r linux-x64 --self-contained false -o "$out/linux/app"
cp "$here/linux/Procfile" "$out/linux/app/"
(cd "$out/linux/app" && zip -qr "$out/expertpdf-eb-linux.zip" .)

ls -la "$out"/*.zip
