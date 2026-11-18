using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PDFForms;

namespace ExpertPdf.Samples.Pages
{
    public class PdfFormFillingModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public bool ChkFlatten { get; set; }

        public IActionResult OnPost()
        {
            // the form to fill
            string file = Path.Combine(Startup.ServerPath, "wwwroot", "files", "w-9.pdf");

            // load the form manager
            PdfFormManager form = new PdfFormManager();
            form.Load(file);

            // fill some text fields
            SetText(form, "f1_01", "This is my name");
            SetText(form, "f1_02", "This is my business name");
            SetText(form, "f1_07", "This is my address");
            SetText(form, "f1_08", "This is my city");
            SetText(form, "f1_11", "123");
            SetText(form, "f1_12", "45");
            SetText(form, "f1_13", "6789");

            // check a check box
            FormFieldCheckBox individual = form.Fields["c1_1"] as FormFieldCheckBox;
            if (individual != null)
            {
                individual.Checked = true;
            }

            // flatten the fields if asked
            form.Flatten = ChkFlatten;

            // save pdf document
            byte[] pdf = form.Save();

            // close pdf document
            form.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }

        private static void SetText(PdfFormManager form, string name, string text)
        {
            FormFieldTextBox field = form.Fields[name] as FormFieldTextBox;
            if (field != null)
            {
                field.Text = text;
            }
        }
    }
}
