using System;
using System.IO;
using ExpertPdf.Anywhere.PDFSplit;

namespace SplitPdfCommandLine
{
    /// <summary>
    /// A command line tool that splits a PDF document into documents of a given number of pages.
    /// Run it without arguments to split the document that comes with the sample.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            string sampleFolder = AppDomain.CurrentDomain.BaseDirectory;

            string srcPdfFile = null;
            string outFile = null;
            string password = null;
            int startPageNumber = 1;
            int splitPageCount = 1;
            int repeatCount = 0;      // 0 = repeat to the end of the document
            // the other ways to split: at the chapters, a page list, the odd or even pages, a maximum file size
            bool atBookmarks = false;
            string pageList = null;
            string selection = null;  // "odd" or "even"
            int maxSizeKB = 0;

            if (args.Length == 0)
            {
                // no arguments: show the syntax, then split the bundled document in documents of 2 pages
                ShowUsage();
                Console.WriteLine();
                Console.WriteLine("No arguments given: splitting report.pdf in documents of 2 pages.");
                srcPdfFile = Path.Combine(sampleFolder, "report.pdf");
                outFile = Path.Combine(sampleFolder, "SplitDocument.pdf");
                splitPageCount = 2;
            }

            try
            {
                foreach (string argument in args)
                {
                    string value;
                    if (TryGetValue(argument, "/pdf:", out value))
                        srcPdfFile = Path.GetFullPath(value);
                    else if (TryGetValue(argument, "/out:", out value))
                        outFile = Path.GetFullPath(value);
                    else if (TryGetValue(argument, "/startpage:", out value))
                        startPageNumber = ParsePositive(value, "start page number");
                    else if (TryGetValue(argument, "/splitsize:", out value))
                        splitPageCount = ParsePositive(value, "split size");
                    else if (TryGetValue(argument, "/repeatcount:", out value))
                        repeatCount = ParsePositive(value, "repeat count");
                    else if (TryGetValue(argument, "/password:", out value))
                        password = value;
                    else if (argument.Equals("/bookmarks", StringComparison.OrdinalIgnoreCase))
                        atBookmarks = true;
                    else if (TryGetValue(argument, "/pages:", out value))
                        pageList = value;
                    else if (argument.Equals("/odd", StringComparison.OrdinalIgnoreCase) || argument.Equals("/even", StringComparison.OrdinalIgnoreCase))
                        selection = argument.Substring(1).ToLowerInvariant();
                    else if (TryGetValue(argument, "/maxsize:", out value))
                        maxSizeKB = ParsePositive(value, "maximum size");
                    else
                        throw new ArgumentException("Unknown argument: " + argument);
                }
                if (srcPdfFile == null)
                    throw new ArgumentException("The source PDF document is not specified.");
                if (outFile == null)
                    outFile = Path.GetFullPath("SplitDocument.pdf");
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
                PDFSplitManager splitManager = new PDFSplitManager();
                // splitManager.LicenseKey = "your license key";

                // the user or the owner password of a protected source document
                splitManager.SourcePdfPassword = password;

                // the page numbers start at 1
                int pageCount = splitManager.GetPdfPageCount(File.ReadAllBytes(srcPdfFile));
                Console.WriteLine("{0}: {1} pages", Path.GetFileName(srcPdfFile), pageCount);

                if (atBookmarks)
                {
                    // one document per top level bookmark, named after it, in a folder named after the out file
                    string folder = Path.Combine(Path.GetDirectoryName(outFile), Path.GetFileNameWithoutExtension(outFile));
                    foreach (string chapter in splitManager.SplitByBookmarksFromFileToFolder(srcPdfFile, folder))
                        Console.WriteLine("  {0} ({1} page(s))", chapter, splitManager.GetPdfPageCount(chapter));
                    return 0;
                }
                if (pageList != null || selection != null)
                {
                    // one document with the listed pages ("1-3,7,10-"), or with the odd or even pages
                    if (pageList != null)
                        splitManager.ExtractPagesFromFileToFile(srcPdfFile, outFile, pageList);
                    else
                        splitManager.ExtractPagesFromFileToFile(srcPdfFile, outFile, selection == "odd" ? PageSelection.OddPages : PageSelection.EvenPages);
                    Console.WriteLine("  {0} ({1} page(s))", outFile, splitManager.GetPdfPageCount(outFile));
                    return 0;
                }
                if (maxSizeKB > 0)
                {
                    // documents of at most maxsize KB each: <out name>_1.pdf, <out name>_2.pdf...
                    foreach (string part in splitManager.SplitBySizeFromFileToFile(srcPdfFile, outFile, maxSizeKB * 1024L))
                        Console.WriteLine("  {0} ({1} page(s), {2} KB)", part, splitManager.GetPdfPageCount(part), new FileInfo(part).Length / 1024);
                    return 0;
                }

                // each new document is saved as <out name>_<index>.pdf: SplitDocument_1.pdf, SplitDocument_2.pdf...
                if (repeatCount == 0)
                    splitManager.ExtractPagesFromFileToFile(srcPdfFile, outFile, startPageNumber, splitPageCount, true);
                else
                    splitManager.ExtractPagesFromFileToFile(srcPdfFile, outFile, startPageNumber, splitPageCount, repeatCount);

                // list the documents that were created
                int parts = (pageCount - startPageNumber + splitPageCount) / splitPageCount;
                if (repeatCount > 0 && repeatCount < parts)
                    parts = repeatCount;
                string basePath = Path.Combine(Path.GetDirectoryName(outFile), Path.GetFileNameWithoutExtension(outFile));
                for (int index = 1; index <= parts; index++)
                {
                    string part = String.Format("{0}_{1}{2}", basePath, index, Path.GetExtension(outFile));
                    Console.WriteLine("  {0} ({1} page(s))", part, splitManager.GetPdfPageCount(part));
                }
                return 0;
            }
            catch (Exception ex)
            {
                // SplitPDFException for documents that cannot be read (or a wrong password), IO exceptions for missing files
                Console.WriteLine("Could not split the document: " + ex.Message);
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

        static int ParsePositive(string value, string what)
        {
            int number;
            if (!int.TryParse(value, out number) || number < 1)
                throw new ArgumentException(String.Format("Invalid {0}: {1}", what, value));
            return number;
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: SplitPdfCommandLine /pdf:source_file [/startpage:number] [/splitsize:pages]");
            Console.WriteLine("           [/repeatcount:count] [/password:source_password] [/out:file]");
            Console.WriteLine("       SplitPdfCommandLine /pdf:source_file {/bookmarks | /pages:list | /odd | /even | /maxsize:KB}");
            Console.WriteLine("           [/password:source_password] [/out:file]");
            Console.WriteLine();
            Console.WriteLine("/bookmarks: one document per chapter (top level bookmark), saved in the folder <out>\\");
            Console.WriteLine("/pages:1-3,7,10- or /odd or /even: one document with these pages, saved as <out>");
            Console.WriteLine("/maxsize:KB: documents of at most KB kilobytes each, saved as <out>_1.pdf, <out>_2.pdf, ...");
            Console.WriteLine("By default the split starts at the first page, makes documents of 1 page and repeats");
            Console.WriteLine("to the end of the document. The documents are saved as <out>_1.pdf, <out>_2.pdf, ...");
            Console.WriteLine("Example: SplitPdfCommandLine /pdf:\"C:\\My Documents\\Report.pdf\" /startpage:3 /splitsize:2 /repeatcount:2 /out:Part.pdf");
        }
    }
}
