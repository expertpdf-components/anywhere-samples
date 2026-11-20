using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere;

namespace ExpertPdf.Samples.Pages
{
    public class PdfToWordConverterModel : PageModel
    {
        public void OnGet()
        {
        }

        // An uploaded PDF, or nothing - the shipped files/document.pdf is converted then.
        [BindProperty]
        public IFormFile FilePdf { get; set; }

        [BindProperty]
        public string DdlLayout { get; set; } = "Flowing";

        [BindProperty]
        public string DdlFormat { get; set; } = "Docx";

        [BindProperty]
        public string TxtStartPage { get; set; } = "1";

        [BindProperty]
        public string TxtEndPage { get; set; } = "0";

        [BindProperty]
        public bool ChkImages { get; set; } = true;

        [BindProperty]
        public bool ChkHeadersAndFooters { get; set; } = true;

        [BindProperty]
        public bool ChkLinks { get; set; } = true;

        [BindProperty]
        public bool ChkBookmarks { get; set; } = true;

        [BindProperty]
        public bool ChkEmbedFonts { get; set; } = true;

        public List<SelectListItem> Layouts { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Flowing", Text = "Flowing - a document to edit" },
            new SelectListItem { Value = "Exact", Text = "Exact - looks like the PDF page for page" },
        };

        public List<SelectListItem> Formats { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Docx", Text = "Word (.docx)" },
            new SelectListItem { Value = "Rtf", Text = "Rich Text Format (.rtf)" },
            new SelectListItem { Value = "Odt", Text = "OpenDocument text (.odt)" },
            new SelectListItem { Value = "Html", Text = "Web page (.html)" },
            new SelectListItem { Value = "Markdown", Text = "Markdown (.md)" },
        };

        public IActionResult OnPost()
        {
            // instantiate the pdf to word converter
            PdfToWordConverter converter = new PdfToWordConverter();
            // converter.LicenseKey = "your license key";

            // how the pages are laid out, and the format written
            converter.Layout = (PdfToWordLayout)Enum.Parse(typeof(PdfToWordLayout), DdlLayout);
            converter.OutputFormat = (PdfToWordFormat)Enum.Parse(typeof(PdfToWordFormat), DdlFormat);

            // the pages to convert (an end page of 0 means the last page)
            int startPage = 1;
            int.TryParse(TxtStartPage, out startPage);
            int endPage = 0;
            int.TryParse(TxtEndPage, out endPage);
            converter.StartPageNumber = Math.Max(1, startPage);
            converter.EndPageNumber = Math.Max(0, endPage);

            // what is carried into the document
            converter.Images = ChkImages;
            converter.HeadersAndFooters = ChkHeadersAndFooters;
            converter.Links = ChkLinks;
            converter.Bookmarks = ChkBookmarks;
            converter.EmbedFonts = ChkEmbedFonts;

            // convert the uploaded pdf, or the sample one shipped with the application
            byte[] document;
            if (FilePdf != null && FilePdf.Length > 0)
            {
                using (Stream uploaded = FilePdf.OpenReadStream())
                {
                    document = converter.ConvertToWord(uploaded);
                }
            }
            else
            {
                string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "document.pdf");
                document = converter.ConvertToWord(pdfFile);
            }

            // return the converted document
            string extension;
            string contentType;
            switch (converter.OutputFormat)
            {
                case PdfToWordFormat.Rtf:
                    extension = "rtf"; contentType = "application/rtf"; break;
                case PdfToWordFormat.Odt:
                    extension = "odt"; contentType = "application/vnd.oasis.opendocument.text"; break;
                case PdfToWordFormat.Html:
                    extension = "html"; contentType = "text/html"; break;
                case PdfToWordFormat.Markdown:
                    extension = "md"; contentType = "text/markdown"; break;
                default:
                    extension = "docx";
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
            }
            FileResult fileResult = new FileContentResult(document, contentType);
            fileResult.FileDownloadName = "Document." + extension;
            return fileResult;
        }
    }
}
