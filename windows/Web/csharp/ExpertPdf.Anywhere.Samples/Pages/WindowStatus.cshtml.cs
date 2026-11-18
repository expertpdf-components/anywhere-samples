using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class WindowStatusModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/window-status.html");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string TxtWindowStatus { get; set; } = "ready";

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";


            // wait until the page sets window.status to this text; give up after 30 seconds
            if (!string.IsNullOrEmpty(TxtWindowStatus))
                converter.WindowStatus = TxtWindowStatus;
            converter.NavigationTimeout = 30;

            // convert the url to pdf
            byte[] pdf = converter.GetPdfBytesFromUrl(TxtUrl);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "WindowStatus.pdf";
            return fileResult;
        }
    }
}
