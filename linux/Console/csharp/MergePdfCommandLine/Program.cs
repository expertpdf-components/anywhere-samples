using System;
using System.Collections.Generic;
using System.IO;
using ExpertPdf.Anywhere.MergePdf;

namespace MergePdfCommandLine
{
    /// <summary>
    /// A command line tool that merges PDF documents, images and empty pages into one PDF document.
    /// Run it without arguments to merge the documents that come with the sample.
    /// </summary>
    class Program
    {
        // one element of the merged document, in the order given on the command line
        class MergeItem
        {
            public string Kind;   // "pdf", "image" or "emptypage"
            public string Path;
        }

        static int Main(string[] args)
        {
            string sampleFolder = AppDomain.CurrentDomain.BaseDirectory;

            List<MergeItem> items = new List<MergeItem>();
            string outFile = null;
            PdfDocumentOptions options = new PdfDocumentOptions();
            string title = null;
            string userPassword = null;
            string ownerPassword = null;
            bool documentBookmarks = false;
            bool booklet = false;
            bool twoSided = false;

            if (args.Length == 0)
            {
                // no arguments: show the syntax, then merge the bundled documents
                ShowUsage();
                Console.WriteLine();
                Console.WriteLine("No arguments given: merging the sample documents.");
                items.Add(new MergeItem { Kind = "pdf", Path = Path.Combine(sampleFolder, "report.pdf") });
                items.Add(new MergeItem { Kind = "image", Path = Path.Combine(sampleFolder, "logo.jpg") });
                items.Add(new MergeItem { Kind = "emptypage" });
                items.Add(new MergeItem { Kind = "pdf", Path = Path.Combine(sampleFolder, "invoice.pdf") });
                title = "Report and invoice";
                documentBookmarks = true;
                outFile = Path.Combine(sampleFolder, "MergedDocument.pdf");
            }

            try
            {
                foreach (string argument in args)
                {
                    string value;
                    if (TryGetValue(argument, "/pdf:", out value))
                        items.Add(new MergeItem { Kind = "pdf", Path = Path.GetFullPath(value) });
                    else if (TryGetValue(argument, "/image:", out value))
                        items.Add(new MergeItem { Kind = "image", Path = Path.GetFullPath(value) });
                    else if (argument.Equals("/emptypage", StringComparison.OrdinalIgnoreCase))
                        items.Add(new MergeItem { Kind = "emptypage" });
                    else if (TryGetValue(argument, "/out:", out value))
                        outFile = Path.GetFullPath(value);
                    // the page size and orientation apply to the pages the library creates (images and empty
                    // pages); the pages of the appended PDF documents keep their own size
                    else if (TryGetValue(argument, "/pagesize:", out value))
                        options.PdfPageSize = ParseEnum<PdfPageSize>(value, "page size");
                    else if (TryGetValue(argument, "/orientation:", out value))
                        options.PdfPageOrientation = ParseEnum<PDFPageOrientation>(value, "page orientation");
                    else if (TryGetValue(argument, "/compression:", out value))
                        options.PdfCompressionLevel = ParseEnum<PDFCompressionLevel>(value, "compression level");
                    else if (TryGetValue(argument, "/standard:", out value))
                        options.PdfStandardSubset = ParseEnum<PdfStandardSubset>(value, "PDF standard");
                    else if (TryGetValue(argument, "/title:", out value))
                        title = value;
                    else if (TryGetValue(argument, "/userpswd:", out value))
                        userPassword = value;
                    else if (TryGetValue(argument, "/ownerpswd:", out value))
                        ownerPassword = value;
                    else if (argument.Equals("/bookmarks", StringComparison.OrdinalIgnoreCase))
                        documentBookmarks = true;
                    else if (argument.Equals("/booklet", StringComparison.OrdinalIgnoreCase))
                        booklet = true;
                    else if (argument.Equals("/booklet:twosided", StringComparison.OrdinalIgnoreCase))
                        booklet = twoSided = true;
                    else
                        throw new ArgumentException("Unknown argument: " + argument);
                }

                if (items.Count == 0)
                    throw new ArgumentException("Nothing to merge: give at least one /pdf:, /image: or /emptypage argument.");
                if (outFile == null)
                    outFile = Path.GetFullPath("MergedDocument.pdf");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                ShowUsage();
                return 1;
            }

            try
            {
                // the options are read when the merge object is created: set them first
                PDFMerge pdfMerge = new PDFMerge(options);
                // pdfMerge.LicenseKey = "your license key";

                if (title != null)
                    pdfMerge.DocumentInfo.Title = title;
                pdfMerge.DocumentInfo.Application = "ExpertPdf MergePdf command line sample";

                if (!String.IsNullOrEmpty(userPassword))
                    pdfMerge.SecurityOptions.UserPassword = userPassword;
                if (!String.IsNullOrEmpty(ownerPassword))
                    pdfMerge.SecurityOptions.OwnerPassword = ownerPassword;

                // one bookmark for each merged document or image file (named after the file), with the
                // bookmarks of the document nested under it
                if (documentBookmarks)
                    pdfMerge.BookmarkOptions.AddDocumentBookmarks = true;
                // a booklet: two pages on each landscape sheet of the page size, folded in the middle
                if (booklet)
                {
                    pdfMerge.BookletOptions.Enabled = true;
                    pdfMerge.BookletOptions.PageSize = options.PdfPageSize;
                    pdfMerge.BookletOptions.TwoSided = twoSided;
                }

                foreach (MergeItem item in items)
                {
                    switch (item.Kind)
                    {
                        case "pdf":
                            pdfMerge.AppendPDFFile(item.Path);
                            Console.WriteLine("  PDF    {0} ({1} pages)", Path.GetFileName(item.Path), PDFMerge.GetPageCount(item.Path));
                            break;
                        case "image":
                            // each image is placed on a new page
                            pdfMerge.AppendImageFile(item.Path);
                            Console.WriteLine("  image  {0}", Path.GetFileName(item.Path));
                            break;
                        default:
                            pdfMerge.AppendEmptyPage();
                            Console.WriteLine("  empty page");
                            break;
                    }
                }

                // count the pages before saving: a password protected result cannot be opened without its password
                int pageCount = pdfMerge.GetCurrentPageCount();
                pdfMerge.SaveMergedPDFToFile(outFile);

                Console.WriteLine();
                Console.WriteLine("Merged document: {0} ({1} pages, {2})", outFile, pageCount, options.PdfStandardSubset);
                if (booklet)
                    Console.WriteLine("Booklet: {0} sheets", pdfMerge.GetCurrentPageCount());
                return 0;
            }
            catch (Exception ex)
            {
                // MergePDFException for documents that cannot be read, IO exceptions for missing files
                Console.WriteLine("Could not merge the documents: " + ex.Message);
                return 2;
            }
        }

