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
    public class PdfConverterViewerPreferencesModel : PageModel
    {
        public void OnGet()
        {
            DdlPageLayout = "OneColumn";
            DdlPageMode = "UseNone";
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string DdlPageLayout { get; set; }

        public List<SelectListItem> PageLayouts { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "SinglePage", Text = "SinglePage" },
            new SelectListItem { Value = "OneColumn", Text = "OneColumn" },
            new SelectListItem { Value = "TwoColumnLeft", Text = "TwoColumnLeft" },
            new SelectListItem { Value = "TwoColumnRight", Text = "TwoColumnRight" },
        };

        [BindProperty]
        public string DdlPageMode { get; set; }

        public List<SelectListItem> PageModes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "UseNone", Text = "UseNone" },
            new SelectListItem { Value = "UseOutlines", Text = "UseOutlines" },
            new SelectListItem { Value = "UseThumbs", Text = "UseThumbs" },
            new SelectListItem { Value = "FullScreen", Text = "FullScreen" },
            new SelectListItem { Value = "UseOC", Text = "UseOC" },
            new SelectListItem { Value = "UseAttachments", Text = "UseAttachments" },
        };

        [BindProperty]
        public bool ChkCenterWindow { get; set; }

        [BindProperty]
        public bool ChkDisplayDocTitle { get; set; }

        [BindProperty]
        public bool ChkFitWindow { get; set; }

        [BindProperty]
        public bool ChkHideMenuBar { get; set; }

        [BindProperty]
        public bool ChkHideToolbar { get; set; }

        [BindProperty]
        public bool ChkHideWindowUI { get; set; }

        public IActionResult OnPost()
        {
            // read parameters from the webpage
            ViewerPageLayout pageLayout = (ViewerPageLayout)Enum.Parse(
                typeof(ViewerPageLayout), DdlPageLayout, true);

            ViewerPageMode pageMode = (ViewerPageMode)Enum.Parse(
                typeof(ViewerPageMode), DdlPageMode, true);

            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // set converter options
            converter.PdfViewerPreferences.CenterWindow = ChkCenterWindow;
            converter.PdfViewerPreferences.DisplayDocTitle = ChkDisplayDocTitle;
            converter.PdfViewerPreferences.FitWindow = ChkFitWindow;
            converter.PdfViewerPreferences.HideMenuBar = ChkHideMenuBar;
            converter.PdfViewerPreferences.HideToolbar = ChkHideToolbar;
            converter.PdfViewerPreferences.HideWindowUI = ChkHideWindowUI;

            converter.PdfViewerPreferences.PageLayout = pageLayout;
            converter.PdfViewerPreferences.PageMode = pageMode;
            converter.PdfViewerPreferences.NonFullScreenPageMode =
                ViewerFullScreenExitMode.UseNone;

            // create a new pdf document converting an url
            Document doc = converter.GetPdfDocumentObjectFromUrl(TxtUrl);

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
