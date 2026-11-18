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
    public class MediaTypesModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string DdlCssMediaType { get; set; }

        public List<SelectListItem> CssMediaTypes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Screen", Text = "Screen" },
            new SelectListItem { Value = "Print", Text = "Print" },
        };

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // set css media type
            converter.UsePrintCSSMediaType = DdlCssMediaType == "Print";

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
