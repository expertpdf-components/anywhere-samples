using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace HtmlToPdfConverter
{
    /// <summary>
    /// Converts a web page, an HTML file or an HTML string to PDF (or to an image) and exposes the most used
    /// converter settings as command line options. Run it without arguments to convert the bundled sample.html.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (HasFlag(args, "--help") || HasFlag(args, "-h") || HasFlag(args, "/?"))
            {
                PrintUsage();
                return 0;
            }

            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("Argument error: " + ex.Message);
                Console.Error.WriteLine("Run with --help to see the options.");
                return 2;
            }

            // no input on the command line: convert the page that ships with the sample
            if (options.Url == null && options.HtmlFile == null && options.HtmlString == null)
                options.HtmlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.html");

            try
            {
                string output = options.ImageFormat != null ? ConvertToImage(options) : ConvertToPdf(options);
                Console.WriteLine("Created " + Path.GetFullPath(output));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Conversion failed: " + ex.Message);
                return 1;
            }
        }

        private static string ConvertToPdf(Options o)
        {
            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";

            // page setup: the size and margins of the PDF pages, in points (1/72 inch)
            converter.PdfDocumentOptions.PdfPageSize = ParseEnum(o.PageSize, PdfPageSize.A4);
            converter.PdfDocumentOptions.PdfPageOrientation = ParseEnum(o.Orientation, PDFPageOrientation.Portrait);
            converter.PdfDocumentOptions.LeftMargin = o.MarginLeft;
            converter.PdfDocumentOptions.RightMargin = o.MarginRight;
            converter.PdfDocumentOptions.TopMargin = o.MarginTop;
            converter.PdfDocumentOptions.BottomMargin = o.MarginBottom;

            // the width of the browser window the page is laid out in, in pixels; the content is then scaled to
            // the PDF page width. A height of 0 means the whole page.
            converter.PageWidth = o.BrowserWidth;
            converter.PageHeight = o.BrowserHeight;

            converter.NavigationTimeout = o.NavigationTimeout;
            // extra time for pages that keep changing after they are loaded (animations, scripts)
            converter.ConversionDelay = o.ConversionDelay;
            // the most the conversion may take once the page is loaded: a page that never finishes fails with a
            // timeout instead of keeping the application waiting
            converter.MaxConversionTime = o.MaxConversionTime;
            // convert what is loaded when the navigation timeout is reached, instead of failing
            converter.RenderOnTimeout = o.RenderOnTimeout;

            // the last page ends where the content ends (no trailing blank page). Ink measures the painted
            // content, Marker the end of the page layout.
            converter.ManagedContentTrimming = !o.NoManagedTrimming;
            converter.ManagedContentTrimmingMode = ParseEnum(o.TrimMode, ManagedContentTrimmingMode.Ink);

            converter.PdfDocumentOptions.InternalLinksEnabled = o.InternalLinks;
            converter.PdfDocumentOptions.LiveUrlsEnabled = o.ExternalLinks;
            converter.UsePrintCSSMediaType = o.PrintCss;

            // bookmarks for the elements matched by these CSS selectors, here the headings
            if (o.Bookmarks)
                converter.PdfBookmarkOptions.HtmlElementSelectors = new string[] { "h1", "h2" };

            // the table of contents needs to know where the headings are in the PDF: the converter maps them
            if (o.TableOfContents)
                converter.HtmlElementsMappingOptions.HtmlElementSelectors = new string[] { "h1", "h2", "h3" };

            // convert only a part of the page: the element with an id, or the element a CSS selector matches
            if (o.RenderElementId != null)
                converter.RenderElementId = o.RenderElementId;
            if (o.RenderElementSelector != null)
                converter.RenderElementSelector = o.RenderElementSelector;

            // keep a subset of pages, wait for the page to say it is ready
            if (o.PageRanges != null)
                converter.PageRanges = o.PageRanges;
            if (o.WindowStatus != null)
                converter.WindowStatus = o.WindowStatus;

            // accessibility and archiving: a tagged PDF keeps the logical structure of the page for screen
            // readers and declares PDF/UA-1; PDF/A is the archiving standard (the A levels also imply tagging)
            converter.Tagged = o.Tagged;
            if (o.Language != null)
                converter.Language = o.Language;
            converter.PdfStandardSubset = ParseEnum(o.PdfStandard, PdfStandardSubset.Full);

            // document information
            if (o.Title != null) converter.PdfDocumentInfo.Title = o.Title;
            if (o.Author != null) converter.PdfDocumentInfo.AuthorName = o.Author;
            if (o.Subject != null) converter.PdfDocumentInfo.Subject = o.Subject;
            if (o.Keywords != null) converter.PdfDocumentInfo.Keywords = o.Keywords;

            // security: a user password is needed to open the document, the owner password to change permissions
            if (o.UserPassword != null) converter.PdfSecurityOptions.UserPassword = o.UserPassword;
            if (o.OwnerPassword != null) converter.PdfSecurityOptions.OwnerPassword = o.OwnerPassword;
            if (o.NoPrint) converter.PdfSecurityOptions.CanPrint = false;
            if (o.NoCopy) converter.PdfSecurityOptions.CanCopyContent = false;

            // what the converter sends to the web server
            foreach (KeyValuePair<string, string> header in o.HttpHeaders)
                converter.HttpHeaders.Add(header.Key, header.Value);
            foreach (KeyValuePair<string, string> cookie in o.Cookies)
                converter.HttpCookies.Add(cookie.Key, cookie.Value);
            // with POST parameters the page is requested with the POST method, like a submitted form
            foreach (KeyValuePair<string, string> field in o.PostFields)
                converter.HttpPostParameters.Add(field.Key, field.Value);

            if (o.HeaderHtml != null)
            {
                converter.PdfDocumentOptions.ShowHeader = true;
                converter.PdfHeaderOptions.HeaderHeight = 50;
                converter.PdfHeaderOptions.DrawHeaderLine = false;
                converter.PdfHeaderOptions.HtmlToPdfArea = new HtmlToPdfArea(o.HeaderHtml, null);
            }

            if (o.FooterHtml != null || o.PageNumbers)
            {
                converter.PdfDocumentOptions.ShowFooter = true;
                converter.PdfFooterOptions.FooterHeight = 40;
                converter.PdfFooterOptions.DrawFooterLine = false;
                if (o.FooterHtml != null)
                    converter.PdfFooterOptions.HtmlToPdfArea = new HtmlToPdfArea(o.FooterHtml, null);
                if (o.PageNumbers)
                {
                    // &p; and &P; are replaced with the page number and the page count
                    TextArea pageNumbers = new TextArea(0, 15, "Page &p; of &P;", new Font("Arial", 9));
                    pageNumbers.TextAlign = HorizontalTextAlign.Right;
                    converter.PdfFooterOptions.TextArea = pageNumbers;
                }
            }

            string output = o.Output ?? "output.pdf";
            Console.WriteLine("Converting... (Ctrl+C cancels)");

            // The conversion runs asynchronously; Ctrl+C cancels the wait for it. The result is a Document, which can
            // still be changed before it is saved.
            CancellationTokenSource cancel = new CancellationTokenSource();
            ConsoleCancelEventHandler onCancel = delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                cancel.Cancel();
            };
            Console.CancelKeyPress += onCancel;
            Document document;
            try
            {
                if (o.Url != null)
                    document = converter.GetPdfDocumentObjectFromUrlAsync(o.Url, cancel.Token).GetAwaiter().GetResult();
                else if (o.HtmlFile != null)
                    document = converter.GetPdfDocumentObjectFromUrlAsync(Path.GetFullPath(o.HtmlFile), cancel.Token).GetAwaiter().GetResult();
                else
                    // the base URL resolves the relative images and style sheets of the HTML string
                    document = converter.GetPdfDocumentObjectFromHtmlStringAsync(o.HtmlString, o.BaseUrl, cancel.Token).GetAwaiter().GetResult();
            }
            finally
            {
                Console.CancelKeyPress -= onCancel;
            }

            try
            {
                Console.WriteLine("PDF pages: " + converter.ConversionSummary.PdfPageCount);
                if (o.TableOfContents)
                    AddTableOfContents(document, converter);
                document.Save(output);
            }
            finally
            {
                document.Close();
            }
            return output;
        }

        // A table of contents at the start, with links and page numbers. The PDF viewer numbers the table pages i, ii...
        // and the converted pages 1, 2, 3... (page labels). A named destination lets other documents and web pages link
        // to the table: output.pdf#toc.
        private static void AddTableOfContents(Document document, PdfConverter converter)
        {
            TableOfContents toc = new TableOfContents();
            toc.AddEntries(document, converter.HtmlElementsMappingOptions.HtmlElementsMappingResult);
            if (toc.Entries.Count == 0)
            {
                Console.WriteLine("No headings found for the table of contents.");
                return;
            }

            // the converted pages are numbered from 1; the range moves with them when the table is inserted before them
            document.AddPageLabelRange(0, PageLabelStyle.Decimal);
            int tocPages = document.InsertTableOfContents(toc, 0);
            document.AddPageLabelRange(0, PageLabelStyle.LowercaseRoman);
            document.AddNamedDestination("toc", new ExplicitDestination(document.Pages[0]));
            Console.WriteLine("Table of contents: " + toc.Entries.Count + " entries on " + tocPages + " page(s)");
        }

        private static string ConvertToImage(Options o)
        {
            ImgConverter converter = new ImgConverter();
            // converter.LicenseKey = "your license key";

            converter.PageWidth = o.BrowserWidth;
            converter.PageHeight = o.BrowserHeight;
            converter.NavigationTimeout = o.NavigationTimeout;
            converter.ConversionDelay = o.ConversionDelay;
            converter.MaxConversionTime = o.MaxConversionTime;
            converter.RenderOnTimeout = o.RenderOnTimeout;

            ImageFormat imageFormat = ParseEnum(o.ImageFormat, ImageFormat.Png);
            string output = o.Output ?? "output." + imageFormat.ToString().ToLowerInvariant();
            Console.WriteLine("Rendering...");

            if (o.Url != null)
                converter.SaveImageFromUrlToFile(o.Url, imageFormat, output);
            else if (o.HtmlFile != null)
                converter.SaveImageFromHtmlFileToFile(Path.GetFullPath(o.HtmlFile), imageFormat, output);
            else if (o.BaseUrl != null)
                converter.SaveImageFromHtmlStringToFile(o.HtmlString, imageFormat, output, o.BaseUrl);
            else
                converter.SaveImageFromHtmlStringToFile(o.HtmlString, imageFormat, output);

            return output;
        }

        private static T ParseEnum<T>(string value, T defaultValue) where T : struct
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            T result;
            if (!Enum.TryParse(value, true, out result))
                throw new ArgumentException("'" + value + "' is not one of: " + string.Join(", ", Enum.GetNames(typeof(T))));
            return result;
        }

        private static bool HasFlag(string[] args, string name)
        {
            foreach (string a in args)
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("HtmlToPdfConverter - converts a web page or HTML to PDF or to an image with ExpertPdf.");
            Console.WriteLine();
            Console.WriteLine("Usage: HtmlToPdfConverter [input] [options]");
            Console.WriteLine("       (no input: converts sample.html from the application folder)");
            Console.WriteLine();
            Console.WriteLine("Input:");
            Console.WriteLine("  --url <address>             web page to convert, e.g. https://www.html-to-pdf.net");
            Console.WriteLine("  --html-file <path>          HTML file to convert");
            Console.WriteLine("  --html <string>             HTML string to convert");
            Console.WriteLine("  --base-url <address>        base address of the relative links of --html");
            Console.WriteLine();
            Console.WriteLine("Output:");
            Console.WriteLine("  -o, --output <path>         output file (default output.pdf, or output.<format> for images)");
            Console.WriteLine("  --image <format>            render an image instead of a PDF: Png, Jpeg, Bmp, Tiff");
            Console.WriteLine();
            Console.WriteLine("Rendering:");
            Console.WriteLine("  --browser-width <px>        browser window width (default 1024)");
            Console.WriteLine("  --browser-height <px>       browser window height (default 0 = the whole page)");
            Console.WriteLine("  --timeout <s>               navigation timeout in seconds (default 60)");
            Console.WriteLine("  --delay <s>                 wait before converting the loaded page (default 0)");
            Console.WriteLine("  --max-conversion-time <s>   most the conversion may take after the page loads (default 60)");
            Console.WriteLine("  --render-on-timeout         convert what is loaded when the navigation timeout is reached");
            Console.WriteLine("  --no-managed-trimming       keep every page whole, up to the end of the paper");
            Console.WriteLine("  --trim-mode <mode>          Ink | Marker: how the end of the content is found (default Ink)");
            Console.WriteLine("  --print-css                 use the print CSS media type instead of screen");
            Console.WriteLine("  --element-id <id>           convert only the element with this id");
            Console.WriteLine("  --element-selector <css>    convert only the element matched by this CSS selector");
            Console.WriteLine("  --window-status <text>      wait until the page sets window.status to this text");
            Console.WriteLine();
            Console.WriteLine("PDF:");
            Console.WriteLine("  --page-size <size>          A4, Letter, Legal, A3, A5, ... (default A4)");
            Console.WriteLine("  --orientation <o>           Portrait | Landscape");
            Console.WriteLine("  --margins <pt>              all four page margins in points");
            Console.WriteLine("  --page-ranges <ranges>      keep only these pages, e.g. 1-2,4");
            Console.WriteLine("  --bookmarks                 create bookmarks from the H1 and H2 headings");
            Console.WriteLine("  --toc                       table of contents of the H1-H3 headings at the start, with");
            Console.WriteLine("                              roman page labels for its pages");
            Console.WriteLine("  --no-internal-links         do not convert links to anchors in the page");
            Console.WriteLine("  --no-external-links         do not convert links to other pages");
            Console.WriteLine("  --header-html <html>        HTML header on every page");
            Console.WriteLine("  --footer-html <html>        HTML footer on every page");
            Console.WriteLine("  --page-numbers              'Page X of Y' in the footer");
            Console.WriteLine("  --tagged                    tagged (accessible) PDF, PDF/UA-1");
            Console.WriteLine("  --language <lang>           language of a tagged PDF, e.g. en-US");
            Console.WriteLine("  --pdf-standard <subset>     Full, Pdf_A_1b, Pdf_A_2b, Pdf_A_3b, Pdf_A_3u, Pdf_A_3a, Pdf_A_4, Pdf_X_1a");
            Console.WriteLine("  --title, --author, --subject, --keywords <text>   document information");
            Console.WriteLine("  --user-password <p>         password needed to open the PDF");
            Console.WriteLine("  --owner-password <p>        password needed to change the permissions");
            Console.WriteLine("  --no-print, --no-copy       remove the print or the copy permission");
            Console.WriteLine();
            Console.WriteLine("HTTP:");
            Console.WriteLine("  --header <name=value>       HTTP header sent with the request (repeatable)");
            Console.WriteLine("  --cookie <name=value>       HTTP cookie (repeatable)");
            Console.WriteLine("  --post <name=value>         POST the page with this form field (repeatable)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  HtmlToPdfConverter --url https://www.html-to-pdf.net -o site.pdf --page-numbers");
            Console.WriteLine("  HtmlToPdfConverter --html-file sample.html --tagged --language en-US -o accessible.pdf");
            Console.WriteLine("  HtmlToPdfConverter --url https://www.html-to-pdf.net --image Png -o site.png");
            Console.WriteLine("  HtmlToPdfConverter --html-file sample.html --toc --page-numbers -o book.pdf");
        }

        private sealed class Options
        {
            public string Url, HtmlFile, HtmlString, BaseUrl, Output, ImageFormat;
            public int BrowserWidth = 1024, BrowserHeight = 0, NavigationTimeout = 60, ConversionDelay = 0, MaxConversionTime = 60;
            public bool PrintCss, Bookmarks, InternalLinks = true, ExternalLinks = true, PageNumbers, Tagged, NoPrint, NoCopy;
            public bool RenderOnTimeout, NoManagedTrimming, TableOfContents;
            public string TrimMode;
            public string RenderElementId, RenderElementSelector, WindowStatus, PageRanges;
            public string PageSize, Orientation, PdfStandard, Language;
            public int MarginLeft, MarginRight, MarginTop, MarginBottom;
            public string HeaderHtml, FooterHtml, Title, Author, Subject, Keywords, UserPassword, OwnerPassword;
            public List<KeyValuePair<string, string>> HttpHeaders = new List<KeyValuePair<string, string>>();
            public List<KeyValuePair<string, string>> Cookies = new List<KeyValuePair<string, string>>();
            public List<KeyValuePair<string, string>> PostFields = new List<KeyValuePair<string, string>>();

            public static Options Parse(string[] args)
            {
                Options o = new Options();
                for (int i = 0; i < args.Length; i++)
                {
                    string name = args[i].ToLowerInvariant();
                    switch (name)
                    {
                        case "--url": o.Url = Value(args, ref i); break;
                        case "--html-file": o.HtmlFile = Value(args, ref i); break;
                        case "--html": o.HtmlString = Value(args, ref i); break;
                        case "--base-url": o.BaseUrl = Value(args, ref i); break;
                        case "-o":
                        case "--output": o.Output = Value(args, ref i); break;
                        case "--image": o.ImageFormat = Value(args, ref i); break;
                        case "--browser-width": o.BrowserWidth = Number(args, ref i); break;
                        case "--browser-height": o.BrowserHeight = Number(args, ref i); break;
                        case "--timeout": o.NavigationTimeout = Number(args, ref i); break;
                        case "--delay": o.ConversionDelay = Number(args, ref i); break;
                        case "--max-conversion-time": o.MaxConversionTime = Number(args, ref i); break;
                        case "--render-on-timeout": o.RenderOnTimeout = true; break;
                        case "--no-managed-trimming": o.NoManagedTrimming = true; break;
                        case "--trim-mode": o.TrimMode = Value(args, ref i); break;
                        case "--toc": o.TableOfContents = true; break;
                        case "--print-css": o.PrintCss = true; break;
                        case "--element-id": o.RenderElementId = Value(args, ref i); break;
                        case "--element-selector": o.RenderElementSelector = Value(args, ref i); break;
                        case "--window-status": o.WindowStatus = Value(args, ref i); break;
                        case "--page-size": o.PageSize = Value(args, ref i); break;
                        case "--orientation": o.Orientation = Value(args, ref i); break;
                        case "--margins": o.MarginLeft = o.MarginRight = o.MarginTop = o.MarginBottom = Number(args, ref i); break;
                        case "--page-ranges": o.PageRanges = Value(args, ref i); break;
                        case "--bookmarks": o.Bookmarks = true; break;
                        case "--no-internal-links": o.InternalLinks = false; break;
                        case "--no-external-links": o.ExternalLinks = false; break;
                        case "--header-html": o.HeaderHtml = Value(args, ref i); break;
                        case "--footer-html": o.FooterHtml = Value(args, ref i); break;
                        case "--page-numbers": o.PageNumbers = true; break;
                        case "--tagged": o.Tagged = true; break;
                        case "--language": o.Language = Value(args, ref i); break;
                        case "--pdf-standard": o.PdfStandard = Value(args, ref i); break;
                        case "--title": o.Title = Value(args, ref i); break;
                        case "--author": o.Author = Value(args, ref i); break;
                        case "--subject": o.Subject = Value(args, ref i); break;
                        case "--keywords": o.Keywords = Value(args, ref i); break;
                        case "--user-password": o.UserPassword = Value(args, ref i); break;
                        case "--owner-password": o.OwnerPassword = Value(args, ref i); break;
                        case "--no-print": o.NoPrint = true; break;
                        case "--no-copy": o.NoCopy = true; break;
                        case "--header": o.HttpHeaders.Add(Pair(args, ref i)); break;
                        case "--cookie": o.Cookies.Add(Pair(args, ref i)); break;
                        case "--post": o.PostFields.Add(Pair(args, ref i)); break;
                        default: throw new ArgumentException("unknown option " + args[i]);
                    }
                }

                int inputs = (o.Url != null ? 1 : 0) + (o.HtmlFile != null ? 1 : 0) + (o.HtmlString != null ? 1 : 0);
                if (inputs > 1)
                    throw new ArgumentException("use only one of --url, --html-file and --html");
                return o;
            }

            private static string Value(string[] args, ref int i)
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException(args[i] + " needs a value");
                return args[++i];
            }

            private static int Number(string[] args, ref int i)
            {
                string option = args[i];
                string text = Value(args, ref i);
                int n;
                if (!int.TryParse(text, out n) || n < 0)
                    throw new ArgumentException(option + " needs a positive number, not '" + text + "'");
                return n;
            }

            private static KeyValuePair<string, string> Pair(string[] args, ref int i)
            {
                string option = args[i];
                string text = Value(args, ref i);
                int eq = text.IndexOf('=');
                if (eq <= 0)
                    throw new ArgumentException(option + " needs name=value, not '" + text + "'");
                return new KeyValuePair<string, string>(text.Substring(0, eq), text.Substring(eq + 1));
            }
        }
    }
}
