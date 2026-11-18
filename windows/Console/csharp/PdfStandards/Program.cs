using System;
using System.IO;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace PdfStandards
{
    /// <summary>
    /// Creates accessible (tagged) and archival (PDF/A) documents from the bundled sample.html:
    ///   tagged.pdf             tagged PDF that declares PDF/UA-1, for screen readers
    ///   pdfua-2.pdf            PDF/UA-2 (a PDF 2.0 document)
    ///   pdfa-2b.pdf            PDF/A-2b, for long-term archiving
    ///   pdfa-3b-with-file.pdf  PDF/A-3b with the HTML source embedded as an associated file
    ///   pdfa-3u.pdf            PDF/A-3u: also guarantees the text can be extracted as Unicode
    ///   pdfa-3a.pdf            PDF/A-3a: archival and accessible (tagged)
    ///   pdfa-4-pdfua-2.pdf     PDF/A-4 and PDF/UA-2 together
    /// </summary>
    internal static class Program
    {
        private static string s_appFolder = AppDomain.CurrentDomain.BaseDirectory;

        private static int Main(string[] args)
        {
            if (args.Length > 1 || (args.Length > 0 && (args[0] == "--help" || args[0] == "/?")))
            {
                Console.WriteLine("Usage: PdfStandards [html file]");
                Console.WriteLine("  html file  the page to convert (default: sample.html from the application folder)");
                return 0;
            }

            string htmlFile = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(s_appFolder, "sample.html");
            Console.WriteLine("Converting " + htmlFile);

            try
            {
                CreateTaggedPdf(htmlFile, PdfUAStandard.Pdf_UA_1, "tagged.pdf");
                CreateTaggedPdf(htmlFile, PdfUAStandard.Pdf_UA_2, "pdfua-2.pdf");
                CreatePdfA(htmlFile, PdfStandardSubset.Pdf_A_2b, PdfUAStandard.None, "pdfa-2b.pdf");
                CreatePdfA3WithAssociatedFile(htmlFile, "pdfa-3b-with-file.pdf");
                CreatePdfA(htmlFile, PdfStandardSubset.Pdf_A_3u, PdfUAStandard.None, "pdfa-3u.pdf");
                CreatePdfA(htmlFile, PdfStandardSubset.Pdf_A_3a, PdfUAStandard.None, "pdfa-3a.pdf");
                CreatePdfA(htmlFile, PdfStandardSubset.Pdf_A_4, PdfUAStandard.Pdf_UA_2, "pdfa-4-pdfua-2.pdf");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Conversion failed: " + ex.Message);
                return 1;
            }

            Console.WriteLine("The documents are in " + s_appFolder);
            return 0;
        }

        private static PdfConverter CreateConverter()
        {
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";
            converter.PdfDocumentOptions.LeftMargin = 30;
            converter.PdfDocumentOptions.RightMargin = 30;
            converter.PdfDocumentOptions.TopMargin = 30;
            converter.PdfDocumentOptions.BottomMargin = 30;
            // the title is shown by PDF readers and read out by screen readers; PDF/UA and PDF/A require it
            converter.PdfDocumentInfo.Title = "ExpertPdf HTML to PDF Converter - Sample Page";
            converter.PdfDocumentInfo.AuthorName = "ExpertPdf";
            return converter;
        }

        private static void CreateTaggedPdf(string htmlFile, PdfUAStandard standard, string fileName)
        {
            PdfConverter converter = CreateConverter();

            // The structure (headings, paragraphs, lists, tables, images with their alt text, links) comes from the
            // HTML, so well-formed, semantic HTML gives the best accessible PDF. The language is the language
            // screen readers use to read the text. PDF/UA-2 writes the document as PDF 2.0.
            converter.AccessibilityStandard = standard;
            converter.Language = "en-US";

            converter.SavePdfFromHtmlFileToFile(htmlFile, Path.Combine(s_appFolder, fileName));
            Console.WriteLine("  " + fileName + ": " + standard + ", " + converter.ConversionSummary.PdfPageCount + " pages");
        }

        private static void CreatePdfA(string htmlFile, PdfStandardSubset standard, PdfUAStandard accessibility,
            string fileName)
        {
            PdfConverter converter = CreateConverter();

            // PDF/A embeds the fonts and the color profile and leaves out everything that depends on the outside
            // world, so the document looks the same in many years. The A levels (PDF/A-3a) are tagged as well, and
            // PDF/A-4 combines with PDF/UA-2 (PDF/A-2 and PDF/A-3 combine with PDF/UA-1).
            converter.PdfStandardSubset = standard;
            converter.AccessibilityStandard = accessibility;
            converter.Language = "en-US";

            converter.SavePdfFromHtmlFileToFile(htmlFile, Path.Combine(s_appFolder, fileName));
            Console.WriteLine("  " + fileName + ": " + standard
                + (accessibility != PdfUAStandard.None ? " + " + accessibility : string.Empty));
        }

        private static void CreatePdfA3WithAssociatedFile(string htmlFile, string fileName)
        {
            PdfConverter converter = CreateConverter();
            converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_3b;

            // get the PDF as a Document object to add more content before saving it
            Document document = converter.GetPdfDocumentObjectFromHtmlFile(htmlFile);
            try
            {
                // PDF/A-3 can embed any file, linked to the document by a relationship: here the HTML the PDF was
                // created from (Source). Data, Alternative and Supplement are the other common relationships.
                document.AddAssociatedFile(htmlFile, AttachmentRelationship.Source, "text/html",
                    "The HTML page this document was created from");

                document.Save(Path.Combine(s_appFolder, fileName));
            }
            finally
            {
                document.Close();
            }
            Console.WriteLine("  " + fileName + ": PDF/A-3b with " + Path.GetFileName(htmlFile) + " as an associated file");
        }
    }
}
