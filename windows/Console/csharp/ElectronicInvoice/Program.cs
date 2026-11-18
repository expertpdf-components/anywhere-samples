using System;
using System.IO;
using ExpertPdf.Anywhere.HtmlToPdf;
using ExpertPdf.Anywhere.PdfDocument;

namespace ElectronicInvoice
{
    /// <summary>
    /// Creates a ZUGFeRD / Factur-X electronic invoice: the invoice page (invoice.html) is converted to a PDF/A-3
    /// document and the same invoice as structured XML (factur-x.xml, profile EN 16931) is embedded in it. People read
    /// the PDF; the buyer's accounting software reads the XML.
    /// Run it without arguments to convert the bundled invoice.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 2 || (args.Length > 0 && (args[0] == "--help" || args[0] == "/?")))
            {
                Console.WriteLine("Usage: ElectronicInvoice [invoice html] [invoice xml]");
                Console.WriteLine("  defaults: invoice.html and factur-x.xml from the application folder");
                return 0;
            }

            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            string invoiceHtml = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(appFolder, "invoice.html");
            string invoiceXml = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(appFolder, "factur-x.xml");

            PdfConverter converter = new PdfConverter();
            // converter.LicenseKey = "your license key";
            converter.PdfDocumentOptions.PdfPageSize = PdfPageSize.A4;
            converter.PdfDocumentOptions.LeftMargin = 20;
            converter.PdfDocumentOptions.RightMargin = 20;
            converter.PdfDocumentOptions.TopMargin = 20;
            converter.PdfDocumentOptions.BottomMargin = 20;
            converter.PdfDocumentInfo.Title = "Invoice F-2026-1017";
            converter.PdfDocumentInfo.AuthorName = "Alpine Data Systems GmbH";

            // ZUGFeRD and Factur-X invoices are PDF/A-3 documents. PDF/A-3a is the tagged, accessible level of
            // PDF/A-3: the invoice keeps the structure of the page (headings, tables) and its reading order.
            converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_3a;
            converter.Language = "en-US";

            string output = Path.Combine(appFolder, "invoice-facturx.pdf");
            try
            {
                Document document = converter.GetPdfDocumentObjectFromHtmlFile(invoiceHtml);
                try
                {
                    // Embeds the XML as factur-x.xml and writes the Factur-X metadata. The profile must match the
                    // content of the XML (EN 16931 here, also called COMFORT); Alternative says the XML is another
                    // representation of the visible invoice.
                    using (FileStream xml = File.OpenRead(invoiceXml))
                    {
                        document.AddZugferdInvoice(xml, ZugferdProfile.En16931, AttachmentRelationship.Alternative);
                    }
                    document.Save(output);
                }
                finally
                {
                    document.Close();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("The invoice could not be created: " + ex.Message);
                return 1;
            }

            Console.WriteLine("Created {0} ({1}) with the embedded factur-x.xml", output, converter.PdfStandardSubset);
            return 0;
        }
    }
}
