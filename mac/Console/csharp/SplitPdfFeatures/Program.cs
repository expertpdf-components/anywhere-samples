using System;
using System.IO;
using ExpertPdf.Anywhere.PDFSplit;

namespace SplitPdfFeatures
{
    /// <summary>
    /// Shows the features of the split library: page ranges and chunks, from and to files or memory,
    /// document information and security for the new documents, password protected sources, one document
    /// per chapter (bookmark), page lists, odd and even pages, and a maximum file size.
    /// The page numbers start at 1.
    /// </summary>
    class Program
    {
        static string sampleFolder = AppDomain.CurrentDomain.BaseDirectory;
        static string report = Path.Combine(sampleFolder, "report.pdf");   // 6 pages, "Page N of 6" on each page
        static string outFolder = Path.Combine(sampleFolder, "SplitOutput");

        static int Main(string[] args)
        {
            try
            {
                // the new documents go to their own folder, emptied at each run
                if (Directory.Exists(outFolder))
                    Directory.Delete(outFolder, true);
                Directory.CreateDirectory(outFolder);

                PDFSplitManager splitManager = new PDFSplitManager();
                // splitManager.LicenseKey = "your license key";

                // the page count, from a file or from memory
                byte[] reportBytes = File.ReadAllBytes(report);
                Console.WriteLine("report.pdf: {0} pages ({1} pages from memory)", splitManager.GetPdfPageCount(report),
                    splitManager.GetPdfPageCount(reportBytes));
                Console.WriteLine();

                ExtractRange(splitManager);
                ChunksToMemory(splitManager, reportBytes);
                MemoryToFile(splitManager, reportBytes);
                ProtectAndSplitProtected();
                SplitAtChapters(splitManager);
                PageListsAndOddEven(splitManager, reportBytes);
                SplitBySize(splitManager);

                Console.WriteLine();
                Console.WriteLine("The new documents are in " + outFolder);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        // A range of pages from a file into a new file: pages 2 to 4.
        static void ExtractRange(PDFSplitManager splitManager)
        {
            string outFile = Path.Combine(outFolder, "Pages-2-4.pdf");
            splitManager.ExtractPagesFromFileToFile(report, outFile, 2, 3);
            PrintResult(splitManager, outFile);
        }

        // Split into documents kept in memory: no temporary files, the bytes go wherever you need them
        // (a database, a web response, another ExpertPdf library).
        static void ChunksToMemory(PDFSplitManager splitManager, byte[] reportBytes)
        {
            // three documents of one page, starting with page 1
            PDFSplitDoc[] onePageDocs = splitManager.ExtractPagesFromFileToMemory(report, 1, 1, 3);
            for (int i = 0; i < onePageDocs.Length; i++)
            {
                string outFile = Path.Combine(outFolder, String.Format("Page-{0}.pdf", i + 1));
                onePageDocs[i].SaveToFile(outFile);
                PrintResult(splitManager, outFile);
            }

            // documents of 4 pages until the end of the document: pages 1-4 and 5-6
            PDFSplitDoc[] chunks = splitManager.ExtractPagesFromMemoryToMemory(reportBytes, 1, 4, true);
            foreach (PDFSplitDoc chunk in chunks)
            {
                Console.WriteLine("  chunk in memory: {0} bytes, {1} page(s)", chunk.Buffer.Length, splitManager.GetPdfPageCount(chunk.Buffer));
            }

            // a single range in memory: the last page
            PDFSplitDoc lastPage = splitManager.ExtractPagesFromMemoryToMemory(reportBytes, 6, 1);
            File.WriteAllBytes(Path.Combine(outFolder, "LastPage.pdf"), lastPage.Buffer);
            PrintResult(splitManager, Path.Combine(outFolder, "LastPage.pdf"));
        }

        // From memory to files. With a repeat count or "repeat to end", each document is saved as
        // <file name>_<index>.pdf.
        static void MemoryToFile(PDFSplitManager splitManager, byte[] reportBytes)
        {
            // two documents of 3 pages: Half_1.pdf (pages 1-3) and Half_2.pdf (pages 4-6)
            splitManager.ExtractPagesFromMemoryToFile(reportBytes, Path.Combine(outFolder, "Half.pdf"), 1, 3, 2);
            PrintResult(splitManager, Path.Combine(outFolder, "Half_1.pdf"));
            PrintResult(splitManager, Path.Combine(outFolder, "Half_2.pdf"));

            // documents of 2 pages from page 3 to the end: Pair_1.pdf (pages 3-4) and Pair_2.pdf (pages 5-6)
            splitManager.ExtractPagesFromMemoryToFile(reportBytes, Path.Combine(outFolder, "Pair.pdf"), 3, 2, true);
            PrintResult(splitManager, Path.Combine(outFolder, "Pair_1.pdf"));
            PrintResult(splitManager, Path.Combine(outFolder, "Pair_2.pdf"));
        }

        // The new documents can have their own document information and security. A protected document is
        // split by giving its password in SourcePdfPassword.
        static void ProtectAndSplitProtected()
        {
            PdfDocumentOptions options = new PdfDocumentOptions();
            options.PdfCompressionLevel = PDFCompressionLevel.Best;
            PDFSplitManager protector = new PDFSplitManager(options);

            protector.PdfDocumentInfo.Title = "Report - first part";
            protector.PdfDocumentInfo.AuthorName = "Sample Company";
            protector.PdfDocumentInfo.Subject = "Pages 1 to 3 of the annual report";
            protector.PdfDocumentInfo.Keywords = "report, split";
            protector.PdfDocumentInfo.CreatedDate = DateTime.Now;

            // a password to open the documents, and no printing for the readers who open them with it
            protector.PdfSecurityOptions.UserPassword = "secret";
            protector.PdfSecurityOptions.OwnerPassword = "owner";
            protector.PdfSecurityOptions.CanPrint = false;
            protector.PdfSecurityOptions.KeySize = EncryptionKeySize.EncryptKey256Bit;
            protector.PdfSecurityOptions.Algorithm = EncryptionAlgorithm.AES;

            string protectedFile = Path.Combine(outFolder, "Protected-1-3.pdf");
            protector.ExtractPagesFromFileToFile(report, protectedFile, 1, 3);
            Console.WriteLine("Protected-1-3.pdf: pages 1-3, AES 256 bit, open password \"secret\", printing not allowed");

            // split the protected document: without SourcePdfPassword it could not be opened
            PDFSplitManager splitter = new PDFSplitManager();
            splitter.SourcePdfPassword = "secret";
            string outFile = Path.Combine(outFolder, "FromProtected-Page-2.pdf");
            splitter.ExtractPagesFromFileToFile(protectedFile, outFile, 2, 1);
            PrintResult(splitter, outFile);
        }

        // One document per chapter: the top level bookmarks of report.pdf ("1. Summary", "2. Sales by region"...).
        // The files are named after the bookmarks; in memory, PDFSplitDoc.Title gives the bookmark title.
        static void SplitAtChapters(PDFSplitManager splitManager)
        {
            string chaptersFolder = Path.Combine(outFolder, "Chapters");
            string[] files = splitManager.SplitByBookmarksFromFileToFolder(report, chaptersFolder);
            foreach (string file in files)
                PrintResult(splitManager, file);

            PDFSplitDoc[] chapters = splitManager.SplitByBookmarksFromFileToMemory(report);
            foreach (PDFSplitDoc chapter in chapters)
                Console.WriteLine("  chapter in memory: \"{0}\", pages {1}", chapter.Title, String.Join(",", Array.ConvertAll(chapter.PageNumbers, n => n.ToString())));
        }

        // A list of pages ("1-3,7,10-": page numbers start at 1, "10-" goes to the end, the pages are taken in
        // the order of the list) and the odd or even pages.
        static void PageListsAndOddEven(PDFSplitManager splitManager, byte[] reportBytes)
        {
            string selected = Path.Combine(outFolder, "Pages-6-1-to-3.pdf");
            splitManager.ExtractPagesFromFileToFile(report, selected, "6,1-3");
            PrintResult(splitManager, selected);

            string odd = Path.Combine(outFolder, "OddPages.pdf");
            splitManager.ExtractPagesFromFileToFile(report, odd, PageSelection.OddPages);
            PrintResult(splitManager, odd);

            PDFSplitDoc even = splitManager.ExtractPagesFromMemoryToMemory(reportBytes, PageSelection.EvenPages);
            File.WriteAllBytes(Path.Combine(outFolder, "EvenPages.pdf"), even.Buffer);
            PrintResult(splitManager, Path.Combine(outFolder, "EvenPages.pdf"));
        }

        // Documents that each stay under a maximum file size (for example an e-mail attachment limit). Each
        // document takes as many pages as fit; a page larger than the limit on its own makes a document of its own.
        static void SplitBySize(PDFSplitManager splitManager)
        {
            long maxSize = 200 * 1024;    // 200 KB
            string[] parts = splitManager.SplitBySizeFromFileToFile(report, Path.Combine(outFolder, "BySize.pdf"), maxSize);
            foreach (string part in parts)
                Console.WriteLine("{0}: {1} page(s), {2} bytes (limit {3})", Path.GetFileName(part), splitManager.GetPdfPageCount(part), new FileInfo(part).Length, maxSize);
        }

        static void PrintResult(PDFSplitManager splitManager, string fileName)
        {
            Console.WriteLine("{0}: {1} page(s)", Path.GetFileName(fileName), splitManager.GetPdfPageCount(fileName));
        }
    }
}
