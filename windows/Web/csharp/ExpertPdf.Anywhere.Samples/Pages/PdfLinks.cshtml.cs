using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class PdfLinksModel : PageModel
    {
        public void OnGet()
        {
        }

public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();

            // add a new page to the document
            PdfPage page1 = doc.AddPage();

            // add a second page to the document
            PdfPage page2 = doc.AddPage();

            // define a rendering result object
            AddElementResult result;

            // create a new pdf font
            PdfFont font = doc.AddFont(StdFontBaseFamily.Helvetica);
            font.Size = 14;
            font.IsUnderline = true;

            // create a new text element and add it to the first page
            TextElement text1 = new TextElement(0, 0,
                "First page (no link on this text)", font);
            page1.AddElement(text1);

            // create a new text element and add it to the second page
            TextElement text2 = new TextElement(0, 0,
                "Second page (no link on this text)", font);
            page2.AddElement(text2);

            // create external link in 2 steps

            // 1 - create the link text
            TextElement linkText1 = new TextElement(0, 50,
                "External link (click to go to www.html-to-pdf.net)", font);
            result = page1.AddElement(linkText1);

            // 2 - add the link using the text rendering rectangle
            LinkUrlElement extLink1 = new LinkUrlElement(
                result.EndPageBounds, "https://www.html-to-pdf.net");
            page1.AddElement(extLink1);

            // create internal link in 2 steps

            // 1 - create the link text
            TextElement linkText2 = new TextElement(0, 100,
                "Internal link (click to go to the second page)", font);
            result = page1.AddElement(linkText2);

            // 2 - add the link using the text rendering rectangle
            InternalLinkElement intLink1 = new InternalLinkElement(
                result.EndPageBounds, new ExplicitDestination(page2));
            page1.AddElement(intLink1);

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
