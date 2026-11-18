using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class ZugferdInvoiceModel : PageModel
    {
        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/invoice.html");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            converter.PdfDocumentOptions.LeftMargin = 20;
            converter.PdfDocumentOptions.RightMargin = 20;
            converter.PdfDocumentOptions.TopMargin = 20;
            converter.PdfDocumentOptions.BottomMargin = 20;
            converter.PdfDocumentInfo.Title = "Invoice F-2026-1017";
            converter.PdfDocumentInfo.AuthorName = "Alpine Data Systems GmbH";

            // ZUGFeRD / Factur-X invoices are PDF/A-3; this one is also tagged and accessible (PDF/A-3a)
            converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_3a;
            converter.Language = "en-US";

            // convert the invoice page to a Document object
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

            // embed the invoice XML as factur-x.xml and write the Factur-X metadata; the profile must match the XML
            // (EN 16931, also called COMFORT) and Alternative says the XML is another form of the visible invoice
            string xmlFile = Path.Combine(Startup.WebRootPath, "files", "factur-x.xml");
            doc.AddZugferdInvoice(xmlFile, ZugferdProfile.En16931, AttachmentRelationship.Alternative);

            // save pdf document
            byte[] pdf = doc.Save();

            // close pdf document
            doc.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Invoice-F-2026-1017.pdf";
            return fileResult;
        }
    }
}
