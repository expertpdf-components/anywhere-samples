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
    public class PdfConverterSecurityModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtUrl { get; set; }

        [BindProperty]
        public string TxtUserPassword { get; set; }

        [BindProperty]
        public string TxtOwnerPassword { get; set; }

        [BindProperty]
        public bool ChkCanAssembleDocument { get; set; }

        [BindProperty]
        public bool ChkCanCopyContent { get; set; }

        [BindProperty]
        public bool ChkCanEditAnnotations { get; set; }

        [BindProperty]
        public bool ChkCanEditContent { get; set; }

        [BindProperty]
        public bool ChkCanFillFormFields { get; set; }

        [BindProperty]
        public bool ChkCanPrint { get; set; }

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();

            // set document passwords
            if (!string.IsNullOrEmpty(TxtUserPassword))
            {
                converter.PdfSecurityOptions.UserPassword = TxtUserPassword;
            }
            if (!string.IsNullOrEmpty(TxtOwnerPassword))
            {
                converter.PdfSecurityOptions.OwnerPassword = TxtOwnerPassword;
            }

            //set document permissions
            converter.PdfSecurityOptions.CanAssembleDocument = ChkCanAssembleDocument;
            converter.PdfSecurityOptions.CanCopyContent = ChkCanCopyContent;
            converter.PdfSecurityOptions.CanEditAnnotations = ChkCanEditAnnotations;
            converter.PdfSecurityOptions.CanEditContent = ChkCanEditContent;
            converter.PdfSecurityOptions.CanFillFormFields = ChkCanFillFormFields;
            converter.PdfSecurityOptions.CanPrint = ChkCanPrint;

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
