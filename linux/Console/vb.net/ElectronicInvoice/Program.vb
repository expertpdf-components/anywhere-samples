Imports ExpertPdf.Anywhere.HtmlToPdf
Imports ExpertPdf.Anywhere.PdfDocument

''' <summary>
''' Creates a ZUGFeRD / Factur-X electronic invoice: the invoice page (invoice.html) is converted to a PDF/A-3
''' document and the same invoice as structured XML (factur-x.xml, profile EN 16931) is embedded in it. People read
''' the PDF; the buyer's accounting software reads the XML.
''' Run it without arguments to convert the bundled invoice.
''' </summary>
Friend Module Program

    Function Main(args As String()) As Integer
        If args.Length > 2 OrElse (args.Length > 0 AndAlso (args(0) = "--help" OrElse args(0) = "/?")) Then
            Console.WriteLine("Usage: ElectronicInvoice [invoice html] [invoice xml]")
            Console.WriteLine("  defaults: invoice.html and factur-x.xml from the application folder")
            Return 0
        End If

        Dim appFolder As String = AppDomain.CurrentDomain.BaseDirectory
        Dim invoiceHtml As String = If(args.Length > 0, Path.GetFullPath(args(0)), Path.Combine(appFolder, "invoice.html"))
        Dim invoiceXml As String = If(args.Length > 1, Path.GetFullPath(args(1)), Path.Combine(appFolder, "factur-x.xml"))

        Dim converter As New PdfConverter()
        ' converter.LicenseKey = "your license key"
        converter.PdfDocumentOptions.PdfPageSize = PdfPageSize.A4
        converter.PdfDocumentOptions.LeftMargin = 20
        converter.PdfDocumentOptions.RightMargin = 20
        converter.PdfDocumentOptions.TopMargin = 20
        converter.PdfDocumentOptions.BottomMargin = 20
        converter.PdfDocumentInfo.Title = "Invoice F-2026-1017"
        converter.PdfDocumentInfo.AuthorName = "Alpine Data Systems GmbH"

        ' ZUGFeRD and Factur-X invoices are PDF/A-3 documents. PDF/A-3a is the tagged, accessible level of
        ' PDF/A-3: the invoice keeps the structure of the page (headings, tables) and its reading order.
        converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_3a
        converter.Language = "en-US"

        Dim output As String = Path.Combine(appFolder, "invoice-facturx.pdf")
        Try
            Dim document As Document = converter.GetPdfDocumentObjectFromHtmlFile(invoiceHtml)
            Try
                ' Embeds the XML as factur-x.xml and writes the Factur-X metadata. The profile must match the
                ' content of the XML (EN 16931 here, also called COMFORT); Alternative says the XML is another
                ' representation of the visible invoice.
                Using xml As FileStream = File.OpenRead(invoiceXml)
                    document.AddZugferdInvoice(xml, ZugferdProfile.En16931, AttachmentRelationship.Alternative)
                End Using
                document.Save(output)
            Finally
                document.Close()
            End Try
        Catch ex As Exception
            Console.Error.WriteLine("The invoice could not be created: " & ex.Message)
            Return 1
        End Try

        Console.WriteLine("Created {0} ({1}) with the embedded factur-x.xml", output, converter.PdfStandardSubset)
        Return 0
    End Function

End Module
