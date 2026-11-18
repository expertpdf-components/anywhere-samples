using ExpertPdf.Anywhere;
using ExpertPdf.Anywhere.HtmlToPdf;

// HTML to PDF as an HTTP service whose conversions run in the expertpdf/engine container.
//   GET  /convert?url=https://www.html-to-pdf.net   converts a web page
//   POST /convert   (body = html)                    converts an HTML string
//   GET  /text?url=https://www.html-to-pdf.net      converts a web page and returns its text
//
// The code is the same as for a local engine. What sends the work to the engine is configuration -
// in compose.yaml here, or the platform settings in the cloud/remote-engine samples:
//   EXPERTPDF_RENDERING_ENGINE=ChromiumRemote
//   EXPERTPDF_ENGINE_TOKEN=<one of the engine's tokens>
//   EXPERTPDF_ENGINE_ENDPOINT (not set: the default http://expertpdf-engine:8080 is the engine service)
// The same three can be set in code instead, once at startup:
//   GlobalSettings.RenderingEngine = RenderingEngine.ChromiumRemote;
//   GlobalSettings.EngineEndpoint = "http://expertpdf-engine:8080";
//   GlobalSettings.EngineAccessToken = "...";

var builder = WebApplication.CreateBuilder(args);
string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls("http://0.0.0.0:" + port);
var app = builder.Build();

app.MapGet("/", () => Results.Text(
    "ExpertPdf HTML to PDF service (engine: " + GlobalSettings.RenderingEngine + " at " +
    GlobalSettings.EngineEndpoint + "). GET /convert?url=..., POST /convert with an HTML body, " +
    "GET /text?url=..."));

app.MapGet("/convert", async (string url, CancellationToken aborted) =>
{
    if (string.IsNullOrEmpty(url))
        return Results.BadRequest("Pass the page to convert: /convert?url=https://...");
    // The awaited conversion holds no thread while the engine works; a client that goes away
    // cancels the conversion in the engine.
    byte[] pdf = await new PdfConverter().GetPdfBytesFromUrlAsync(url, aborted);
    return Results.File(pdf, "application/pdf", "document.pdf");
});

app.MapPost("/convert", async (HttpRequest request, CancellationToken aborted) =>
{
    string html = await new StreamReader(request.Body).ReadToEndAsync();
    if (string.IsNullOrEmpty(html))
        return Results.BadRequest("POST the HTML to convert as the request body.");
    byte[] pdf = await new PdfConverter().GetPdfBytesFromHtmlStringAsync(html, aborted);
    return Results.File(pdf, "application/pdf", "document.pdf");
});

// The PDF tools run in the engine too.
app.MapGet("/text", async (string url, CancellationToken aborted) =>
{
    byte[] pdf = await new PdfConverter().GetPdfBytesFromUrlAsync(url, aborted);
    using var textConverter = new PdfToTextConverter();
    using var stream = new MemoryStream(pdf);
    return Results.Text(textConverter.ConvertToText(stream));
});

app.Run();
