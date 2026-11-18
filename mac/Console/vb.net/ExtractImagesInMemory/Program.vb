Imports System
Imports System.Collections.Generic
Imports System.IO
Imports ExpertPdf.Anywhere.Drawing
Imports ExpertPdf.Anywhere

''' <summary>
''' Extracts the images of a PDF document into memory, in one list, and keeps only the photos and charts:
''' small images (logos, icons, bullets) and repeated images are skipped by the extractor. The PDF document is
''' read from memory, as it would come from a database or an upload. Run it without arguments to process the
''' sample.pdf document that comes with the sample.
''' </summary>
Module Program

    ' images smaller than this (in pixels) are decorations, not content
    Private Const MinimumSize As Integer = 100

    Function Main(args As String()) As Integer
        Dim appFolder As String = AppDomain.CurrentDomain.BaseDirectory
        Dim pdfFile As String = If(args.Length > 0, args(0), Path.Combine(appFolder, "sample.pdf"))
        Dim pdfBytes As Byte() = File.ReadAllBytes(pdfFile)

        Dim outFolder As String = Path.Combine(appFolder, "photos")
        Directory.CreateDirectory(outFolder)

        Dim extractor As New PdfImgExtract()

        ' Without a license key the extracted images have an evaluation notice.
        ' extractor.LicenseKey = "your license key"

        Dim pageCount As Integer = extractor.GetPageCount(New MemoryStream(pdfBytes))
        Console.WriteLine("{0}: {1} pages", Path.GetFileName(pdfFile), pageCount)

        ' an image narrower or shorter than this is skipped
        extractor.MinimumWidth = MinimumSize
        extractor.MinimumHeight = MinimumSize

        ' an image with the same pixels as one extracted before (a logo on every page) is skipped
        extractor.SkipDuplicateImages = True

        ' the format of the extracted image files: here JPEG
        extractor.ExtractImageFormat = ImageFormat.Jpeg

        Dim images As List(Of ExtractedImage)
        Try
            ' all the images in one list, with the page of each image; nothing is saved by the extractor
            images = extractor.ExtractImagesToMemory(New MemoryStream(pdfBytes))
        Catch ex As PdfImgExtractException
            Console.WriteLine("The extraction failed: " & ex.Message)
            Return 2
        End Try

        ' the number of images kept on each page
        Dim imagesPerPage As New SortedDictionary(Of Integer, Integer)()
        For Each image As ExtractedImage In images
            ' your own file names: here named after the page and the position on the page. Image holds the
            ' bytes of the image file, in the ExtractImageFormat format
            Dim imageFile As String = Path.Combine(outFolder, String.Format("page{0}-{1}.jpg", image.PageNumber,
                image.IndexOnPage + 1))
            File.WriteAllBytes(imageFile, image.Image)
            Console.WriteLine("  page {0}: saved {1} ({2:N0} bytes)", image.PageNumber, Path.GetFileName(imageFile),
                image.Image.Length)

            Dim found As Integer
            imagesPerPage.TryGetValue(image.PageNumber, found)
            imagesPerPage(image.PageNumber) = found + 1
        Next

        Console.WriteLine()
        For page As Integer = 1 To pageCount
            Dim found As Integer
            imagesPerPage.TryGetValue(page, found)
            Console.WriteLine("Page {0}: {1} image(s) of at least {2} x {2} pixels", page, found, MinimumSize)
        Next
        Console.WriteLine("{0} image(s) saved in {1}", images.Count, outFolder)
        Return 0
    End Function

End Module
