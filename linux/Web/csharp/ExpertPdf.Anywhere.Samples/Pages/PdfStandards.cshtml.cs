using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class PdfStandardsModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/sample.html");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string DdlStandard { get; set; } = "Pdf_A_3b";

        public List<SelectListItem> Standards { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Pdf_A_1b", Text = "PDF/A-1b" },
            new SelectListItem { Value = "Pdf_A_1a", Text = "PDF/A-1a (tagged)" },
            new SelectListItem { Value = "Pdf_A_2b", Text = "PDF/A-2b" },
            new SelectListItem { Value = "Pdf_A_2u", Text = "PDF/A-2u" },
            new SelectListItem { Value = "Pdf_A_2a", Text = "PDF/A-2a (tagged)" },
            new SelectListItem { Value = "Pdf_A_3b", Text = "PDF/A-3b" },
            new SelectListItem { Value = "Pdf_A_3u", Text = "PDF/A-3u" },
            new SelectListItem { Value = "Pdf_A_3a", Text = "PDF/A-3a (tagged)" },
            new SelectListItem { Value = "Pdf_A_4", Text = "PDF/A-4" },
            new SelectListItem { Value = "Pdf_A_4e", Text = "PDF/A-4e" },
            new SelectListItem { Value = "Pdf_A_4f", Text = "PDF/A-4f" },
        };

        [BindProperty]
        public bool CbAttachSource { get; set; } = true;

        public IActionResult OnPost()
        {
            PdfStandardSubset standard = (PdfStandardSubset)Enum.Parse(typeof(PdfStandardSubset), DdlStandard);

            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";


            // the PDF/A level; the A levels are also tagged, so they need a language for the text
            converter.PdfStandardSubset = standard;
            if (standard == PdfStandardSubset.Pdf_A_1a || standard == PdfStandardSubset.Pdf_A_2a ||
                standard == PdfStandardSubset.Pdf_A_3a)
                converter.Language = "en-US";

            // PDF/A requires a title
            converter.PdfDocumentInfo.Title = "ExpertPdf Anywhere - PDF/A Sample";

            // get the PDF as a Document object to add an associated file before saving it
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

            // PDF/A-3 documents can embed any file, linked to the document by a relationship; here the HTML page the
            // PDF was created from (Source). The stream is read when the document is saved: keep it open until then.
            bool pdfA3 = standard == PdfStandardSubset.Pdf_A_3b || standard == PdfStandardSubset.Pdf_A_3u ||
                standard == PdfStandardSubset.Pdf_A_3a;
            MemoryStream htmlStream = null;
            if (CbAttachSource && pdfA3)
            {
                using (HttpClient http = new HttpClient())
                {
                    htmlStream = new MemoryStream(http.GetByteArrayAsync(TxtUrl).Result);
                }
                doc.AddAssociatedFile(htmlStream, "page.html", AttachmentRelationship.Source, "text/html",
                    "The HTML page this document was created from");
            }

            byte[] pdf;
            try
            {
                // save pdf document
                pdf = doc.Save();
            }
            finally
            {
                // close pdf document
                doc.Close();
                if (htmlStream != null)
                    htmlStream.Dispose();
            }

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "PdfA.pdf";
            return fileResult;
        }
    }
}
