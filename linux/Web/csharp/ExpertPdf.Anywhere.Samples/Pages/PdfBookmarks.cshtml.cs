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
    public class PdfBookmarksModel : PageModel
    {
        public void OnGet()
        {
        }

public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();

            // instruct the document to show the bookmarks panel when opened in a viewer
            doc.ViewerPreferences.PageMode = ViewerPageMode.UseOutlines;

            // add a new page to the document
            PdfPage page1 = doc.AddPage();

            // add a second page to the document
            PdfPage page2 = doc.AddPage();

            // add a third page to the document
            PdfPage page3 = doc.AddPage();

            // define a rendering result object
            AddElementResult result;

            // create a new pdf font
            PdfFont font = doc.AddFont(StdFontBaseFamily.Helvetica);
            font.Size = 14;

            // create a new text element and add it to the first page
            TextElement text1 = new TextElement(0, 0, "First page", font);
            page1.AddElement(text1);

            // create a new text element and add it to the second page
            TextElement text2 = new TextElement(0, 0, "Second page", font);
            page2.AddElement(text2);

            // create a new text element and add it to the third page
            TextElement text3 = new TextElement(0, 0, "Third page", font);
            page3.AddElement(text3);

            // create a first level bookmark pointing to the first page
            Bookmark book1 = doc.AddBookmark("First page", new ExplicitDestination(page1,
                new ExpertPdf.Anywhere.Drawing.PointF(0, 0)));

            // create a first level bookmark pointing to the second page
            Bookmark book2 = doc.AddBookmark("Second page", new ExplicitDestination(page2,
                new ExpertPdf.Anywhere.Drawing.PointF(0, 0)));

            // create a first level bookmark pointing to the third page
            Bookmark book3 = doc.AddBookmark("Third page", new ExplicitDestination(page3,
                new ExpertPdf.Anywhere.Drawing.PointF(0, 0)));

            // other text on page 2
            TextElement text21 = new TextElement(0, 100, "Text 1 on page 2", font);
            result = page2.AddElement(text21);

            // add a level 2 bookmark to this text 
            // (using the bookmark for the page 2 as parent and 
            // the text rendering location as bookmark destination)
            doc.AddBookmark("Text 1", new ExplicitDestination(page2,
                result.EndPageBounds.Location), book2);

            // other text on page 2
            TextElement text22 = new TextElement(0, 400, "Text 2 on page 2", font);
            result = page2.AddElement(text22);

            // add a level 2 bookmark to this text 
            // (using the bookmark for the page 2 as parent and the text rendering 
            // location as bookmark destination) some styles are set for this bookmark
            Bookmark book = doc.AddBookmark("Text 2", new ExplicitDestination(page2,
                result.EndPageBounds.Location), book2);
            book.Color = ExpertPdf.Anywhere.Drawing.Color.Red;
            book.Style = PdfBookmarkStyle.Italic;

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
