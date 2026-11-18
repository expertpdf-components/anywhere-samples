using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class RepeatTableHeaderModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/table/table_with_repeated_head.html");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            converter.PdfDocumentOptions.TopMargin = 20;
            converter.PdfDocumentOptions.BottomMargin = 20;

            // lay out the page for printing: the thead of the table is repeated on every page with the print media type
            converter.UsePrintCSSMediaType = true;

            // convert the url to pdf
            byte[] pdf = converter.GetPdfBytesFromUrl(TxtUrl);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "RepeatTableHeader.pdf";
            return fileResult;
        }
    }
}
