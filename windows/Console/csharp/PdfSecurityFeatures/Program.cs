using System;
using System.IO;
using ExpertPdf.Anywhere.PDFSecurity;

namespace PdfSecurityFeatures
{
    /// <summary>
    /// Shows the security features: open passwords, permissions without a password, the encryption
    /// algorithms and key sizes, working from files or memory, reading the security settings of a document, and
    /// removing the security again.
    /// </summary>
    class Program
    {
        static string sampleFolder = AppDomain.CurrentDomain.BaseDirectory;
        static string report = Path.Combine(sampleFolder, "report.pdf");
        static string outFolder = Path.Combine(sampleFolder, "SecurityOutput");

        static int Main(string[] args)
        {
            try
            {
                Directory.CreateDirectory(outFolder);

                UserPasswordOnly();
                PermissionsOnly();
                EncryptionAlgorithms();
                ReadSecurityInfo();
                RemoveSecurity();

                Console.WriteLine();
                Console.WriteLine("The documents are in " + outFolder);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        // A password to open the document. Without an owner password, the user password is also the owner
        // password: whoever opens the document has all the rights.
        static void UserPasswordOnly()
        {
            PdfSecurityOptions options = new PdfSecurityOptions();
            options.UserPassword = "open";

            PdfSecurityManager securityManager = new PdfSecurityManager(options);
            // securityManager.LicenseKey = "your license key";

            // from a file to memory
            byte[] securedBytes = securityManager.GetSecuredPdfBytes(report);
            File.WriteAllBytes(Path.Combine(outFolder, "UserPasswordOnly.pdf"), securedBytes);
            Console.WriteLine("UserPasswordOnly.pdf: opens with the password \"open\", which gives all the rights");
        }

        // No open password, but restricted permissions: anyone can read the document, only the owner
        // password holder can print in high quality, copy or change it.
        static void PermissionsOnly()
        {
            PdfSecurityOptions options = new PdfSecurityOptions();
            options.OwnerPassword = "owner";
            options.CanPrint = true;
            options.CanFullQualityPrint = false;     // low resolution printing only
            options.CanCopyContent = false;
            options.CanAccessibilityCopyContent = true;   // screen readers can still read the text
            options.CanEditContent = false;
            options.CanEditAnnotations = false;
            options.CanFillFormFields = true;
            options.CanAssembleDocument = false;

            // from memory to a file
            byte[] sourceBytes = File.ReadAllBytes(report);
            new PdfSecurityManager(options).SaveSecuredPdfToFile(sourceBytes, Path.Combine(outFolder, "PermissionsOnly.pdf"));
            Console.WriteLine("PermissionsOnly.pdf: opens without a password; low quality printing, form filling and accessibility only");
        }

        // AES is the default algorithm and 128 bit the default key size. RC4 with 40 bit keys is only for
        // very old viewers; use AES 256 for new documents.
        static void EncryptionAlgorithms()
        {
            byte[] sourceBytes = File.ReadAllBytes(report);
            Encrypt(sourceBytes, EncryptionAlgorithm.RC4, EncryptionKeySize.EncryptKey40Bit, "Encrypted-RC4-40.pdf");
            Encrypt(sourceBytes, EncryptionAlgorithm.RC4, EncryptionKeySize.EncryptKey128Bit, "Encrypted-RC4-128.pdf");
            Encrypt(sourceBytes, EncryptionAlgorithm.AES, EncryptionKeySize.EncryptKey128Bit, "Encrypted-AES-128.pdf");
            Encrypt(sourceBytes, EncryptionAlgorithm.AES, EncryptionKeySize.EncryptKey256Bit, "Encrypted-AES-256.pdf");
        }

        static void Encrypt(byte[] sourceBytes, EncryptionAlgorithm algorithm, EncryptionKeySize keySize, string fileName)
        {
            PdfSecurityOptions options = new PdfSecurityOptions();
            options.UserPassword = "user";
            options.OwnerPassword = "owner";
            options.Algorithm = algorithm;
            options.KeySize = keySize;

            // from memory to memory
            byte[] securedBytes = new PdfSecurityManager(options).GetSecuredPdfBytes(sourceBytes);
            File.WriteAllBytes(Path.Combine(outFolder, fileName), securedBytes);
            Console.WriteLine("{0}: {1}, {2}, passwords user/owner", fileName, algorithm, keySize);
        }

        // GetSecurityInfo reads the security settings without changing the document. The encryption settings and the
        // permissions are read even without the password; a password is needed only for the signature information.
        static void ReadSecurityInfo()
        {
            PdfSecurityManager securityManager = new PdfSecurityManager();
            foreach (string name in new string[] { "Encrypted-RC4-40.pdf", "Encrypted-RC4-128.pdf", "Encrypted-AES-128.pdf",
                "Encrypted-AES-256.pdf", "PermissionsOnly.pdf" })
            {
                PdfSecurityInfo info = securityManager.GetSecurityInfo(Path.Combine(outFolder, name));
                Console.WriteLine("{0}: {1} {2} bit (revision {3}), open password: {4}, owner password needed: {5}",
                    name, info.Algorithm, info.KeyLength, info.Revision, info.IsUserPasswordRequired, info.IsOwnerPasswordRequired);
                Console.WriteLine("    print: {0}, high quality print: {1}, copy: {2}, edit: {3}, fill forms: {4}, assemble: {5}",
                    info.CanPrint, info.CanFullQualityPrint, info.CanCopyContent, info.CanEditContent,
                    info.CanFillFormFields, info.CanAssembleDocument);
            }

            PdfSecurityInfo plain = securityManager.GetSecurityInfo(report);
            Console.WriteLine("report.pdf: encrypted: {0}, signed: {1}", plain.IsEncrypted, plain.IsSigned);
        }

        // Removing the security needs the user or the owner password of the document.
        static void RemoveSecurity()
        {
            PdfSecurityManager securityManager = new PdfSecurityManager();

            // from a file to memory, with the owner password
            byte[] plainBytes = securityManager.GetUnSecuredPdfBytes(Path.Combine(outFolder, "Encrypted-AES-256.pdf"), "owner");
            File.WriteAllBytes(Path.Combine(outFolder, "Unsecured-AES-256.pdf"), plainBytes);
            Console.WriteLine("Unsecured-AES-256.pdf: the security of Encrypted-AES-256.pdf removed with the owner password");

            // from memory to a file, with the user password
            byte[] securedBytes = File.ReadAllBytes(Path.Combine(outFolder, "Encrypted-RC4-128.pdf"));
            securityManager.SaveUnSecuredPdfToFile(securedBytes, Path.Combine(outFolder, "Unsecured-RC4-128.pdf"), "user");
            Console.WriteLine("Unsecured-RC4-128.pdf: the security of Encrypted-RC4-128.pdf removed with the user password");

            // a wrong password is reported with an exception
            try
            {
                securityManager.GetUnSecuredPdfBytes(securedBytes, "wrong password");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wrong password: " + ex.GetType().Name + " - " + ex.Message);
            }
        }
    }
}
