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
    public class TableOfContentsModel : PageModel
    {
        // number of items in the table of contents (TOC)
        private int TOC_Items = 13;

        // CSS class for TOC items (titles and targets)
        private string TOC_Class = "toc";

        // font size for page numbers in the table of contents
        private int TOC_Page_Font_Size = 12;

        // CSS class for PDF bookmarks
        private string Bookmarks_Class = "bookmark";

        // Each TOC item is represented by a title with ID TOC_Title{0}
        // and a target (internal link) with ID TOC_Target{0}
        // Both TOC titles and targets must have the same CSS Class ("toc" in our sample)

        public void OnGet()
        {
            TxtUrl = Helper.WWWRoot(Request) + Url.Content("~/files/document2.html");
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public bool CbGeneratedToc { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            converter.PdfDocumentOptions.TopMargin = 20;
            converter.PdfDocumentOptions.RightMargin = 20;
            converter.PdfDocumentOptions.BottomMargin = 20;
            converter.PdfDocumentOptions.LeftMargin = 20;

            // set the css selectors for the automatic bookmarks 
            // (elements with CSS class "bookmark")
            converter.PdfBookmarkOptions.HtmlElementSelectors =
                new string[] { "*." + Bookmarks_Class };

            // set the css selectors for the TOC related elements
            // (elements with CSS class "toc")
            // (alternatively, full list of IDs can be specified)
            converter.HtmlElementsMappingOptions.HtmlElementSelectors =
                new string[] { "*." + TOC_Class };

            // display the bookmarks when the document is opened in a viewer
            converter.PdfViewerPreferences.PageMode = ViewerPageMode.UseOutlines;

            // footer settings
            converter.PdfDocumentOptions.ShowFooter = true;
            converter.PdfFooterOptions.FooterHeight = 30;

            // page numbers can be added using a TextArea object
            TextArea text = new TextArea(0, 10,
                "Page: &p; of &P;  ",
                new ExpertPdf.Anywhere.Drawing.Font("Arial", 10));
            text.TextAlign = HorizontalTextAlign.Right;
            converter.PdfFooterOptions.AddTextArea(text);

            // create a new pdf document converting an url
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

            // create font for page numbers
            PdfFont pageNumberFont = doc.Fonts.Add(
                new Font("Arial", TOC_Page_Font_Size, FontStyle.Regular),
                true);

            // get the right coordinate of the table of contents 
            // to position the page numbers
            float tocRight = doc.Pages[0].ClientRectangle.Width - 50;

            // add page numbers for the table of contents items
            for (int tocItem = 1; tocItem <= TOC_Items; tocItem++)
            {
                string tocTitleID = String.Format("TOC_Title{0}", tocItem);
                string tocTargetID = String.Format("TOC_Target{0}", tocItem);

                HtmlElementMapping tocTitle = converter.HtmlElementsMappingOptions
                    .HtmlElementsMappingResult.GetElementByHtmlId(tocTitleID);
                HtmlElementMapping tocTarget = converter.HtmlElementsMappingOptions
                    .HtmlElementsMappingResult.GetElementByHtmlId(tocTargetID);

                if (tocTitle == null || tocTarget == null)
                {
                    // TOC items not found
                    continue;
                }

                // get the TOC title page and rendering rectangle
                PdfPage tocPage = doc.Pages[tocTitle.PdfRectangles[0].PageIndex];
                RectangleF tocTitleRectangle = tocTitle.PdfRectangles[0].Rectangle;

                // get the page number of target where the TOC entry points
                int tocTargetPageNumber = tocTarget.PdfRectangles[0].PageIndex + 1;

                // add dashed line from TOC title to the page number
                LineElement dashedLine = new LineElement(
                    tocTitleRectangle.Right + 5,
                    tocTitleRectangle.Y + tocTitleRectangle.Height - 4,
                    tocRight, tocTitleRectangle.Y + tocTitleRectangle.Height - 4);
                dashedLine.LineStyle.LineWidth = 1;
                dashedLine.LineStyle.LineDashStyle = LineDashStyle.Dash;
                dashedLine.ForeColor = Color.Black;
                tocPage.AddElement(dashedLine);

                // create the page number text element to the right of the TOC title
                TextElement pageNumberTextElement = new TextElement(tocRight + 5,
                        tocTitleRectangle.Y, -1, tocTitleRectangle.Height,
                        tocTargetPageNumber.ToString(), pageNumberFont);
                pageNumberTextElement.TextAlign = HorizontalTextAlign.Left;
                pageNumberTextElement.VerticalTextAlign = VerticalTextAlign.Middle;
                pageNumberTextElement.ForeColor = Color.Black;

                // add the page number to the right of the TOC entry
                tocPage.AddElement(pageNumberTextElement);
            }

            // optionally, a generated table of contents page at the start
            if (CbGeneratedToc)
                AddGeneratedTableOfContents(doc, converter);

            // save pdf document
            byte[] pdf = doc.Save();

            // close pdf document
            doc.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;

        }

        // A table of contents page added at the start, generated from the h1 and h2 headings the converter mapped (the TOC
        // targets of the test document), with page numbers and links. The PDF viewer numbers the new page i and the
        // converted pages 1, 2, 3... (page labels), so the numbers written in the page's own table still match.
        private static void AddGeneratedTableOfContents(Document doc, PdfConverter converter)
        {
            TableOfContents toc = new TableOfContents();
            toc.Title = "Contents (generated)";
            HtmlElementMappingsCollection mappings = converter.HtmlElementsMappingOptions.HtmlElementsMappingResult;
            for (int i = 0; i < mappings.Count; i++)
            {
                HtmlElementMapping heading = mappings[i];
                string tag = heading.HtmlElementTagName.ToLowerInvariant();
                if (tag != "h1" && tag != "h2")
                    continue;
                HtmlElementPdfRectangle place = heading.PdfRectangles[0];
                toc.AddEntry(heading.HtmlElementText.Trim(), tag == "h1" ? 1 : 2, doc.Pages[place.PageIndex], place.Rectangle.Y);
            }

            // the converted pages are numbered from 1; the range moves with them when the table is inserted before them
            doc.AddPageLabelRange(0, PageLabelStyle.Decimal);
            doc.InsertTableOfContents(toc, 0);
            doc.AddPageLabelRange(0, PageLabelStyle.LowercaseRoman);
        }
    }
}
