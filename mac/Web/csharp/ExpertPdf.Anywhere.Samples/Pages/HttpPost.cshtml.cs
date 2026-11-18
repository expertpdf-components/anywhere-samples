using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class HttpPostModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/ViewHttpPost");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string TxtName1 { get; set; } = "customer";

        [BindProperty]
        public string TxtValue1 { get; set; } = "Muster Handel AG";

        [BindProperty]
        public string TxtName2 { get; set; } = "order";

        [BindProperty]
        public string TxtValue2 { get; set; } = "PO-7781";

        [BindProperty]
        public string TxtName3 { get; set; } = "period";

        [BindProperty]
        public string TxtValue3 { get; set; } = "Q3 2026";

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";


            // with at least one POST parameter the page is requested with the POST method and these name/value pairs
            // are sent as the form data
            AddField(converter, TxtName1, TxtValue1);
            AddField(converter, TxtName2, TxtValue2);
            AddField(converter, TxtName3, TxtValue3);

            // convert the url to pdf
            byte[] pdf = converter.GetPdfBytesFromUrl(TxtUrl);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "HttpPost.pdf";
            return fileResult;
        }

        private static void AddField(PdfConverter converter, string name, string value)
        {
            if (!string.IsNullOrEmpty(name))
                converter.HttpPostParameters.Add(name, value ?? string.Empty);
        }
    }
}
