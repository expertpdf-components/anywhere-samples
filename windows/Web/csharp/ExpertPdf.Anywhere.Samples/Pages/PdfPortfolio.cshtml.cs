using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.PDFPortfolio;

namespace ExpertPdf.Samples.Pages
{
    public class PdfPortfolioModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string DdlViewMode { get; set; } = "Details";

        public List<SelectListItem> ViewModes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Details", Text = "Details" },
            new SelectListItem { Value = "Tile", Text = "Tile" },
        };

        public IActionResult OnPost()
        {
            // the files to put in the portfolio
            string filesFolder = Path.Combine(Startup.ServerPath, "wwwroot", "files");
            string filePdf = Path.Combine(filesFolder, "doc2.pdf");
            string fileHtml = Path.Combine(filesFolder, "document.html");
            string fileImage = Path.Combine(filesFolder, "logo.png");

            // instantiate the portfolio manager
            PdfPortfolioManager portfolio = new PdfPortfolioManager();

            // add the files
            PortfolioFile pdfFile = new PortfolioFile(filePdf);
            pdfFile.Description = "PDF document";
            portfolio.Files.Add(pdfFile);

            PortfolioFile htmlFile = new PortfolioFile(fileHtml);
            htmlFile.Description = "HTML document";
            portfolio.Files.Add(htmlFile);

            PortfolioFile imageFile = new PortfolioFile(fileImage);
            imageFile.Description = "Image file";
            portfolio.Files.Add(imageFile);

            // how the viewer lists the files, and the file it opens first
            portfolio.ViewMode = (PortfolioViewMode)Enum.Parse(typeof(PortfolioViewMode), DdlViewMode);
            portfolio.StartupFileName = "doc2.pdf";

            // save pdf document
            byte[] pdf = portfolio.Save();

            // close pdf document
            portfolio.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
