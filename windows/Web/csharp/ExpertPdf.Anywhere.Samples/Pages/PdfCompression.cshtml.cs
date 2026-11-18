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
    public class PdfCompressionModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string DdlPdfCompressionLevel { get; set; }

        public List<SelectListItem> CompressionLevels { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "NoCompression", Text = "No Compression" },
            new SelectListItem { Value = "Normal", Text = "Normal" },
            new SelectListItem { Value = "Best", Text = "Best"  },
        };

        public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();
            doc.Margins = new Margins(20, 20, 20, 20);

            // set compression level
            doc.CompressionLevel = (PdfCompressionLevel)Enum.Parse(
                typeof(PdfCompressionLevel), DdlPdfCompressionLevel, true);

            // create a new pdf font
            PdfFont font = doc.AddFont(StdFontBaseFamily.Helvetica);
            font.Size = 16;

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // create a new text element and add it to the page
            TextElement text = new TextElement(0, 0,
                Helper.SomeVeryLongText(), font);
            page.AddElement(text);

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
