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
    public class PdfViewerPreferencesModel : PageModel
    {
        public void OnGet()
        {
        }

public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // create a new pdf font
            PdfFont font = doc.AddFont(StdFontBaseFamily.Helvetica);
            font.Size = 20;

            // create a new text element and add it to the page
            TextElement text = new TextElement(50, 50,
                "ExpertPdf for .NET - Pdf Viewer Preferences Sample", font);
            page.AddElement(text);

            // set the pdf viewer preferences
            doc.ViewerPreferences.CenterWindow = true;
            doc.ViewerPreferences.DisplayDocTitle = true;
            doc.ViewerPreferences.FitWindow = true;
            doc.ViewerPreferences.HideMenuBar = true;
            doc.ViewerPreferences.HideToolbar = true;
            doc.ViewerPreferences.HideWindowUI = false;

            doc.ViewerPreferences.NonFullScreenPageMode =
                ViewerFullScreenExitMode.UseNone;
            doc.ViewerPreferences.PageLayout =
                ViewerPageLayout.SinglePage;
            doc.ViewerPreferences.PageMode =
                ViewerPageMode.UseNone;

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
