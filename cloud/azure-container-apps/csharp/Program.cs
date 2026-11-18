using ExpertPdf.Anywhere.HtmlToPdf;

// HTML to PDF as an HTTP service in a container (Azure Container Apps, Google Cloud Run, ...).
//   GET  /convert?url=https://www.html-to-pdf.net   converts a web page
//   POST /convert   (body = html)                    converts an HTML string
// The container listens on the port in the PORT environment variable (8080 by default).

var builder = WebApplication.CreateBuilder(args);
string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls("http://0.0.0.0:" + port);
var app = builder.Build();

app.MapGet("/", () => Results.Text(
    "ExpertPdf HTML to PDF service. GET /convert?url=... or POST /convert with an HTML body."));

app.MapGet("/convert", (string url) =>
{
    if (string.IsNullOrEmpty(url))
        return Results.BadRequest("Pass the page to convert: /convert?url=https://...");
    return Pdf(NewConverter().GetPdfBytesFromUrl(url));
});

app.MapPost("/convert", async (HttpRequest request) =>
{
    string html = await new StreamReader(request.Body).ReadToEndAsync();
    if (string.IsNullOrEmpty(html))
        return Results.BadRequest("POST the HTML to convert as the request body.");
    return Pdf(NewConverter().GetPdfBytesFromHtmlString(html));
});

app.Run();

static PdfConverter NewConverter()
{
    PdfConverter converter = new PdfConverter();
    // converter.LicenseKey = "your license key";
    return converter;
}

static IResult Pdf(byte[] pdf)
{
    return Results.File(pdf, "application/pdf", "document.pdf");
}
