using System;
using ExpertPdf.Anywhere.Drawing;
using System.IO;
using ExpertPdf.Anywhere.MergePdf;

namespace MergePdfFeatures
{
    /// <summary>
    /// Shows the features of the merge library, one merged document per feature:
    /// page ranges, documents and images from memory, document information and security,
    /// password protected sources, PDF/A output, inserting and reordering pages, a bookmark for each
    /// merged document and booklets.
    /// </summary>
    class Program
    {
        static string sampleFolder = AppDomain.CurrentDomain.BaseDirectory;
        static string report = Path.Combine(sampleFolder, "report.pdf");     // 6 pages, "Page N of 6" on each page
        static string invoice = Path.Combine(sampleFolder, "invoice.pdf");   // 1 page
        static string logo = Path.Combine(sampleFolder, "logo.jpg");

        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("report.pdf has {0} pages, invoice.pdf has {1} page", PDFMerge.GetPageCount(report), PDFMerge.GetPageCount(invoice));
                Console.WriteLine();

                MergePageRanges();
                MergeFromMemory();
                MergeWithSecurity();
                MergeProtectedSources();
                MergeToPdfA();
                InsertAndReorder();
                BookmarkPerDocument();
                MergeToBooklet();

                Console.WriteLine();
                Console.WriteLine("The merged documents are in " + sampleFolder);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        // Take only some pages of each source document. The page indexes are zero based.
        static void MergePageRanges()
        {
            PDFMerge pdfMerge = new PDFMerge();
            // pdfMerge.LicenseKey = "your license key";

            // the first two pages of the report
            pdfMerge.AppendPDFFile(report, 0, 2);
            // the whole invoice
            pdfMerge.AppendPDFFile(invoice);
            // the report from the fifth page to the end
            pdfMerge.AppendPDFFile(report, 4);
            // a length of -1 also means "to the end": here, the last page only
            pdfMerge.AppendPDFFile(report, 5, -1);

            string outFile = Path.Combine(sampleFolder, "1-PageRanges.pdf");
            pdfMerge.SaveMergedPDFToFile(outFile);
            PrintResult(outFile, "report pages 1-2, the invoice, report pages 5-6, report page 6");
        }

        // Merge documents you already have in memory or in a stream (for example created by another
        // ExpertPdf library or read from a database), plus images, without temporary files.
        static void MergeFromMemory()
        {
            // the page size and orientation are used for the pages the library creates: images and empty pages
            PdfDocumentOptions options = new PdfDocumentOptions();
            options.PdfPageSize = PdfPageSize.Letter;
            options.PdfPageOrientation = PDFPageOrientation.Landscape;
            options.PdfCompressionLevel = PDFCompressionLevel.Best;

            PDFMerge pdfMerge = new PDFMerge(options);

            // a document as a byte array
            byte[] invoiceBytes = File.ReadAllBytes(invoice);
            pdfMerge.AppendPDFBytes(invoiceBytes);

            // one page (the third) of a document read from a stream; the stream must stay open
            // until the merged document is saved
            using (FileStream reportStream = File.OpenRead(report))
            {
                pdfMerge.AppendPDFStream(reportStream, 2, 1);
                Console.WriteLine("  pages so far: " + pdfMerge.GetCurrentPageCount());

                // an image file and an image created in code, each on a new page
                pdfMerge.AppendImageFile(logo);
                pdfMerge.AppendImageObject(DrawChart());

                // render to memory: store the bytes, send them in a web response, etc.
                byte[] mergedBytes = pdfMerge.RenderMergedPDFDocument();

                string outFile = Path.Combine(sampleFolder, "2-FromMemory.pdf");
                File.WriteAllBytes(outFile, mergedBytes);
                PrintResult(outFile, "invoice (bytes), report page 3 (stream), logo image, chart image");
            }
        }

