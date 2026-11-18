using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class PdfElementsPositionsModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // create a new pdf font
            PdfFont font = doc.AddFont(StdFontBaseFamily.Helvetica);
            font.Size = 20;

            // define a rendering result object
            AddElementResult result;

            // create a new text element and add it to the page
            // Important: get the rendering result returned by AddElement() 
            // into the AddElementResult object
            TextElement text = new TextElement(0, 0, "Hello world!", font);
            result = page.AddElement(text);

            // add 10 more text element, leaving 30pt between the text lines
            for (int i = 1; i <= 10; i++)
            {
                TextElement elem = new TextElement(0,
                    result.EndPageBounds.Bottom + 30, "Text line " + i, font);
                result = page.AddElement(elem);
            }

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
