using System;
using System.Diagnostics;
using System.IO;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace BatchConversion
{
    /// <summary>
    /// Converts every .html file of a folder to PDF. Run it without arguments to convert the pages of the bundled
    /// html folder into a pdf folder next to the application.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 2 || (args.Length > 0 && (args[0] == "--help" || args[0] == "/?")))
            {
                Console.WriteLine("Usage: BatchConversion [html folder] [pdf folder]");
                Console.WriteLine("  html folder  the folder with the .html files (default: the html folder of the application)");
                Console.WriteLine("  pdf folder   where the PDF files are written (default: the pdf folder of the application)");
                return 0;
            }

            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            string htmlFolder = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(appFolder, "html");
            string pdfFolder = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(appFolder, "pdf");

            if (!Directory.Exists(htmlFolder))
            {
                Console.Error.WriteLine("The folder '" + htmlFolder + "' does not exist.");
                return 2;
            }

            string[] htmlFiles = Directory.GetFiles(htmlFolder, "*.htm*", SearchOption.TopDirectoryOnly);
            if (htmlFiles.Length == 0)
            {
                Console.WriteLine("There is no .html file in " + htmlFolder);
                return 0;
            }

            Directory.CreateDirectory(pdfFolder);
            Console.WriteLine("Converting " + htmlFiles.Length + " files from " + htmlFolder);

            int failed = 0;
            Stopwatch total = Stopwatch.StartNew();
            foreach (string htmlFile in htmlFiles)
            {
                string pdfFile = Path.Combine(pdfFolder, Path.GetFileNameWithoutExtension(htmlFile) + ".pdf");
                Stopwatch watch = Stopwatch.StartNew();
                try
                {
                    // a new converter for each file: the settings of one conversion never leak into the next one
                    PdfConverter converter = new PdfConverter();
                    // converter.LicenseKey = "your license key";
                    converter.PdfDocumentOptions.PdfPageSize = PdfPageSize.A4;
                    converter.PdfDocumentOptions.LeftMargin = 20;
                    converter.PdfDocumentOptions.RightMargin = 20;
                    converter.PdfDocumentOptions.TopMargin = 20;
                    converter.PdfDocumentOptions.BottomMargin = 20;
                    // one broken page must not stop the batch: give up on a page after 30 seconds
                    converter.NavigationTimeout = 30;

                    converter.SavePdfFromHtmlFileToFile(htmlFile, pdfFile);
                    Console.WriteLine("  {0} -> {1} ({2} pages, {3:F1} s)", Path.GetFileName(htmlFile), Path.GetFileName(pdfFile),
                        converter.ConversionSummary.PdfPageCount, watch.Elapsed.TotalSeconds);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine("  {0} failed: {1}", Path.GetFileName(htmlFile), ex.Message);
                }
            }

            Console.WriteLine("Done in {0:F1} s: {1} converted, {2} failed. The PDF files are in {3}",
                total.Elapsed.TotalSeconds, htmlFiles.Length - failed, failed, pdfFolder);
            return failed == 0 ? 0 : 1;
        }
    }
}
