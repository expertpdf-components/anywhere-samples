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
    public class PdfStampsModel : PageModel
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
            font.Size = 24;

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // create a new text element and add it to the page
            TextElement text = new TextElement(0, 0, Helper.SomeLongText(), font);
            page.AddElement(text);

            // get image path
            string imgFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "logo.jpg");

            // stamp all pages - add a template containing an image to the bottom right of 
            // the page the image should repeat on all pdf pages automatically
            Template template = doc.AddTemplate(doc.Pages[0].ClientRectangle);
            ImageElement img = new ImageElement(
                doc.Pages[0].ClientRectangle.Width - 300,
                doc.Pages[0].ClientRectangle.Height - 150, imgFile);
            template.AddElement(img);

            // save pdf document
            byte[] pdf = doc.Save();

            // close pdf document
            doc.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Document.pdf";
            return fileResult;

        }
    }
}
