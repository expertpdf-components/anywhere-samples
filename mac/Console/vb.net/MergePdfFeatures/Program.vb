Imports ExpertPdf.Anywhere.Drawing
Imports System.IO
Imports ExpertPdf.Anywhere.MergePdf

''' <summary>
''' Shows the features of the merge library, one merged document per feature:
''' page ranges, documents and images from memory, document information and security,
''' password protected sources, PDF/A output, inserting and reordering pages, a bookmark for each
''' merged document and booklets.
''' </summary>
Module Program

    Private ReadOnly sampleFolder As String = AppDomain.CurrentDomain.BaseDirectory
    Private ReadOnly report As String = Path.Combine(sampleFolder, "report.pdf")     ' 6 pages, "Page N of 6" on each page
    Private ReadOnly invoice As String = Path.Combine(sampleFolder, "invoice.pdf")   ' 1 page
    Private ReadOnly logo As String = Path.Combine(sampleFolder, "logo.jpg")

    Function Main(args As String()) As Integer
        Try
            Console.WriteLine("report.pdf has {0} pages, invoice.pdf has {1} page", PDFMerge.GetPageCount(report), PDFMerge.GetPageCount(invoice))
            Console.WriteLine()

            MergePageRanges()
            MergeFromMemory()
            MergeWithSecurity()
            MergeProtectedSources()
            MergeToPdfA()
            InsertAndReorder()
            BookmarkPerDocument()
            MergeToBooklet()

            Console.WriteLine()
            Console.WriteLine("The merged documents are in " & sampleFolder)
            Return 0
        Catch ex As Exception
            Console.WriteLine("Error: " & ex.Message)
            Return 1
        End Try
    End Function

    ' Take only some pages of each source document. The page indexes are zero based.
    Private Sub MergePageRanges()
        Dim pdfMerge As New PDFMerge()
        ' pdfMerge.LicenseKey = "your license key"

        ' the first two pages of the report
        pdfMerge.AppendPDFFile(report, 0, 2)
        ' the whole invoice
        pdfMerge.AppendPDFFile(invoice)
        ' the report from the fifth page to the end
        pdfMerge.AppendPDFFile(report, 4)
        ' a length of -1 also means "to the end": here, the last page only
        pdfMerge.AppendPDFFile(report, 5, -1)

        Dim outFile As String = Path.Combine(sampleFolder, "1-PageRanges.pdf")
        pdfMerge.SaveMergedPDFToFile(outFile)
        PrintResult(outFile, "report pages 1-2, the invoice, report pages 5-6, report page 6")
    End Sub

    ' Merge documents you already have in memory or in a stream (for example created by another
    ' ExpertPdf library or read from a database), plus images, without temporary files.
    Private Sub MergeFromMemory()
        ' the page size and orientation are used for the pages the library creates: images and empty pages
        Dim options As New PdfDocumentOptions()
        options.PdfPageSize = PdfPageSize.Letter
        options.PdfPageOrientation = PDFPageOrientation.Landscape
        options.PdfCompressionLevel = PDFCompressionLevel.Best

        Dim pdfMerge As New PDFMerge(options)

        ' a document as a byte array
        Dim invoiceBytes As Byte() = File.ReadAllBytes(invoice)
        pdfMerge.AppendPDFBytes(invoiceBytes)

        ' one page (the third) of a document read from a stream; the stream must stay open
        ' until the merged document is saved
        Using reportStream As FileStream = File.OpenRead(report)
            pdfMerge.AppendPDFStream(reportStream, 2, 1)
            Console.WriteLine("  pages so far: " & pdfMerge.GetCurrentPageCount())

            ' an image file and an image created in code, each on a new page
            pdfMerge.AppendImageFile(logo)
            pdfMerge.AppendImageObject(DrawChart())

            ' render to memory: store the bytes, send them in a web response, etc.
            Dim mergedBytes As Byte() = pdfMerge.RenderMergedPDFDocument()

            Dim outFile As String = Path.Combine(sampleFolder, "2-FromMemory.pdf")
            File.WriteAllBytes(outFile, mergedBytes)
            PrintResult(outFile, "invoice (bytes), report page 3 (stream), logo image, chart image")
        End Using
    End Sub

    ' Set the document information and protect the merged document with passwords and permissions.
    Private Sub MergeWithSecurity()
        Dim pdfMerge As New PDFMerge()

        pdfMerge.DocumentInfo.Title = "Report with invoice"
        pdfMerge.DocumentInfo.Subject = "Merged with ExpertPdf MergePdf"
        pdfMerge.DocumentInfo.AuthorName = "Sample Company"
        pdfMerge.DocumentInfo.Keywords = "report, invoice, merge"
        pdfMerge.DocumentInfo.CreatedDate = DateTime.Now

        ' the user password opens the document; the owner password also allows changing the permissions
        pdfMerge.SecurityOptions.UserPassword = "user"
        pdfMerge.SecurityOptions.OwnerPassword = "owner"
        pdfMerge.SecurityOptions.KeySize = EncryptionKeySize.EncryptKey256Bit
        pdfMerge.SecurityOptions.Algorithm = EncryptionAlgorithm.AES
        ' readers opening it with the user password may print, but not copy or change the content
        pdfMerge.SecurityOptions.CanPrint = True
        pdfMerge.SecurityOptions.CanCopyContent = False
        pdfMerge.SecurityOptions.CanEditContent = False
        pdfMerge.SecurityOptions.CanEditAnnotations = False
        pdfMerge.SecurityOptions.CanAssembleDocument = False

        pdfMerge.AppendPDFFile(report)
        pdfMerge.AppendPDFFile(invoice)

        ' save to any writable stream
        Dim outFile As String = Path.Combine(sampleFolder, "3-Secured.pdf")
        Using output As FileStream = File.Create(outFile)
            pdfMerge.SaveMergedPDFToStream(output)
        End Using
        Console.WriteLine("3-Secured.pdf: report and invoice, AES 256 bit, user password ""user"", owner password ""owner""")
    End Sub

    ' Append documents that are protected with a password: give the user or the owner password.
    ' The merged document is not protected unless you set its SecurityOptions.
    Private Sub MergeProtectedSources()
        Dim pdfMerge As New PDFMerge()

        ' the whole protected document (-1 = all the pages from the start index)
        pdfMerge.AppendPDFFile(Path.Combine(sampleFolder, "3-Secured.pdf"), "user", 0, -1)

        ' the last page of the same protected document, from memory
        Dim securedBytes As Byte() = File.ReadAllBytes(Path.Combine(sampleFolder, "3-Secured.pdf"))
        pdfMerge.AppendPDFBytes(securedBytes, "owner", 6, 1)

        Dim outFile As String = Path.Combine(sampleFolder, "4-FromProtectedSources.pdf")
        pdfMerge.SaveMergedPDFToFile(outFile)
        PrintResult(outFile, "3-Secured.pdf opened with its passwords, saved without protection")
    End Sub

    ' Produce a PDF/A document for long term archiving. The pages are imported as they are, so a PDF/A merge
    ' takes only documents that conform to the level: here each source is converted to PDF/A-2b first.
    Private Sub MergeToPdfA()
        Dim options As New PdfDocumentOptions()
        options.PdfStandardSubset = PdfStandardSubset.Pdf_A_2b

        Dim pdfMerge As New PDFMerge(options)
        pdfMerge.DocumentInfo.Title = "Archived report"
        pdfMerge.DocumentInfo.AuthorName = "Sample Company"

        pdfMerge.AppendPDFBytes(ToPdfA2b(report))
        pdfMerge.AppendPDFBytes(ToPdfA2b(invoice))

        Dim outFile As String = Path.Combine(sampleFolder, "5-PdfA-2b.pdf")
        pdfMerge.SaveMergedPDFToFile(outFile)
        PrintResult(outFile, "report and invoice as PDF/A-2b")
    End Sub

    ' a document converted to PDF/A-2b: the fonts embedded, the color profile attached, the metadata written
    Private Function ToPdfA2b(pdfFile As String) As Byte()
        Dim document As New ExpertPdf.Anywhere.PdfDocument.Document(pdfFile)
        Try
            document.ConvertToPdfA(ExpertPdf.Anywhere.HtmlToPdf.PdfStandardSubset.Pdf_A_2b)
            Return document.Save()
        Finally
            document.Close()
        End Try
    End Function

    ' Put a document between the pages of another and change the order of the merged pages. The positions
    ' are zero based: 0 inserts before the first page, GetCurrentPageCount() at the end.
    Private Sub InsertAndReorder()
        Dim pdfMerge As New PDFMerge()

        pdfMerge.AppendPDFFile(report)                 ' report pages 1-6
        pdfMerge.InsertPDFFile(1, invoice)             ' the invoice after the first report page
        pdfMerge.InsertEmptyPage(0)                    ' an empty page in front
        pdfMerge.InsertPDFFile(pdfMerge.GetCurrentPageCount(), report, 0, 1)   ' report page 1 again, at the end

        ' move the last page (the second copy of report page 1) to the front: newOrder(i) is the current
        ' index of the page that goes to position i
        Dim count As Integer = pdfMerge.GetCurrentPageCount()
        Dim newOrder(count - 1) As Integer
        newOrder(0) = count - 1
        For i As Integer = 1 To count - 1
            newOrder(i) = i - 1
        Next
        pdfMerge.ReorderPages(newOrder)

        Dim outFile As String = Path.Combine(sampleFolder, "6-InsertAndReorder.pdf")
        pdfMerge.SaveMergedPDFToFile(outFile)
        PrintResult(outFile, "report page 1, empty page, report page 1, invoice, report pages 2-6")
    End Sub

    ' One bookmark for each merged document, with the bookmarks of the document nested under it.
    Private Sub BookmarkPerDocument()
        Dim pdfMerge As New PDFMerge()
        pdfMerge.BookmarkOptions.AddDocumentBookmarks = True
        ' the titles come from the document information (report.pdf and invoice.pdf have a title);
        ' BookmarkTitleSource.FileName uses the file names instead
        pdfMerge.BookmarkOptions.TitleSource = BookmarkTitleSource.DocumentTitle

        pdfMerge.AppendPDFFile(report)
        pdfMerge.AppendPDFFile(invoice)
        ' a title of your own for the document added last
        pdfMerge.AppendImageFile(logo)
        pdfMerge.SetBookmarkTitle("Company logo")

        Dim outFile As String = Path.Combine(sampleFolder, "7-Bookmarks.pdf")
        pdfMerge.SaveMergedPDFToFile(outFile)
        PrintResult(outFile, "a bookmark for the report (with its chapters), the invoice and the logo")
    End Sub

    ' A booklet: two pages on each landscape sheet, in the order that gives a booklet when the printed
    ' sheets are folded in the middle. Two sided: print the sheets on both sides, flipping on the short edge.
    Private Sub MergeToBooklet()
        Dim pdfMerge As New PDFMerge()
        pdfMerge.BookletOptions.Enabled = True
        pdfMerge.BookletOptions.PageSize = PdfPageSize.A4    ' A4 sheets, so A5 pages
        pdfMerge.BookletOptions.TwoSided = True

        pdfMerge.AppendPDFFile(report)
        pdfMerge.AppendPDFFile(invoice)    ' 7 pages: the booklet gets an empty page at the end

        Dim outFile As String = Path.Combine(sampleFolder, "8-Booklet.pdf")
        pdfMerge.SaveMergedPDFToFile(outFile)
        PrintResult(outFile, "4 sheets: 8|1, 2|7, 6|3, 4|5 (page 8 empty)")
    End Sub

    ' a simple bar chart drawn in HTML and rendered to a PNG image by the HTML to image converter, to show
    ' how to merge an image created in code (the bytes of any PNG, JPEG, BMP, GIF or TIFF image will do)
    Private Function DrawChart() As Byte()
        Dim values As Integer() = {180, 260, 140, 320}
        Dim colors As String() = {"steelblue", "seagreen", "orange", "indianred"}
        Dim bars As String = String.Empty
        For i As Integer = 0 To values.Length - 1
            bars &= String.Format("<div style='position:absolute; left:{0}px; bottom:60px; width:120px; " &
                "height:{1}px; background:{2}'></div><div style='position:absolute; left:{3}px; bottom:20px'>" &
                "Q{4}</div>", 80 + i * 170, values(i), colors(i), 120 + i * 170, i + 1)
        Next
        Dim html As String = "<html><body style='margin:0; width:800px; height:500px; position:relative; " &
            "font:26px Arial; background:white'><div style='position:absolute; left:20px; top:20px'>" &
            "Sales by quarter</div>" & bars & "</body></html>"

        Dim converter As New ExpertPdf.Anywhere.HtmlToPdf.ImgConverter(800, 500)
        ' converter.LicenseKey = "your license key"
        Return converter.GetImageBytesFromHtmlString(html, ImageFormat.Png)
    End Function

    Private Sub PrintResult(fileName As String, content As String)
        Console.WriteLine("{0}: {1} pages - {2}", Path.GetFileName(fileName), PDFMerge.GetPageCount(fileName), content)
    End Sub

End Module
