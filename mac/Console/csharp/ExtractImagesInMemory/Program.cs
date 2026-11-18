using System;
using System.Collections.Generic;
using System.IO;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere;

namespace ExtractImagesInMemory
{
    /// <summary>
    /// Extracts the images of a PDF document into memory, in one list, and keeps only the photos and charts:
    /// small images (logos, icons, bullets) and repeated images are skipped by the extractor. The PDF document is
    /// read from memory, as it would come from a database or an upload. Run it without arguments to process the
    /// sample.pdf document that comes with the sample.
    /// </summary>
    internal static class Program
    {
        // images smaller than this (in pixels) are decorations, not content
        private const int MinimumSize = 100;

        private static int Main(string[] args)
        {
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            string pdfFile = args.Length > 0 ? args[0] : Path.Combine(appFolder, "sample.pdf");
            byte[] pdfBytes = File.ReadAllBytes(pdfFile);

            string outFolder = Path.Combine(appFolder, "photos");
            Directory.CreateDirectory(outFolder);

            PdfImgExtract extractor = new PdfImgExtract();

            // Without a license key the extracted images have an evaluation notice.
            // extractor.LicenseKey = "your license key";

            int pageCount = extractor.GetPageCount(new MemoryStream(pdfBytes));
            Console.WriteLine("{0}: {1} pages", Path.GetFileName(pdfFile), pageCount);

            // an image narrower or shorter than this is skipped
            extractor.MinimumWidth = MinimumSize;
            extractor.MinimumHeight = MinimumSize;

            // an image with the same pixels as one extracted before (a logo on every page) is skipped
            extractor.SkipDuplicateImages = true;

            // the format of the extracted image files: here JPEG
            extractor.ExtractImageFormat = ImageFormat.Jpeg;

            List<ExtractedImage> images;
            try
            {
                // all the images in one list, with the page of each image; nothing is saved by the extractor
                images = extractor.ExtractImagesToMemory(new MemoryStream(pdfBytes));
            }
            catch (PdfImgExtractException ex)
            {
                Console.WriteLine("The extraction failed: " + ex.Message);
                return 2;
            }

            // the number of images kept on each page
            SortedDictionary<int, int> imagesPerPage = new SortedDictionary<int, int>();
            foreach (ExtractedImage image in images)
            {
                // your own file names: here named after the page and the position on the page. Image holds the
                // bytes of the image file, in the ExtractImageFormat format
                string file = Path.Combine(outFolder, string.Format("page{0}-{1}.jpg", image.PageNumber, image.IndexOnPage + 1));
                File.WriteAllBytes(file, image.Image);
                Console.WriteLine("  page {0}: saved {1} ({2:N0} bytes)", image.PageNumber, Path.GetFileName(file),
                    image.Image.Length);

                int found;
                imagesPerPage.TryGetValue(image.PageNumber, out found);
                imagesPerPage[image.PageNumber] = found + 1;
            }

            Console.WriteLine();
            for (int page = 1; page <= pageCount; page++)
            {
                int found;
                imagesPerPage.TryGetValue(page, out found);
                Console.WriteLine("Page {0}: {1} image(s) of at least {2} x {2} pixels", page, found, MinimumSize);
            }
            Console.WriteLine("{0} image(s) saved in {1}", images.Count, outFolder);
            return 0;
        }
    }
}
