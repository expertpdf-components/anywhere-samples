using System;
using System.IO;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere;

namespace ExtractPdfImages
{
    /// <summary>
    /// A command line tool that extracts the images embedded in a PDF document to image files.
    /// Run it without arguments to extract the images of the sample.pdf document that comes with the sample.
    /// </summary>
    internal static class Program
    {
        private static readonly string AppFolder = AppDomain.CurrentDomain.BaseDirectory;

        private static int Main(string[] args)
        {
            // the defaults: the bundled sample document, all pages, PNG files in the "extracted" folder
            string pdfFile = Path.Combine(AppFolder, "sample.pdf");
            string outFolder = Path.Combine(AppFolder, "extracted");
            ImageFormat format = ImageFormat.Png;
            int startPage = 1;
            int endPage = 0;
            string prefix = "image_";
            string password = null;
            int minWidth = 0;
            int minHeight = 0;
            bool skipDuplicates = false;

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
                    case "/start": startPage = int.Parse(value); break;
                    case "/end": endPage = int.Parse(value); break;
                    case "/prefix": prefix = value; break;
                    case "/userpswd": password = value; break;
                    case "/minwidth": minWidth = int.Parse(value); break;
                    case "/minheight": minHeight = int.Parse(value); break;
                    case "/nodup": skipDuplicates = true; break;
                    case "/imgformat":
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

            // the extractor saves the images in an existing folder
            Directory.CreateDirectory(outFolder);

            PdfImgExtract extractor = new PdfImgExtract();

            // Without a license key the extracted images have an evaluation notice.
            // extractor.LicenseKey = "your license key";

            // the pages to extract the images from: EndPageNumber = 0 means "up to the last page"
            extractor.StartPageNumber = startPage;
            extractor.EndPageNumber = endPage;

            // the files are named <ExtractPrefix><page number>_<image index, from 0>.<format>
            extractor.ExtractPath = outFolder;
            extractor.ExtractPrefix = prefix;
            extractor.ExtractImageFormat = format;

            // the password needed to open a protected document (null for a document without a password)
            extractor.UserPassword = password;

            // skip the small images (icons, bullets, lines) and extract a repeated image (a logo on every page) once
            extractor.MinimumWidth = minWidth;
            extractor.MinimumHeight = minHeight;
            extractor.SkipDuplicateImages = skipDuplicates;

            // the event is raised for every image, before it is saved: the place to log, filter or index the images
            int count = 0;
            extractor.ImageExtractedEvent += delegate (ImageExtractedEventArgs e)
            {
                count++;
                // ExtractedBitmap holds the bytes of the image file, in the ExtractImageFormat format
                Console.WriteLine("  page {0}: {1:N0} bytes", e.PageNumber, e.ExtractedBitmap.Length);
            };

            Console.WriteLine("Extracting the images of " + pdfFile);
            try
            {
                // true: save the images in the ExtractPath folder
                extractor.ExtractImages(pdfFile, true);
            }
            catch (PdfImgExtractException ex)
            {
                Console.WriteLine("The extraction failed: " + ex.Message);
                return 2;
            }

            DocumentInformation info = extractor.DocumentInformation;
            Console.WriteLine("{0} images extracted from {1} pages, saved as {2} files in {3}",
                count, info != null ? info.PageCount : 0, format.ToString().ToUpperInvariant(), outFolder);
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
            Console.WriteLine("Usage: ExtractPdfImages [/pdf:file.pdf] [/out:folder] [/imgformat:png|jpg|bmp|tiff]");
            Console.WriteLine("                        [/start:n] [/end:n] [/prefix:name] [/userpswd:password]");
            Console.WriteLine("                        [/minwidth:n] [/minheight:n] [/nodup]");
            Console.WriteLine("Without arguments the images of sample.pdf are saved as PNG files in the extracted folder.");
            Console.WriteLine("Examples:");
            Console.WriteLine("  ExtractPdfImages /pdf:sample.pdf /imgformat:jpg /start:4 /end:4");
            Console.WriteLine("  ExtractPdfImages /pdf:sample.pdf /minwidth:100 /minheight:100 /nodup");
            Console.WriteLine("  ExtractPdfImages /pdf:sample-protected.pdf /userpswd:open");
            return error == null ? 0 : 1;
        }
    }
}
