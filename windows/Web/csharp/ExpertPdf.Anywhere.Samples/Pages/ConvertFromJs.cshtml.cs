using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class ConvertFromJsModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/js-conversion.html");
            DdlStartupMode = "Manual";
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string DdlStartupMode { get; set; }

        public List<SelectListItem> StartupModes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Automatic", Text = "Automatic" },
            new SelectListItem { Value = "Manual", Text = "Manual" },
        };

        [BindProperty]
        public string TxtTimeout { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // set startup mode
            converter.StartupMode = (StartupMode)Enum.Parse(
                typeof(StartupMode), DdlStartupMode, true);

            // set timeout
            int timeout = 10;
            try
            {
                timeout = Convert.ToInt32(TxtTimeout);
            }
            catch { }
            converter.NavigationTimeout = timeout;

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
