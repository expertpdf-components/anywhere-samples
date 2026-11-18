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
    public class PageBreaksModel : PageModel
    {
        public void OnGet()
        {
            TxtHtmlCode = Helper.PageBreaksText();
        }

        [BindProperty]
        public string TxtHtmlCode { get; set; }

        [BindProperty]
        public string TxtBaseUrl { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            converter.PdfDocumentOptions.TopMargin = 10;
            converter.PdfDocumentOptions.BottomMargin = 10;
            converter.PdfDocumentOptions.LeftMargin = 10;
            converter.PdfDocumentOptions.RightMargin = 10;

            // create a new pdf document converting an url
            Document doc = converter.GetPdfDocumentObjectFromHtmlString(TxtHtmlCode, TxtBaseUrl);

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
