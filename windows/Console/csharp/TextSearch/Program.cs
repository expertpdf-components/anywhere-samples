using System;
using System.IO;
using ExpertPdf.Anywhere;

namespace TextSearch
{
    /// <summary>
    /// Finds words in a PDF document and reads the text of an area of a page: here, the fields of an invoice.
    /// The document is processed from memory, as it would come from a database or an upload.
    /// Run it without arguments to search the sample.pdf document that comes with the sample.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string pdfFile = args.Length > 0 ? args[0] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.pdf");
            string word = args.Length > 1 ? args[1] : "harbor";

            // the stream overloads take any Stream: a MemoryStream over the bytes of a document from a database,
            // an upload stream, a FileStream
            byte[] pdfBytes = File.ReadAllBytes(pdfFile);

            PdfToTextConverter converter = new PdfToTextConverter();

            // Without a license key only the first 5 pages are searched and the extracted text has an evaluation notice.
            // converter.LicenseKey = "your license key";

            // the document information alone, without extracting the text
            DocumentInformation info = converter.GetDocumentInformation(new MemoryStream(pdfBytes));
            Console.WriteLine("{0}: \"{1}\" by {2}, {3} pages", Path.GetFileName(pdfFile), info.Title, info.Author, info.PageCount);

            // the size of each page in points (1/72 inch) and its rotation; DisplayWidth and DisplayHeight are the size
            // as a viewer shows the page (width and height swapped on a page rotated by 90 or 270 degrees)
            foreach (PdfPageInfo page in info.Pages)
            {
                Console.WriteLine("  page {0}: {1:0.##} x {2:0.##} points ({3:0} x {4:0} mm), rotation {5}",
                    page.PageNumber, page.Width, page.Height, page.Width * 25.4 / 72, page.Height * 25.4 / 72, page.Rotation);
            }

            try
            {
                // 1. Search for a word. The positions are in points (1/72 inch), from the top left corner of the page.
                //    Not case sensitive, whole words only: "harbor" matches "Harbor", but not "harbors".
                TextPosition[] positions = converter.ExtractTextPositions(new MemoryStream(pdfBytes), word, false, true);
                Console.WriteLine();
                Console.WriteLine("\"{0}\" (any case, whole word) was found {1} times:", word, positions.Length);
                foreach (TextPosition position in positions)
                {
                    Console.WriteLine("  page {0}  x={1,6:0.0}  y={2,6:0.0}  width={3,5:0.0}  height={4,4:0.0}",
                        position.PageNumber, position.X, position.Y, position.Width, position.Height);
                }

                // the same search, case sensitive
                int exactMatches = converter.ExtractTextPositions(new MemoryStream(pdfBytes), word, true, true).Length;
                Console.WriteLine("\"{0}\" (case sensitive) was found {1} times", word, exactMatches);

                // 2. Read the fields of the invoice on page 3. The values have no fixed position in every document,
                //    but their labels do not change: find the label, then read the text on its right.
                Console.WriteLine();
                Console.WriteLine("Invoice fields:");
                foreach (string label in new string[] { "Invoice number:", "Invoice date:", "Customer:", "Total due:" })
                {
                    Console.WriteLine("  {0,-16} {1}", label, ReadValueRightOf(converter, pdfBytes, label));
                }

                // 3. Several words at once: one result for each word, with the pages where it was found.
                string[] words = { "harbor", "invoice", "revenue", "dividend" };
                TextSearchResult[] results = converter.SearchTexts(new MemoryStream(pdfBytes), words, false, true);
                Console.WriteLine();
                Console.WriteLine("Several words at once:");
                foreach (TextSearchResult result in results)
                {
                    Console.Write("  {0,-10} {1,3} times", result.SearchText, result.Count);
                    foreach (int pageNumber in result.PageNumbers)
                        Console.Write(", page {0}: {1}", pageNumber, result.GetPositionsOnPage(pageNumber).Length);
                    Console.WriteLine();
                }
            }
            catch (PdfToTextException ex)
            {
                Console.WriteLine("The search failed: " + ex.Message);
                return 2;
            }

            return 0;
        }

        // the right margin of the text on an A4 page (595 x 842 points)
        private const double RightMargin = 555;

        // finds the label, then extracts the text of a band that starts after the label and goes to the right margin
        private static string ReadValueRightOf(PdfToTextConverter converter, byte[] pdfBytes, string label)
        {
            TextPosition[] found = converter.ExtractTextPositions(new MemoryStream(pdfBytes), label, true, false);
            if (found.Length == 0)
                return "(label not found)";

            TextPosition labelPosition = found[0];
            double x = labelPosition.X + labelPosition.Width + 5;
            double y = labelPosition.Y - 2;
            double width = RightMargin - x;
            double height = labelPosition.Height + 4;

            string value = converter.ExtractTextAtCoords(new MemoryStream(pdfBytes), labelPosition.PageNumber, x, y, width, height);
            return value.Trim();
        }
    }
}