        // Set the document information and protect the merged document with passwords and permissions.
        static void MergeWithSecurity()
        {
            PDFMerge pdfMerge = new PDFMerge();

            pdfMerge.DocumentInfo.Title = "Report with invoice";
            pdfMerge.DocumentInfo.Subject = "Merged with ExpertPdf MergePdf";
            pdfMerge.DocumentInfo.AuthorName = "Sample Company";
            pdfMerge.DocumentInfo.Keywords = "report, invoice, merge";
            pdfMerge.DocumentInfo.CreatedDate = DateTime.Now;

            // the user password opens the document; the owner password also allows changing the permissions
            pdfMerge.SecurityOptions.UserPassword = "user";
            pdfMerge.SecurityOptions.OwnerPassword = "owner";
            pdfMerge.SecurityOptions.KeySize = EncryptionKeySize.EncryptKey256Bit;
            pdfMerge.SecurityOptions.Algorithm = EncryptionAlgorithm.AES;
            // readers opening it with the user password may print, but not copy or change the content
            pdfMerge.SecurityOptions.CanPrint = true;
            pdfMerge.SecurityOptions.CanCopyContent = false;
            pdfMerge.SecurityOptions.CanEditContent = false;
            pdfMerge.SecurityOptions.CanEditAnnotations = false;
            pdfMerge.SecurityOptions.CanAssembleDocument = false;

            pdfMerge.AppendPDFFile(report);
            pdfMerge.AppendPDFFile(invoice);

            // save to any writable stream
            string outFile = Path.Combine(sampleFolder, "3-Secured.pdf");
            using (FileStream output = File.Create(outFile))
            {
                pdfMerge.SaveMergedPDFToStream(output);
            }
            Console.WriteLine("3-Secured.pdf: report and invoice, AES 256 bit, user password \"user\", owner password \"owner\"");
        }

        // Append documents that are protected with a password: give the user or the owner password.
        // The merged document is not protected unless you set its SecurityOptions.
        static void MergeProtectedSources()
        {
            PDFMerge pdfMerge = new PDFMerge();

            // the whole protected document (-1 = all the pages from the start index)
            pdfMerge.AppendPDFFile(Path.Combine(sampleFolder, "3-Secured.pdf"), "user", 0, -1);

            // the last page of the same protected document, from memory
            byte[] securedBytes = File.ReadAllBytes(Path.Combine(sampleFolder, "3-Secured.pdf"));
            pdfMerge.AppendPDFBytes(securedBytes, "owner", 6, 1);

            string outFile = Path.Combine(sampleFolder, "4-FromProtectedSources.pdf");
            pdfMerge.SaveMergedPDFToFile(outFile);
            PrintResult(outFile, "3-Secured.pdf opened with its passwords, saved without protection");
        }

        // Produce a PDF/A document for long term archiving. The pages are imported as they are, so a PDF/A merge
        // takes only documents that conform to the level: here each source is converted to PDF/A-2b first.
        static void MergeToPdfA()
        {
            PdfDocumentOptions options = new PdfDocumentOptions();
            options.PdfStandardSubset = PdfStandardSubset.Pdf_A_2b;

            PDFMerge pdfMerge = new PDFMerge(options);
            pdfMerge.DocumentInfo.Title = "Archived report";
            pdfMerge.DocumentInfo.AuthorName = "Sample Company";

            pdfMerge.AppendPDFBytes(ToPdfA2b(report));
            pdfMerge.AppendPDFBytes(ToPdfA2b(invoice));

            string outFile = Path.Combine(sampleFolder, "5-PdfA-2b.pdf");
            pdfMerge.SaveMergedPDFToFile(outFile);
            PrintResult(outFile, "report and invoice as PDF/A-2b");
        }

        // a document converted to PDF/A-2b: the fonts embedded, the color profile attached, the metadata written
        static byte[] ToPdfA2b(string pdfFile)
        {
            ExpertPdf.Anywhere.PdfDocument.Document document = new ExpertPdf.Anywhere.PdfDocument.Document(pdfFile);
            try
            {
                document.ConvertToPdfA(ExpertPdf.Anywhere.HtmlToPdf.PdfStandardSubset.Pdf_A_2b);
                return document.Save();
            }
            finally
            {
                document.Close();
            }
        }

