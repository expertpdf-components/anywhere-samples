using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere;

namespace ExpertPdf.Samples.Pages
{
    public class PdfToPowerPointConverterModel : PageModel
    {
        public void OnGet()
        {
        }

        // An uploaded PDF, or nothing - the shipped files/presentation.pdf is converted then.
        [BindProperty]
        public IFormFile FilePdf { get; set; }

        [BindProperty]
        public string TxtStartPage { get; set; } = "1";

        [BindProperty]
        public string TxtEndPage { get; set; } = "0";

        [BindProperty]
        public bool ChkTables { get; set; } = true;

        [BindProperty]
        public bool ChkImages { get; set; } = true;

        [BindProperty]
        public bool ChkHeadersAndFooters { get; set; } = true;

        [BindProperty]
        public bool ChkLinks { get; set; } = true;

        public IActionResult OnPost()
        {
            // instantiate the pdf to powerpoint converter
            PdfToPowerPointConverter converter = new PdfToPowerPointConverter();
            // converter.LicenseKey = "your license key";

            // the pages to convert, a slide each (an end page of 0 means the last page)
            int startPage = 1;
            int.TryParse(TxtStartPage, out startPage);
            int endPage = 0;
            int.TryParse(TxtEndPage, out endPage);
            converter.StartPageNumber = Math.Max(1, startPage);
            converter.EndPageNumber = Math.Max(0, endPage);

            // tables as PowerPoint tables, the pictures, the page headers and footers and the links
            converter.Tables = ChkTables;
            converter.Images = ChkImages;
            converter.HeadersAndFooters = ChkHeadersAndFooters;
            converter.Links = ChkLinks;

            // convert the uploaded pdf, or the sample one shipped with the application
            byte[] presentation;
            if (FilePdf != null && FilePdf.Length > 0)
            {
                using (Stream uploaded = FilePdf.OpenReadStream())
                {
                    presentation = converter.ConvertToPowerPoint(uploaded);
                }
            }
            else
            {
                string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "presentation.pdf");
                presentation = converter.ConvertToPowerPoint(pdfFile);
            }

            // return the converted presentation
            FileResult fileResult = new FileContentResult(presentation,
                "application/vnd.openxmlformats-officedocument.presentationml.presentation");
            fileResult.FileDownloadName = "Presentation.pptx";
            return fileResult;
        }
    }
}
