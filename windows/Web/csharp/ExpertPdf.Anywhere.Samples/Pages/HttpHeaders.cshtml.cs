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
    public class HttpHeadersModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/ViewHttpHeaders");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string TxtName1 { get; set; }

        [BindProperty]
        public string TxtValue1 { get; set; }

        [BindProperty]
        public string TxtName2 { get; set; }

        [BindProperty]
        public string TxtValue2 { get; set; }

        [BindProperty]
        public string TxtName3 { get; set; }

        [BindProperty]
        public string TxtValue3 { get; set; }

        [BindProperty]
        public string TxtName4 { get; set; }

        [BindProperty]
        public string TxtValue4 { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // set the HTTP headers
            converter.HttpHeaders.Add(TxtName1, TxtValue1);
            converter.HttpHeaders.Add(TxtName2, TxtValue2);
            converter.HttpHeaders.Add(TxtName3, TxtValue3);
            converter.HttpHeaders.Add(TxtName4, TxtValue4);

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
