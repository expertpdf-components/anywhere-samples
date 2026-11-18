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
    public class PartialConversionModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string TxtElements { get; set; }

        [BindProperty]
        public string TxtSelector { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // specify the id of the section that will be converted to pdf
            if (!string.IsNullOrEmpty(TxtElements))
            {
                converter.RenderElementId = TxtElements;
            }

            // or a CSS selector; when both are set the selector wins
            if (!string.IsNullOrEmpty(TxtSelector))
            {
                converter.RenderElementSelector = TxtSelector;
            }

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
