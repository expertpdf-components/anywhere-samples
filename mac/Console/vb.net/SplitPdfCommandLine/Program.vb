Imports System.IO
Imports ExpertPdf.Anywhere.PDFSplit

''' <summary>
''' A command line tool that splits a PDF document into documents of a given number of pages.
''' Run it without arguments to split the document that comes with the sample.
''' </summary>
Module Program

    Function Main(args As String()) As Integer
        Dim sampleFolder As String = AppDomain.CurrentDomain.BaseDirectory

        Dim srcPdfFile As String = Nothing
        Dim outFile As String = Nothing
        Dim password As String = Nothing
        Dim startPageNumber As Integer = 1
        Dim splitPageCount As Integer = 1
        Dim repeatCount As Integer = 0      ' 0 = repeat to the end of the document
        ' the other ways to split: at the chapters, a page list, the odd or even pages, a maximum file size
        Dim atBookmarks As Boolean = False
        Dim pageList As String = Nothing
        Dim selection As String = Nothing   ' "odd" or "even"
        Dim maxSizeKB As Integer = 0

        If args.Length = 0 Then
            ' no arguments: show the syntax, then split the bundled document in documents of 2 pages
            ShowUsage()
            Console.WriteLine()
            Console.WriteLine("No arguments given: splitting report.pdf in documents of 2 pages.")
            srcPdfFile = Path.Combine(sampleFolder, "report.pdf")
            outFile = Path.Combine(sampleFolder, "SplitDocument.pdf")
            splitPageCount = 2
        End If

        Try
            For Each argument As String In args
                Dim value As String = Nothing
                If TryGetValue(argument, "/pdf:", value) Then
                    srcPdfFile = Path.GetFullPath(value)
                ElseIf TryGetValue(argument, "/out:", value) Then
                    outFile = Path.GetFullPath(value)
                ElseIf TryGetValue(argument, "/startpage:", value) Then
                    startPageNumber = ParsePositive(value, "start page number")
                ElseIf TryGetValue(argument, "/splitsize:", value) Then
                    splitPageCount = ParsePositive(value, "split size")
                ElseIf TryGetValue(argument, "/repeatcount:", value) Then
                    repeatCount = ParsePositive(value, "repeat count")
                ElseIf TryGetValue(argument, "/password:", value) Then
                    password = value
                ElseIf argument.Equals("/bookmarks", StringComparison.OrdinalIgnoreCase) Then
                    atBookmarks = True
                ElseIf TryGetValue(argument, "/pages:", value) Then
                    pageList = value
                ElseIf argument.Equals("/odd", StringComparison.OrdinalIgnoreCase) OrElse argument.Equals("/even", StringComparison.OrdinalIgnoreCase) Then
                    selection = argument.Substring(1).ToLowerInvariant()
                ElseIf TryGetValue(argument, "/maxsize:", value) Then
                    maxSizeKB = ParsePositive(value, "maximum size")
                Else
                    Throw New ArgumentException("Unknown argument: " & argument)
                End If
            Next
            If srcPdfFile Is Nothing Then
                Throw New ArgumentException("The source PDF document is not specified.")
            End If
            If outFile Is Nothing Then
                outFile = Path.GetFullPath("SplitDocument.pdf")
            End If
        Catch ex As ArgumentException
            Console.WriteLine(ex.Message)
            Console.WriteLine()
            ShowUsage()
            Return 1
        End Try

        Try
            Dim splitManager As New PDFSplitManager()
            ' splitManager.LicenseKey = "your license key"

            ' the user or the owner password of a protected source document
            splitManager.SourcePdfPassword = password

            ' the page numbers start at 1
            Dim pageCount As Integer = splitManager.GetPdfPageCount(File.ReadAllBytes(srcPdfFile))
            Console.WriteLine("{0}: {1} pages", Path.GetFileName(srcPdfFile), pageCount)

            If atBookmarks Then
                ' one document per top level bookmark, named after it, in a folder named after the out file
                Dim folder As String = Path.Combine(Path.GetDirectoryName(outFile), Path.GetFileNameWithoutExtension(outFile))
                For Each chapter As String In splitManager.SplitByBookmarksFromFileToFolder(srcPdfFile, folder)
                    Console.WriteLine("  {0} ({1} page(s))", chapter, splitManager.GetPdfPageCount(chapter))
                Next
                Return 0
            End If
            If pageList IsNot Nothing OrElse selection IsNot Nothing Then
                ' one document with the listed pages ("1-3,7,10-"), or with the odd or even pages
                If pageList IsNot Nothing Then
                    splitManager.ExtractPagesFromFileToFile(srcPdfFile, outFile, pageList)
                Else
                    splitManager.ExtractPagesFromFileToFile(srcPdfFile, outFile, If(selection = "odd", PageSelection.OddPages, PageSelection.EvenPages))
                End If
                Console.WriteLine("  {0} ({1} page(s))", outFile, splitManager.GetPdfPageCount(outFile))
                Return 0
            End If
            If maxSizeKB > 0 Then
                ' documents of at most maxsize KB each: <out name>_1.pdf, <out name>_2.pdf...
                For Each part As String In splitManager.SplitBySizeFromFileToFile(srcPdfFile, outFile, maxSizeKB * 1024L)
                    Console.WriteLine("  {0} ({1} page(s), {2} KB)", part, splitManager.GetPdfPageCount(part), New FileInfo(part).Length \ 1024)
                Next
                Return 0
            End If

            ' each new document is saved as <out name>_<index>.pdf: SplitDocument_1.pdf, SplitDocument_2.pdf...
            If repeatCount = 0 Then
                splitManager.ExtractPagesFromFileToFile(srcPdfFile, outFile, startPageNumber, splitPageCount, True)
            Else
                splitManager.ExtractPagesFromFileToFile(srcPdfFile, outFile, startPageNumber, splitPageCount, repeatCount)
            End If

            ' list the documents that were created
            Dim parts As Integer = (pageCount - startPageNumber + splitPageCount) \ splitPageCount
            If repeatCount > 0 AndAlso repeatCount < parts Then
                parts = repeatCount
            End If
            Dim basePath As String = Path.Combine(Path.GetDirectoryName(outFile), Path.GetFileNameWithoutExtension(outFile))
            For index As Integer = 1 To parts
                Dim part As String = String.Format("{0}_{1}{2}", basePath, index, Path.GetExtension(outFile))
                Console.WriteLine("  {0} ({1} page(s))", part, splitManager.GetPdfPageCount(part))
            Next
            Return 0
        Catch ex As Exception
            ' SplitPDFException for documents that cannot be read (or a wrong password), IO exceptions for missing files
            Console.WriteLine("Could not split the document: " & ex.Message)
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

    Private Function ParsePositive(value As String, what As String) As Integer
        Dim number As Integer
        If Not Integer.TryParse(value, number) OrElse number < 1 Then
            Throw New ArgumentException(String.Format("Invalid {0}: {1}", what, value))
        End If
        Return number
    End Function

    Private Sub ShowUsage()
        Console.WriteLine("Usage: SplitPdfCommandLine /pdf:source_file [/startpage:number] [/splitsize:pages]")
        Console.WriteLine("           [/repeatcount:count] [/password:source_password] [/out:file]")
        Console.WriteLine("       SplitPdfCommandLine /pdf:source_file {/bookmarks | /pages:list | /odd | /even | /maxsize:KB}")
        Console.WriteLine("           [/password:source_password] [/out:file]")
        Console.WriteLine()
        Console.WriteLine("/bookmarks: one document per chapter (top level bookmark), saved in the folder <out>\")
        Console.WriteLine("/pages:1-3,7,10- or /odd or /even: one document with these pages, saved as <out>")
        Console.WriteLine("/maxsize:KB: documents of at most KB kilobytes each, saved as <out>_1.pdf, <out>_2.pdf, ...")
        Console.WriteLine("By default the split starts at the first page, makes documents of 1 page and repeats")
        Console.WriteLine("to the end of the document. The documents are saved as <out>_1.pdf, <out>_2.pdf, ...")
        Console.WriteLine("Example: SplitPdfCommandLine /pdf:""C:\My Documents\Report.pdf"" /startpage:3 /splitsize:2 /repeatcount:2 /out:Part.pdf")
    End Sub

End Module
