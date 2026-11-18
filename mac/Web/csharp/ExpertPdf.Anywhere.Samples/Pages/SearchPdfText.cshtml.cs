using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class SearchPdfTextModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtSearch { get; set; } = "PDF";

        [BindProperty]
        public bool ChkCaseSensitive { get; set; }

        [BindProperty]
        public bool ChkWholeWords { get; set; } = true;

        public IActionResult OnPost()
        {
            string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "expertpdf.pdf");

            // find every occurrence of the text; page numbers start at 1, positions are in points
            // from the top-left corner of the page
            PdfToTextConverter converter = new PdfToTextConverter();
            TextPosition[] positions = converter.ExtractTextPositions(pdfFile, TxtSearch,
                ChkCaseSensitive, ChkWholeWords);

            // mark each occurrence with a rectangle on a copy of the document
            Document doc = new Document(pdfFile);
            foreach (TextPosition position in positions)
            {
                PdfPage page = doc.Pages[position.PageNumber - 1];

                RectangleElement mark = new RectangleElement(position.X - 1, position.Y - 1,
                    position.Width + 2, position.Height + 2);
                mark.ForeColor = ExpertPdf.Anywhere.Drawing.Color.Red;
                mark.LineStyle.LineWidth = 1;
                page.AddElement(mark);
            }

            // the number of occurrences in the document title
            doc.DocumentInformation.Title = string.Format("'{0}' found {1} times", TxtSearch,
                positions.Length);

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
