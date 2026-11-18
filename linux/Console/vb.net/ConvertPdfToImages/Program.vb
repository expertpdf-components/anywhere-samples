Imports System
Imports ExpertPdf.Anywhere.Drawing
Imports System.IO
Imports ExpertPdf.Anywhere

''' <summary>
''' A command line tool that converts the pages of a PDF document to image files (PNG, JPEG, BMP, TIFF)
''' or to one multi-page TIFF file. Run it without arguments to convert the pages of the sample.pdf document
''' that comes with the sample to PNG files.
''' </summary>
Module Program

    Private ReadOnly AppFolder As String = AppDomain.CurrentDomain.BaseDirectory

    Function Main(args As String()) As Integer
        ' the defaults: the bundled sample document, all pages, PNG files at 120 DPI in the "images" folder
        Dim pdfFile As String = Path.Combine(AppFolder, "sample.pdf")
        Dim outFolder As String = Path.Combine(AppFolder, "images")
        Dim format As ImageFormat = ImageFormat.Png
        Dim multiPageTiff As Boolean = False
        Dim resolution As Integer = 120
        Dim gray As Boolean = False
        Dim startPage As Integer = 1
        Dim endPage As Integer = 0
        Dim prefix As String = Nothing
        Dim password As String = Nothing
        Dim jpegQuality As Integer = 91
        Dim pngCompressionLevel As Integer = -1
        Dim compression As PdfToImageTiffCompression = PdfToImageTiffCompression.Lzw
        Dim thumbnailSize As Integer = 0

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
                Case "/dpi" : resolution = Integer.Parse(value)
                Case "/gray" : gray = True
                Case "/start" : startPage = Integer.Parse(value)
                Case "/end" : endPage = Integer.Parse(value)
                Case "/prefix" : prefix = value
                Case "/userpswd" : password = value
                Case "/tiff" : multiPageTiff = True
                Case "/quality" : jpegQuality = Integer.Parse(value)
                Case "/pngcompression" : pngCompressionLevel = Integer.Parse(value)
                Case "/thumbs" : thumbnailSize = Integer.Parse(value)
                Case "/compression"
                    Select Case value
                        Case "lzw" : compression = PdfToImageTiffCompression.Lzw
                        Case "none" : compression = PdfToImageTiffCompression.None
                        Case "rle" : compression = PdfToImageTiffCompression.Rle
                        Case "ccitt3" : compression = PdfToImageTiffCompression.Ccitt3
                        Case "ccitt4" : compression = PdfToImageTiffCompression.Ccitt4
                        Case Else : Return Usage("Unknown TIFF compression: " & value)
                    End Select
                Case "/format"
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

        ' page_1.png, page_2.png, ... or sample.tiff for a multi-page TIFF made from sample.pdf
        If prefix Is Nothing Then prefix = If(multiPageTiff, Path.GetFileNameWithoutExtension(pdfFile), If(thumbnailSize > 0, "thumb_", "page_"))

        ' the converter saves the images in an existing folder
        Directory.CreateDirectory(outFolder)

        Dim converter As New PdfToImageConverter()

        ' Without a license key the images have an evaluation notice.
        ' converter.LicenseKey = "your license key"

        ' the pages to convert: EndPageNumber = 0 means "up to the last page"
        converter.StartPageNumber = startPage
        converter.EndPageNumber = endPage

        ' the resolution decides the size of the images: an A4 page is 8.27 x 11.69 inches,
        ' 992 x 1403 pixels at 120 DPI; use 200-300 DPI for printing or OCR, 36-72 DPI for thumbnails
        converter.Resolution = resolution

        ' gray images are smaller and enough for archiving and OCR
        converter.ColorSpace = If(gray, PdfToImageColorSpace.Gray, PdfToImageColorSpace.RGB)

        ' where and how the image files are saved: <ImagesPath>\<ImagesPrefix>1.<format>, <ImagesPrefix>2.<format>, ...
        ' numbered from 1 for the first converted page
        converter.ImagesPath = outFolder
        converter.ImagesPrefix = prefix
        converter.ImagesFormat = format

        ' JPEG files: 0 (smallest) to 100 (best quality); TIFF files: LZW (the default), none, RLE (PackBits),
        ' or CCITT Group 3 / Group 4 - black and white images, the smallest files for text pages and fax services
        converter.JpegQuality = jpegQuality
        converter.TiffCompression = compression

        ' PNG files: the compression level, 0 (no compression, the fastest) to 9 (the smallest files); the pixels
        ' are the same at every level. -1 (the default) keeps the renderer's output
        converter.PngCompressionLevel = pngCompressionLevel

        ' the password needed to open a protected document (Nothing for a document without a password)
        converter.UserPassword = password

        Dim started As DateTime = DateTime.Now.AddSeconds(-1)
        Try
            If thumbnailSize > 0 Then
                ' thumbnails that fit in a thumbnailSize x thumbnailSize box, keeping the aspect ratio of the
                ' pages; the resolution is chosen for each page, the Resolution property is not used. The thumbnails
                ' are image files in the ImagesFormat format.
                Dim thumbnails As Byte()() = converter.ConvertToThumbnails(pdfFile, thumbnailSize, thumbnailSize)
                Dim extension As String = If(format = ImageFormat.Jpeg, ".jpg", "." & format.ToString().ToLowerInvariant())
                For i As Integer = 0 To thumbnails.Length - 1
                    File.WriteAllBytes(Path.Combine(outFolder, prefix & (i + 1) & extension), thumbnails(i))
                Next
            ElseIf multiPageTiff Then
                ' one TIFF file with a frame for each page: <ImagesPath>\<ImagesPrefix>.tiff
                converter.ConvertToTiffInFolder(pdfFile)
            Else
                ' one image file for each page
                converter.ConvertToImagesInFolder(pdfFile)
            End If
        Catch ex As PdfToImageException
            Console.WriteLine("The conversion failed: " & ex.Message)
            Return 2
        End Try

        Dim info As DocumentInformation = converter.DocumentInformation
        Console.WriteLine("Converted: {0} ({1} pages)", pdfFile, If(info IsNot Nothing, info.PageCount, 0))
        Console.WriteLine("  {0} DPI, {1}, {2}", resolution, If(gray, "gray", "color"),
            If(thumbnailSize > 0, "PNG thumbnails of at most " & thumbnailSize & " pixels",
               If(multiPageTiff, "one multi-page TIFF file", format.ToString().ToUpperInvariant() & " files")))
        For Each file As String In Directory.GetFiles(outFolder, prefix & "*")
            If IO.File.GetLastWriteTime(file) >= started Then
                Console.WriteLine("  {0} ({1:N0} bytes)", file, New FileInfo(file).Length)
            End If
        Next
        Return 0
    End Function

    ' a relative path is looked for in the current folder, then next to the program (where the sample files are)
    Private Function InputFile(path As String) As String
        If IO.Path.IsPathRooted(path) OrElse File.Exists(path) Then Return IO.Path.GetFullPath(path)
        Return IO.Path.Combine(AppFolder, path)
    End Function

    Private Function Usage(errorMessage As String) As Integer
        If errorMessage IsNot Nothing Then Console.WriteLine(errorMessage)
        Console.WriteLine("Usage: ConvertPdfToImages [/pdf:file.pdf] [/out:folder] [/format:png|jpg|bmp|tiff] [/tiff]")
        Console.WriteLine("                          [/dpi:n] [/gray] [/start:n] [/end:n] [/prefix:name] [/userpswd:password]")
        Console.WriteLine("                          [/quality:0-100] [/compression:lzw|none|rle|ccitt3|ccitt4] [/pngcompression:0-9]")
        Console.WriteLine("                          [/thumbs:size]")
        Console.WriteLine("Without arguments the pages of sample.pdf are converted to PNG files in the images folder.")
        Console.WriteLine("Examples:")
        Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /format:jpg /dpi:72 /start:1 /end:2")
        Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /tiff /gray /dpi:200")
        Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /tiff /compression:ccitt4 /dpi:200")
        Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /format:jpg /quality:60")
        Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /pngcompression:9")
        Console.WriteLine("  ConvertPdfToImages /pdf:sample.pdf /thumbs:160")
        Console.WriteLine("  ConvertPdfToImages /pdf:sample-protected.pdf /userpswd:open")
        Return If(errorMessage Is Nothing, 0, 1)
    End Function

End Module
