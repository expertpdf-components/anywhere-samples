using System;
using ExpertPdf.Anywhere.Drawing;
using System.IO;
using ExpertPdf.Anywhere;

namespace ConvertPdfToImages
{
    /// <summary>
    /// A command line tool that converts the pages of a PDF document to image files (PNG, JPEG, BMP, TIFF)
    /// or to one multi-page TIFF file. Run it without arguments to convert the pages of the sample.pdf document
    /// that comes with the sample to PNG files.
    /// </summary>
    internal static class Program
    {
        private static readonly string AppFolder = AppDomain.CurrentDomain.BaseDirectory;

        private static int Main(string[] args)
        {
            // the defaults: the bundled sample document, all pages, PNG files at 120 DPI in the "images" folder
            string pdfFile = Path.Combine(AppFolder, "sample.pdf");
            string outFolder = Path.Combine(AppFolder, "images");
            ImageFormat format = ImageFormat.Png;
            bool multiPageTiff = false;
            int resolution = 120;
            bool gray = false;
            int startPage = 1;
            int endPage = 0;
            string prefix = null;
            string password = null;
            int jpegQuality = 91;
            int pngCompressionLevel = -1;
            PdfToImageTiffCompression compression = PdfToImageTiffCompression.Lzw;
            int thumbnailSize = 0;

            foreach (string arg in args)
            {
                string name = arg, value = null;
                int colon = arg.IndexOf(':');
                if (colon > 0)
                {
                    name = arg.Substring(0, colon);
                    value = arg.Substring(colon + 1).Trim('"');
                }

                switch (name.ToLowerInvariant())
                {
                    case "/pdf": pdfFile = InputFile(value); break;
                    case "/out": outFolder = Path.GetFullPath(value); break;
                    case "/dpi": resolution = int.Parse(value); break;
                    case "/gray": gray = true; break;
                    case "/start": startPage = int.Parse(value); break;
                    case "/end": endPage = int.Parse(value); break;
                    case "/prefix": prefix = value; break;
                    case "/userpswd": password = value; break;
                    case "/tiff": multiPageTiff = true; break;
                    case "/quality": jpegQuality = int.Parse(value); break;
                    case "/pngcompression": pngCompressionLevel = int.Parse(value); break;
                    case "/thumbs": thumbnailSize = int.Parse(value); break;
                    case "/compression":
                        switch (value)
                        {
                            case "lzw": compression = PdfToImageTiffCompression.Lzw; break;
                            case "none": compression = PdfToImageTiffCompression.None; break;
                            case "rle": compression = PdfToImageTiffCompression.Rle; break;
                            case "ccitt3": compression = PdfToImageTiffCompression.Ccitt3; break;
                            case "ccitt4": compression = PdfToImageTiffCompression.Ccitt4; break;
                            default: return Usage("Unknown TIFF compression: " + value);
                        }
                        break;
                    case "/format":
                        switch (value)
                        {
                            case "png": format = ImageFormat.Png; break;
                            case "jpg": format = ImageFormat.Jpeg; break;
                            case "bmp": format = ImageFormat.Bmp; break;
                            case "tiff": format = ImageFormat.Tiff; break;
                            default: return Usage("Unknown image format: " + value);
                        }
                        break;
                    case "/?":
                    case "/help":
                        return Usage(null);
                    default:
                        return Usage("Unknown argument: " + arg);
                }
            }

            if (!File.Exists(pdfFile))
                return Usage("The PDF file was not found: " + pdfFile);

            // page_1.png, page_2.png, ... or sample.tiff for a multi-page TIFF made from sample.pdf
            if (prefix == null)
                prefix = multiPageTiff ? Path.GetFileNameWithoutExtension(pdfFile) : thumbnailSize > 0 ? "thumb_" : "page_";

            // the converter saves the images in an existing folder
            Directory.CreateDirectory(outFolder);

            PdfToImageConverter converter = new PdfToImageConverter();

            // Without a license key the images have an evaluation notice.
            // converter.LicenseKey = "your license key";

            // the pages to convert: EndPageNumber = 0 means "up to the last page"
            converter.StartPageNumber = startPage;
            converter.EndPageNumber = endPage;

            // the resolution decides the size of the images: an A4 page is 8.27 x 11.69 inches,
            // 992 x 1403 pixels at 120 DPI; use 200-300 DPI for printing or OCR, 36-72 DPI for thumbnails
            converter.Resolution = resolution;

            // gray images are smaller and enough for archiving and OCR
            converter.ColorSpace = gray ? PdfToImageColorSpace.Gray : PdfToImageColorSpace.RGB;

            // where and how the image files are saved: <ImagesPath>\<ImagesPrefix>1.<format>, <ImagesPrefix>2.<format>, ...
            // numbered from 1 for the first converted page
            converter.ImagesPath = outFolder;
            converter.ImagesPrefix = prefix;
            converter.ImagesFormat = format;

            // JPEG files: 0 (smallest) to 100 (best quality); TIFF files: LZW (the default), none, RLE (PackBits),
            // or CCITT Group 3 / Group 4 - black and white images, the smallest files for text pages and fax services
            converter.JpegQuality = jpegQuality;
            converter.TiffCompression = compression;

            // PNG files: the compression level, 0 (no compression, the fastest) to 9 (the smallest files); the pixels
            // are the same at every level. -1 (the default) keeps the renderer's output
            converter.PngCompressionLevel = pngCompressionLevel;

            // the password needed to open a protected document (null for a document without a password)
            converter.UserPassword = password;

            DateTime started = DateTime.Now.AddSeconds(-1);
            try
            {
                if (thumbnailSize > 0)
                {
                    // thumbnails that fit in a thumbnailSize x thumbnailSize box, keeping the aspect ratio of the
                    // pages; the resolution is chosen for each page, the Resolution property is not used. The thumbnails
                    // are image files in the ImagesFormat format.
                    byte[][] thumbnails = converter.ConvertToThumbnails(pdfFile, thumbnailSize, thumbnailSize);
                    string extension = format == ImageFormat.Jpeg ? ".jpg" : "." + format.ToString().ToLowerInvariant();
                    for (int i = 0; i < thumbnails.Length; i++)
                        File.WriteAllBytes(Path.Combine(outFolder, prefix + (i + 1) + extension), thumbnails[i]);
                }
                else if (multiPageTiff)
                {
                    // one TIFF file with a frame for each page: <ImagesPath>\<ImagesPrefix>.tiff
                    converter.ConvertToTiffInFolder(pdfFile);
                }
                else
                {
                    // one image file for each page
                    converter.ConvertToImagesInFolder(pdfFile);
                }
            }
            catch (PdfToImageException ex)
            {
                Console.WriteLine("The conversion failed: " + ex.Message);
                return 2;
            }

            DocumentInformation info = converter.DocumentInformation;
            Console.WriteLine("Converted: {0} ({1} pages)", pdfFile, info != null ? info.PageCount : 0);
            Console.WriteLine("  {0} DPI, {1}, {2}", resolution, gray ? "gray" : "color",
                thumbnailSize > 0 ? "PNG thumbnails of at most " + thumbnailSize + " pixels"
                : multiPageTiff ? "one multi-page TIFF file" : format.ToString().ToUpperInvariant() + " files");
            foreach (string file in Directory.GetFiles(outFolder, prefix + "*"))
            {
                if (File.GetLastWriteTime(file) >= started)
                    Console.WriteLine("  {0} ({1:N0} bytes)", file, new FileInfo(file).Length);
            }
            return 0;
        }

