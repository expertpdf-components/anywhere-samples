using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using ExpertPdf.Anywhere.HtmlToPdf;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ExpertPdf.Anywhere.Samples.Lambda;

/// <summary>
/// HTML to PDF in an AWS Lambda function, called through a function URL (or API Gateway HTTP API).
///   GET  ?url=https://www.html-to-pdf.net   converts a web page
///   POST (body = html)                       converts an HTML string
/// The PDF is returned in the response. Lambda limits a response to 6 MB (and the PDF is base64
/// encoded in it), so a larger PDF is stored in the S3 bucket named by the PDF_BUCKET environment
/// variable and the function redirects to a temporary link to it.
/// </summary>
public class Function
{
    // Lambda's 6 MB limit applies to the base64 text: about 4.4 MB of PDF.
    private const int MaxInlinePdfBytes = 4_400_000;

    private static readonly AmazonS3Client S3 = new AmazonS3Client();

    public async Task<APIGatewayHttpApiV2ProxyResponse> Handler(
        APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        try
        {
            byte[] pdf = Convert(request);

            if (pdf.Length <= MaxInlinePdfBytes)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Headers = new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/pdf",
                        ["Content-Disposition"] = "inline; filename=document.pdf"
                    },
                    Body = System.Convert.ToBase64String(pdf),
                    IsBase64Encoded = true
                };
            }

            string bucket = Environment.GetEnvironmentVariable("PDF_BUCKET");
            if (string.IsNullOrEmpty(bucket))
                return Text(HttpStatusCode.RequestEntityTooLarge,
                    "The PDF is " + pdf.Length + " bytes, over what a Lambda response can carry. " +
                    "Set the PDF_BUCKET environment variable to an S3 bucket to receive large PDFs.");

            string key = "pdf/" + Guid.NewGuid().ToString("N") + ".pdf";
            using (var stream = new MemoryStream(pdf))
            {
                await S3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    InputStream = stream,
                    ContentType = "application/pdf"
                });
            }
            string link = await S3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Redirect,
                Headers = new Dictionary<string, string> { ["Location"] = link }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.ToString());
            return Text(HttpStatusCode.InternalServerError, "Conversion failed: " + ex.Message);
        }
    }

    private static byte[] Convert(APIGatewayHttpApiV2ProxyRequest request)
    {
        PdfConverter converter = new PdfConverter();
        // converter.LicenseKey = "your license key";

        string url = null;
        request.QueryStringParameters?.TryGetValue("url", out url);
        if (!string.IsNullOrEmpty(url))
            return converter.GetPdfBytesFromUrl(url);

        string html = request.Body;
        if (request.IsBase64Encoded && html != null)
            html = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(html));
        if (string.IsNullOrEmpty(html))
            html = "<h1>ExpertPdf Anywhere on AWS Lambda</h1><p>Pass ?url=... or POST some HTML.</p>";
        return converter.GetPdfBytesFromHtmlString(html);
    }

    private static APIGatewayHttpApiV2ProxyResponse Text(HttpStatusCode status, string message)
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = (int)status,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain; charset=utf-8" },
            Body = message
        };
    }
}
