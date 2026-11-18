using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class DocumentPropertiesModel : PageModel
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

            // create a new text element and add it to the page
            TextElement text = new TextElement(50, 50,
                "ExpertPdf for .NET - Document Properties Sample", font);
            page.AddElement(text);

            // set the document properties
            doc.DocumentInformation.Title = "ExpertPdf for .NET Sample";
            doc.DocumentInformation.Author = "ExpertPdf Samples";
            doc.DocumentInformation.Subject = "ExpertPdf Html To Pdf Converter Samples";
            doc.DocumentInformation.Keywords = "pdf library, sample code";
            doc.DocumentInformation.CreationDate = DateTime.Now;

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
