using System;
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
    public class ConvertMultipleUrlsToPdfModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtUrl1 { get; set; }

        [BindProperty]
        public string TxtUrl2 { get; set; }

        [BindProperty]
        public string TxtUrl3 { get; set; }

        public IActionResult OnPost()
        {
            // create a new pdf document 
            Document doc = new Document();

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // add the first url
            HtmlToPdfElement html1 = new HtmlToPdfElement(TxtUrl1);
            AddElementResult result = page.AddElement(html1);

            // get the last page of the previous conversion
            PdfPage page2 = doc.Pages[result.EndPageIndex];

            // add the second url
            HtmlToPdfElement html2 = new HtmlToPdfElement(result.EndPageBounds.Left,
                result.EndPageBounds.Bottom, TxtUrl2);
            result = page2.AddElement(html2);

            // get the last page of the previous conversion
            PdfPage page3 = doc.Pages[result.EndPageIndex];

            // add the third url
            HtmlToPdfElement html3 = new HtmlToPdfElement(result.EndPageBounds.Left,
                result.EndPageBounds.Bottom, TxtUrl3);
            result = page3.AddElement(html3);

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