        // a relative path is looked for in the current folder, then next to the program (where the sample files are)
        private static string InputFile(string path)
        {
            if (Path.IsPathRooted(path) || File.Exists(path))
                return Path.GetFullPath(path);
            return Path.Combine(AppFolder, path);
        }

        private static int Usage(string error)
        {
            if (error != null)
                Console.WriteLine(error);
            Console.WriteLine("Usage: ConvertPdfToImages [/pdf:file.pdf] [/out:folder] [/format:png|jpg|bmp|tiff] [/tiff]");
            Console.WriteLine("                          [/dpi:n] [/gray] [/start:n] [/end:n] [/prefix:name] [/userpswd:password]");
            Console.WriteLine("                          [/quality:0-100] [/compression:lzw|none|rle|ccitt3|ccitt4] [/pngcompression:0-9]");
            Console.WriteLine("                          [/thumbs:size]");
            Console.WriteLine("Without arguments the pages of sample.pdf are converted to PNG files in the images folder.");
            Console.WriteLine("Examples:");
            Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /format:jpg /dpi:72 /start:1 /end:2");
            Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /tiff /gray /dpi:200");
            Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /tiff /compression:ccitt4 /dpi:200");
            Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /format:jpg /quality:60");
            Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /pngcompression:9");
            Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /thumbs:160");
            Console.WriteLine("  ConvertPdfToImages /pdf:sample-protected.pdf /userpswd:open");
            return error == null ? 0 : 1;
        }
    }
}
