using System;
using ExpertPdf.Anywhere.Drawing;
using System.IO;
using ExpertPdf.Anywhere;

namespace PdfToImageInMemory
{
    /// <summary>
    /// Converts PDF pages to images in memory, without temporary files: a contact sheet with a thumbnail of every
    /// page, one page as a JPEG file and a gray multi-page TIFF document for an archive. The PDF document is read from memory, as it would
    /// come from a database or an upload. Run it without arguments to process the sample.pdf document that comes
    /// with the sample.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            string pdfFile = args.Length > 0 ? args[0] : Path.Combine(appFolder, "sample.pdf");
            byte[] pdfBytes = File.ReadAllBytes(pdfFile);

            PdfToImageConverter converter = new PdfToImageConverter();

            // Without a license key the images have an evaluation notice.
            // converter.LicenseKey = "your license key";

            try
            {
                int pageCount = converter.GetPageCount(new MemoryStream(pdfBytes));
                Console.WriteLine("{0}: {1} pages", Path.GetFileName(pdfFile), pageCount);

                // 1. Thumbnails that fit in a 120 x 120 pixels box, keeping the aspect ratio of the pages: the bytes
                //    of an image file for each page (PNG, the ImagesFormat default); your code decides what to do with
                //    them. Here they are placed side by side on the page of a PDF contact sheet.
                byte[][] thumbnails = converter.ConvertToThumbnails(new MemoryStream(pdfBytes), 120, 120);
                string contactSheet = Path.Combine(appFolder, "contact-sheet.pdf");
                SaveContactSheet(thumbnails, contactSheet);
                Console.WriteLine("Contact sheet with {0} thumbnails: {1}", thumbnails.Length, contactSheet);

                // the document information is available after a conversion
                DocumentInformation info = converter.DocumentInformation;
                if (info != null)
                    Console.WriteLine("Title: {0}, author: {1}", info.Title, info.Author);

                // 2. One page as the bytes of a JPEG file, for a web response or a database. The StartPageNumber
                //    and EndPageNumber properties are not used.
                converter.Resolution = 96;
                converter.JpegQuality = 80;
                byte[] jpeg = converter.ConvertPageToImageBytes(new MemoryStream(pdfBytes), 1, ImageFormat.Jpeg);
                string jpegFile = Path.Combine(appFolder, "page-1.jpg");
                File.WriteAllBytes(jpegFile, jpeg);
                Console.WriteLine("Page 1 as JPEG (96 DPI, quality 80, {0:N0} bytes): {1}", jpeg.Length, jpegFile);

                // 3. A gray multi-page TIFF document of pages 2 to 4, for an archive or a fax service.
                //    ConvertToTiff returns the bytes of the TIFF file: store them anywhere.
                converter.Resolution = 150;
                converter.ColorSpace = PdfToImageColorSpace.Gray;
                converter.StartPageNumber = 2;
                converter.EndPageNumber = 4;
                byte[] tiff = converter.ConvertToTiff(new MemoryStream(pdfBytes));
                string tiffFile = Path.Combine(appFolder, "pages-2-4.tiff");
                File.WriteAllBytes(tiffFile, tiff);
                Console.WriteLine("Multi-page TIFF of pages 2 to 4 (150 DPI, gray, {0:N0} bytes): {1}", tiff.Length, tiffFile);
            }
            catch (PdfToImageException ex)
            {
                Console.WriteLine("The conversion failed: " + ex.Message);
                return 2;
            }

            return 0;
        }

        // places the page images side by side on the pages of a PDF document, five in a row
        private static void SaveContactSheet(byte[][] pages, string file)
        {
            const float cell = 100;
            ExpertPdf.Anywhere.PdfDocument.Document sheet = new ExpertPdf.Anywhere.PdfDocument.Document();
            try
            {
                ExpertPdf.Anywhere.PdfDocument.PdfPage page = null;
                for (int i = 0; i < pages.Length; i++)
                {
                    int onPage = i % 35;
                    if (onPage == 0)
                        page = sheet.AddPage();
                    // each image at its own size (pixels at 96 DPI), in a cell of the grid
                    page.AddElement(new ExpertPdf.Anywhere.PdfDocument.ImageElement(onPage % 5 * cell,
                        onPage / 5 * cell, pages[i]));
                }
                sheet.Save(file);
            }
            finally
            {
                sheet.Close();
            }
        }
    }
}
