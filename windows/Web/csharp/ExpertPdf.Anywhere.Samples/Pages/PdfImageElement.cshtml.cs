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
    public class PdfImageElementModel : PageModel
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

            // get image path
            string imgFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "logo.jpg");

            // define a rendering result object
            AddElementResult result;

            // create image element from file path with real image size
            ImageElement img1 = new ImageElement(0, 0, imgFile);
            result = page.AddElement(img1);

            // create image element from the image bytes with fixed size in pdf
            // (aspect ratio not preserved)
            byte[] img = System.IO.File.ReadAllBytes(imgFile);
            ImageElement img2 = new ImageElement(0,
                result.EndPageBounds.Bottom + 50, 100, 100, img);
            result = page.AddElement(img2);

            // add text element
            PdfFont font1 = doc.AddFont(StdFontBaseFamily.Helvetica);
            font1.Size = 10;
            TextElement text1 = new TextElement(0,
                result.EndPageBounds.Bottom + 50, Helper.SomeText(), font1);
            page.AddElement(text1);

            // add image over text (no transparency)
            ImageElement img3 = new ImageElement(0,
                result.EndPageBounds.Bottom + 50, imgFile);
            result = page.AddElement(img3);

            // add image over text (with transparency) next to the previous image
            ImageElement img4 = new ImageElement(
                result.EndPageBounds.Right + 30,
                result.EndPageBounds.Top, imgFile);
            img4.Transparency = 50;
            result = page.AddElement(img4);

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
