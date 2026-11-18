using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class AccessiblePdfModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/sample.html");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string DdlStandard { get; set; } = "UA1";

        [BindProperty]
        public string TxtTitle { get; set; } = "ExpertPdf Anywhere sample";

        [BindProperty]
        public string TxtLanguage { get; set; } = "en-US";

        public List<SelectListItem> Standards { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "UA1", Text = "PDF/UA-1" },
            new SelectListItem { Value = "UA2", Text = "PDF/UA-2 (PDF 2.0)" },
            new SelectListItem { Value = "A3A_UA1", Text = "PDF/A-3a + PDF/UA-1" },
            new SelectListItem { Value = "A2A_UA1", Text = "PDF/A-2a + PDF/UA-1" },
            new SelectListItem { Value = "A4_UA2", Text = "PDF/A-4 + PDF/UA-2" },
        };

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            // the accessibility standard, and the archiving level declared with it
            switch (DdlStandard)
            {
                case "UA2":
                    converter.AccessibilityStandard = PdfUAStandard.Pdf_UA_2;
                    break;
                case "A3A_UA1":
                    converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_3a;
                    converter.AccessibilityStandard = PdfUAStandard.Pdf_UA_1;
                    break;
                case "A2A_UA1":
                    converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_2a;
                    converter.AccessibilityStandard = PdfUAStandard.Pdf_UA_1;
                    break;
                case "A4_UA2":
                    converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_4;
                    converter.AccessibilityStandard = PdfUAStandard.Pdf_UA_2;
                    break;
                default:
                    converter.AccessibilityStandard = PdfUAStandard.Pdf_UA_1;
                    break;
            }

            // an accessible document needs a title and a language
            converter.PdfDocumentInfo.Title = TxtTitle;
            converter.Language = TxtLanguage;

            // convert the url to pdf
            byte[] pdf = converter.GetPdfBytesFromUrl(TxtUrl);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Accessible.pdf";
            return fileResult;
        }
    }
}
