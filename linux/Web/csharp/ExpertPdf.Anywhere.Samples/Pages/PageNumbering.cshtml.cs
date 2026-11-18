using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class PageNumberingModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();
            doc.Margins = new Margins(10, 10, 0, 0);

            // create a new pdf font
            PdfFont font = doc.AddFont(StdFontBaseFamily.Helvetica);
            font.Size = 24;

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // footer template (100 points in height) with text element
            doc.FooterTemplate = doc.AddTemplate(doc.Pages[0].ClientRectangle.Width, 100);
            TextElement text1 = new TextElement(0, 50,
                "Page: &p; of &P;.", font);
            text1.ForeColor = ExpertPdf.Anywhere.Drawing.Color.Blue;
            doc.FooterTemplate.AddElement(text1);

            // create a new text element and add it to the page
            // if page elements are added after header and footer is set, 
            // they will not be displayed in those areas.
            TextElement text = new TextElement(0, 0, Helper.SomeLongText(), font);
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
