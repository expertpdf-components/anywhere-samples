using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class ConvertUrlToPdfModel : PageModel
    {
        public void OnGet()
        {
            DdlPageSize = "A4";
        }

        [BindProperty]
        public string TxtUrl { get; set; }

[BindProperty]
        public string DdlPageSize { get; set; }

        public List<SelectListItem> PageSizes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "A1", Text = "A1" },
            new SelectListItem { Value = "A2", Text = "A2" },
            new SelectListItem { Value = "A3", Text = "A3" },
            new SelectListItem { Value = "A4", Text = "A4" },
            new SelectListItem { Value = "A5", Text = "A5" },
            new SelectListItem { Value = "Letter", Text = "Letter" },
            new SelectListItem { Value = "HalfLetter", Text = "HalfLetter" },
            new SelectListItem { Value = "Ledger", Text = "Ledger" },
            new SelectListItem { Value = "Legal", Text = "Legal" },
        };

        [BindProperty]
        public string DdlPageOrientation { get; set; }

        public List<SelectListItem> PageOrientations { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Portrait", Text = "Portrait" },
            new SelectListItem { Value = "Landscape", Text = "Landscape" },
        };

        [BindProperty]
        public string TxtWidth { get; set; }

        [BindProperty]
        public string TxtHeight { get; set; }

        [BindProperty]
        public string TxtPageRanges { get; set; }

        public IActionResult OnPost()
        {
            // read parameters from the webpage
            PdfPageSize pageSize =
                (PdfPageSize)Enum.Parse(typeof(PdfPageSize), DdlPageSize, true);

            PDFPageOrientation pdfOrientation =
                (PDFPageOrientation)Enum.Parse(typeof(PDFPageOrientation),
                DdlPageOrientation, true);

            int webPageWidth = 1024;
            try
            {
                webPageWidth = System.Convert.ToInt32(TxtWidth);
            }
            catch { }

            int webPageHeight = 0;
            try
            {
                webPageHeight = System.Convert.ToInt32(TxtHeight);
            }
            catch { }

            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // set converter options
            converter.PdfDocumentOptions.PdfPageSize = pageSize;
            converter.PdfDocumentOptions.PdfPageOrientation = pdfOrientation;
            converter.PageWidth = webPageWidth;
            converter.PageHeight = webPageHeight;

            // keep only some of the pages, for example "1-2,4"
            if (!string.IsNullOrEmpty(TxtPageRanges))
                converter.PageRanges = TxtPageRanges;

            // create a new pdf document converting an url
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

            // save pdf document
            byte[] pdf = doc.Save();

            // close pdf document
            doc.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;

        }

    }
}
