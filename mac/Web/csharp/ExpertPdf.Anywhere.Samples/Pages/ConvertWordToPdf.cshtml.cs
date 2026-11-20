using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.WordToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class ConvertWordToPdfModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string DdlPageSetup { get; set; } = "Document";

        [BindProperty]
        public string DdlPageSize { get; set; } = "A4";

        [BindProperty]
        public string DdlPageOrientation { get; set; } = "Portrait";

        [BindProperty]
        public string DdlBookmarks { get; set; } = "Headings";

        [BindProperty]
        public string DdlStandard { get; set; } = "Full";

        [BindProperty]
        public bool ChkPageNumbers { get; set; } = true;

        public List<SelectListItem> PageSetups { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Document", Text = "Keep the page sizes of the document" },
            new SelectListItem { Value = "Options", Text = "Lay the document out on the page size below" },
        };

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

        public List<SelectListItem> BookmarkSources { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Headings", Text = "From the headings" },
            new SelectListItem { Value = "Bookmarks", Text = "From the Word bookmarks" },
            new SelectListItem { Value = "None", Text = "No bookmarks" },
        };

        public List<SelectListItem> Standards { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Full", Text = "Full PDF" },
            new SelectListItem { Value = "Pdf_A_2b", Text = "PDF/A-2b" },
            new SelectListItem { Value = "Pdf_A_3a", Text = "PDF/A-3a (tagged)" },
        };

        public IActionResult OnPost()
        {
            string wordFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "document.docx");

            // instantiate the word to pdf converter
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            // keep the page sizes of the document, or lay it out again on the page size of the options
            converter.PageSetup = (WordPageSetup)Enum.Parse(typeof(WordPageSetup), DdlPageSetup);
            converter.PdfDocumentOptions.PdfPageSize = (PdfPageSize)Enum.Parse(typeof(PdfPageSize), DdlPageSize);
            converter.PdfDocumentOptions.PdfPageOrientation =
                (PDFPageOrientation)Enum.Parse(typeof(PDFPageOrientation), DdlPageOrientation);

            // what the bookmarks of the pdf are built from
            converter.Bookmarks = (WordBookmarks)Enum.Parse(typeof(WordBookmarks), DdlBookmarks);

            // the PDF standard; PDF/A-3a is also tagged (accessible)
            converter.PdfStandardSubset = (PdfStandardSubset)Enum.Parse(typeof(PdfStandardSubset), DdlStandard);

            // page numbers in the footer; the converter shows a footer by default, so it is
            // switched on or off here, and its default text is cleared to show only the page number
            converter.PdfDocumentOptions.ShowFooter = ChkPageNumbers;
            if (ChkPageNumbers)
            {
                converter.PdfFooterOptions.FooterText = string.Empty;
                converter.PdfFooterOptions.ShowPageNumber = true;
            }

            // convert the word document to pdf
            byte[] pdf = converter.GetPdfBytesFromWordFile(wordFile);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
