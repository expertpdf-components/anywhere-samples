using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere;

namespace ExpertPdf.Samples.Pages
{
    public class PdfToExcelConverterModel : PageModel
    {
        public void OnGet()
        {
        }

        // An uploaded PDF, or nothing - the shipped files/workbook.pdf is converted then.
        [BindProperty]
        public IFormFile FilePdf { get; set; }

        [BindProperty]
        public string DdlSheets { get; set; } = "PerTable";

        [BindProperty]
        public string TxtCulture { get; set; } = "";

        [BindProperty]
        public string TxtStartPage { get; set; } = "1";

        [BindProperty]
        public string TxtEndPage { get; set; } = "0";

        [BindProperty]
        public bool ChkTextOutsideTables { get; set; } = true;

        [BindProperty]
        public bool ChkImages { get; set; } = true;

        public List<SelectListItem> SheetModes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "PerTable", Text = "A worksheet per table" },
            new SelectListItem { Value = "PerPage", Text = "A worksheet per page" },
        };

        public IActionResult OnPost()
        {
            // instantiate the pdf to excel converter
            PdfToExcelConverter converter = new PdfToExcelConverter();
            // converter.LicenseKey = "your license key";

            // a worksheet per table, or per page
            converter.Sheets = (PdfToExcelSheets)Enum.Parse(typeof(PdfToExcelSheets), DdlSheets);

            // the culture the numbers and dates of the PDF are read in (empty: the current culture);
            // cells holding them get typed values, so formulas and sorting work on them
            if (!string.IsNullOrWhiteSpace(TxtCulture))
            {
                converter.Culture = TxtCulture.Trim();
            }

            // the pages to convert (an end page of 0 means the last page)
            int startPage = 1;
            int.TryParse(TxtStartPage, out startPage);
            int endPage = 0;
            int.TryParse(TxtEndPage, out endPage);
            converter.StartPageNumber = Math.Max(1, startPage);
            converter.EndPageNumber = Math.Max(0, endPage);

            // the text around the tables, and the pictures
            converter.TextOutsideTables = ChkTextOutsideTables;
            converter.Images = ChkImages;

            // convert the uploaded pdf, or the sample one shipped with the application
            byte[] workbook;
            if (FilePdf != null && FilePdf.Length > 0)
            {
                using (Stream uploaded = FilePdf.OpenReadStream())
                {
                    workbook = converter.ConvertToExcel(uploaded);
                }
            }
            else
            {
                string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "workbook.pdf");
                workbook = converter.ConvertToExcel(pdfFile);
            }

            // return the converted workbook
            FileResult fileResult = new FileContentResult(workbook,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName = "Workbook.xlsx";
            return fileResult;
        }
    }
}
