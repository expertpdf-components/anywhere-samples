Imports System
Imports System.IO
Imports ExpertPdf.Anywhere.Drawing
Imports ExpertPdf.Anywhere

''' <summary>
''' A command line tool that extracts the images embedded in a PDF document to image files.
''' Run it without arguments to extract the images of the sample.pdf document that comes with the sample.
''' </summary>
Module Program

    Private ReadOnly AppFolder As String = AppDomain.CurrentDomain.BaseDirectory

    ' the number of images extracted so far
    Private count As Integer

    Function Main(args As String()) As Integer
        ' the defaults: the bundled sample document, all pages, PNG files in the "extracted" folder
        Dim pdfFile As String = Path.Combine(AppFolder, "sample.pdf")
        Dim outFolder As String = Path.Combine(AppFolder, "extracted")
        Dim format As ImageFormat = ImageFormat.Png
        Dim startPage As Integer = 1
        Dim endPage As Integer = 0
        Dim prefix As String = "image_"
        Dim password As String = Nothing
        Dim minWidth As Integer = 0
        Dim minHeight As Integer = 0
        Dim skipDuplicates As Boolean = False

        For Each arg As String In args
            Dim name As String = arg
            Dim value As String = Nothing
            Dim colon As Integer = arg.IndexOf(":"c)
            If colon > 0 Then
                name = arg.Substring(0, colon)
                value = arg.Substring(colon + 1).Trim(""""c)
            End If

            Select Case name.ToLowerInvariant()
                Case "/pdf" : pdfFile = InputFile(value)
                Case "/out" : outFolder = Path.GetFullPath(value)
                Case "/start" : startPage = Integer.Parse(value)
                Case "/end" : endPage = Integer.Parse(value)
                Case "/prefix" : prefix = value
                Case "/userpswd" : password = value
                Case "/minwidth" : minWidth = Integer.Parse(value)
                Case "/minheight" : minHeight = Integer.Parse(value)
                Case "/nodup" : skipDuplicates = True
                Case "/imgformat"
                    Select Case value
                        Case "png" : format = ImageFormat.Png
                        Case "jpg" : format = ImageFormat.Jpeg
                        Case "bmp" : format = ImageFormat.Bmp
                        Case "tiff" : format = ImageFormat.Tiff
                        Case Else : Return Usage("Unknown image format: " & value)
                    End Select
                Case "/?", "/help"
                    Return Usage(Nothing)
                Case Else
                    Return Usage("Unknown argument: " & arg)
            End Select
        Next

        If Not File.Exists(pdfFile) Then Return Usage("The PDF file was not found: " & pdfFile)

        ' the extractor saves the images in an existing folder
        Directory.CreateDirectory(outFolder)

        Dim extractor As New PdfImgExtract()

        ' Without a license key the extracted images have an evaluation notice.
        ' extractor.LicenseKey = "your license key"

        ' the pages to extract the images from: EndPageNumber = 0 means "up to the last page"
        extractor.StartPageNumber = startPage
        extractor.EndPageNumber = endPage

        ' the files are named <ExtractPrefix><page number>_<image index, from 0>.<format>
        extractor.ExtractPath = outFolder
        extractor.ExtractPrefix = prefix
        extractor.ExtractImageFormat = format

        ' the password needed to open a protected document (Nothing for a document without a password)
        extractor.UserPassword = password

        ' skip the small images (icons, bullets, lines) and extract a repeated image (a logo on every page) once
        extractor.MinimumWidth = minWidth
        extractor.MinimumHeight = minHeight
        extractor.SkipDuplicateImages = skipDuplicates

        ' the event is raised for every image, before it is saved: the place to log, filter or index the images
        AddHandler extractor.ImageExtractedEvent, AddressOf OnImageExtracted

        Console.WriteLine("Extracting the images of " & pdfFile)
        Try
            ' True: save the images in the ExtractPath folder
            extractor.ExtractImages(pdfFile, True)
        Catch ex As PdfImgExtractException
            Console.WriteLine("The extraction failed: " & ex.Message)
            Return 2
        End Try

        Dim info As DocumentInformation = extractor.DocumentInformation
        Console.WriteLine("{0} images extracted from {1} pages, saved as {2} files in {3}",
            count, If(info IsNot Nothing, info.PageCount, 0), format.ToString().ToUpperInvariant(), outFolder)
        Return 0
    End Function

    Private Sub OnImageExtracted(e As ImageExtractedEventArgs)
        count += 1
        ' ExtractedBitmap holds the bytes of the image file, in the ExtractImageFormat format
        Console.WriteLine("  page {0}: {1:N0} bytes", e.PageNumber, e.ExtractedBitmap.Length)
    End Sub

    ' a relative path is looked for in the current folder, then next to the program (where the sample files are)
    Private Function InputFile(path As String) As String
        If IO.Path.IsPathRooted(path) OrElse File.Exists(path) Then Return IO.Path.GetFullPath(path)
        Return IO.Path.Combine(AppFolder, path)
    End Function

    Private Function Usage(errorMessage As String) As Integer
        If errorMessage IsNot Nothing Then Console.WriteLine(errorMessage)
        Console.WriteLine("Usage: ExtractPdfImages [/pdf:file.pdf] [/out:folder] [/imgformat:png|jpg|bmp|tiff]")
        Console.WriteLine("                        [/start:n] [/end:n] [/prefix:name] [/userpswd:password]")
        Console.WriteLine("                        [/minwidth:n] [/minheight:n] [/nodup]")
        Console.WriteLine("Without arguments the images of sample.pdf are saved as PNG files in the extracted folder.")
        Console.WriteLine("Examples:")
        Console.WriteLine("  ExtractPdfImages /pdf:sample.pdf /imgformat:jpg /start:4 /end:4")
        Console.WriteLine("  ExtractPdfImages /pdf:sample.pdf /minwidth:100 /minheight:100 /nodup")
        Console.WriteLine("  ExtractPdfImages /pdf:sample-protected.pdf /userpswd:open")
        Return If(errorMessage Is Nothing, 0, 1)
    End Function

End Module
