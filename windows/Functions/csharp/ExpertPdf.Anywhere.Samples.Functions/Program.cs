using Microsoft.Extensions.Hosting;

// Azure Functions v4 on the .NET isolated worker. Nothing ExpertPdf-specific happens here -
// the converter is created per request in PdfFunctions.
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
