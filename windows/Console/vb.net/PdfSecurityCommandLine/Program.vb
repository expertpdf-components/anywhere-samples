Imports System.IO
Imports ExpertPdf.Anywhere.PDFSecurity

''' <summary>
''' A command line tool that protects a PDF document with passwords and permissions, or removes the protection
''' of a document you have the password of. Run it without arguments to protect the document that comes with
''' the sample and then remove the protection again.
''' </summary>
Module Program

    Function Main(args As String()) As Integer
        If args.Length = 0 Then
            Return RunDemo()
        End If

        Dim srcPdfFile As String = Nothing
        Dim outFile As String = Nothing
        Dim removeSecurity As Boolean = False
        Dim showInfo As Boolean = False
        Dim options As New PdfSecurityOptions()

        Try
            For Each argument As String In args
                Dim value As String = Nothing
                If TryGetValue(argument, "/pdf:", value) Then
                    srcPdfFile = Path.GetFullPath(value)
                ElseIf TryGetValue(argument, "/out:", value) Then
                    outFile = Path.GetFullPath(value)
                ElseIf TryGetValue(argument, "/userpswd:", value) Then
                    options.UserPassword = value
                ElseIf TryGetValue(argument, "/ownerpswd:", value) Then
                    options.OwnerPassword = value
                ElseIf TryGetValue(argument, "/keysize:", value) Then
                    options.KeySize = ParseKeySize(value)
                ElseIf TryGetValue(argument, "/algo:", value) Then
                    options.Algorithm = ParseAlgorithm(value)
                ElseIf IsSwitch(argument, "/noprint") Then
                    options.CanPrint = False
                ElseIf IsSwitch(argument, "/nofullqualityprint") Then
                    options.CanFullQualityPrint = False
                ElseIf IsSwitch(argument, "/nocopy") Then
                    options.CanCopyContent = False
                ElseIf IsSwitch(argument, "/noaccessibilitycopy") Then
                    options.CanAccessibilityCopyContent = False
                ElseIf IsSwitch(argument, "/noeditcontent") Then
                    options.CanEditContent = False
                ElseIf IsSwitch(argument, "/noeditannot") Then
                    options.CanEditAnnotations = False
                ElseIf IsSwitch(argument, "/nofillform") Then
                    options.CanFillFormFields = False
                ElseIf IsSwitch(argument, "/noassembly") Then
                    options.CanAssembleDocument = False
                ElseIf IsSwitch(argument, "/removesecurity") Then
                    removeSecurity = True
                ElseIf IsSwitch(argument, "/info") Then
                    showInfo = True
                Else
                    Throw New ArgumentException("Unknown argument: " & argument)
                End If
            Next

            If srcPdfFile Is Nothing Then
                Throw New ArgumentException("The source PDF document is not specified.")
            End If
            If outFile Is Nothing Then
                outFile = Path.GetFullPath(If(removeSecurity, "UnsecuredDocument.pdf", "SecuredDocument.pdf"))
            End If
            If removeSecurity AndAlso String.IsNullOrEmpty(options.OwnerPassword) AndAlso String.IsNullOrEmpty(options.UserPassword) Then
                Throw New ArgumentException("The owner or the user password is required to remove the security.")
            End If
        Catch ex As ArgumentException
            Console.WriteLine(ex.Message)
            Console.WriteLine()
            ShowUsage()
            Return 1
        End Try

        Try
            Dim securityManager As New PdfSecurityManager(options)
            ' securityManager.LicenseKey = "your license key"

            If showInfo Then
                ' the password is needed only for the signature information of a document with an open password
                Dim infoPassword As String = If(Not String.IsNullOrEmpty(options.OwnerPassword), options.OwnerPassword, options.UserPassword)
                PrintSecurityInfo(srcPdfFile, securityManager.GetSecurityInfo(srcPdfFile, infoPassword))
            ElseIf removeSecurity Then
                ' the owner password gives full rights; the user password also works when it is the only one known
                Dim password As String = If(Not String.IsNullOrEmpty(options.OwnerPassword), options.OwnerPassword, options.UserPassword)
                securityManager.SaveUnSecuredPdfToFile(srcPdfFile, outFile, password)
                Console.WriteLine("Security removed: " & outFile)
            Else
                securityManager.SaveSecuredPdfToFile(srcPdfFile, outFile)
                Console.WriteLine("Secured document: " & outFile)
            End If
            Return 0
        Catch ex As Exception
            ' SecurePdfException for documents that cannot be read or a wrong password
            Console.WriteLine("Error: " & ex.Message)
            Return 2
        End Try
    End Function

    ' no arguments: protect the bundled document, then remove the protection again
    Private Function RunDemo() As Integer
        ShowUsage()
        Console.WriteLine()
        Console.WriteLine("No arguments given: protecting report.pdf, then removing the protection.")

        Dim sampleFolder As String = AppDomain.CurrentDomain.BaseDirectory
        Dim source As String = Path.Combine(sampleFolder, "report.pdf")
        Dim secured As String = Path.Combine(sampleFolder, "SecuredDocument.pdf")
        Dim unsecured As String = Path.Combine(sampleFolder, "UnsecuredDocument.pdf")

        Try
            Dim options As New PdfSecurityOptions()
            ' "user" opens the document with the permissions below; "owner" opens it with all the rights
            options.UserPassword = "user"
            options.OwnerPassword = "owner"
            options.CanPrint = False
            options.CanCopyContent = False
            options.CanEditContent = False
            options.KeySize = EncryptionKeySize.EncryptKey256Bit
            options.Algorithm = EncryptionAlgorithm.AES

            Dim securityManager As New PdfSecurityManager(options)
            ' securityManager.LicenseKey = "your license key"
            securityManager.SaveSecuredPdfToFile(source, secured)
            Console.WriteLine("Secured document: {0} (AES 256 bit, passwords user/owner, no printing, no copying)", secured)

            ' read the security settings back, without changing the document
            Dim infoManager As New PdfSecurityManager()
            PrintSecurityInfo(secured, infoManager.GetSecurityInfo(secured))

            ' removing the security needs one of the passwords
            Dim unsecureManager As New PdfSecurityManager()
            unsecureManager.SaveUnSecuredPdfToFile(secured, unsecured, "owner")
            Console.WriteLine("Security removed: " & unsecured)
            Return 0
        Catch ex As Exception
            Console.WriteLine("Error: " & ex.Message)
            Return 2
        End Try
    End Function

    Private Sub PrintSecurityInfo(file As String, info As PdfSecurityInfo)
        Console.WriteLine("Security of " & file & ":")
        If info.IsEncrypted Then
            Console.WriteLine("  encrypted: {0}, {1} bit, revision {2}", info.Algorithm, info.KeyLength, info.Revision)
        Else
            Console.WriteLine("  not encrypted")
        End If
        Console.WriteLine("  open password: {0}, owner password needed for full rights: {1}",
            If(info.IsUserPasswordRequired, "yes", "no"), If(info.IsOwnerPasswordRequired, "yes", "no"))
        Console.WriteLine("  print: {0}, high quality print: {1}, copy: {2}, accessibility copy: {3}",
            info.CanPrint, info.CanFullQualityPrint, info.CanCopyContent, info.CanAccessibilityCopyContent)
        Console.WriteLine("  edit content: {0}, edit annotations: {1}, fill forms: {2}, assemble: {3}",
            info.CanEditContent, info.CanEditAnnotations, info.CanFillFormFields, info.CanAssembleDocument)
        If info.IsOpened Then
            Console.WriteLine("  signatures: {0}, certified: {1}", info.SignatureCount, If(info.IsCertified, info.CertificationLevel.ToString(), "no"))
        Else
            Console.WriteLine("  signatures: unknown (give the password to read them)")
        End If
    End Sub

    Private Function IsSwitch(argument As String, name As String) As Boolean
        Return argument.Equals(name, StringComparison.OrdinalIgnoreCase)
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

    Private Function ParseKeySize(value As String) As EncryptionKeySize
        Select Case value
            Case "40"
                Return EncryptionKeySize.EncryptKey40Bit
            Case "128"
                Return EncryptionKeySize.EncryptKey128Bit
            Case "256"
                Return EncryptionKeySize.EncryptKey256Bit
            Case Else
                Throw New ArgumentException("The key size must be 40, 128 or 256.")
        End Select
    End Function

    Private Function ParseAlgorithm(value As String) As EncryptionAlgorithm
        Select Case value.ToUpperInvariant()
            Case "AES"
                Return EncryptionAlgorithm.AES
            Case "RC4"
                Return EncryptionAlgorithm.RC4
            Case Else
                Throw New ArgumentException("The encryption algorithm must be RC4 or AES.")
        End Select
    End Function

    Private Sub ShowUsage()
        Console.WriteLine("Usage: PdfSecurityCommandLine /pdf:source_file [/out:file]")
        Console.WriteLine("           [/userpswd:password] [/ownerpswd:password] [/keysize:40|128|256] [/algo:RC4|AES]")
        Console.WriteLine("           [/noprint] [/nofullqualityprint] [/nocopy] [/noaccessibilitycopy]")
        Console.WriteLine("           [/noeditcontent] [/noeditannot] [/nofillform] [/noassembly] [/removesecurity] [/info]")
        Console.WriteLine()
        Console.WriteLine("With /removesecurity the owner or the user password of the source document is required.")
        Console.WriteLine("/info shows the security settings of the source document and changes nothing.")
        Console.WriteLine("Protect: PdfSecurityCommandLine /pdf:report.pdf /noprint /keysize:256 /ownerpswd:owner /userpswd:user /out:Secured.pdf")
        Console.WriteLine("Remove:  PdfSecurityCommandLine /pdf:Secured.pdf /ownerpswd:owner /removesecurity /out:Unsecured.pdf")
        Console.WriteLine("Show:    PdfSecurityCommandLine /pdf:Secured.pdf /info")
    End Sub

End Module
