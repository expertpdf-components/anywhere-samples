using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class HtmlToPdfConverterLinksModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/document.html");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public bool ChkInternalLinks { get; set; }

        [BindProperty]
        public bool ChkExternalLinks { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // set links options
            converter.PdfDocumentOptions.InternalLinksEnabled = ChkInternalLinks;
            converter.PdfDocumentOptions.LiveUrlsEnabled = ChkExternalLinks;

            // create a new pdf document converting an url
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

            // save pdf document
            byte[] pdf = doc.Save();

            // close pdf document
            doc.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;

        }

    }
}
