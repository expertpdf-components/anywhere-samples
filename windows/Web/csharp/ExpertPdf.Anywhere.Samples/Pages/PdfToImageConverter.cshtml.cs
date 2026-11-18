using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere;
using ExpertPdf.Anywhere.Drawing;

namespace ExpertPdf.Samples.Pages
{
    public class PdfToImageConverterModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtPage { get; set; } = "1";

        [BindProperty]
        public string TxtResolution { get; set; } = "96";

        [BindProperty]
        public string DdlImageFormat { get; set; } = "png";

        [BindProperty]
        public bool ChkGray { get; set; }

        public List<SelectListItem> ImageFormats { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "png", Text = "PNG" },
            new SelectListItem { Value = "jpg", Text = "JPEG" },
            new SelectListItem { Value = "bmp", Text = "BMP" },
        };

        public IActionResult OnPost()
        {
            string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "expertpdf.pdf");

            ImageFormat imageFormat = ImageFormat.Png;
            string mimeType = "image/png";
            if (DdlImageFormat == "jpg")
            {
                imageFormat = ImageFormat.Jpeg;
                mimeType = "image/jpeg";
            }
            else if (DdlImageFormat == "bmp")
            {
                imageFormat = ImageFormat.Bmp;
                mimeType = "image/bmp";
            }

            int pageNumber = 1;
            int.TryParse(TxtPage, out pageNumber);
            int resolution = 96;
            int.TryParse(TxtResolution, out resolution);

            // instantiate the pdf to image converter
            PdfToImageConverter converter = new PdfToImageConverter();

            // the resolution and the color space of the image
            converter.Resolution = resolution;
            converter.ColorSpace = ChkGray ? PdfToImageColorSpace.Gray : PdfToImageColorSpace.RGB;

            // render the page; page numbers start at 1
            byte[] image = converter.ConvertPageToImageBytes(pdfFile, Math.Max(1, pageNumber), imageFormat);

            // return the image
            FileResult fileResult = new FileContentResult(image, mimeType);
            fileResult.FileDownloadName = "image." + DdlImageFormat;
            return fileResult;
        }
    }
}
