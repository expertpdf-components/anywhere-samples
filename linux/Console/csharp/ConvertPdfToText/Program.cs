using System;
using System.IO;
using System.Text;
using ExpertPdf.Anywhere;

namespace ConvertPdfToText
{
    /// <summary>
    /// A command line tool that extracts the text of a PDF document to a text file or to a HTML file.
    /// Run it without arguments to convert the sample.pdf document that comes with the sample.
    /// </summary>
    internal static class Program
    {
        private static readonly string AppFolder = AppDomain.CurrentDomain.BaseDirectory;

        private static int Main(string[] args)
        {
            // the defaults: the bundled sample document, the original layout, a .txt file next to the program
            string pdfFile = Path.Combine(AppFolder, "sample.pdf");
            string outFile = null;
            string password = null;
            TextLayout layout = TextLayout.OriginalLayout;
            int startPage = 1;
            int endPage = 0;
            bool markPageBreaks = false;
            bool html = false;
            string htmlCharset = "utf-8";
            bool clipText = true;
            bool perPage = false;

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
                    case "/out": outFile = Path.GetFullPath(value); break;
                    case "/userpswd": password = value; break;
                    case "/start": startPage = int.Parse(value); break;
                    case "/end": endPage = int.Parse(value); break;
                    case "/pagebreaks": markPageBreaks = true; break;
                    case "/noclip": clipText = false; break;
                    case "/pages": perPage = true; break;
                    case "/html":
                        html = true;
                        if (!string.IsNullOrEmpty(value))
                            htmlCharset = value;
                        break;
                    case "/layout":
                        if (value == "reading")
                            layout = TextLayout.ReadingLayout;
                        else if (value == "original")
                            layout = TextLayout.OriginalLayout;
                        else
                            return Usage("Unknown layout: " + value);
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

            if (outFile == null)
                outFile = Path.Combine(AppFolder, Path.GetFileNameWithoutExtension(pdfFile) + (html ? ".html" : ".txt"));

            PdfToTextConverter converter = new PdfToTextConverter();

            // Without a license key the text of the first 5 pages is extracted and an evaluation notice is added.
            // converter.LicenseKey = "your license key";

            // OriginalLayout keeps the columns of a page side by side, as they are printed;
            // ReadingLayout puts the text in reading order (a whole column, then the next one) - better for indexing
            converter.Layout = layout;

            // the pages to convert: EndPageNumber = 0 means "up to the last page"
            converter.StartPageNumber = startPage;
            converter.EndPageNumber = endPage;

            // a form feed character (PAGE_BREAK_MARK) after the text of each page lets you split the result into pages
            converter.MarkPageBreaks = markPageBreaks;

            // text hidden by a clipping path is not visible on the page; ClipText = false extracts it too
            converter.ClipText = clipText;

            // a HTML document with the title, author, subject and keywords of the PDF document as meta tags
            converter.AddHtmlMetaTags = html;
            converter.HtmlCharset = htmlCharset;

            // the password needed to open a protected document (null for a document without a password)
            converter.UserPassword = password;

            try
            {
                if (perPage)
                    return SavePages(converter, pdfFile, outFile);

                string text = converter.ConvertToText(pdfFile);

                // the HTML charset and the encoding of the file must match
                File.WriteAllText(outFile, text, html ? Encoding.GetEncoding(htmlCharset) : Encoding.UTF8);

                // the document information is available after the conversion
                DocumentInformation info = converter.DocumentInformation;
                Console.WriteLine("Converted: " + pdfFile);
                if (info != null)
                {
                    Console.WriteLine("  Title:    " + info.Title);
                    Console.WriteLine("  Author:   " + info.Author);
                    Console.WriteLine("  Subject:  " + info.Subject);
                    Console.WriteLine("  Keywords: " + info.Keywords);
                    Console.WriteLine("  Pages:    " + info.PageCount);
                }
                Console.WriteLine("  Layout:   " + layout + (markPageBreaks ? ", page breaks marked" : "") + (clipText ? "" : ", hidden text included"));
                Console.WriteLine("Output: " + outFile + " (" + text.Length + " characters)");
                return 0;
            }
            catch (PdfToTextException ex)
            {
                Console.WriteLine("The conversion failed: " + ex.Message);
                return 2;
            }
        }

        // the text of each page in a file of its own: <name>-page1.txt, <name>-page2.txt, ...
        private static int SavePages(PdfToTextConverter converter, string pdfFile, string outFile)
        {
            // one call, one string for each page (from StartPageNumber to EndPageNumber)
            string[] pages = converter.ConvertToTextPages(pdfFile);

            string baseName = Path.Combine(Path.GetDirectoryName(outFile), Path.GetFileNameWithoutExtension(outFile));
            Console.WriteLine("Converted: " + pdfFile);
            for (int i = 0; i < pages.Length; i++)
            {
                int pageNumber = converter.StartPageNumber + i;
                string pageFile = baseName + "-page" + pageNumber + ".txt";
                File.WriteAllText(pageFile, pages[i], Encoding.UTF8);
                Console.WriteLine("  page {0}: {1} ({2} characters)", pageNumber, pageFile, pages[i].Length);
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
            Console.WriteLine("Usage: ConvertPdfToText [/pdf:file.pdf] [/out:file] [/layout:original|reading] [/start:n] [/end:n]");
            Console.WriteLine("                        [/pagebreaks] [/pages] [/html[:charset]] [/noclip] [/userpswd:password]");
            Console.WriteLine("/pages saves the text of each page in a file of its own (sample-page1.txt, ...).");
            Console.WriteLine("Without arguments the sample.pdf document is converted to sample.txt.");
            Console.WriteLine("Examples:");
            Console.WriteLine("  ConvertPdfToText /pdf:sample.pdf /layout:reading /pagebreaks");
            Console.WriteLine("  ConvertPdfToText /pdf:sample.pdf /html /start:2 /end:3");
            Console.WriteLine("  ConvertPdfToText /pdf:sample.pdf /pages /layout:reading");
            Console.WriteLine("  ConvertPdfToText /pdf:sample-protected.pdf /userpswd:open");
            return error == null ? 0 : 1;
        }
    }
}
