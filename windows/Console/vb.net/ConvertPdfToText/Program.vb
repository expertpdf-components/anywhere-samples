Imports System
Imports System.IO
Imports System.Text
Imports ExpertPdf.Anywhere

''' <summary>
''' A command line tool that extracts the text of a PDF document to a text file or to a HTML file.
''' Run it without arguments to convert the sample.pdf document that comes with the sample.
''' </summary>
Module Program

    Private ReadOnly AppFolder As String = AppDomain.CurrentDomain.BaseDirectory

    Function Main(args As String()) As Integer
        ' the defaults: the bundled sample document, the original layout, a .txt file next to the program
        Dim pdfFile As String = Path.Combine(AppFolder, "sample.pdf")
        Dim outFile As String = Nothing
        Dim password As String = Nothing
        Dim layout As TextLayout = TextLayout.OriginalLayout
        Dim startPage As Integer = 1
        Dim endPage As Integer = 0
        Dim markPageBreaks As Boolean = False
        Dim html As Boolean = False
        Dim htmlCharset As String = "utf-8"
        Dim clipText As Boolean = True
        Dim perPage As Boolean = False

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
                Case "/out" : outFile = Path.GetFullPath(value)
                Case "/userpswd" : password = value
                Case "/start" : startPage = Integer.Parse(value)
                Case "/end" : endPage = Integer.Parse(value)
                Case "/pagebreaks" : markPageBreaks = True
                Case "/noclip" : clipText = False
                Case "/pages" : perPage = True
                Case "/html"
                    html = True
                    If Not String.IsNullOrEmpty(value) Then htmlCharset = value
                Case "/layout"
                    If value = "reading" Then
                        layout = TextLayout.ReadingLayout
                    ElseIf value = "original" Then
                        layout = TextLayout.OriginalLayout
                    Else
                        Return Usage("Unknown layout: " & value)
                    End If
                Case "/?", "/help"
                    Return Usage(Nothing)
                Case Else
                    Return Usage("Unknown argument: " & arg)
            End Select
        Next

        If Not File.Exists(pdfFile) Then Return Usage("The PDF file was not found: " & pdfFile)

        If outFile Is Nothing Then
            outFile = Path.Combine(AppFolder, Path.GetFileNameWithoutExtension(pdfFile) & If(html, ".html", ".txt"))
        End If

        Dim converter As New PdfToTextConverter()

        ' Without a license key the text of the first 5 pages is extracted and an evaluation notice is added.
        ' converter.LicenseKey = "your license key"

        ' OriginalLayout keeps the columns of a page side by side, as they are printed;
        ' ReadingLayout puts the text in reading order (a whole column, then the next one) - better for indexing
        converter.Layout = layout

        ' the pages to convert: EndPageNumber = 0 means "up to the last page"
        converter.StartPageNumber = startPage
        converter.EndPageNumber = endPage

        ' a form feed character (PAGE_BREAK_MARK) after the text of each page lets you split the result into pages
        converter.MarkPageBreaks = markPageBreaks

        ' text hidden by a clipping path is not visible on the page; ClipText = False extracts it too
        converter.ClipText = clipText

        ' a HTML document with the title, author, subject and keywords of the PDF document as meta tags
        converter.AddHtmlMetaTags = html
        converter.HtmlCharset = htmlCharset

        ' the password needed to open a protected document (Nothing for a document without a password)
        converter.UserPassword = password

        Try
            If perPage Then Return SavePages(converter, pdfFile, outFile)

            Dim text As String = converter.ConvertToText(pdfFile)

            ' the HTML charset and the encoding of the file must match
            File.WriteAllText(outFile, text, If(html, Encoding.GetEncoding(htmlCharset), Encoding.UTF8))

            ' the document information is available after the conversion
            Dim info As DocumentInformation = converter.DocumentInformation
            Console.WriteLine("Converted: " & pdfFile)
            If info IsNot Nothing Then
                Console.WriteLine("  Title:    " & info.Title)
                Console.WriteLine("  Author:   " & info.Author)
                Console.WriteLine("  Subject:  " & info.Subject)
                Console.WriteLine("  Keywords: " & info.Keywords)
                Console.WriteLine("  Pages:    " & info.PageCount)
            End If
            Console.WriteLine("  Layout:   " & layout.ToString() & If(markPageBreaks, ", page breaks marked", "") & If(clipText, "", ", hidden text included"))
            Console.WriteLine("Output: " & outFile & " (" & text.Length & " characters)")
            Return 0
        Catch ex As PdfToTextException
            Console.WriteLine("The conversion failed: " & ex.Message)
            Return 2
        End Try
    End Function

    ' the text of each page in a file of its own: <name>-page1.txt, <name>-page2.txt, ...
    Private Function SavePages(converter As PdfToTextConverter, pdfFile As String, outFile As String) As Integer
        ' one call, one string for each page (from StartPageNumber to EndPageNumber)
        Dim pages As String() = converter.ConvertToTextPages(pdfFile)

        Dim baseName As String = IO.Path.Combine(IO.Path.GetDirectoryName(outFile), IO.Path.GetFileNameWithoutExtension(outFile))
        Console.WriteLine("Converted: " & pdfFile)
        For i As Integer = 0 To pages.Length - 1
            Dim pageNumber As Integer = converter.StartPageNumber + i
            Dim pageFile As String = baseName & "-page" & pageNumber & ".txt"
            File.WriteAllText(pageFile, pages(i), Encoding.UTF8)
            Console.WriteLine("  page {0}: {1} ({2} characters)", pageNumber, pageFile, pages(i).Length)
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
        Console.WriteLine("Usage: ConvertPdfToText [/pdf:file.pdf] [/out:file] [/layout:original|reading] [/start:n] [/end:n]")
        Console.WriteLine("                        [/pagebreaks] [/pages] [/html[:charset]] [/noclip] [/userpswd:password]")
        Console.WriteLine("/pages saves the text of each page in a file of its own (sample-page1.txt, ...).")
        Console.WriteLine("Without arguments the sample.pdf document is converted to sample.txt.")
        Console.WriteLine("Examples:")
        Console.WriteLine("  ConvertPdfToText /pdf:sample.pdf /layout:reading /pagebreaks")
        Console.WriteLine("  ConvertPdfToText /pdf:sample.pdf /html /start:2 /end:3")
        Console.WriteLine("  ConvertPdfToText /pdf:sample.pdf /pages /layout:reading")
        Console.WriteLine("  ConvertPdfToText /pdf:sample-protected.pdf /userpswd:open")
        Return If(errorMessage Is Nothing, 0, 1)
    End Function

End Module
