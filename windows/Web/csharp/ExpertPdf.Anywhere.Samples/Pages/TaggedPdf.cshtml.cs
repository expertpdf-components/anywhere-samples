using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class TaggedPdfModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/sample.html");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string TxtTitle { get; set; } = "ExpertPdf HTML to PDF Converter - Sample Page";

        [BindProperty]
        public string TxtLanguage { get; set; } = "en-US";

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";


            // create a tagged PDF: the structure of the HTML (headings, lists, tables, figures with alternate text,
            // links, reading order) is kept in the PDF for screen readers
            converter.Tagged = true;

            // the language the text is read in, and the title shown instead of the file name; both are required
            // by PDF/UA (without a title, the title of the HTML page is used)
            converter.Language = TxtLanguage;
            converter.PdfDocumentInfo.Title = TxtTitle;

            // convert the url to pdf
            byte[] pdf = converter.GetPdfBytesFromUrl(TxtUrl);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Tagged.pdf";
            return fileResult;
        }
    }
}
