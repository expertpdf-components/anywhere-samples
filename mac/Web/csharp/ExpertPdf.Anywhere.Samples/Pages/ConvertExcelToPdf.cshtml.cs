using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.ExcelToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class ConvertExcelToPdfModel : PageModel
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
        public string DdlLayout { get; set; } = "Automatic";

        [BindProperty]
        public string DdlGridLines { get; set; } = "Automatic";

        [BindProperty]
        public bool ChkSheetBookmarks { get; set; } = true;

        [BindProperty]
        public string DdlStandard { get; set; } = "Full";

        [BindProperty]
        public bool ChkPageNumbers { get; set; } = true;

        public List<SelectListItem> PageSetups { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Document", Text = "Each sheet's own page setup" },
            new SelectListItem { Value = "Options", Text = "Lay the sheets out on the page size below" },
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

        public List<SelectListItem> Layouts { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Automatic", Text = "The sheet's own print scaling" },
            new SelectListItem { Value = "NoScaling", Text = "Actual size" },
            new SelectListItem { Value = "FitSheetOnOnePage", Text = "Fit each sheet on one page" },
            new SelectListItem { Value = "FitAllColumnsOnOnePage", Text = "Fit all columns on one page" },
            new SelectListItem { Value = "FitAllRowsOnOnePage", Text = "Fit all rows on one page" },
        };

        public List<SelectListItem> GridLineModes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Automatic", Text = "As each sheet's print setting says" },
            new SelectListItem { Value = "Visible", Text = "Draw the gridlines" },
            new SelectListItem { Value = "Hidden", Text = "No gridlines" },
        };

        public List<SelectListItem> Standards { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Full", Text = "Full PDF" },
            new SelectListItem { Value = "Pdf_A_2b", Text = "PDF/A-2b" },
            new SelectListItem { Value = "Pdf_A_3a", Text = "PDF/A-3a (tagged)" },
        };

        public IActionResult OnPost()
        {
            string excelFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "workbook.xlsx");

            // instantiate the excel to pdf converter
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            // print each sheet as its own page setup says, or lay the sheets out again on the
            // page size of the options
            converter.PageSetup = (ExcelPageSetup)Enum.Parse(typeof(ExcelPageSetup), DdlPageSetup);
            converter.PdfDocumentOptions.PdfPageSize = (PdfPageSize)Enum.Parse(typeof(PdfPageSize), DdlPageSize);
            converter.PdfDocumentOptions.PdfPageOrientation =
                (PDFPageOrientation)Enum.Parse(typeof(PDFPageOrientation), DdlPageOrientation);

            // how the sheets are scaled onto their pages, and the gridlines
            converter.Layout = (ExcelLayout)Enum.Parse(typeof(ExcelLayout), DdlLayout);
            converter.GridLines = (ExcelGridLines)Enum.Parse(typeof(ExcelGridLines), DdlGridLines);

            // one bookmark per sheet, named after it
            converter.SheetBookmarks = ChkSheetBookmarks;

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

            // convert the excel workbook to pdf
            byte[] pdf = converter.GetPdfBytesFromExcelFile(excelFile);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
