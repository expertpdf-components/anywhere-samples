# ExpertPdf Anywhere - samples

Samples for ExpertPdf Anywhere, the cross-platform ExpertPdf library for .NET, for Windows, Linux and macOS:
console apps in C# and VB.NET, and an ASP.NET Core Razor Pages web app with one page per feature.

## Folder layout

```
windows/   ExpertPdf.Anywhere.Samples.Windows.sln   Console (C#, VB.NET), Web (C#), Functions (C#)
linux/     ExpertPdf.Anywhere.Samples.Linux.sln     Console (C#, VB.NET), Web (C#), Functions (C#)
mac/       ExpertPdf.Anywhere.Samples.Mac.sln       Console (C#, VB.NET), Web (C#)
docker/    Linux and Windows container images (web, console) and the remote engine with Docker Compose
cloud/     AWS Lambda, AWS Elastic Beanstalk, Google Cloud Run, Azure Container Apps, and the remote
           engine on Azure Container Apps, Amazon ECS, Google Cloud Run and Kubernetes
```

The three copies are the same code. They differ only in the package they reference, so open the solution
for the operating system the samples run on. There is no Functions sample under `mac/`, because Azure
Functions does not host on macOS - develop it on a Mac against the `linux/` copy.

## Packages

The samples restore from nuget.org. Each references one platform package, which brings the library
(`ExpertPdf.Anywhere`) together with the engines for that platform:

| Platform | Package |
|---|---|
| Windows x64 | `ExpertPdf.Anywhere.Windows` |
| Linux x64 | `ExpertPdf.Anywhere.Linux` |
| Linux ARM64 | `ExpertPdf.Anywhere.Linux.Arm64` |
| macOS (Apple Silicon) | `ExpertPdf.Anywhere.MacOS` |

The `linux/` projects pick `ExpertPdf.Anywhere.Linux.Arm64` on an ARM64 machine or for `-r linux-arm64`, and
`ExpertPdf.Anywhere.Linux` otherwise (override with `-p:AnywherePlatform=Linux.Arm64`). They also reference
`ExpertPdf.Anywhere.Fonts`, fonts for every script, for Linux hosts and containers that have none installed.

To reference the packages yourself instead: `ExpertPdf.Anywhere` and one `ExpertPdf.Anywhere.Native.<rid>`
package (`win-x64`, `win-x86`, `linux-x64`, `linux-arm64` or `osx-arm64`) for the platform the application
runs on. The engines run in their own process, so a 32-bit application can use the x64 engine and an x64
application under Rosetta can use the Apple Silicon one.

## Requirements

The samples target .NET 10. Built with the .NET 11 SDK they also target .NET 11; pick the framework with
`dotnet run -f net10.0` or `dotnet run -f net11.0`. The library itself runs on .NET 11, .NET 10, .NET 9 and
.NET 8, and on .NET Framework 4.6.1 or later.

## Console samples

Each console sample is a project of its own, under `Console/csharp` and `Console/vb.net`:

| Sample | What it shows |
|---|---|
| `HtmlToPdfConverter` | A web page, an HTML file or an HTML string to PDF or to an image, with the converter's options on the command line (`--help` lists them) |
| `BatchConversion` | Every HTML file of a folder converted to PDF |
| `MultithreadedPerformance` | Conversions on many threads at once, with the throughput measured |
| `PageObjects` | Reading and changing what a page of an existing PDF draws: text replaced, an image moved, a layer hidden |
| `PdfStandards` | PDF/A, PDF/X and tagged PDF/UA output |
| `ElectronicInvoice` | A ZUGFeRD / Factur-X electronic invoice: a PDF/A-3 document with the invoice XML inside |
| `MergePdfCommandLine`, `MergePdfFeatures` | Merging PDF documents |
| `SplitPdfCommandLine`, `SplitPdfFeatures` | Splitting a PDF document |
| `PdfSecurityCommandLine`, `PdfSecurityFeatures` | Passwords, permissions and encryption |
| `PdfDigitalSignature` | Signing a PDF document |
| `ConvertPdfToText`, `TextSearch` | Text extraction, search, and the text of an area of a page |
| `ConvertPdfToImages`, `PdfToImageInMemory` | Rendering PDF pages to images |
| `ExtractPdfImages`, `ExtractImagesInMemory` | Extracting the images of a PDF document |

