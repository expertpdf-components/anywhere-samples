using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class HtmlToPdfHeadersAndFootersModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public bool showHeaderOnFirstPage { get; set; }

        [BindProperty]
        public bool showHeaderOnOddPages { get; set; }

        [BindProperty]
        public bool showHeaderOnEvenPages { get; set; }

        [BindProperty]
        public int headerHeight { get; set; }

        [BindProperty]
        public bool showFooterOnFirstPage { get; set; }

        [BindProperty]
        public bool showFooterOnOddPages { get; set; }

        [BindProperty]
        public bool showFooterOnEvenPages { get; set; }

        [BindProperty]
        public int footerHeight { get; set; }

        [BindProperty]
        public bool showPageNumbering { get; set; }

        public IActionResult OnPost()
        {
            // get parameters
            string headerUrl = Path.Combine(Startup.ServerPath, "wwwroot", "files", "header.html");
            string footerUrl = Path.Combine(Startup.ServerPath, "wwwroot", "files", "footer.html");

            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // header settings
            converter.PdfDocumentOptions.ShowHeader = showHeaderOnFirstPage ||
                showHeaderOnOddPages || showHeaderOnEvenPages;
            converter.PdfHeaderOptions.ShowOnFirstPage = showHeaderOnFirstPage;
            converter.PdfHeaderOptions.ShowOnOddPages = showHeaderOnOddPages;
            converter.PdfHeaderOptions.ShowOnEvenPages = showHeaderOnEvenPages;
            converter.PdfHeaderOptions.HeaderHeight = headerHeight;
            converter.PdfHeaderOptions.DrawHeaderLine = false;

            HtmlToPdfArea headerHtml = new HtmlToPdfArea(headerUrl);
            converter.PdfHeaderOptions.AddHtmlToPdfArea(headerHtml);

            // footer settings
            converter.PdfDocumentOptions.ShowFooter = showFooterOnFirstPage ||
                showFooterOnOddPages || showFooterOnEvenPages;
            converter.PdfFooterOptions.ShowOnFirstPage = showFooterOnFirstPage;
            converter.PdfFooterOptions.ShowOnOddPages = showFooterOnOddPages;
            converter.PdfFooterOptions.ShowOnEvenPages = showFooterOnEvenPages;
            converter.PdfFooterOptions.FooterHeight = footerHeight;
            converter.PdfFooterOptions.DrawFooterLine = false;

            HtmlToPdfArea footerHtml = new HtmlToPdfArea(footerUrl);
            converter.PdfFooterOptions.AddHtmlToPdfArea(footerHtml);

            // add page numbering element to the footer
            if (showPageNumbering)
            {
                // page numbers can be added using a PdfTextSection object
                TextArea text = new TextArea(0, 10,
                    "Page: &p; of &P;  ",
                    new ExpertPdf.Anywhere.Drawing.Font("Arial", 8));
                text.TextAlign = HorizontalTextAlign.Right;
                converter.PdfFooterOptions.AddTextArea(text);
            }

            // create a new pdf document converting an url
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

            // custom header on page 3
            if (doc.Pages.Count >= 3)
            {
                PdfPage page = doc.Pages[2];

                Template customHeader = doc.AddTemplate(
                    page.PageSize.Width, headerHeight);
                HtmlToPdfElement customHtml = new HtmlToPdfElement(
                    "<div><b>This is the custom header that will " +
                    "appear only on page 3!</b></div>",
                    string.Empty);
                customHeader.AddElement(customHtml);

                page.CustomHeaderTemplate = customHeader;
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
