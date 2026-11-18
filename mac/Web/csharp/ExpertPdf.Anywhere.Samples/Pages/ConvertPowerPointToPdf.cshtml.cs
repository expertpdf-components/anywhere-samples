using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.PowerPointToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class ConvertPowerPointToPdfModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string DdlPageSetup { get; set; } = "Document";

        [BindProperty]
        public string DdlPageSize { get; set; } = "A4";

        [BindProperty]
        public string DdlPageOrientation { get; set; } = "Landscape";

        [BindProperty]
        public string DdlPublish { get; set; } = "Slides";

        [BindProperty]
        public int DdlSlidesPerHandout { get; set; } = 6;

        [BindProperty]
        public string DdlStandard { get; set; } = "Full";

        [BindProperty]
        public bool ChkPageNumbers { get; set; } = false;

        public List<SelectListItem> PageSetups { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Document", Text = "Pages the size of the slides" },
            new SelectListItem { Value = "Options", Text = "Scale the slides onto the page size below" },
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

        public List<SelectListItem> PublishOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Slides", Text = "Slides - one page per slide" },
            new SelectListItem { Value = "NotesPages", Text = "Notes pages - each slide with its notes" },
            new SelectListItem { Value = "Handouts", Text = "Handouts - several slides per page" },
        };

        public List<SelectListItem> SlidesPerHandoutOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "1" },
            new SelectListItem { Value = "2", Text = "2" },
            new SelectListItem { Value = "3", Text = "3" },
            new SelectListItem { Value = "4", Text = "4" },
            new SelectListItem { Value = "6", Text = "6" },
            new SelectListItem { Value = "9", Text = "9" },
        };

        public List<SelectListItem> Standards { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Full", Text = "Full PDF" },
            new SelectListItem { Value = "Pdf_A_2b", Text = "PDF/A-2b" },
            new SelectListItem { Value = "Pdf_A_3a", Text = "PDF/A-3a (tagged)" },
        };

        public IActionResult OnPost()
        {
            string powerPointFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "presentation.pptx");

            // instantiate the powerpoint to pdf converter
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            // pages the size of the slides, or the slides scaled onto the page size of the options
            converter.PageSetup = (PowerPointPageSetup)Enum.Parse(typeof(PowerPointPageSetup), DdlPageSetup);
            converter.PdfDocumentOptions.PdfPageSize = (PdfPageSize)Enum.Parse(typeof(PdfPageSize), DdlPageSize);
            converter.PdfDocumentOptions.PdfPageOrientation =
                (PDFPageOrientation)Enum.Parse(typeof(PDFPageOrientation), DdlPageOrientation);

            // what is published: the slides, the notes pages or handouts with several slides on a page
            converter.Publish = (PowerPointPublish)Enum.Parse(typeof(PowerPointPublish), DdlPublish);
            converter.SlidesPerHandout = DdlSlidesPerHandout;

            // the PDF standard; PDF/A-3a is also tagged (accessible)
            converter.PdfStandardSubset = (PdfStandardSubset)Enum.Parse(typeof(PdfStandardSubset), DdlStandard);

            // page numbers in the footer
            if (ChkPageNumbers)
            {
                converter.PdfDocumentOptions.ShowFooter = true;
                converter.PdfFooterOptions.ShowPageNumber = true;
            }

            // convert the powerpoint presentation to pdf
            byte[] pdf = converter.GetPdfBytesFromPowerPointFile(powerPointFile);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