        // Put a document between the pages of another and change the order of the merged pages. The positions
        // are zero based: 0 inserts before the first page, GetCurrentPageCount() at the end.
        static void InsertAndReorder()
        {
            PDFMerge pdfMerge = new PDFMerge();

            pdfMerge.AppendPDFFile(report);                 // report pages 1-6
            pdfMerge.InsertPDFFile(1, invoice);             // the invoice after the first report page
            pdfMerge.InsertEmptyPage(0);                    // an empty page in front
            pdfMerge.InsertPDFFile(pdfMerge.GetCurrentPageCount(), report, 0, 1);   // report page 1 again, at the end

            // move the last page (the second copy of report page 1) to the front: newOrder[i] is the current
            // index of the page that goes to position i
            int count = pdfMerge.GetCurrentPageCount();
            int[] newOrder = new int[count];
            newOrder[0] = count - 1;
            for (int i = 1; i < count; i++)
                newOrder[i] = i - 1;
            pdfMerge.ReorderPages(newOrder);

            string outFile = Path.Combine(sampleFolder, "6-InsertAndReorder.pdf");
            pdfMerge.SaveMergedPDFToFile(outFile);
            PrintResult(outFile, "report page 1, empty page, report page 1, invoice, report pages 2-6");
        }

        // One bookmark for each merged document, with the bookmarks of the document nested under it.
        static void BookmarkPerDocument()
        {
            PDFMerge pdfMerge = new PDFMerge();
            pdfMerge.BookmarkOptions.AddDocumentBookmarks = true;
            // the titles come from the document information (report.pdf and invoice.pdf have a title);
            // BookmarkTitleSource.FileName uses the file names instead
            pdfMerge.BookmarkOptions.TitleSource = BookmarkTitleSource.DocumentTitle;

            pdfMerge.AppendPDFFile(report);
            pdfMerge.AppendPDFFile(invoice);
            // a title of your own for the document added last
            pdfMerge.AppendImageFile(logo);
            pdfMerge.SetBookmarkTitle("Company logo");

            string outFile = Path.Combine(sampleFolder, "7-Bookmarks.pdf");
            pdfMerge.SaveMergedPDFToFile(outFile);
            PrintResult(outFile, "a bookmark for the report (with its chapters), the invoice and the logo");
        }

        // A booklet: two pages on each landscape sheet, in the order that gives a booklet when the printed
        // sheets are folded in the middle. Two sided: print the sheets on both sides, flipping on the short edge.
        static void MergeToBooklet()
        {
            PDFMerge pdfMerge = new PDFMerge();
            pdfMerge.BookletOptions.Enabled = true;
            pdfMerge.BookletOptions.PageSize = PdfPageSize.A4;    // A4 sheets, so A5 pages
            pdfMerge.BookletOptions.TwoSided = true;

            pdfMerge.AppendPDFFile(report);
            pdfMerge.AppendPDFFile(invoice);    // 7 pages: the booklet gets an empty page at the end

            string outFile = Path.Combine(sampleFolder, "8-Booklet.pdf");
            pdfMerge.SaveMergedPDFToFile(outFile);
            PrintResult(outFile, "4 sheets: 8|1, 2|7, 6|3, 4|5 (page 8 empty)");
        }

        // a simple bar chart drawn in HTML and rendered to a PNG image by the HTML to image converter, to show
        // how to merge an image created in code (the bytes of any PNG, JPEG, BMP, GIF or TIFF image will do)
        static byte[] DrawChart()
        {
            int[] values = { 180, 260, 140, 320 };
            string[] colors = { "steelblue", "seagreen", "orange", "indianred" };
            string bars = string.Empty;
            for (int i = 0; i < values.Length; i++)
                bars += string.Format("<div style='position:absolute; left:{0}px; bottom:60px; width:120px; " +
                    "height:{1}px; background:{2}'></div><div style='position:absolute; left:{3}px; bottom:20px'>" +
                    "Q{4}</div>", 80 + i * 170, values[i], colors[i], 120 + i * 170, i + 1);
            string html = "<html><body style='margin:0; width:800px; height:500px; position:relative; " +
                "font:26px Arial; background:white'><div style='position:absolute; left:20px; top:20px'>" +
                "Sales by quarter</div>" + bars + "</body></html>";

            ExpertPdf.Anywhere.HtmlToPdf.ImgConverter converter = new ExpertPdf.Anywhere.HtmlToPdf.ImgConverter(800, 500);
            // converter.LicenseKey = "your license key";
            return converter.GetImageBytesFromHtmlString(html, ImageFormat.Png);
        }

        static void PrintResult(string fileName, string content)
        {
            Console.WriteLine("{0}: {1} pages - {2}", Path.GetFileName(fileName), PDFMerge.GetPageCount(fileName), content);
        }
    }
}
