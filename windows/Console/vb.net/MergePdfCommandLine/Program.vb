Imports System.Collections.Generic
Imports System.IO
Imports ExpertPdf.Anywhere.MergePdf

''' <summary>
''' A command line tool that merges PDF documents, images and empty pages into one PDF document.
''' Run it without arguments to merge the documents that come with the sample.
''' </summary>
Module Program

    ' one element of the merged document, in the order given on the command line
    Private Class MergeItem
        Public Kind As String   ' "pdf", "image" or "emptypage"
        Public Path As String
    End Class

    Function Main(args As String()) As Integer
        Dim sampleFolder As String = AppDomain.CurrentDomain.BaseDirectory

        Dim items As New List(Of MergeItem)()
        Dim outFile As String = Nothing
        Dim options As New PdfDocumentOptions()
        Dim title As String = Nothing
        Dim userPassword As String = Nothing
        Dim ownerPassword As String = Nothing
        Dim documentBookmarks As Boolean = False
        Dim booklet As Boolean = False
        Dim twoSided As Boolean = False

        If args.Length = 0 Then
            ' no arguments: show the syntax, then merge the bundled documents
            ShowUsage()
            Console.WriteLine()
            Console.WriteLine("No arguments given: merging the sample documents.")
            items.Add(New MergeItem With {.Kind = "pdf", .Path = IO.Path.Combine(sampleFolder, "report.pdf")})
            items.Add(New MergeItem With {.Kind = "image", .Path = IO.Path.Combine(sampleFolder, "logo.jpg")})
            items.Add(New MergeItem With {.Kind = "emptypage"})
            items.Add(New MergeItem With {.Kind = "pdf", .Path = IO.Path.Combine(sampleFolder, "invoice.pdf")})
            title = "Report and invoice"
            documentBookmarks = True
            outFile = IO.Path.Combine(sampleFolder, "MergedDocument.pdf")
        End If

        Try
            For Each argument As String In args
                Dim value As String = Nothing
                If TryGetValue(argument, "/pdf:", value) Then
                    items.Add(New MergeItem With {.Kind = "pdf", .Path = IO.Path.GetFullPath(value)})
                ElseIf TryGetValue(argument, "/image:", value) Then
                    items.Add(New MergeItem With {.Kind = "image", .Path = IO.Path.GetFullPath(value)})
                ElseIf argument.Equals("/emptypage", StringComparison.OrdinalIgnoreCase) Then
                    items.Add(New MergeItem With {.Kind = "emptypage"})
                ElseIf TryGetValue(argument, "/out:", value) Then
                    outFile = IO.Path.GetFullPath(value)
                ElseIf TryGetValue(argument, "/pagesize:", value) Then
                    ' the page size and orientation apply to the pages the library creates (images and empty
                    ' pages); the pages of the appended PDF documents keep their own size
                    options.PdfPageSize = ParseEnum(Of PdfPageSize)(value, "page size")
                ElseIf TryGetValue(argument, "/orientation:", value) Then
                    options.PdfPageOrientation = ParseEnum(Of PDFPageOrientation)(value, "page orientation")
                ElseIf TryGetValue(argument, "/compression:", value) Then
                    options.PdfCompressionLevel = ParseEnum(Of PDFCompressionLevel)(value, "compression level")
                ElseIf TryGetValue(argument, "/standard:", value) Then
                    options.PdfStandardSubset = ParseEnum(Of PdfStandardSubset)(value, "PDF standard")
                ElseIf TryGetValue(argument, "/title:", value) Then
                    title = value
                ElseIf TryGetValue(argument, "/userpswd:", value) Then
                    userPassword = value
                ElseIf TryGetValue(argument, "/ownerpswd:", value) Then
                    ownerPassword = value
                ElseIf argument.Equals("/bookmarks", StringComparison.OrdinalIgnoreCase) Then
                    documentBookmarks = True
                ElseIf argument.Equals("/booklet", StringComparison.OrdinalIgnoreCase) Then
                    booklet = True
                ElseIf argument.Equals("/booklet:twosided", StringComparison.OrdinalIgnoreCase) Then
                    booklet = True
                    twoSided = True
                Else
                    Throw New ArgumentException("Unknown argument: " & argument)
                End If
            Next

            If items.Count = 0 Then
                Throw New ArgumentException("Nothing to merge: give at least one /pdf:, /image: or /emptypage argument.")
            End If
            If outFile Is Nothing Then
                outFile = IO.Path.GetFullPath("MergedDocument.pdf")
            End If
        Catch ex As ArgumentException
            Console.WriteLine(ex.Message)
            Console.WriteLine()
            ShowUsage()
            Return 1
        End Try

        Try
            ' the options are read when the merge object is created: set them first
            Dim pdfMerge As New PDFMerge(options)
            ' pdfMerge.LicenseKey = "your license key"

            If title IsNot Nothing Then
                pdfMerge.DocumentInfo.Title = title
            End If
            pdfMerge.DocumentInfo.Application = "ExpertPdf MergePdf command line sample"

            If Not String.IsNullOrEmpty(userPassword) Then
                pdfMerge.SecurityOptions.UserPassword = userPassword
            End If
            If Not String.IsNullOrEmpty(ownerPassword) Then
                pdfMerge.SecurityOptions.OwnerPassword = ownerPassword
            End If

            ' one bookmark for each merged document or image file (named after the file), with the
            ' bookmarks of the document nested under it
            If documentBookmarks Then
                pdfMerge.BookmarkOptions.AddDocumentBookmarks = True
            End If
            ' a booklet: two pages on each landscape sheet of the page size, folded in the middle
            If booklet Then
                pdfMerge.BookletOptions.Enabled = True
                pdfMerge.BookletOptions.PageSize = options.PdfPageSize
                pdfMerge.BookletOptions.TwoSided = twoSided
            End If

            For Each item As MergeItem In items
                Select Case item.Kind
                    Case "pdf"
                        pdfMerge.AppendPDFFile(item.Path)
                        Console.WriteLine("  PDF    {0} ({1} pages)", IO.Path.GetFileName(item.Path), PDFMerge.GetPageCount(item.Path))
                    Case "image"
                        ' each image is placed on a new page
                        pdfMerge.AppendImageFile(item.Path)
                        Console.WriteLine("  image  {0}", IO.Path.GetFileName(item.Path))
                    Case Else
                        pdfMerge.AppendEmptyPage()
                        Console.WriteLine("  empty page")
                End Select
            Next

            ' count the pages before saving: a password protected result cannot be opened without its password
            Dim pageCount As Integer = pdfMerge.GetCurrentPageCount()
            pdfMerge.SaveMergedPDFToFile(outFile)

            Console.WriteLine()
            Console.WriteLine("Merged document: {0} ({1} pages, {2})", outFile, pageCount, options.PdfStandardSubset)
            If booklet Then
                Console.WriteLine("Booklet: {0} sheets", pdfMerge.GetCurrentPageCount())
            End If
            Return 0
        Catch ex As Exception
            ' MergePDFException for documents that cannot be read, IO exceptions for missing files
            Console.WriteLine("Could not merge the documents: " & ex.Message)
            Return 2
        End Try
    End Function

    Private Function TryGetValue(argument As String, name As String, ByRef value As String) As Boolean
        value = Nothing
        If Not argument.StartsWith(name, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If
        value = argument.Substring(name.Length).Trim().Trim(""""c)
        If value.Length = 0 Then
            Throw New ArgumentException("Missing value for " & name)
        End If
        Return True
    End Function

    Private Function ParseEnum(Of T)(value As String, what As String) As T
        Try
            Return CType([Enum].Parse(GetType(T), value, True), T)
        Catch ex As ArgumentException
            Throw New ArgumentException(String.Format("Invalid {0}: {1}. Valid values: {2}", what, value,
                String.Join(", ", [Enum].GetNames(GetType(T)))))
        End Try
    End Function

    Private Sub ShowUsage()
        Console.WriteLine("Usage: MergePdfCommandLine {/pdf:file | /image:file | /emptypage}+ [/out:file]")
        Console.WriteLine("           [/pagesize:A4|Letter|...] [/orientation:Portrait|Landscape]")
        Console.WriteLine("           [/compression:NoCompression|BestSpeed|BelowNormal|Normal|AboveNormal|Best]")
        Console.WriteLine("           [/standard:Full|Pdf_A_1b|Pdf_A_2b|Pdf_A_3b|Pdf_A_3u|Pdf_X_1a]")
        Console.WriteLine("           [/title:text] [/userpswd:password] [/ownerpswd:password]")
        Console.WriteLine("           [/bookmarks] [/booklet | /booklet:twosided]")
        Console.WriteLine()
        Console.WriteLine("The documents, images and empty pages are merged in the order you give them.")
        Console.WriteLine("Page size and orientation apply to the image and empty pages.")
        Console.WriteLine("/bookmarks adds a bookmark for each document, named after the file.")
        Console.WriteLine("/booklet puts two pages on each landscape sheet of the page size, in booklet order.")
        Console.WriteLine("Example: MergePdfCommandLine /pdf:""C:\My Documents\Report.pdf"" /emptypage /image:chart.png /out:Merged.pdf")
    End Sub

End Module
