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
    public class PdfConverterPropertiesModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // create a new pdf document converting an url
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

            // get conversion result (contains document info from the web page)
            HtmlDocumentInfo result = converter.HtmlDocumentInfo;

            // set the document properties
            doc.DocumentInformation.Title = result.HtmlDocTitle;
            doc.DocumentInformation.Subject = result.HtmlDocDescription;
            doc.DocumentInformation.Keywords = result.HtmlDocKeywords;

            doc.DocumentInformation.Author = "ExpertPdf Samples";
            doc.DocumentInformation.CreationDate = DateTime.Now;

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
