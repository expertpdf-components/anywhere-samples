using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class ModifyExistingPdfModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // the initial file
            string file = Path.Combine(Startup.ServerPath, "wwwroot", "files", "doc1.pdf");

            // load the pdf document
            Document doc = new Document(file);

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // create a new pdf font (component standard font)
            PdfFont font = doc.AddFont(StdFontBaseFamily.Helvetica);
            font.Size = 20;

            // create text element and add it to the new page
            TextElement text = new TextElement(100, 100,
                "Sample text added to an existing pdf document.", font);
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
