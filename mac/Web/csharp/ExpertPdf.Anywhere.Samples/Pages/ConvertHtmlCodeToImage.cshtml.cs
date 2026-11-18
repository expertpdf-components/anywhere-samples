using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace ExpertPdf.Samples.Pages
{
    public class ConvertHtmlCodeToImageModel : PageModel
    {
        public void OnGet()
        {
            TxtHtmlCode = @"<html>
 <body>
  <br/>
  Hello World from www.html-to-pdf.net.
  <br/>
  <br/>
  <br/>
  <br/>
  Hello World from www.html-to-pdf.net.
  <br/>
  <br/>
 </body>
</html>
";
        }

        [BindProperty]
        public string TxtHtmlCode { get; set; }

        [BindProperty]
        public string TxtBaseUrl { get; set; }

        [BindProperty]
        public string DdlImageFormat { get; set; }

        public List<SelectListItem> ImageFormats { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "png", Text = "PNG" },
            new SelectListItem { Value = "jpeg", Text = "JPEG" },
            new SelectListItem { Value = "bmp", Text = "BMP" },
        };

        [BindProperty]
        public string TxtWidth { get; set; }

        [BindProperty]
        public string TxtHeight { get; set; }

        public IActionResult OnPost()
        {
            // read parameters from the webpage
            string htmlString = TxtHtmlCode;
            string baseUrl = TxtBaseUrl;

            string image_format = DdlImageFormat;
            ImageFormat imageFormat = ImageFormat.Png;
            if (image_format == "jpg")
            {
                imageFormat = ImageFormat.Jpeg;
            }
            else if (image_format == "bmp")
            {
                imageFormat = ImageFormat.Bmp;
            }

            int webPageWidth = 1024;
            try
            {
                webPageWidth = Convert.ToInt32(TxtWidth);
            }
            catch { }

            int webPageHeight = 0;
            try
            {
                webPageHeight = Convert.ToInt32(TxtHeight);
            }
            catch { }

            // instantiate a html to image converter object
            ImgConverter imgConverter = new ImgConverter();

            // set converter options
            imgConverter.PageWidth = webPageWidth;
            imgConverter.PageHeight = webPageHeight;

            // convert to an image in the format chosen
            byte[] img = imgConverter.GetImageBytesFromHtmlString(htmlString, imageFormat, baseUrl);

            // return resulted image
            FileResult fileResult = new FileContentResult(img, "image/" +
                imageFormat.ToString().ToLower());
            fileResult.FileDownloadName = "image." + image_format;
            return fileResult;
        }

    }
}
