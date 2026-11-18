using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class WebElementsLocationModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string TxtElements { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // set the css selectors for the web elements
            // whose positions will be retrieved
            if (!string.IsNullOrEmpty(TxtElements))
            {
                converter.HtmlElementsMappingOptions.HtmlElementSelectors =
                    TxtElements.Split(new char[] { ',' });
            }

            // create a new pdf document converting an url
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

            // get all web elements and mark/highlight them with a green rectangle
            foreach (HtmlElementMapping element in
                converter.HtmlElementsMappingOptions.HtmlElementsMappingResult)
            {
                // each web element has several rendering rectangles 
                // (if it is displayed in more than 1 page)
                foreach (HtmlElementPdfRectangle htmlElementRectangle
                    in element.PdfRectangles)
                {
                    // get the PDF page
                    PdfPage page = doc.Pages[htmlElementRectangle.PageIndex];
                    RectangleF rectangle = htmlElementRectangle.Rectangle;

                    // highlight the HTML element
                    RectangleElement greenRectangle = new RectangleElement(
                        rectangle.X, rectangle.Y,
                        rectangle.Width, rectangle.Height);
                    greenRectangle.ForeColor = Color.Green;

                    page.AddElement(greenRectangle);
                }
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
