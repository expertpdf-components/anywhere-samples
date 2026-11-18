using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere.PDFRedaction;

namespace ExpertPdf.Samples.Pages
{
    public class PdfRedactionModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtText { get; set; } = "ExpertPdf";

        [BindProperty]
        public bool ChkCaseSensitive { get; set; }

        [BindProperty]
        public bool ChkWholeWords { get; set; } = true;

        [BindProperty]
        public string TxtPattern { get; set; }

        [BindProperty]
        public string DdlFillColor { get; set; } = "Black";

        [BindProperty]
        public string TxtOverlay { get; set; }

        [BindProperty]
        public string TxtAreaPage { get; set; } = "1";

        [BindProperty]
        public string TxtAreaX { get; set; }

        [BindProperty]
        public string TxtAreaY { get; set; }

        [BindProperty]
        public string TxtAreaWidth { get; set; }

        [BindProperty]
        public string TxtAreaHeight { get; set; }

        public List<SelectListItem> FillColors { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Black", Text = "Black" },
            new SelectListItem { Value = "Gray", Text = "Gray" },
            new SelectListItem { Value = "White", Text = "White" },
        };

        public IActionResult OnPost()
        {
            string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "expertpdf.pdf");

            // load the document to redact
            PdfRedactionManager manager = new PdfRedactionManager();
            manager.Load(pdfFile);

            // the color that covers the redacted areas
            switch (DdlFillColor)
            {
                case "Gray": manager.FillColor = Color.Gray; break;
                case "White": manager.FillColor = Color.White; break;
                default: manager.FillColor = Color.Black; break;
            }

            // a text written in each redacted area: white on black and gray, black on white
            if (!string.IsNullOrEmpty(TxtOverlay))
            {
                manager.OverlayText = TxtOverlay;
                manager.OverlayTextColor = DdlFillColor == "White" ? Color.Black : Color.White;
            }

            // every occurrence of a text
            if (!string.IsNullOrEmpty(TxtText))
            {
                manager.AddRedaction(TxtText, ChkCaseSensitive, ChkWholeWords);
            }

            // every match of a regular expression
            if (!string.IsNullOrEmpty(TxtPattern))
            {
                RegexOptions options = ChkCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                manager.AddRedaction(new Regex(TxtPattern, options, TimeSpan.FromSeconds(2)));
            }

            // a rectangle on a page
            float width, height;
            if (TryNumber(TxtAreaWidth, out width) && TryNumber(TxtAreaHeight, out height)
                && width > 0 && height > 0)
            {
                float x, y, page;
                TryNumber(TxtAreaX, out x);
                TryNumber(TxtAreaY, out y);
                int pageNumber = TryNumber(TxtAreaPage, out page) ? (int)page : 1;
                manager.AddRedaction(pageNumber - 1, new RectangleF(x, y, width, height));
            }

            // remove the content and save the redacted document
            manager.Redact();
            byte[] pdf = manager.Save();

            // close the manager
            manager.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }

        private static bool TryNumber(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
