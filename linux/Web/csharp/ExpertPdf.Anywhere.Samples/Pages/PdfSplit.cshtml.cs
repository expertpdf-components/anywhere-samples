using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class PdfSplitModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // the initial file
            string file1 = Path.Combine(Startup.ServerPath, "wwwroot", "files", "doc.pdf");

            // load the pdf document (do not close this document until the final Save)
            Document doc1 = new Document(file1);

            // create a new pdf document
            Document doc = new Document();

            // add 2 pages to the new document
            if (doc1.Pages.Count > 2)
            {
                doc.AddPage(doc1.Pages[1]);
                doc.AddPage(doc1.Pages[2]);
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
