Imports System.IO
Imports ExpertPdf.Anywhere.PDFSecurity

''' <summary>
''' Shows the security features: open passwords, permissions without a password, the encryption
''' algorithms and key sizes, working from files or memory, reading the security settings of a document, and
''' removing the security again.
''' </summary>
Module Program

    Private ReadOnly sampleFolder As String = AppDomain.CurrentDomain.BaseDirectory
    Private ReadOnly report As String = Path.Combine(sampleFolder, "report.pdf")
    Private ReadOnly outFolder As String = Path.Combine(sampleFolder, "SecurityOutput")

    Function Main(args As String()) As Integer
        Try
            Directory.CreateDirectory(outFolder)

            UserPasswordOnly()
            PermissionsOnly()
            EncryptionAlgorithms()
            ReadSecurityInfo()
            RemoveSecurity()

            Console.WriteLine()
            Console.WriteLine("The documents are in " & outFolder)
            Return 0
        Catch ex As Exception
            Console.WriteLine("Error: " & ex.Message)
            Return 1
        End Try
    End Function

    ' A password to open the document. Without an owner password, the user password is also the owner
    ' password: whoever opens the document has all the rights.
    Private Sub UserPasswordOnly()
        Dim options As New PdfSecurityOptions()
        options.UserPassword = "open"

        Dim securityManager As New PdfSecurityManager(options)
        ' securityManager.LicenseKey = "your license key"

        ' from a file to memory
        Dim securedBytes As Byte() = securityManager.GetSecuredPdfBytes(report)
        File.WriteAllBytes(Path.Combine(outFolder, "UserPasswordOnly.pdf"), securedBytes)
        Console.WriteLine("UserPasswordOnly.pdf: opens with the password ""open"", which gives all the rights")
    End Sub

    ' No open password, but restricted permissions: anyone can read the document, only the owner
    ' password holder can print in high quality, copy or change it.
    Private Sub PermissionsOnly()
        Dim options As New PdfSecurityOptions()
        options.OwnerPassword = "owner"
        options.CanPrint = True
        options.CanFullQualityPrint = False     ' low resolution printing only
        options.CanCopyContent = False
        options.CanAccessibilityCopyContent = True   ' screen readers can still read the text
        options.CanEditContent = False
        options.CanEditAnnotations = False
        options.CanFillFormFields = True
        options.CanAssembleDocument = False

        ' from memory to a file
        Dim sourceBytes As Byte() = File.ReadAllBytes(report)
        Dim securityManager As New PdfSecurityManager(options)
        securityManager.SaveSecuredPdfToFile(sourceBytes, Path.Combine(outFolder, "PermissionsOnly.pdf"))
        Console.WriteLine("PermissionsOnly.pdf: opens without a password; low quality printing, form filling and accessibility only")
    End Sub

    ' AES is the default algorithm and 128 bit the default key size. RC4 with 40 bit keys is only for
    ' very old viewers; use AES 256 for new documents.
    Private Sub EncryptionAlgorithms()
        Dim sourceBytes As Byte() = File.ReadAllBytes(report)
        Encrypt(sourceBytes, EncryptionAlgorithm.RC4, EncryptionKeySize.EncryptKey40Bit, "Encrypted-RC4-40.pdf")
        Encrypt(sourceBytes, EncryptionAlgorithm.RC4, EncryptionKeySize.EncryptKey128Bit, "Encrypted-RC4-128.pdf")
        Encrypt(sourceBytes, EncryptionAlgorithm.AES, EncryptionKeySize.EncryptKey128Bit, "Encrypted-AES-128.pdf")
        Encrypt(sourceBytes, EncryptionAlgorithm.AES, EncryptionKeySize.EncryptKey256Bit, "Encrypted-AES-256.pdf")
    End Sub

    Private Sub Encrypt(sourceBytes As Byte(), algorithm As EncryptionAlgorithm, keySize As EncryptionKeySize, fileName As String)
        Dim options As New PdfSecurityOptions()
        options.UserPassword = "user"
        options.OwnerPassword = "owner"
        options.Algorithm = algorithm
        options.KeySize = keySize

        ' from memory to memory
        Dim securedBytes As Byte() = New PdfSecurityManager(options).GetSecuredPdfBytes(sourceBytes)
        File.WriteAllBytes(Path.Combine(outFolder, fileName), securedBytes)
        Console.WriteLine("{0}: {1}, {2}, passwords user/owner", fileName, algorithm, keySize)
    End Sub

    ' GetSecurityInfo reads the security settings without changing the document. The encryption settings and the
    ' permissions are read even without the password; a password is needed only for the signature information.
    Private Sub ReadSecurityInfo()
        Dim securityManager As New PdfSecurityManager()
        For Each name As String In New String() {"Encrypted-RC4-40.pdf", "Encrypted-RC4-128.pdf", "Encrypted-AES-128.pdf",
                "Encrypted-AES-256.pdf", "PermissionsOnly.pdf"}
            Dim info As PdfSecurityInfo = securityManager.GetSecurityInfo(Path.Combine(outFolder, name))
            Console.WriteLine("{0}: {1} {2} bit (revision {3}), open password: {4}, owner password needed: {5}",
                name, info.Algorithm, info.KeyLength, info.Revision, info.IsUserPasswordRequired, info.IsOwnerPasswordRequired)
            Console.WriteLine("    print: {0}, high quality print: {1}, copy: {2}, edit: {3}, fill forms: {4}, assemble: {5}",
                info.CanPrint, info.CanFullQualityPrint, info.CanCopyContent, info.CanEditContent,
                info.CanFillFormFields, info.CanAssembleDocument)
        Next

        Dim plain As PdfSecurityInfo = securityManager.GetSecurityInfo(report)
        Console.WriteLine("report.pdf: encrypted: {0}, signed: {1}", plain.IsEncrypted, plain.IsSigned)
    End Sub

    ' Removing the security needs the user or the owner password of the document.
    Private Sub RemoveSecurity()
        Dim securityManager As New PdfSecurityManager()

        ' from a file to memory, with the owner password
        Dim plainBytes As Byte() = securityManager.GetUnSecuredPdfBytes(Path.Combine(outFolder, "Encrypted-AES-256.pdf"), "owner")
        File.WriteAllBytes(Path.Combine(outFolder, "Unsecured-AES-256.pdf"), plainBytes)
        Console.WriteLine("Unsecured-AES-256.pdf: the security of Encrypted-AES-256.pdf removed with the owner password")

        ' from memory to a file, with the user password
        Dim securedBytes As Byte() = File.ReadAllBytes(Path.Combine(outFolder, "Encrypted-RC4-128.pdf"))
        securityManager.SaveUnSecuredPdfToFile(securedBytes, Path.Combine(outFolder, "Unsecured-RC4-128.pdf"), "user")
        Console.WriteLine("Unsecured-RC4-128.pdf: the security of Encrypted-RC4-128.pdf removed with the user password")

        ' a wrong password is reported with an exception
        Try
            securityManager.GetUnSecuredPdfBytes(securedBytes, "wrong password")
        Catch ex As Exception
            Console.WriteLine("Wrong password: " & ex.GetType().Name & " - " & ex.Message)
        End Try
    End Sub

End Module
