Imports System
Imports System.IO
Imports ExpertPdf.Anywhere

''' <summary>
''' Finds words in a PDF document and reads the text of an area of a page: here, the fields of an invoice.
''' The document is processed from memory, as it would come from a database or an upload.
''' Run it without arguments to search the sample.pdf document that comes with the sample.
''' </summary>
Module Program

    ' the right margin of the text on an A4 page (595 x 842 points)
    Private Const RightMargin As Double = 555

    Function Main(args As String()) As Integer
        Dim pdfFile As String = If(args.Length > 0, args(0), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.pdf"))
        Dim word As String = If(args.Length > 1, args(1), "harbor")

        ' the stream overloads take any Stream: a MemoryStream over the bytes of a document from a database,
        ' an upload stream, a FileStream
        Dim pdfBytes As Byte() = File.ReadAllBytes(pdfFile)

        Dim converter As New PdfToTextConverter()

        ' Without a license key only the first 5 pages are searched and the extracted text has an evaluation notice.
        ' converter.LicenseKey = "your license key"

        ' the document information alone, without extracting the text
        Dim info As DocumentInformation = converter.GetDocumentInformation(New MemoryStream(pdfBytes))
        Console.WriteLine("{0}: ""{1}"" by {2}, {3} pages", Path.GetFileName(pdfFile), info.Title, info.Author, info.PageCount)

        ' the size of each page in points (1/72 inch) and its rotation; DisplayWidth and DisplayHeight are the size
        ' as a viewer shows the page (width and height swapped on a page rotated by 90 or 270 degrees)
        For Each page As PdfPageInfo In info.Pages
            Console.WriteLine("  page {0}: {1:0.##} x {2:0.##} points ({3:0} x {4:0} mm), rotation {5}",
                page.PageNumber, page.Width, page.Height, page.Width * 25.4 / 72, page.Height * 25.4 / 72, page.Rotation)
        Next

        Try
            ' 1. Search for a word. The positions are in points (1/72 inch), from the top left corner of the page.
            '    Not case sensitive, whole words only: "harbor" matches "Harbor", but not "harbors".
            Dim positions As TextPosition() = converter.ExtractTextPositions(New MemoryStream(pdfBytes), word, False, True)
            Console.WriteLine()
            Console.WriteLine("""{0}"" (any case, whole word) was found {1} times:", word, positions.Length)
            For Each position As TextPosition In positions
                Console.WriteLine("  page {0}  x={1,6:0.0}  y={2,6:0.0}  width={3,5:0.0}  height={4,4:0.0}",
                    position.PageNumber, position.X, position.Y, position.Width, position.Height)
            Next

            ' the same search, case sensitive
            Dim exactMatches As Integer = converter.ExtractTextPositions(New MemoryStream(pdfBytes), word, True, True).Length
            Console.WriteLine("""{0}"" (case sensitive) was found {1} times", word, exactMatches)

            ' 2. Read the fields of the invoice on page 3. The values have no fixed position in every document,
            '    but their labels do not change: find the label, then read the text on its right.
            Console.WriteLine()
            Console.WriteLine("Invoice fields:")
            For Each label As String In New String() {"Invoice number:", "Invoice date:", "Customer:", "Total due:"}
                Console.WriteLine("  {0,-16} {1}", label, ReadValueRightOf(converter, pdfBytes, label))
            Next

            ' 3. Several words at once: one result for each word, with the pages where it was found.
            Dim words As String() = {"harbor", "invoice", "revenue", "dividend"}
            Dim results As TextSearchResult() = converter.SearchTexts(New MemoryStream(pdfBytes), words, False, True)
            Console.WriteLine()
            Console.WriteLine("Several words at once:")
            For Each result As TextSearchResult In results
                Console.Write("  {0,-10} {1,3} times", result.SearchText, result.Count)
                For Each pageNumber As Integer In result.PageNumbers
                    Console.Write(", page {0}: {1}", pageNumber, result.GetPositionsOnPage(pageNumber).Length)
                Next
                Console.WriteLine()
            Next
        Catch ex As PdfToTextException
            Console.WriteLine("The search failed: " & ex.Message)
            Return 2
        End Try

        Return 0
    End Function

    ' finds the label, then extracts the text of a band that starts after the label and goes to the right margin
    Private Function ReadValueRightOf(converter As PdfToTextConverter, pdfBytes As Byte(), label As String) As String
        Dim found As TextPosition() = converter.ExtractTextPositions(New MemoryStream(pdfBytes), label, True, False)
        If found.Length = 0 Then Return "(label not found)"

        Dim labelPosition As TextPosition = found(0)
        Dim x As Double = labelPosition.X + labelPosition.Width + 5
        Dim y As Double = labelPosition.Y - 2
        Dim width As Double = RightMargin - x
        Dim height As Double = labelPosition.Height + 4

        Dim value As String = converter.ExtractTextAtCoords(New MemoryStream(pdfBytes), labelPosition.PageNumber, x, y, width, height)
        Return value.Trim()
    End Function

End Module
