using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere;
using ExpertPdf.Anywhere.Drawing;

namespace ExpertPdf.Samples.Pages
{
    public class PdfImageExtractModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtMinimumSize { get; set; } = "50";

        [BindProperty]
        public string DdlImageFormat { get; set; } = "png";

        [BindProperty]
        public bool ChkSkipDuplicates { get; set; } = true;

        public List<SelectListItem> ImageFormats { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "png", Text = "PNG" },
            new SelectListItem { Value = "jpg", Text = "JPEG" },
        };

        public IActionResult OnPost()
        {
            string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "expertpdf.pdf");

            int minimumSize = 0;
            int.TryParse(TxtMinimumSize, out minimumSize);

            // instantiate the images extractor
            PdfImgExtract extractor = new PdfImgExtract();

            // leave out small images and images repeated on several pages
            extractor.MinimumWidth = minimumSize;
            extractor.MinimumHeight = minimumSize;
            extractor.SkipDuplicateImages = ChkSkipDuplicates;

            // the format of the extracted images
            extractor.ExtractImageFormat = DdlImageFormat == "jpg" ? ImageFormat.Jpeg : ImageFormat.Png;

            // extract the images to memory
            List<ExtractedImage> images = extractor.ExtractImagesToMemory(pdfFile);

            // put the images in a zip archive
            MemoryStream zipStream = new MemoryStream();
            using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (ExtractedImage image in images)
                {
                    string name = string.Format("page{0}-image{1}.{2}", image.PageNumber,
                        image.IndexOnPage + 1, DdlImageFormat);
                    ZipArchiveEntry entry = zip.CreateEntry(name);
                    using (Stream entryStream = entry.Open())
                    {
                        entryStream.Write(image.Image, 0, image.Image.Length);
                    }
                }
            }

            // return the zip archive
            FileResult fileResult = new FileContentResult(zipStream.ToArray(), "application/zip");
            fileResult.FileDownloadName = "images.zip";
            return fileResult;
        }
    }
}
