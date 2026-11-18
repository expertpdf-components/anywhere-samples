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
    public class PdfMergeModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // the 2 initial files
            string file1 = Path.Combine(Startup.ServerPath, "wwwroot", "files", "doc1.pdf");
            string file2 = Path.Combine(Startup.ServerPath, "wwwroot", "files", "doc2.pdf");

            // load the 2 pdf documents
            Document doc1 = new Document(file1);
            Document doc2 = new Document(file2);

            // create a new pdf document
            Document doc = new Document();

            // add the pages of those 2 documents to the new document
            doc.AppendDocument(doc1);
            doc.AppendDocument(doc2);

            // save pdf document
            byte[] pdf = doc.Save();

            // close pdf document
            doc.Close();
            doc1.Close();
            doc2.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
