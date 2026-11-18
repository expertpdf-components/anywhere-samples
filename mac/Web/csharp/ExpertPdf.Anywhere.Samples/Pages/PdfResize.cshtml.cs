using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;
using ExpertPdf.Anywhere.PDFResize;

namespace ExpertPdf.Samples.Pages
{
    public class PdfResizeModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string DdlPageSize { get; set; } = "Letter";

        [BindProperty]
        public string DdlPageOrientation { get; set; } = "Portrait";

        [BindProperty]
        public string TxtMargin { get; set; } = "36";

        [BindProperty]
        public string DdlHorizontalAlign { get; set; } = "Center";

        [BindProperty]
        public string DdlVerticalAlign { get; set; } = "Middle";

        [BindProperty]
        public bool ChkAllowScale { get; set; } = true;

        [BindProperty]
        public bool ChkKeepAspectRatio { get; set; } = true;

        public List<SelectListItem> PageSizes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "A3", Text = "A3" },
            new SelectListItem { Value = "A4", Text = "A4" },
            new SelectListItem { Value = "A5", Text = "A5" },
            new SelectListItem { Value = "Letter", Text = "Letter" },
            new SelectListItem { Value = "Legal", Text = "Legal" },
        };

        public List<SelectListItem> PageOrientations { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Portrait", Text = "Portrait" },
            new SelectListItem { Value = "Landscape", Text = "Landscape" },
        };

        public List<SelectListItem> HorizontalAligns { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Left", Text = "Left" },
            new SelectListItem { Value = "Center", Text = "Center" },
            new SelectListItem { Value = "Right", Text = "Right" },
        };

        public List<SelectListItem> VerticalAligns { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Top", Text = "Top" },
            new SelectListItem { Value = "Middle", Text = "Middle" },
            new SelectListItem { Value = "Bottom", Text = "Bottom" },
        };

        public IActionResult OnPost()
        {
            string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "doc2.pdf");

            // the new page size
            PageSize pageSize = PageSize.Letter;
            switch (DdlPageSize)
            {
                case "A3": pageSize = PageSize.A3; break;
                case "A4": pageSize = PageSize.A4; break;
                case "A5": pageSize = PageSize.A5; break;
                case "Legal": pageSize = PageSize.Legal; break;
            }

            float margin = 0;
            float.TryParse(TxtMargin, NumberStyles.Float, CultureInfo.InvariantCulture, out margin);

            // load the document to resize
            PdfResizeManager resizer = new PdfResizeManager();
            resizer.Load(pdfFile);

            // the new pages
            resizer.PageSize = pageSize;
            resizer.PageOrientation =
                (PDFPageOrientation)Enum.Parse(typeof(PDFPageOrientation), DdlPageOrientation);
            resizer.PageMargins = new Margins(margin);

            // how the content is placed on them
            resizer.AllowScale = ChkAllowScale;
            resizer.KeepAspectRatio = ChkKeepAspectRatio;
            resizer.HorizontalAlign = (HorizontalAlign)Enum.Parse(typeof(HorizontalAlign), DdlHorizontalAlign);
            resizer.VerticalAlign = (VerticalAlign)Enum.Parse(typeof(VerticalAlign), DdlVerticalAlign);

            // save pdf document
            byte[] pdf = resizer.Save();

            // close pdf document
            resizer.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
