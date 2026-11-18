using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class AsyncConversionModel : PageModel
    {
        public void OnGet()
        {
            string files = Helper.WWWRoot(Request) + Url.Content("~/files/");
            TxtUrl1 = files + "document.html";
            TxtUrl2 = files + "document2.html";
            TxtUrl3 = files + "sample.html";
        }

        [BindProperty]
        public string TxtUrl1 { get; set; }

        [BindProperty]
        public string TxtUrl2 { get; set; }

        [BindProperty]
        public string TxtUrl3 { get; set; }

        [BindProperty]
        public bool ChkOneAfterAnother { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            string[] urls = new[] { TxtUrl1, TxtUrl2, TxtUrl3 }
                .Where(url => !string.IsNullOrWhiteSpace(url)).ToArray();

            Stopwatch watch = Stopwatch.StartNew();
            Document[] docs;

            if (ChkOneAfterAnother)
            {
                // for comparison: each conversion waits for the one before it
                docs = new Document[urls.Length];
                for (int i = 0; i < urls.Length; i++)
                {
                    PdfConverter converter = new PdfConverter();
                    docs[i] = await converter.GetPdfDocumentObjectFromUrlAsync(urls[i], HttpContext.RequestAborted);
                }
            }
            else
            {
                // start all the conversions, then await them together: the pages load and render at
                // the same time. One converter object per conversion.
                docs = await Task.WhenAll(urls.Select(url =>
                {
                    PdfConverter converter = new PdfConverter();
                    return converter.GetPdfDocumentObjectFromUrlAsync(url, HttpContext.RequestAborted);
                }));
            }

            watch.Stop();

            // join the converted documents into one
            Document doc = new Document();
            foreach (Document converted in docs)
            {
                doc.AppendDocument(converted);
            }

            // the time the conversions took, readable in the viewer's document properties
            doc.DocumentInformation.Title = string.Format("{0} pages converted {1} in {2:0.0} seconds",
                urls.Length, ChkOneAfterAnother ? "one after another" : "at the same time",
                watch.Elapsed.TotalSeconds);

            // save pdf document
            byte[] pdf = doc.Save();

            // close pdf documents
            doc.Close();
            foreach (Document converted in docs)
            {
                converted.Close();
            }

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
