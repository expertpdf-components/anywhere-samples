using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere;

namespace ExpertPdf.Samples.Pages
{
    public class PdfToTextConverterModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtStartPage { get; set; } = "1";

        [BindProperty]
        public string TxtEndPage { get; set; } = "0";

        [BindProperty]
        public string DdlLayout { get; set; } = "ReadingLayout";

        [BindProperty]
        public bool ChkMarkPageBreaks { get; set; }

        public List<SelectListItem> Layouts { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "ReadingLayout", Text = "Reading order" },
            new SelectListItem { Value = "OriginalLayout", Text = "Original layout" },
        };

        public IActionResult OnPost()
        {
            string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "expertpdf.pdf");

            // instantiate the pdf to text converter
            PdfToTextConverter converter = new PdfToTextConverter();

            // the pages to extract
            int startPage = 1;
            int.TryParse(TxtStartPage, out startPage);
            int endPage = 0;
            int.TryParse(TxtEndPage, out endPage);
            converter.StartPageNumber = Math.Max(1, startPage);
            converter.EndPageNumber = Math.Max(0, endPage);

            // the layout of the extracted text
            converter.Layout = (TextLayout)Enum.Parse(typeof(TextLayout), DdlLayout);

            // a form feed character between the pages
            converter.MarkPageBreaks = ChkMarkPageBreaks;

            // extract the text
            string text = converter.ConvertToText(pdfFile);

            // return the text as a file
            FileResult fileResult = new FileContentResult(Encoding.UTF8.GetBytes(text), "text/plain");
            fileResult.FileDownloadName = "Sample.txt";
            return fileResult;
        }
    }
}
