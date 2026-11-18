Imports System.IO
Imports ExpertPdf.Anywhere.PDFSplit

''' <summary>
''' Shows the features of the split library: page ranges and chunks, from and to files or memory,
''' document information and security for the new documents, password protected sources, one document
''' per chapter (bookmark), page lists, odd and even pages, and a maximum file size.
''' The page numbers start at 1.
''' </summary>
Module Program

    Private ReadOnly sampleFolder As String = AppDomain.CurrentDomain.BaseDirectory
    Private ReadOnly report As String = Path.Combine(sampleFolder, "report.pdf")   ' 6 pages, "Page N of 6" on each page
    Private ReadOnly outFolder As String = Path.Combine(sampleFolder, "SplitOutput")

    Function Main(args As String()) As Integer
        Try
            ' the new documents go to their own folder, emptied at each run
            If Directory.Exists(outFolder) Then
                Directory.Delete(outFolder, True)
            End If
            Directory.CreateDirectory(outFolder)

            Dim splitManager As New PDFSplitManager()
            ' splitManager.LicenseKey = "your license key"

            ' the page count, from a file or from memory
            Dim reportBytes As Byte() = File.ReadAllBytes(report)
            Console.WriteLine("report.pdf: {0} pages ({1} pages from memory)", splitManager.GetPdfPageCount(report),
                splitManager.GetPdfPageCount(reportBytes))
            Console.WriteLine()

            ExtractRange(splitManager)
            ChunksToMemory(splitManager, reportBytes)
            MemoryToFile(splitManager, reportBytes)
            ProtectAndSplitProtected()
            SplitAtChapters(splitManager)
            PageListsAndOddEven(splitManager, reportBytes)
            SplitBySize(splitManager)

            Console.WriteLine()
            Console.WriteLine("The new documents are in " & outFolder)
            Return 0
        Catch ex As Exception
            Console.WriteLine("Error: " & ex.Message)
            Return 1
        End Try
    End Function

    ' A range of pages from a file into a new file: pages 2 to 4.
    Private Sub ExtractRange(splitManager As PDFSplitManager)
        Dim outFile As String = Path.Combine(outFolder, "Pages-2-4.pdf")
        splitManager.ExtractPagesFromFileToFile(report, outFile, 2, 3)
        PrintResult(splitManager, outFile)
    End Sub

    ' Split into documents kept in memory: no temporary files, the bytes go wherever you need them
    ' (a database, a web response, another ExpertPdf library).
    Private Sub ChunksToMemory(splitManager As PDFSplitManager, reportBytes As Byte())
        ' three documents of one page, starting with page 1
        Dim onePageDocs As PDFSplitDoc() = splitManager.ExtractPagesFromFileToMemory(report, 1, 1, 3)
        For i As Integer = 0 To onePageDocs.Length - 1
            Dim outFile As String = Path.Combine(outFolder, String.Format("Page-{0}.pdf", i + 1))
            onePageDocs(i).SaveToFile(outFile)
            PrintResult(splitManager, outFile)
        Next

        ' documents of 4 pages until the end of the document: pages 1-4 and 5-6
        Dim chunks As PDFSplitDoc() = splitManager.ExtractPagesFromMemoryToMemory(reportBytes, 1, 4, True)
        For Each chunk As PDFSplitDoc In chunks
            Console.WriteLine("  chunk in memory: {0} bytes, {1} page(s)", chunk.Buffer.Length, splitManager.GetPdfPageCount(chunk.Buffer))
        Next

        ' a single range in memory: the last page
        Dim lastPage As PDFSplitDoc = splitManager.ExtractPagesFromMemoryToMemory(reportBytes, 6, 1)
        File.WriteAllBytes(Path.Combine(outFolder, "LastPage.pdf"), lastPage.Buffer)
        PrintResult(splitManager, Path.Combine(outFolder, "LastPage.pdf"))
    End Sub

    ' From memory to files. With a repeat count or "repeat to end", each document is saved as
    ' <file name>_<index>.pdf.
    Private Sub MemoryToFile(splitManager As PDFSplitManager, reportBytes As Byte())
        ' two documents of 3 pages: Half_1.pdf (pages 1-3) and Half_2.pdf (pages 4-6)
        splitManager.ExtractPagesFromMemoryToFile(reportBytes, Path.Combine(outFolder, "Half.pdf"), 1, 3, 2)
        PrintResult(splitManager, Path.Combine(outFolder, "Half_1.pdf"))
        PrintResult(splitManager, Path.Combine(outFolder, "Half_2.pdf"))

        ' documents of 2 pages from page 3 to the end: Pair_1.pdf (pages 3-4) and Pair_2.pdf (pages 5-6)
        splitManager.ExtractPagesFromMemoryToFile(reportBytes, Path.Combine(outFolder, "Pair.pdf"), 3, 2, True)
        PrintResult(splitManager, Path.Combine(outFolder, "Pair_1.pdf"))
        PrintResult(splitManager, Path.Combine(outFolder, "Pair_2.pdf"))
    End Sub

    ' The new documents can have their own document information and security. A protected document is
    ' split by giving its password in SourcePdfPassword.
    Private Sub ProtectAndSplitProtected()
        Dim options As New PdfDocumentOptions()
        options.PdfCompressionLevel = PDFCompressionLevel.Best
        Dim protector As New PDFSplitManager(options)

        protector.PdfDocumentInfo.Title = "Report - first part"
        protector.PdfDocumentInfo.AuthorName = "Sample Company"
        protector.PdfDocumentInfo.Subject = "Pages 1 to 3 of the annual report"
        protector.PdfDocumentInfo.Keywords = "report, split"
        protector.PdfDocumentInfo.CreatedDate = DateTime.Now

        ' a password to open the documents, and no printing for the readers who open them with it
        protector.PdfSecurityOptions.UserPassword = "secret"
        protector.PdfSecurityOptions.OwnerPassword = "owner"
        protector.PdfSecurityOptions.CanPrint = False
        protector.PdfSecurityOptions.KeySize = EncryptionKeySize.EncryptKey256Bit
        protector.PdfSecurityOptions.Algorithm = EncryptionAlgorithm.AES

        Dim protectedFile As String = Path.Combine(outFolder, "Protected-1-3.pdf")
        protector.ExtractPagesFromFileToFile(report, protectedFile, 1, 3)
        Console.WriteLine("Protected-1-3.pdf: pages 1-3, AES 256 bit, open password ""secret"", printing not allowed")

        ' split the protected document: without SourcePdfPassword it could not be opened
        Dim splitter As New PDFSplitManager()
        splitter.SourcePdfPassword = "secret"
        Dim outFile As String = Path.Combine(outFolder, "FromProtected-Page-2.pdf")
        splitter.ExtractPagesFromFileToFile(protectedFile, outFile, 2, 1)
        PrintResult(splitter, outFile)
    End Sub

    ' One document per chapter: the top level bookmarks of report.pdf ("1. Summary", "2. Sales by region"...).
    ' The files are named after the bookmarks; in memory, PDFSplitDoc.Title gives the bookmark title.
    Private Sub SplitAtChapters(splitManager As PDFSplitManager)
        Dim chaptersFolder As String = Path.Combine(outFolder, "Chapters")
        Dim files As String() = splitManager.SplitByBookmarksFromFileToFolder(report, chaptersFolder)
        For Each chapterFile As String In files
            PrintResult(splitManager, chapterFile)
        Next

        Dim chapters As PDFSplitDoc() = splitManager.SplitByBookmarksFromFileToMemory(report)
        For Each chapter As PDFSplitDoc In chapters
            Console.WriteLine("  chapter in memory: ""{0}"", pages {1}", chapter.Title, String.Join(",", Array.ConvertAll(chapter.PageNumbers, Function(n) n.ToString())))
        Next
    End Sub

    ' A list of pages ("1-3,7,10-": page numbers start at 1, "10-" goes to the end, the pages are taken in
    ' the order of the list) and the odd or even pages.
    Private Sub PageListsAndOddEven(splitManager As PDFSplitManager, reportBytes As Byte())
        Dim selected As String = Path.Combine(outFolder, "Pages-6-1-to-3.pdf")
        splitManager.ExtractPagesFromFileToFile(report, selected, "6,1-3")
        PrintResult(splitManager, selected)

        Dim odd As String = Path.Combine(outFolder, "OddPages.pdf")
        splitManager.ExtractPagesFromFileToFile(report, odd, PageSelection.OddPages)
        PrintResult(splitManager, odd)

        Dim even As PDFSplitDoc = splitManager.ExtractPagesFromMemoryToMemory(reportBytes, PageSelection.EvenPages)
        File.WriteAllBytes(Path.Combine(outFolder, "EvenPages.pdf"), even.Buffer)
        PrintResult(splitManager, Path.Combine(outFolder, "EvenPages.pdf"))
    End Sub

    ' Documents that each stay under a maximum file size (for example an e-mail attachment limit). Each
    ' document takes as many pages as fit; a page larger than the limit on its own makes a document of its own.
    Private Sub SplitBySize(splitManager As PDFSplitManager)
        Dim maxSize As Long = 200 * 1024    ' 200 KB
        Dim parts As String() = splitManager.SplitBySizeFromFileToFile(report, Path.Combine(outFolder, "BySize.pdf"), maxSize)
        For Each part As String In parts
            Console.WriteLine("{0}: {1} page(s), {2} bytes (limit {3})", Path.GetFileName(part), splitManager.GetPdfPageCount(part), New FileInfo(part).Length, maxSize)
        Next
    End Sub

    Private Sub PrintResult(splitManager As PDFSplitManager, fileName As String)
        Console.WriteLine("{0}: {1} page(s)", Path.GetFileName(fileName), splitManager.GetPdfPageCount(fileName))
    End Sub

End Module
