# Builds the two Elastic Beanstalk source bundles of the ExpertPdf Anywhere web sample:
#   out\expertpdf-eb-windows.zip   for the "Windows Server ... running IIS" platform
#   out\expertpdf-eb-linux.zip     for the "Amazon Linux 2023 ... running .NET 10" platform
# Run from this folder: powershell -ExecutionPolicy Bypass -File build-bundles.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# ZipFile.CreateFromDirectory in Windows PowerShell names the entries with backslashes, which Linux
# extracts as file names containing a backslash; this writes forward slashes on every PowerShell.
function New-Zip([string]$dir, [string]$zip) {
    $root = (Resolve-Path $dir).Path.TrimEnd('\') + '\'
    $archive = [IO.Compression.ZipFile]::Open($zip, 'Create')
    try {
        foreach ($file in Get-ChildItem $dir -Recurse -File) {
            $name = $file.FullName.Substring($root.Length).Replace('\', '/')
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $name)
        }
    } finally { $archive.Dispose() }
}
$here = $PSScriptRoot
$samples = Resolve-Path (Join-Path $here '..\..')
$out = Join-Path $here 'out'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null

# Windows / IIS: published self-contained, so the site does not depend on the .NET version the platform
# installs; the deployment manifest tells Beanstalk to install site.zip as the IIS site.
$site = Join-Path $out 'windows\site'
dotnet publish (Join-Path $samples 'windows\Web\csharp\ExpertPdf.Anywhere.Samples') -c Release -f net10.0 -r win-x64 --self-contained true -o $site
if ($LASTEXITCODE -ne 0) { throw 'publish failed (windows)' }
# A <rewrite> section in web.config matters only for a sub-application; the site is installed at the
# root here, and the section needs a module the IIS of the Beanstalk platform may not have (IIS then
# answers 500.19), so it is removed if present.
$config = Join-Path $site 'web.config'
[xml]$xml = Get-Content $config
$rewrite = $xml.configuration.'system.webServer'.rewrite
if ($rewrite) { [void]$rewrite.ParentNode.RemoveChild($rewrite) }
# Self-contained, IIS starts ExpertPdf.Anywhere.Samples.exe itself; it takes no dll argument.
$xml.configuration.'system.webServer'.aspNetCore.SetAttribute('arguments', '')
$xml.Save($config)
$bundle = Join-Path $out 'windows\bundle'
New-Item -ItemType Directory -Path $bundle | Out-Null
New-Zip $site (Join-Path $bundle 'site.zip')
Copy-Item (Join-Path $here 'windows\aws-windows-deployment-manifest.json') $bundle
New-Zip $bundle (Join-Path $out 'expertpdf-eb-windows.zip')

# Amazon Linux (".NET 10" platform): the published app plus a Procfile; Beanstalk runs it behind
# nginx, which forwards to port 5000.
$app = Join-Path $out 'linux\app'
dotnet publish (Join-Path $samples 'linux\Web\csharp\ExpertPdf.Anywhere.Samples') -c Release -f net10.0 -r linux-x64 --self-contained false -o $app
if ($LASTEXITCODE -ne 0) { throw 'publish failed (linux)' }
Copy-Item (Join-Path $here 'linux\Procfile') $app
New-Zip $app (Join-Path $out 'expertpdf-eb-linux.zip')

Get-ChildItem $out -Filter *.zip | ForEach-Object { '{0}  {1:N0} MB' -f $_.Name, ($_.Length / 1MB) }
