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
    public class PdfTextElementModel : PageModel
    {
        public void OnGet()
        {
        }

public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();

            // create a new pdf font (component standard font)
            PdfFont font1 = doc.AddFont(StdFontBaseFamily.Helvetica);
            font1.Size = 20;

            // create a new pdf font (system font)
            PdfFont font2 = doc.AddFont(new ExpertPdf.Anywhere.Drawing.Font("Arial", 15));

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // define a rendering result object
            AddElementResult result;

            // create text element 
            TextElement text1 = new TextElement(0, 0,
                "Regular text using standard font Helvetica, size 20.", font1);
            result = page.AddElement(text1);

            // create text element 
            TextElement text2 = new TextElement(0, result.EndPageBounds.Bottom + 20,
                "Regular text using system font Verdana, size 15.", font2);
            result = page.AddElement(text2);

            // create colored text element 
            TextElement text3 = new TextElement(0, result.EndPageBounds.Bottom + 20, 200,
                "Red text over light blue background, using system font Verdana, size 15, limited at a width of 200 points.", font2);
            text3.ForeColor = ExpertPdf.Anywhere.Drawing.Color.Red;
            text3.BackColor = ExpertPdf.Anywhere.Drawing.Color.FromArgb(173, 216, 230);
            result = page.AddElement(text3);

            // create colored text element, right aligned
            TextElement text4 = new TextElement(0, result.EndPageBounds.Bottom + 20, 300,
                "Red text over light green background, right aligned, using system font Verdana, size 15, limited at a width of 300 points.", font2);
            text4.ForeColor = ExpertPdf.Anywhere.Drawing.Color.Red;
            text4.BackColor = ExpertPdf.Anywhere.Drawing.Color.FromArgb(144, 238, 144);
            text4.TextAlign = HorizontalTextAlign.Right;
            result = page.AddElement(text4);

            // create text element 
            TextElement text5 = new TextElement(0, result.EndPageBounds.Bottom + 200, 300,
                "Regular text using system font Verdana, size 15, rotated 45 degrees.", font2);
            text5.TextAngle = 45; // rotate 45 degrees counter-clockwise
            result = page.AddElement(text5);

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
