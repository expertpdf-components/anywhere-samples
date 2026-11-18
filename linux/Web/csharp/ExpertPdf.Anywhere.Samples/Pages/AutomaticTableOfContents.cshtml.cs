using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class AutomaticTableOfContentsModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string DdlMode { get; set; } = "FillExisting";

        public List<SelectListItem> Modes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "FillExisting", Text = "Number the page's own table of contents" },
            new SelectListItem { Value = "InsertHtml", Text = "Write a table of contents into the page" },
            new SelectListItem { Value = "GeneratePages", Text = "Draw a table of contents on pages of its own" },
        };

        public IActionResult OnPost()
        {
            string url = Helper.WWWRoot(Request) + Url.Content("~/files/document2.html");

            HtmlTableOfContentsMode mode =
                (HtmlTableOfContentsMode)Enum.Parse(typeof(HtmlTableOfContentsMode), DdlMode);

            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            converter.PdfDocumentOptions.TopMargin = 20;
            converter.PdfDocumentOptions.RightMargin = 20;
            converter.PdfDocumentOptions.BottomMargin = 20;
            converter.PdfDocumentOptions.LeftMargin = 20;

            // the table of contents
            HtmlTableOfContentsOptions toc = converter.TableOfContentsOptions;
            toc.Mode = mode;
            switch (mode)
            {
                case HtmlTableOfContentsMode.FillExisting:
                    // the element that holds the page's own table of contents: every internal link
                    // inside it gets a leader and the number of the page its target is on
                    toc.ContainerSelector = "div.content > p:first-of-type";
                    break;

                case HtmlTableOfContentsMode.InsertHtml:
                    // a table of contents is written into the page from its headings, styled by the
                    // page's CSS (nav.expertpdf-toc), then numbered
                    toc.HeadingSelectors = new string[] { "h1.toc", "h2.toc" };
                    toc.Title = "Contents";
                    converter.CustomCSS = "nav.expertpdf-toc { font-size: 18px; } " +
                        "nav.expertpdf-toc li.expertpdf-toc-level-1 > a { font-weight: bold; }";
                    break;

                case HtmlTableOfContentsMode.GeneratePages:
                    // the table of contents is drawn on pages of its own, at the start of the document
                    toc.HeadingSelectors = new string[] { "h1.toc", "h2.toc" };
                    toc.PagePosition = HtmlTableOfContentsPagePosition.Start;
                    toc.Title = "Contents";
                    toc.TitleAlign = HorizontalTextAlign.Center;
                    toc.TitleColor = Color.FromArgb(26, 75, 140);
                    toc.NumberEntries = true;
                    break;
            }

            // convert the url to pdf
            byte[] pdf = converter.GetPdfBytesFromUrl(url);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
