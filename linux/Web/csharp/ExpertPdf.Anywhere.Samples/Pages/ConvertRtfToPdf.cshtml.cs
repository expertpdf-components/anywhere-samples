using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.RtfToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class ConvertRtfToPdfModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string DdlPageSize { get; set; } = "A4";

        [BindProperty]
        public string DdlPageOrientation { get; set; } = "Portrait";

        [BindProperty]
        public string TxtMargin { get; set; } = "36";

        [BindProperty]
        public string TxtFooterText { get; set; } = "Converted with ExpertPdf Anywhere";

        [BindProperty]
        public bool ChkPageNumbers { get; set; } = true;

        public List<SelectListItem> PageSizes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "A4", Text = "A4" },
            new SelectListItem { Value = "A5", Text = "A5" },
            new SelectListItem { Value = "Letter", Text = "Letter" },
            new SelectListItem { Value = "Legal", Text = "Legal" },
        };

        public List<SelectListItem> PageOrientations { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Portrait", Text = "Portrait" },
            new SelectListItem { Value = "Landscape", Text = "Landscape" },
        };

        public IActionResult OnPost()
        {
            string rtfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "sample.rtf");

            int margin = 0;
            int.TryParse(TxtMargin, out margin);

            // instantiate the rtf to pdf converter
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            // the pdf pages
            converter.PdfDocumentOptions.PdfPageSize = (PdfPageSize)Enum.Parse(typeof(PdfPageSize), DdlPageSize);
            converter.PdfDocumentOptions.PdfPageOrientation =
                (PDFPageOrientation)Enum.Parse(typeof(PDFPageOrientation), DdlPageOrientation);
            converter.PdfDocumentOptions.LeftMargin = margin;
            converter.PdfDocumentOptions.RightMargin = margin;
            converter.PdfDocumentOptions.TopMargin = margin;
            converter.PdfDocumentOptions.BottomMargin = margin;

            // the footer
            converter.PdfDocumentOptions.ShowFooter = true;
            converter.PdfFooterOptions.FooterText = TxtFooterText;
            converter.PdfFooterOptions.ShowPageNumber = ChkPageNumbers;
            converter.PdfFooterOptions.DrawFooterLine = true;

            // convert the rtf document to pdf
            byte[] pdf = converter.GetPdfBytesFromRtfFile(rtfFile);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
