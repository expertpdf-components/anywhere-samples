Imports System.Threading
Imports ExpertPdf.Anywhere.HtmlToPdf
Imports ExpertPdf.Anywhere.PdfDocument

''' <summary>
''' Converts a web page, an HTML file or an HTML string to PDF (or to an image) and exposes the most used
''' converter settings as command line options. Run it without arguments to convert the bundled sample.html.
''' </summary>
Friend Module Program

    Function Main(args As String()) As Integer
        If HasFlag(args, "--help") OrElse HasFlag(args, "-h") OrElse HasFlag(args, "/?") Then
            PrintUsage()
            Return 0
        End If

        Dim options As Options
        Try
            options = Options.Parse(args)
        Catch ex As ArgumentException
            Console.Error.WriteLine("Argument error: " & ex.Message)
            Console.Error.WriteLine("Run with --help to see the options.")
            Return 2
        End Try

        ' no input on the command line: convert the page that ships with the sample
        If options.Url Is Nothing AndAlso options.HtmlFile Is Nothing AndAlso options.HtmlString Is Nothing Then
            options.HtmlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.html")
        End If

        Try
            Dim output As String = If(options.ImageFormat IsNot Nothing, ConvertToImage(options), ConvertToPdf(options))
            Console.WriteLine("Created " & Path.GetFullPath(output))
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine("Conversion failed: " & ex.Message)
            Return 1
        End Try
    End Function

    Private Function ConvertToPdf(o As Options) As String
        Dim converter As New PdfConverter()
        ' converter.LicenseKey = "your license key"

        ' page setup: the size and margins of the PDF pages, in points (1/72 inch)
        converter.PdfDocumentOptions.PdfPageSize = ParseEnum(o.PageSize, PdfPageSize.A4)
        converter.PdfDocumentOptions.PdfPageOrientation = ParseEnum(o.Orientation, PDFPageOrientation.Portrait)
        converter.PdfDocumentOptions.LeftMargin = o.MarginLeft
        converter.PdfDocumentOptions.RightMargin = o.MarginRight
        converter.PdfDocumentOptions.TopMargin = o.MarginTop
        converter.PdfDocumentOptions.BottomMargin = o.MarginBottom

        ' the width of the browser window the page is laid out in, in pixels; the content is then scaled to
        ' the PDF page width. A height of 0 means the whole page.
        converter.PageWidth = o.BrowserWidth
        converter.PageHeight = o.BrowserHeight

        converter.NavigationTimeout = o.NavigationTimeout
        ' extra time for pages that keep changing after they are loaded (animations, scripts)
        converter.ConversionDelay = o.ConversionDelay
        ' the most the conversion may take once the page is loaded: a page that never finishes fails with a
        ' timeout instead of keeping the application waiting
        converter.MaxConversionTime = o.MaxConversionTime
        ' convert what is loaded when the navigation timeout is reached, instead of failing
        converter.RenderOnTimeout = o.RenderOnTimeout

        ' the last page ends where the content ends (no trailing blank page). Ink measures the painted
        ' content, Marker the end of the page layout.
        converter.ManagedContentTrimming = Not o.NoManagedTrimming
        converter.ManagedContentTrimmingMode = ParseEnum(o.TrimMode, ManagedContentTrimmingMode.Ink)

        converter.PdfDocumentOptions.InternalLinksEnabled = o.InternalLinks
        converter.PdfDocumentOptions.LiveUrlsEnabled = o.ExternalLinks
        converter.UsePrintCSSMediaType = o.PrintCss

        ' bookmarks for the elements matched by these CSS selectors, here the headings
        If o.Bookmarks Then converter.PdfBookmarkOptions.HtmlElementSelectors = New String() {"h1", "h2"}

        ' the table of contents needs to know where the headings are in the PDF: the converter maps them
        If o.TableOfContents Then converter.HtmlElementsMappingOptions.HtmlElementSelectors = New String() {"h1", "h2", "h3"}

        ' convert only a part of the page: the element with an id, or the element a CSS selector matches
        If o.RenderElementId IsNot Nothing Then converter.RenderElementId = o.RenderElementId
        If o.RenderElementSelector IsNot Nothing Then converter.RenderElementSelector = o.RenderElementSelector

        ' keep a subset of pages, wait for the page to say it is ready
        If o.PageRanges IsNot Nothing Then converter.PageRanges = o.PageRanges
        If o.WindowStatus IsNot Nothing Then converter.WindowStatus = o.WindowStatus

        ' accessibility and archiving: a tagged PDF keeps the logical structure of the page for screen
        ' readers and declares PDF/UA-1; PDF/A is the archiving standard (the A levels also imply tagging)
        converter.Tagged = o.Tagged
        If o.Language IsNot Nothing Then converter.Language = o.Language
        converter.PdfStandardSubset = ParseEnum(o.PdfStandard, PdfStandardSubset.Full)

        ' document information
        If o.Title IsNot Nothing Then converter.PdfDocumentInfo.Title = o.Title
        If o.Author IsNot Nothing Then converter.PdfDocumentInfo.AuthorName = o.Author
        If o.Subject IsNot Nothing Then converter.PdfDocumentInfo.Subject = o.Subject
        If o.Keywords IsNot Nothing Then converter.PdfDocumentInfo.Keywords = o.Keywords

        ' security: a user password is needed to open the document, the owner password to change permissions
        If o.UserPassword IsNot Nothing Then converter.PdfSecurityOptions.UserPassword = o.UserPassword
        If o.OwnerPassword IsNot Nothing Then converter.PdfSecurityOptions.OwnerPassword = o.OwnerPassword
        If o.NoPrint Then converter.PdfSecurityOptions.CanPrint = False
        If o.NoCopy Then converter.PdfSecurityOptions.CanCopyContent = False

        ' what the converter sends to the web server
        For Each header As KeyValuePair(Of String, String) In o.HttpHeaders
            converter.HttpHeaders.Add(header.Key, header.Value)
        Next
        For Each cookie As KeyValuePair(Of String, String) In o.Cookies
            converter.HttpCookies.Add(cookie.Key, cookie.Value)
        Next
        ' with POST parameters the page is requested with the POST method, like a submitted form
        For Each field As KeyValuePair(Of String, String) In o.PostFields
            converter.HttpPostParameters.Add(field.Key, field.Value)
        Next

        If o.HeaderHtml IsNot Nothing Then
            converter.PdfDocumentOptions.ShowHeader = True
            converter.PdfHeaderOptions.HeaderHeight = 50
            converter.PdfHeaderOptions.DrawHeaderLine = False
            converter.PdfHeaderOptions.HtmlToPdfArea = New HtmlToPdfArea(o.HeaderHtml, Nothing)
        End If

        If o.FooterHtml IsNot Nothing OrElse o.PageNumbers Then
            converter.PdfDocumentOptions.ShowFooter = True
            converter.PdfFooterOptions.FooterHeight = 40
            converter.PdfFooterOptions.DrawFooterLine = False
            If o.FooterHtml IsNot Nothing Then
                converter.PdfFooterOptions.HtmlToPdfArea = New HtmlToPdfArea(o.FooterHtml, Nothing)
            End If
            If o.PageNumbers Then
                ' &p; and &P; are replaced with the page number and the page count
                Dim pageNumbers As New TextArea(0, 15, "Page &p; of &P;", New Font("Arial", 9))
                pageNumbers.TextAlign = HorizontalTextAlign.Right
                converter.PdfFooterOptions.TextArea = pageNumbers
            End If
        End If

        Dim output As String = If(o.Output, "output.pdf")
        Console.WriteLine("Converting... (Ctrl+C cancels)")

        ' The conversion runs asynchronously; Ctrl+C cancels the wait for it. The result is a Document, which can
        ' still be changed before it is saved.
        Dim cancel As New CancellationTokenSource()
        Dim onCancel As ConsoleCancelEventHandler = Sub(sender, e)
                                                        e.Cancel = True
                                                        cancel.Cancel()
                                                    End Sub
        AddHandler Console.CancelKeyPress, onCancel
        Dim document As Document
        Try
            If o.Url IsNot Nothing Then
                document = converter.GetPdfDocumentObjectFromUrlAsync(o.Url, cancel.Token).GetAwaiter().GetResult()
            ElseIf o.HtmlFile IsNot Nothing Then
                document = converter.GetPdfDocumentObjectFromUrlAsync(Path.GetFullPath(o.HtmlFile), cancel.Token).GetAwaiter().GetResult()
            Else
                ' the base URL resolves the relative images and style sheets of the HTML string
                document = converter.GetPdfDocumentObjectFromHtmlStringAsync(o.HtmlString, o.BaseUrl, cancel.Token).GetAwaiter().GetResult()
            End If
        Finally
            RemoveHandler Console.CancelKeyPress, onCancel
        End Try

        Try
            Console.WriteLine("PDF pages: " & converter.ConversionSummary.PdfPageCount)
            If o.TableOfContents Then AddTableOfContents(document, converter)
            document.Save(output)
        Finally
            document.Close()
        End Try
        Return output
    End Function

    ' A table of contents at the start, with links and page numbers. The PDF viewer numbers the table pages i, ii...
    ' and the converted pages 1, 2, 3... (page labels). A named destination lets other documents and web pages link
    ' to the table: output.pdf#toc.
    Private Sub AddTableOfContents(document As Document, converter As PdfConverter)
        Dim toc As New TableOfContents()
        toc.AddEntries(document, converter.HtmlElementsMappingOptions.HtmlElementsMappingResult)
        If toc.Entries.Count = 0 Then
            Console.WriteLine("No headings found for the table of contents.")
            Return
        End If

        ' the converted pages are numbered from 1; the range moves with them when the table is inserted before them
        document.AddPageLabelRange(0, PageLabelStyle.Decimal)
        Dim tocPages As Integer = document.InsertTableOfContents(toc, 0)
        document.AddPageLabelRange(0, PageLabelStyle.LowercaseRoman)
        document.AddNamedDestination("toc", New ExplicitDestination(document.Pages(0)))
        Console.WriteLine("Table of contents: " & toc.Entries.Count & " entries on " & tocPages & " page(s)")
    End Sub

    Private Function ConvertToImage(o As Options) As String
        Dim converter As New ImgConverter()
        ' converter.LicenseKey = "your license key"

        converter.PageWidth = o.BrowserWidth
        converter.PageHeight = o.BrowserHeight
        converter.NavigationTimeout = o.NavigationTimeout
        converter.ConversionDelay = o.ConversionDelay
        converter.MaxConversionTime = o.MaxConversionTime
        converter.RenderOnTimeout = o.RenderOnTimeout

        Dim imageFormat As ImageFormat = ParseEnum(o.ImageFormat, ImageFormat.Png)
        Dim output As String = If(o.Output, "output." & imageFormat.ToString().ToLowerInvariant())
        Console.WriteLine("Rendering...")

        If o.Url IsNot Nothing Then
            converter.SaveImageFromUrlToFile(o.Url, imageFormat, output)
        ElseIf o.HtmlFile IsNot Nothing Then
            converter.SaveImageFromHtmlFileToFile(Path.GetFullPath(o.HtmlFile), imageFormat, output)
        ElseIf o.BaseUrl IsNot Nothing Then
            converter.SaveImageFromHtmlStringToFile(o.HtmlString, imageFormat, output, o.BaseUrl)
        Else
            converter.SaveImageFromHtmlStringToFile(o.HtmlString, imageFormat, output)
        End If

        Return output
    End Function

    Private Function ParseEnum(Of T As Structure)(value As String, defaultValue As T) As T
        If String.IsNullOrEmpty(value) Then Return defaultValue
        Dim result As T = defaultValue
        If Not [Enum].TryParse(value, True, result) Then
            Throw New ArgumentException("'" & value & "' is not one of: " & String.Join(", ", [Enum].GetNames(GetType(T))))
        End If
        Return result
    End Function

    Private Function HasFlag(args As String(), name As String) As Boolean
        For Each a As String In args
            If String.Equals(a, name, StringComparison.OrdinalIgnoreCase) Then Return True
        Next
        Return False
    End Function

    Private Sub PrintUsage()
        Console.WriteLine("HtmlToPdfConverter - converts a web page or HTML to PDF or to an image with ExpertPdf.")
        Console.WriteLine()
        Console.WriteLine("Usage: HtmlToPdfConverter [input] [options]")
        Console.WriteLine("       (no input: converts sample.html from the application folder)")
        Console.WriteLine()
        Console.WriteLine("Input:")
        Console.WriteLine("  --url <address>             web page to convert, e.g. https://www.html-to-pdf.net")
        Console.WriteLine("  --html-file <path>          HTML file to convert")
        Console.WriteLine("  --html <string>             HTML string to convert")
        Console.WriteLine("  --base-url <address>        base address of the relative links of --html")
        Console.WriteLine()
        Console.WriteLine("Output:")
        Console.WriteLine("  -o, --output <path>         output file (default output.pdf, or output.<format> for images)")
        Console.WriteLine("  --image <format>            render an image instead of a PDF: Png, Jpeg, Bmp, Tiff")
        Console.WriteLine()
        Console.WriteLine("Rendering:")
        Console.WriteLine("  --browser-width <px>        browser window width (default 1024)")
        Console.WriteLine("  --browser-height <px>       browser window height (default 0 = the whole page)")
        Console.WriteLine("  --timeout <s>               navigation timeout in seconds (default 60)")
        Console.WriteLine("  --delay <s>                 wait before converting the loaded page (default 0)")
        Console.WriteLine("  --max-conversion-time <s>   most the conversion may take after the page loads (default 60)")
        Console.WriteLine("  --render-on-timeout         convert what is loaded when the navigation timeout is reached")
        Console.WriteLine("  --no-managed-trimming       keep every page whole, up to the end of the paper")
        Console.WriteLine("  --trim-mode <mode>          Ink | Marker: how the end of the content is found (default Ink)")
        Console.WriteLine("  --print-css                 use the print CSS media type instead of screen")
        Console.WriteLine("  --element-id <id>           convert only the element with this id")
        Console.WriteLine("  --element-selector <css>    convert only the element matched by this CSS selector")
        Console.WriteLine("  --window-status <text>      wait until the page sets window.status to this text")
        Console.WriteLine()
        Console.WriteLine("PDF:")
        Console.WriteLine("  --page-size <size>          A4, Letter, Legal, A3, A5, ... (default A4)")
        Console.WriteLine("  --orientation <o>           Portrait | Landscape")
        Console.WriteLine("  --margins <pt>              all four page margins in points")
        Console.WriteLine("  --page-ranges <ranges>      keep only these pages, e.g. 1-2,4")
        Console.WriteLine("  --bookmarks                 create bookmarks from the H1 and H2 headings")
        Console.WriteLine("  --toc                       table of contents of the H1-H3 headings at the start, with")
        Console.WriteLine("                              roman page labels for its pages")
        Console.WriteLine("  --no-internal-links         do not convert links to anchors in the page")
        Console.WriteLine("  --no-external-links         do not convert links to other pages")
        Console.WriteLine("  --header-html <html>        HTML header on every page")
        Console.WriteLine("  --footer-html <html>        HTML footer on every page")
        Console.WriteLine("  --page-numbers              'Page X of Y' in the footer")
        Console.WriteLine("  --tagged                    tagged (accessible) PDF, PDF/UA-1")
        Console.WriteLine("  --language <lang>           language of a tagged PDF, e.g. en-US")
        Console.WriteLine("  --pdf-standard <subset>     Full, Pdf_A_1b, Pdf_A_2b, Pdf_A_3b, Pdf_A_3u, Pdf_A_3a, Pdf_A_4, Pdf_X_1a")
        Console.WriteLine("  --title, --author, --subject, --keywords <text>   document information")
        Console.WriteLine("  --user-password <p>         password needed to open the PDF")
        Console.WriteLine("  --owner-password <p>        password needed to change the permissions")
        Console.WriteLine("  --no-print, --no-copy       remove the print or the copy permission")
        Console.WriteLine()
        Console.WriteLine("HTTP:")
        Console.WriteLine("  --header <name=value>       HTTP header sent with the request (repeatable)")
        Console.WriteLine("  --cookie <name=value>       HTTP cookie (repeatable)")
        Console.WriteLine("  --post <name=value>         POST the page with this form field (repeatable)")
        Console.WriteLine()
        Console.WriteLine("Examples:")
        Console.WriteLine("  HtmlToPdfConverter --url https://www.html-to-pdf.net -o site.pdf --page-numbers")
        Console.WriteLine("  HtmlToPdfConverter --html-file sample.html --tagged --language en-US -o accessible.pdf")
        Console.WriteLine("  HtmlToPdfConverter --url https://www.html-to-pdf.net --image Png -o site.png")
        Console.WriteLine("  HtmlToPdfConverter --html-file sample.html --toc --page-numbers -o book.pdf")
    End Sub

    Private NotInheritable Class Options
        Public Url, HtmlFile, HtmlString, BaseUrl, Output, ImageFormat As String
        Public BrowserWidth As Integer = 1024, BrowserHeight As Integer = 0, NavigationTimeout As Integer = 60, ConversionDelay As Integer = 0
        Public MaxConversionTime As Integer = 60
        Public PrintCss, Bookmarks, PageNumbers, Tagged, NoPrint, NoCopy As Boolean
        Public RenderOnTimeout, NoManagedTrimming, TableOfContents As Boolean
        Public TrimMode As String
        Public InternalLinks As Boolean = True, ExternalLinks As Boolean = True
        Public RenderElementId, RenderElementSelector, WindowStatus, PageRanges As String
        Public PageSize, Orientation, PdfStandard, Language As String
        Public MarginLeft, MarginRight, MarginTop, MarginBottom As Integer
        Public HeaderHtml, FooterHtml, Title, Author, Subject, Keywords, UserPassword, OwnerPassword As String
        Public HttpHeaders As New List(Of KeyValuePair(Of String, String))()
        Public Cookies As New List(Of KeyValuePair(Of String, String))()
        Public PostFields As New List(Of KeyValuePair(Of String, String))()

        Public Shared Function Parse(args As String()) As Options
            Dim o As New Options()
            Dim i As Integer = 0
            While i < args.Length
                Select Case args(i).ToLowerInvariant()
                    Case "--url" : o.Url = Value(args, i)
                    Case "--html-file" : o.HtmlFile = Value(args, i)
                    Case "--html" : o.HtmlString = Value(args, i)
                    Case "--base-url" : o.BaseUrl = Value(args, i)
                    Case "-o", "--output" : o.Output = Value(args, i)
                    Case "--image" : o.ImageFormat = Value(args, i)
                    Case "--browser-width" : o.BrowserWidth = Number(args, i)
                    Case "--browser-height" : o.BrowserHeight = Number(args, i)
                    Case "--timeout" : o.NavigationTimeout = Number(args, i)
                    Case "--delay" : o.ConversionDelay = Number(args, i)
                    Case "--max-conversion-time" : o.MaxConversionTime = Number(args, i)
                    Case "--render-on-timeout" : o.RenderOnTimeout = True
                    Case "--no-managed-trimming" : o.NoManagedTrimming = True
                    Case "--trim-mode" : o.TrimMode = Value(args, i)
                    Case "--toc" : o.TableOfContents = True
                    Case "--print-css" : o.PrintCss = True
                    Case "--element-id" : o.RenderElementId = Value(args, i)
                    Case "--element-selector" : o.RenderElementSelector = Value(args, i)
                    Case "--window-status" : o.WindowStatus = Value(args, i)
                    Case "--page-size" : o.PageSize = Value(args, i)
                    Case "--orientation" : o.Orientation = Value(args, i)
                    Case "--margins"
                        Dim m As Integer = Number(args, i)
                        o.MarginLeft = m : o.MarginRight = m : o.MarginTop = m : o.MarginBottom = m
                    Case "--page-ranges" : o.PageRanges = Value(args, i)
                    Case "--bookmarks" : o.Bookmarks = True
                    Case "--no-internal-links" : o.InternalLinks = False
                    Case "--no-external-links" : o.ExternalLinks = False
                    Case "--header-html" : o.HeaderHtml = Value(args, i)
                    Case "--footer-html" : o.FooterHtml = Value(args, i)
                    Case "--page-numbers" : o.PageNumbers = True
                    Case "--tagged" : o.Tagged = True
                    Case "--language" : o.Language = Value(args, i)
                    Case "--pdf-standard" : o.PdfStandard = Value(args, i)
                    Case "--title" : o.Title = Value(args, i)
                    Case "--author" : o.Author = Value(args, i)
                    Case "--subject" : o.Subject = Value(args, i)
                    Case "--keywords" : o.Keywords = Value(args, i)
                    Case "--user-password" : o.UserPassword = Value(args, i)
                    Case "--owner-password" : o.OwnerPassword = Value(args, i)
                    Case "--no-print" : o.NoPrint = True
                    Case "--no-copy" : o.NoCopy = True
                    Case "--header" : o.HttpHeaders.Add(Pair(args, i))
                    Case "--cookie" : o.Cookies.Add(Pair(args, i))
                    Case "--post" : o.PostFields.Add(Pair(args, i))
                    Case Else : Throw New ArgumentException("unknown option " & args(i))
                End Select
                i += 1
            End While

            Dim inputs As Integer = If(o.Url IsNot Nothing, 1, 0) + If(o.HtmlFile IsNot Nothing, 1, 0) + If(o.HtmlString IsNot Nothing, 1, 0)
            If inputs > 1 Then Throw New ArgumentException("use only one of --url, --html-file and --html")
            Return o
        End Function

        Private Shared Function Value(args As String(), ByRef i As Integer) As String
            If i + 1 >= args.Length Then Throw New ArgumentException(args(i) & " needs a value")
            i += 1
            Return args(i)
        End Function

        Private Shared Function Number(args As String(), ByRef i As Integer) As Integer
            Dim optionName As String = args(i)
            Dim text As String = Value(args, i)
            Dim n As Integer
            If Not Integer.TryParse(text, n) OrElse n < 0 Then
                Throw New ArgumentException(optionName & " needs a positive number, not '" & text & "'")
            End If
            Return n
        End Function

        Private Shared Function Pair(args As String(), ByRef i As Integer) As KeyValuePair(Of String, String)
            Dim optionName As String = args(i)
            Dim text As String = Value(args, i)
            Dim eq As Integer = text.IndexOf("="c)
            If eq <= 0 Then Throw New ArgumentException(optionName & " needs name=value, not '" & text & "'")
            Return New KeyValuePair(Of String, String)(text.Substring(0, eq), text.Substring(eq + 1))
        End Function
    End Class

End Module

