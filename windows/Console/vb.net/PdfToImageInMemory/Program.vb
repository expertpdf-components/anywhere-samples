Imports System
Imports ExpertPdf.Anywhere.Drawing
Imports System.IO
Imports ExpertPdf.Anywhere

''' <summary>
''' Converts PDF pages to images in memory, without temporary files: a contact sheet with a thumbnail of every
''' page, one page as a JPEG file and a gray multi-page TIFF document for an archive. The PDF document is read from memory, as it would
''' come from a database or an upload. Run it without arguments to process the sample.pdf document that comes
''' with the sample.
''' </summary>
Module Program

    Function Main(args As String()) As Integer
        Dim appFolder As String = AppDomain.CurrentDomain.BaseDirectory
        Dim pdfFile As String = If(args.Length > 0, args(0), Path.Combine(appFolder, "sample.pdf"))
        Dim pdfBytes As Byte() = File.ReadAllBytes(pdfFile)

        Dim converter As New PdfToImageConverter()

        ' Without a license key the images have an evaluation notice.
        ' converter.LicenseKey = "your license key"

        Try
            Dim pageCount As Integer = converter.GetPageCount(New MemoryStream(pdfBytes))
            Console.WriteLine("{0}: {1} pages", Path.GetFileName(pdfFile), pageCount)

            ' 1. Thumbnails that fit in a 120 x 120 pixels box, keeping the aspect ratio of the pages: the bytes
            '    of an image file for each page (PNG, the ImagesFormat default); your code decides what to do with
            '    them. Here they are placed side by side on the page of a PDF contact sheet.
            Dim thumbnails As Byte()() = converter.ConvertToThumbnails(New MemoryStream(pdfBytes), 120, 120)
            Dim contactSheet As String = Path.Combine(appFolder, "contact-sheet.pdf")
            SaveContactSheet(thumbnails, contactSheet)
            Console.WriteLine("Contact sheet with {0} thumbnails: {1}", thumbnails.Length, contactSheet)

            ' the document information is available after a conversion
            Dim info As DocumentInformation = converter.DocumentInformation
            If info IsNot Nothing Then Console.WriteLine("Title: {0}, author: {1}", info.Title, info.Author)

            ' 2. One page as the bytes of a JPEG file, for a web response or a database. The StartPageNumber
            '    and EndPageNumber properties are not used.
            converter.Resolution = 96
            converter.JpegQuality = 80
            Dim jpeg As Byte() = converter.ConvertPageToImageBytes(New MemoryStream(pdfBytes), 1, ImageFormat.Jpeg)
            Dim jpegFile As String = Path.Combine(appFolder, "page-1.jpg")
            File.WriteAllBytes(jpegFile, jpeg)
            Console.WriteLine("Page 1 as JPEG (96 DPI, quality 80, {0:N0} bytes): {1}", jpeg.Length, jpegFile)

            ' 3. A gray multi-page TIFF document of pages 2 to 4, for an archive or a fax service.
            '    ConvertToTiff returns the bytes of the TIFF file: store them anywhere.
            converter.Resolution = 150
            converter.ColorSpace = PdfToImageColorSpace.Gray
            converter.StartPageNumber = 2
            converter.EndPageNumber = 4
            Dim tiff As Byte() = converter.ConvertToTiff(New MemoryStream(pdfBytes))
            Dim tiffFile As String = Path.Combine(appFolder, "pages-2-4.tiff")
            File.WriteAllBytes(tiffFile, tiff)
            Console.WriteLine("Multi-page TIFF of pages 2 to 4 (150 DPI, gray, {0:N0} bytes): {1}", tiff.Length, tiffFile)
        Catch ex As PdfToImageException
            Console.WriteLine("The conversion failed: " & ex.Message)
            Return 2
        End Try

        Return 0
    End Function

    ' places the page images side by side on the pages of a PDF document, five in a row
    Private Sub SaveContactSheet(pages As Byte()(), file As String)
        Const cell As Single = 100
        Dim sheet As New ExpertPdf.Anywhere.PdfDocument.Document()
        Try
            Dim page As ExpertPdf.Anywhere.PdfDocument.PdfPage = Nothing
            For i As Integer = 0 To pages.Length - 1
                Dim onPage As Integer = i Mod 35
                If onPage = 0 Then page = sheet.AddPage()
                ' each image at its own size (pixels at 96 DPI), in a cell of the grid
                page.AddElement(New ExpertPdf.Anywhere.PdfDocument.ImageElement((onPage Mod 5) * cell,
                    (onPage \ 5) * cell, pages(i)))
            Next
            sheet.Save(file)
        Finally
            sheet.Close()
        End Try
    End Sub

End Module