        static bool TryGetValue(string argument, string name, out string value)
        {
            value = null;
            if (!argument.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                return false;
            value = argument.Substring(name.Length).Trim().Trim('"');
            if (value.Length == 0)
                throw new ArgumentException("Missing value for " + name);
            return true;
        }

        static T ParseEnum<T>(string value, string what)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(String.Format("Invalid {0}: {1}. Valid values: {2}", what, value,
                    String.Join(", ", Enum.GetNames(typeof(T)))));
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: MergePdfCommandLine {/pdf:file | /image:file | /emptypage}+ [/out:file]");
            Console.WriteLine("           [/pagesize:A4|Letter|...] [/orientation:Portrait|Landscape]");
            Console.WriteLine("           [/compression:NoCompression|BestSpeed|BelowNormal|Normal|AboveNormal|Best]");
            Console.WriteLine("           [/standard:Full|Pdf_A_1b|Pdf_A_2b|Pdf_A_3b|Pdf_A_3u|Pdf_X_1a]");
            Console.WriteLine("           [/title:text] [/userpswd:password] [/ownerpswd:password]");
            Console.WriteLine("           [/bookmarks] [/booklet | /booklet:twosided]");
            Console.WriteLine();
            Console.WriteLine("The documents, images and empty pages are merged in the order you give them.");
            Console.WriteLine("Page size and orientation apply to the image and empty pages.");
            Console.WriteLine("/bookmarks adds a bookmark for each document, named after the file.");
            Console.WriteLine("/booklet puts two pages on each landscape sheet of the page size, in booklet order.");
            Console.WriteLine("Example: MergePdfCommandLine /pdf:\"C:\\My Documents\\Report.pdf\" /emptypage /image:chart.png /out:Merged.pdf");
        }
    }
}
