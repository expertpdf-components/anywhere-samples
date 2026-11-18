using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class PdfTemplatesModel : PageModel
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

            // add a template containing an image
            // the image should repeat on all pdf pages automatically
            Template template1 = doc.AddTemplate(new RectangleF(100, 0, 400, 150));
            ImageElement img1 = new ImageElement(0, 0, imgFile);
            template1.AddElement(img1);

            // add another template containing an image behind the existing page elements
            // (under the text) the image should repeat on all pdf pages automatically
            Template template2 = doc.AddTemplate(new RectangleF(100, 200, 400, 150));
            template2.SendToBackground = true;
            ImageElement img2 = new ImageElement(0, 0, imgFile);
            template2.AddElement(img2);

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