```bash
cd windows/Console/csharp/HtmlToPdfConverter
dotnet run
dotnet run -- --url https://www.html-to-pdf.net -o output.pdf
```

Use the `linux/` or `mac/` copy on those systems.

## Web sample

```bash
cd windows/Web/csharp/ExpertPdf.Anywhere.Samples
dotnet run
```

Then open the address printed on the console. Each page converts or builds a document from the values in
its form and shows its own C# source. The same samples run online at https://samples.html-to-pdf.net/anywhere/.

## Azure Functions sample

An HTTP-triggered function that converts HTML to PDF. Azure Functions v4 on the .NET isolated worker,
`net10.0`.

```bash
cd windows/Functions/csharp/ExpertPdf.Anywhere.Samples.Functions
dotnet publish -c Release -o publish
```

Three endpoints: `ConvertHtml` (`?html=...`), `ConvertUrl` (`?url=...`) and `EngineInfo`, which reports the
engine location the converters use and whether the application folder is writable - useful when a
deployment starts but conversions do not.

Deploy the published folder as a zip, the way `func azure functionapp publish`, Visual Studio or
`az functionapp deployment source config-zip` does it. Two things decide whether it works:

- **Use a Dedicated (App Service) or Elastic Premium plan.** The engine is a real browser, so it needs a
  plan that can run one. It does not work on a Consumption plan.
- **Do not turn on Run From Package.** `WEBSITE_RUN_FROM_PACKAGE` mounts the deployment zip as a read-only
  package that the engine cannot be started from. Neither creating a function app on a Dedicated plan nor
  deploying to it sets that, so the default path is fine - this matters only if you enable it yourself.

The Azure Functions topic of the documentation has the details:
https://docs.html-to-pdf.net/anywhere-library/html/Azure-Functions.htm

## Docker samples

`docker/{linux,windows}/{web,console}` are multi-stage Dockerfiles that build the existing web and
`HtmlToPdfConverter` console projects (no copy of the application code) for Linux and Windows containers.
The Linux images run with a plain `docker run` - no `apt-get`, no `--no-sandbox`, no extra capabilities,
no display. `docker/remote-engine` runs an Alpine application that sends every conversion to the
`expertpdf/engine` container, with Docker Compose. See `docker/README.md`.

The build context is this samples folder, and packages restore from nuget.org. To build against
`.nupkg` files you have locally, copy them into a `nupkg` folder here (next to `docker/` and `cloud/`).

## Cloud samples

`cloud/{aws-lambda,aws-elastic-beanstalk,google-cloud-run,azure-container-apps}`, each with a step-by-step
README. The three container samples share one small service (GET or POST `/convert`) and build from this
samples folder like the Docker samples; the Elastic Beanstalk one publishes the `windows/` and `linux/`
web samples into Beanstalk source bundles. Azure App Service and Azure Functions use the `Web` and
`Functions` samples as they are.

`cloud/remote-engine/{azure-container-apps,aws-ecs,google-cloud-run,kubernetes}` deploy the
`docker/remote-engine` application and the engine as two services; see `cloud/remote-engine/README.md`.

The documentation has a topic per platform: https://docs.html-to-pdf.net/anywhere-library/html/Cloud-Deployments.htm

## License key

Without a license key the library runs in evaluation mode and marks the documents it produces. Set your key
once, before the library is used - `LicensingManager.LicenseKey = "your license key";` - or on a converter or
tool (`converter.LicenseKey = "your license key";`, where the samples show it).

## Resources

* Documentation: https://docs.html-to-pdf.net/anywhere-library/html/Index.htm
* Online samples: https://samples.html-to-pdf.net/anywhere/
* Website: https://www.html-to-pdf.net
* Support: office@html-to-pdf.net
