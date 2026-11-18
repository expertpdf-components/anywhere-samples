Imports ExpertPdf.Anywhere.HtmlToPdf

''' <summary>
''' Converts every .html file of a folder to PDF. Run it without arguments to convert the pages of the bundled
''' html folder into a pdf folder next to the application.
''' </summary>
Friend Module Program

    Function Main(args As String()) As Integer
        If args.Length > 2 OrElse (args.Length > 0 AndAlso (args(0) = "--help" OrElse args(0) = "/?")) Then
            Console.WriteLine("Usage: BatchConversion [html folder] [pdf folder]")
            Console.WriteLine("  html folder  the folder with the .html files (default: the html folder of the application)")
            Console.WriteLine("  pdf folder   where the PDF files are written (default: the pdf folder of the application)")
            Return 0
        End If

        Dim appFolder As String = AppDomain.CurrentDomain.BaseDirectory
        Dim htmlFolder As String = If(args.Length > 0, Path.GetFullPath(args(0)), Path.Combine(appFolder, "html"))
        Dim pdfFolder As String = If(args.Length > 1, Path.GetFullPath(args(1)), Path.Combine(appFolder, "pdf"))

        If Not Directory.Exists(htmlFolder) Then
            Console.Error.WriteLine("The folder '" & htmlFolder & "' does not exist.")
            Return 2
        End If

        Dim htmlFiles As String() = Directory.GetFiles(htmlFolder, "*.htm*", SearchOption.TopDirectoryOnly)
        If htmlFiles.Length = 0 Then
            Console.WriteLine("There is no .html file in " & htmlFolder)
            Return 0
        End If

        Directory.CreateDirectory(pdfFolder)
        Console.WriteLine("Converting " & htmlFiles.Length & " files from " & htmlFolder)

        Dim failed As Integer = 0
        Dim total As Stopwatch = Stopwatch.StartNew()
        For Each htmlFile As String In htmlFiles
            Dim pdfFile As String = Path.Combine(pdfFolder, Path.GetFileNameWithoutExtension(htmlFile) & ".pdf")
            Dim watch As Stopwatch = Stopwatch.StartNew()
            Try
                ' a new converter for each file: the settings of one conversion never leak into the next one
                Dim converter As New PdfConverter()
                ' converter.LicenseKey = "your license key"
                converter.PdfDocumentOptions.PdfPageSize = PdfPageSize.A4
                converter.PdfDocumentOptions.LeftMargin = 20
                converter.PdfDocumentOptions.RightMargin = 20
                converter.PdfDocumentOptions.TopMargin = 20
                converter.PdfDocumentOptions.BottomMargin = 20
                ' one broken page must not stop the batch: give up on a page after 30 seconds
                converter.NavigationTimeout = 30

                converter.SavePdfFromHtmlFileToFile(htmlFile, pdfFile)
                Console.WriteLine("  {0} -> {1} ({2} pages, {3:F1} s)", Path.GetFileName(htmlFile), Path.GetFileName(pdfFile),
                    converter.ConversionSummary.PdfPageCount, watch.Elapsed.TotalSeconds)
            Catch ex As Exception
                failed += 1
                Console.WriteLine("  {0} failed: {1}", Path.GetFileName(htmlFile), ex.Message)
            End Try
        Next

        Console.WriteLine("Done in {0:F1} s: {1} converted, {2} failed. The PDF files are in {3}",
            total.Elapsed.TotalSeconds, htmlFiles.Length - failed, failed, pdfFolder)
        Return If(failed = 0, 0, 1)
    End Function

End Module
