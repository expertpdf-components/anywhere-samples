Imports ExpertPdf.Anywhere.HtmlToPdf
Imports ExpertPdf.Anywhere.PdfDocument

''' <summary>
''' Creates accessible (tagged) and archival (PDF/A) documents from the bundled sample.html:
'''   tagged.pdf             tagged PDF that declares PDF/UA-1, for screen readers
'''   pdfua-2.pdf            PDF/UA-2 (a PDF 2.0 document)
'''   pdfa-2b.pdf            PDF/A-2b, for long-term archiving
'''   pdfa-3b-with-file.pdf  PDF/A-3b with the HTML source embedded as an associated file
'''   pdfa-3u.pdf            PDF/A-3u: also guarantees the text can be extracted as Unicode
'''   pdfa-3a.pdf            PDF/A-3a: archival and accessible (tagged)
'''   pdfa-4-pdfua-2.pdf     PDF/A-4 and PDF/UA-2 together
''' </summary>
Friend Module Program

    Private ReadOnly s_appFolder As String = AppDomain.CurrentDomain.BaseDirectory

    Function Main(args As String()) As Integer
        If args.Length > 1 OrElse (args.Length > 0 AndAlso (args(0) = "--help" OrElse args(0) = "/?")) Then
            Console.WriteLine("Usage: PdfStandards [html file]")
            Console.WriteLine("  html file  the page to convert (default: sample.html from the application folder)")
            Return 0
        End If

        Dim htmlFile As String = If(args.Length > 0, Path.GetFullPath(args(0)), Path.Combine(s_appFolder, "sample.html"))
        Console.WriteLine("Converting " & htmlFile)

        Try
            CreateTaggedPdf(htmlFile, PdfUAStandard.Pdf_UA_1, "tagged.pdf")
            CreateTaggedPdf(htmlFile, PdfUAStandard.Pdf_UA_2, "pdfua-2.pdf")
            CreatePdfA(htmlFile, PdfStandardSubset.Pdf_A_2b, PdfUAStandard.None, "pdfa-2b.pdf")
            CreatePdfA3WithAssociatedFile(htmlFile, "pdfa-3b-with-file.pdf")
            CreatePdfA(htmlFile, PdfStandardSubset.Pdf_A_3u, PdfUAStandard.None, "pdfa-3u.pdf")
            CreatePdfA(htmlFile, PdfStandardSubset.Pdf_A_3a, PdfUAStandard.None, "pdfa-3a.pdf")
            CreatePdfA(htmlFile, PdfStandardSubset.Pdf_A_4, PdfUAStandard.Pdf_UA_2, "pdfa-4-pdfua-2.pdf")
        Catch ex As Exception
            Console.Error.WriteLine("Conversion failed: " & ex.Message)
            Return 1
        End Try

        Console.WriteLine("The documents are in " & s_appFolder)
        Return 0
    End Function

    Private Function CreateConverter() As PdfConverter
        Dim converter As New PdfConverter()
        ' converter.LicenseKey = "your license key"
        converter.PdfDocumentOptions.LeftMargin = 30
        converter.PdfDocumentOptions.RightMargin = 30
        converter.PdfDocumentOptions.TopMargin = 30
        converter.PdfDocumentOptions.BottomMargin = 30
        ' the title is shown by PDF readers and read out by screen readers; PDF/UA and PDF/A require it
        converter.PdfDocumentInfo.Title = "ExpertPdf HTML to PDF Converter - Sample Page"
        converter.PdfDocumentInfo.AuthorName = "ExpertPdf"
        Return converter
    End Function

    Private Sub CreateTaggedPdf(htmlFile As String, standard As PdfUAStandard, fileName As String)
        Dim converter As PdfConverter = CreateConverter()

        ' The structure (headings, paragraphs, lists, tables, images with their alt text, links) comes from the
        ' HTML, so well-formed, semantic HTML gives the best accessible PDF. The language is the language
        ' screen readers use to read the text. PDF/UA-2 writes the document as PDF 2.0.
        converter.AccessibilityStandard = standard
        converter.Language = "en-US"

        converter.SavePdfFromHtmlFileToFile(htmlFile, Path.Combine(s_appFolder, fileName))
        Console.WriteLine("  " & fileName & ": " & standard.ToString() & ", " &
            converter.ConversionSummary.PdfPageCount & " pages")
    End Sub

    Private Sub CreatePdfA(htmlFile As String, standard As PdfStandardSubset, accessibility As PdfUAStandard,
            fileName As String)
        Dim converter As PdfConverter = CreateConverter()

        ' PDF/A embeds the fonts and the color profile and leaves out everything that depends on the outside
        ' world, so the document looks the same in many years. The A levels (PDF/A-3a) are tagged as well, and
        ' PDF/A-4 combines with PDF/UA-2 (PDF/A-2 and PDF/A-3 combine with PDF/UA-1).
        converter.PdfStandardSubset = standard
        converter.AccessibilityStandard = accessibility
        converter.Language = "en-US"

        converter.SavePdfFromHtmlFileToFile(htmlFile, Path.Combine(s_appFolder, fileName))
        Console.WriteLine("  " & fileName & ": " & standard.ToString() &
            If(accessibility <> PdfUAStandard.None, " + " & accessibility.ToString(), String.Empty))
    End Sub

    Private Sub CreatePdfA3WithAssociatedFile(htmlFile As String, fileName As String)
        Dim converter As PdfConverter = CreateConverter()
        converter.PdfStandardSubset = PdfStandardSubset.Pdf_A_3b

        ' get the PDF as a Document object to add more content before saving it
        Dim document As Document = converter.GetPdfDocumentObjectFromHtmlFile(htmlFile)
        Try
            ' PDF/A-3 can embed any file, linked to the document by a relationship: here the HTML the PDF was
            ' created from (Source). Data, Alternative and Supplement are the other common relationships.
            document.AddAssociatedFile(htmlFile, AttachmentRelationship.Source, "text/html",
                "The HTML page this document was created from")

            document.Save(Path.Combine(s_appFolder, fileName))
        Finally
            document.Close()
        End Try
        Console.WriteLine("  " & fileName & ": PDF/A-3b with " & Path.GetFileName(htmlFile) & " as an associated file")
    End Sub

End Module
