Imports ExpertPdf.Anywhere.Drawing
Imports System.IO
Imports ExpertPdf.Anywhere.PDFSecurity

''' <summary>
''' Signs PDF documents with a digital certificate (PKCS#12 / .pfx file): from a file to a file, from memory to
''' memory, and from a stream to a stream for a password protected document. Optionally the signatures get a
''' time stamp from a time stamp authority (TSA). Then it certifies a document (a certification signature that
''' allows form filling and signing) with an empty field signed by an approver, and checks every signature
''' it made with ValidateSignatures.
'''
''' certificate.pfx is a self-signed TEST certificate made for this sample (password: expertpdf). Viewers show
''' the signature as valid but the signer as not trusted. Sign your documents with a certificate issued for you
''' by a certification authority.
''' </summary>
Module Program

    Private ReadOnly sampleFolder As String = AppDomain.CurrentDomain.BaseDirectory
    Private ReadOnly certificateFile As String = Path.Combine(sampleFolder, "certificate.pfx")
    Private Const CertificatePassword As String = "expertpdf"

    ' the signature box, in points from the top left corner of the page: the bottom right of an A4 page
    Private ReadOnly signatureBounds As New RectangleF(345, 700, 200, 60)

    Function Main(args As String()) As Integer
        ' an optional time stamp server, for example: PdfDigitalSignature /tsa:https://your-tsa-server/
        Dim tsa As TimeStampAuthority = Nothing
        For Each argument As String In args
            If argument.StartsWith("/tsa:", StringComparison.OrdinalIgnoreCase) Then
                tsa = New TimeStampAuthority(New Uri(argument.Substring(5)))
                ' tsa.UserName = "user"   ' when the server requires a login
                ' tsa.Password = "password"
                Console.WriteLine("Time stamps from " & tsa.Server.ToString())
            Else
                Console.WriteLine("Usage: PdfDigitalSignature [/tsa:time_stamp_server_url]")
                Return 1
            End If
        Next

        Try
            SignFileToFile(tsa)
            SignBytes(tsa)
            SignProtectedStream(tsa)
            CertifyAndApprove(tsa)
            ValidateSignedDocuments()

            Console.WriteLine()
            Console.WriteLine("The signed documents are in " & sampleFolder)
            Return 0
        Catch ex As Exception
            Console.WriteLine("Error: " & ex.Message)
            Return 2
        End Try
    End Function

    ' Sign a file and save the signed document in another file.
    Private Sub SignFileToFile(tsa As TimeStampAuthority)
        Dim securityManager As New PdfSecurityManager()
        ' securityManager.LicenseKey = "your license key"

        Dim signedFile As String = Path.Combine(sampleFolder, "SignedReport.pdf")
        securityManager.Sign(Path.Combine(sampleFolder, "report.pdf"), signedFile,
            certificateFile, CertificatePassword,
            "Approval",                      ' the name of the signature field
            "I approve this report",         ' reason
            "Head office",                   ' location
            "signer@example.com",            ' contact info
            0,                               ' the page of the signature box: the first page (index 0)
            signatureBounds, tsa)
        Console.WriteLine("SignedReport.pdf: report.pdf signed on page 1")
    End Sub

    ' Sign a document you have in memory and get the signed document as bytes.
    Private Sub SignBytes(tsa As TimeStampAuthority)
        Dim invoiceBytes As Byte() = File.ReadAllBytes(Path.Combine(sampleFolder, "invoice.pdf"))

        Dim securityManager As New PdfSecurityManager()
        Dim signedBytes As Byte() = securityManager.Sign(invoiceBytes,
            certificateFile, CertificatePassword, "Issuer", "Invoice issued", "Head office", "billing@example.com",
            0, signatureBounds, tsa)

        File.WriteAllBytes(Path.Combine(sampleFolder, "SignedInvoice.pdf"), signedBytes)
        Console.WriteLine("SignedInvoice.pdf: invoice.pdf signed in memory")
    End Sub

    ' Sign a password protected document read from a stream: SourcePdfPassword opens it.
    Private Sub SignProtectedStream(tsa As TimeStampAuthority)
        ' first protect the report: open password "user", owner password "owner"
        Dim options As New PdfSecurityOptions()
        options.UserPassword = "user"
        options.OwnerPassword = "owner"
        Dim protectedBytes As Byte() = New PdfSecurityManager(options).GetSecuredPdfBytes(Path.Combine(sampleFolder, "report.pdf"))

        Dim securityManager As New PdfSecurityManager()
        securityManager.SourcePdfPassword = "owner"

        Using source As New MemoryStream(protectedBytes),
              destination As FileStream = File.Create(Path.Combine(sampleFolder, "SignedProtected.pdf"))
            ' the signature goes on the last page (index 5) of the 6 pages report
            securityManager.Sign(source, destination, certificateFile, CertificatePassword,
                "Approval", "Approved for distribution", "Head office", "signer@example.com",
                5, signatureBounds, tsa)
        End Using
        Console.WriteLine("SignedProtected.pdf: the protected report signed on page 6")
    End Sub

    ' Certify a document: the author's certification signature states the changes allowed afterwards. Here form
    ' filling and signing are allowed, so an approver can still sign the empty field added before certifying.
    Private Sub CertifyAndApprove(tsa As TimeStampAuthority)
        Dim withField As String = Path.Combine(sampleFolder, "ReportWithApprovalField.pdf")
        Dim certified As String = Path.Combine(sampleFolder, "CertifiedReport.pdf")
        Dim approved As String = Path.Combine(sampleFolder, "CertifiedAndApprovedReport.pdf")

        ' the field of the approver is added first: a certified document can be signed, not given new fields
        Dim fieldManager As New PdfSecurityManager()
        fieldManager.AddSignatureField(Path.Combine(sampleFolder, "report.pdf"), withField,
            "Approval", 0, New RectangleF(40, 700, 200, 60))

        Dim author As New PdfSecurityManager()
        author.CertificationLevel = PdfCertificationLevel.FormFillingAllowed
        author.Sign(withField, certified, certificateFile, CertificatePassword,
            "Author", "I am the author of this report", "Head office", "author@example.com",
            0, signatureBounds, tsa)
        Console.WriteLine("CertifiedReport.pdf: report.pdf certified (form filling and signing allowed)")

        ' the approver signs the existing field: its position comes from the field
        Dim approver As New PdfSecurityManager()
        approver.Sign(certified, approved, certificateFile, CertificatePassword,
            "Approval", "Approved", "Branch office", "approver@example.com",
            0, RectangleF.Empty, tsa)
        Console.WriteLine("CertifiedAndApprovedReport.pdf: the certified report signed by the approver")
    End Sub

    ' Check the signatures: integrity, signer, certificate and time stamp. The sample certificate is self-signed,
    ' so the signer is unknown until you trust its certificate (TrustedRootCertificates).
    Private Sub ValidateSignedDocuments()
        Console.WriteLine()
        Dim securityManager As New PdfSecurityManager()
        For Each name As String In New String() {"SignedReport.pdf", "SignedInvoice.pdf", "CertifiedAndApprovedReport.pdf"}
            Console.WriteLine(name & ":")
            For Each result As SignatureValidationResult In securityManager.ValidateSignatures(Path.Combine(sampleFolder, name))
                PrintResult(result)
            Next
        Next

        ' the protected document is opened with its password
        securityManager.SourcePdfPassword = "user"
        Console.WriteLine("SignedProtected.pdf:")
        For Each result As SignatureValidationResult In securityManager.ValidateSignatures(Path.Combine(sampleFolder, "SignedProtected.pdf"))
            PrintResult(result)
        Next

        ' trust the sample certificate: the signature becomes valid. CheckRevocation = True would also check
        ' online that the certificates are not revoked.
        Dim options As New SignatureValidationOptions()
        options.TrustedRootCertificates.Add(
            System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                certificateFile, CertificatePassword))
        Console.WriteLine("SignedReport.pdf, trusting the sample certificate:")
        For Each result As SignatureValidationResult In New PdfSecurityManager().ValidateSignatures(Path.Combine(sampleFolder, "SignedReport.pdf"), options)
            PrintResult(result)
        Next

        ' the security settings without changing the file
        Dim info As PdfSecurityInfo = New PdfSecurityManager().GetSecurityInfo(Path.Combine(sampleFolder, "CertifiedAndApprovedReport.pdf"))
        Console.WriteLine("CertifiedAndApprovedReport.pdf: {0} signatures, certified: {1} ({2})",
            info.SignatureCount, info.IsCertified, info.CertificationLevel)
    End Sub

    Private Sub PrintResult(result As SignatureValidationResult)
        Console.WriteLine("  {0}: {1}, signed by {2} on {3}", result.FieldName, result.Status, result.SignerName, result.SigningTime)
        Console.WriteLine("    modified after signing: {0}, certificate trusted: {1}, time stamp: {2}{3}",
            result.IsDocumentModified, result.IsCertificateChainTrusted, result.HasTimeStamp,
            If(result.IsCertification, ", certification: " & result.CertificationLevel.ToString(), ""))
        For Each message As String In result.Messages
            Console.WriteLine("    " & message)
        Next
    End Sub

End Module
