using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class HtmlFormsToPdfFieldsModel : PageModel
    {
        public void OnGet()
        {
            TxtHtmlCode = @"<html lang=""en"">
<head><title>Application form</title></head>
<body style=""font-family: Arial"">
    <h1>Application form</h1>
    <form>
        <p>Full name <input type=""text"" name=""fullName"" value=""John Smith""></p>
        <p>Email <input type=""email"" name=""email""></p>
        <p><label><input type=""checkbox"" name=""agree"" checked> I agree</label></p>
        <fieldset><legend>Plan</legend>
            <label><input type=""radio"" name=""plan"" value=""basic""> Basic</label>
            <label><input type=""radio"" name=""plan"" value=""pro"" checked> Pro</label>
        </fieldset>
        <p>Country <select name=""country"">
            <option value=""ro"">Romania</option>
            <option value=""de"" selected>Germany</option>
        </select></p>
        <p>Comments<br><textarea name=""comments"" rows=""3"" cols=""40""></textarea></p>
    </form>
</body>
</html>
";
        }

        [BindProperty]
        public string TxtHtmlCode { get; set; }

        [BindProperty]
        public bool ChkCreateFormFields { get; set; } = true;

        [BindProperty]
        public string DdlStandard { get; set; } = "Full";

        public List<SelectListItem> Standards { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Full", Text = "None" },
            new SelectListItem { Value = "PdfA2B", Text = "PDF/A-2b" },
            new SelectListItem { Value = "UA1", Text = "PDF/UA-1 (accessible)" },
            new SelectListItem { Value = "PdfA3A", Text = "PDF/A-3a + PDF/UA-1" },
        };

        public IActionResult OnPost()
        {
            // instantiate a html to pdf converter object
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            // the form controls become fillable pdf form fields
            converter.CreateFormFields = ChkCreateFormFields;

            // an accessible document needs a title and a language
            if (DdlStandard == "UA1" || DdlStandard == "PdfA3A")
            {
                converter.AccessibilityStandard = PdfUAStandard.Pdf_UA_1;
                converter.PdfDocumentInfo.Title = "Application form";
                converter.Language = "en";
            }
            if (DdlStandard == "PdfA2B") converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_2b;
            if (DdlStandard == "PdfA3A") converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_3a;

            // convert the html code to pdf
            byte[] pdf = converter.GetPdfBytesFromHtmlString(TxtHtmlCode);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Form.pdf";
            return fileResult;
        }
    }
}
