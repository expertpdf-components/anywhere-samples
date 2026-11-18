using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class PageSettingsModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();

            // create a new pdf font
            PdfFont font = doc.AddFont(StdFontBaseFamily.Helvetica);
            font.Size = 18;

            // add a new page to the document 
            // (with specific page size, margins and orientation)
            PdfPage page = doc.AddPage(PageSize.A4,
                new Margins(0f), PDFPageOrientation.Portrait);

            // create a new text element and add it to the page
            TextElement text = new TextElement(0, 0, Helper.SomeText(), font);
            page.AddElement(text);

            // add a new page to the document
            // (with specific page size, margins and orientation)
            PdfPage page2 = doc.AddPage(PageSize.Letter,
                new Margins(20f), PDFPageOrientation.Landscape);

            // create a new text element and add it to the page
            TextElement text2 = new TextElement(0, 0, Helper.SomeText(), font);
            page2.AddElement(text2);

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
