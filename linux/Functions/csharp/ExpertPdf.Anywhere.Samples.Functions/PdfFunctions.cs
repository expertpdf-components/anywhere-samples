using System.Net;
using ExpertPdf.Anywhere.HtmlToPdf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ExpertPdf.Anywhere.Samples.Functions;

/// <summary>
/// Converts HTML to PDF from an HTTP-triggered Azure Function.
/// </summary>
public class PdfFunctions
{
    /// <summary>
    /// GET or POST /api/ConvertHtml?html=... - converts an HTML string and returns the PDF.
    /// </summary>
    [Function("ConvertHtml")]
    public static HttpResponseData ConvertHtml(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        string html = GetParameter(req, "html")
                      ?? "<h1>ExpertPdf Anywhere</h1><p>Converted by an Azure Function.</p>";

        PdfConverter converter = NewConverter();
        byte[] pdf = converter.GetPdfBytesFromHtmlString(html);
        return Pdf(req, pdf, "Document.pdf");
    }

    /// <summary>
    /// GET or POST /api/ConvertUrl?url=https://www.html-to-pdf.net - converts a web page and returns the PDF.
    /// </summary>
    [Function("ConvertUrl")]
    public static HttpResponseData ConvertUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        string url = GetParameter(req, "url") ?? "https://www.html-to-pdf.net";

        PdfConverter converter = NewConverter();
        byte[] pdf = converter.GetPdfBytesFromUrl(url);
        return Pdf(req, pdf, "Document.pdf");
    }

    /// <summary>
    /// GET /api/EngineInfo - reports the application folder, the engine location the converters are
    /// configured with, and whether the application folder is writable. Useful when a deployment
    /// fails to start the engine.
    /// </summary>
    [Function("EngineInfo")]
    public static HttpResponseData EngineInfo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        string appDir = AppContext.BaseDirectory;

        // the converters in this sample keep the default engine location; a deployment that sets
        // CefEnginePath reports that folder here
        string enginePath = NewConverter().CefEnginePath;
        string engineLocation = string.IsNullOrEmpty(enginePath)
            ? "default (the engine folder beside the application)"
            : enginePath;

        string report =
            "Application folder : " + appDir + Environment.NewLine +
            "Engine location    : " + engineLocation + Environment.NewLine +
            "Application folder writable: " + IsWritable(appDir) + Environment.NewLine;

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString(report);
        return response;
    }

    private static PdfConverter NewConverter()
    {
        PdfConverter converter = new PdfConverter();
        // converter.LicenseKey = "your license key";
        return converter;
    }

    private static bool IsWritable(string folder)
    {
        try
        {
            string probe = Path.Combine(folder, Path.GetRandomFileName());
            File.WriteAllText(probe, "probe");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetParameter(HttpRequestData req, string name)
    {
        System.Collections.Specialized.NameValueCollection query =
            System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string value = query[name];
        if (!string.IsNullOrEmpty(value)) return value;

        if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            using StreamReader reader = new StreamReader(req.Body);
            string body = reader.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(body)) return body;
        }

        return null;
    }

    private static HttpResponseData Pdf(HttpRequestData req, byte[] pdf, string fileName)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/pdf");
        response.Headers.Add("Content-Disposition", "attachment; filename=" + fileName);
        response.WriteBytes(pdf);
        return response;
    }
}
