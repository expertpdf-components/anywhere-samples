Imports System.Globalization
Imports ExpertPdf.Anywhere.PdfDocument

''' <summary>
''' Reads and changes what a page of an existing PDF document draws:
'''   letter.pdf          a one page letter the sample builds (a logo, a title, a few lines, a DRAFT mark)
'''   letter-changed.pdf  the same letter with a text replaced and recolored, the logo moved to the right
'''                       and the DRAFT mark in a layer that is hidden when the document opens
''' With a PDF file as argument, the sample lists the objects of its first page instead.
''' </summary>
Module Program

    Private ReadOnly appFolder As String = AppDomain.CurrentDomain.BaseDirectory

    Function Main(args As String()) As Integer
        If args.Length > 1 OrElse (args.Length > 0 AndAlso (args(0) = "--help" OrElse args(0) = "/?")) Then
            Console.WriteLine("Usage: PageObjects [pdf file]")
            Console.WriteLine("  pdf file  a document whose first page objects are listed")
            Return 0
        End If

        Try
            If args.Length = 1 Then
                ListObjects(Path.GetFullPath(args(0)))
                Return 0
            End If

            Dim letter As String = Path.Combine(appFolder, "letter.pdf")
            CreateLetter(letter)
            ListObjects(letter)
            ChangeLetter(letter, Path.Combine(appFolder, "letter-changed.pdf"))
        Catch ex As Exception
            Console.Error.WriteLine("Error: " & ex.Message)
            Return 1
        End Try

        Console.WriteLine("The documents are in " & appFolder)
        Return 0
    End Function

    ' A one page letter with a logo, a title, a few lines of text and a DRAFT mark.
    Private Sub CreateLetter(fileName As String)
        Dim document As New Document()
        ' document.LicenseKey = "your license key"
        Dim page As PdfPage = document.AddPage()

        Dim title As PdfFont = document.AddFont(StdFontBaseFamily.HelveticaBold)
        title.Size = 22
        Dim body As PdfFont = document.AddFont(StdFontBaseFamily.Helvetica)
        body.Size = 12
        Dim mark As PdfFont = document.AddFont(StdFontBaseFamily.HelveticaBold)
        mark.Size = 40

        page.AddElement(New ImageElement(0, 0, 150, Path.Combine(appFolder, "logo.png")))
        page.AddElement(New TextElement(0, 80, "Quarterly Report - ExpertPdf", title))
        page.AddElement(New TextElement(0, 130, "Prepared by the ExpertPdf team for our partners.", body))
        page.AddElement(New TextElement(0, 152, "Revenue grew 12% over the previous quarter.", body))
        page.AddElement(New TextElement(0, 174, "Questions about this report: contact ExpertPdf support.", body))
        page.AddElement(New TextElement(0, 260, "DRAFT", mark, New PdfColor(200, 200, 200)))

        document.Save(fileName)
        document.Close()
        Console.WriteLine("  letter.pdf: created")
    End Sub

    ' Every object of the first page, in the order it is painted.
    Private Sub ListObjects(fileName As String)
        Dim document As New Document(fileName)
        Dim model As PageObjectModel = document.Pages(0).ObjectModel

        Console.WriteLine("  {0}: {1} objects on the first page", Path.GetFileName(fileName), model.Objects.Count)
        For Each pageObject As PageObject In model.Objects
            Dim line As String = "    " & pageObject.Kind.ToString() & " at " & Format(pageObject.Bounds)
            Dim text As TextObject = TryCast(pageObject, TextObject)
            Dim image As ImageObject = TryCast(pageObject, ImageObject)
            If text IsNot Nothing Then
                line &= ": """ & text.Text & """ (" & text.FontName & ")"
            ElseIf image IsNot Nothing Then
                line &= ": " & image.PixelWidth & " x " & image.PixelHeight & " pixels"
            End If
            Console.WriteLine(line)
        Next

        document.Close()
    End Sub

    ' Replace a text, move the logo and put the DRAFT mark in a hidden layer.
    Private Sub ChangeLetter(fileName As String, outFile As String)
        Dim document As New Document(fileName)
        ' document.LicenseKey = "your license key"
        Dim model As PageObjectModel = document.Pages(0).ObjectModel

        Dim draft As Layer = Nothing
        For Each text As TextObject In model.GetTextObjects()
            If text.Text.Contains("ExpertPdf") Then
                ' the new text is written in the object's font, from the same starting point
                text.ReplaceText(text.Text.Replace("ExpertPdf", "My Company"))
                text.FillColor = New PdfColor(200, 30, 30)
            ElseIf text.Text = "DRAFT" Then
                If draft Is Nothing Then
                    draft = document.Layers.Add("Draft", False)
                End If
                text.Layer = draft
            End If
        Next

        ' the logo moves 330 points to the right
        For Each image As ImageObject In model.GetImageObjects()
            image.Translate(330, 0)
        Next

        document.Save(outFile)
        document.Close()
        Console.WriteLine("  letter-changed.pdf: text replaced, logo moved, DRAFT in a hidden layer")
    End Sub

    Private Function Format(r As ExpertPdf.Anywhere.Drawing.RectangleF) As String
        Return String.Format(CultureInfo.InvariantCulture, "({0:0.#}, {1:0.#}) {2:0.#} x {3:0.#} pt",
            r.X, r.Y, r.Width, r.Height)
    End Function

End Module
