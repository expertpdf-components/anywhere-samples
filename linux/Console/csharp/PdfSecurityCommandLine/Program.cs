using System;
using System.IO;
using ExpertPdf.Anywhere.PDFSecurity;

namespace PdfSecurityCommandLine
{
    /// <summary>
    /// A command line tool that protects a PDF document with passwords and permissions, or removes the protection
    /// of a document you have the password of. Run it without arguments to protect the document that comes with
    /// the sample and then remove the protection again.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
                return RunDemo();

            string srcPdfFile = null;
            string outFile = null;
            bool removeSecurity = false;
            bool showInfo = false;
            PdfSecurityOptions options = new PdfSecurityOptions();

            try
            {
                foreach (string argument in args)
                {
                    string value;
                    if (TryGetValue(argument, "/pdf:", out value))
                        srcPdfFile = Path.GetFullPath(value);
                    else if (TryGetValue(argument, "/out:", out value))
                        outFile = Path.GetFullPath(value);
                    else if (TryGetValue(argument, "/userpswd:", out value))
                        options.UserPassword = value;
                    else if (TryGetValue(argument, "/ownerpswd:", out value))
                        options.OwnerPassword = value;
                    else if (TryGetValue(argument, "/keysize:", out value))
                        options.KeySize = ParseKeySize(value);
                    else if (TryGetValue(argument, "/algo:", out value))
                        options.Algorithm = ParseAlgorithm(value);
                    else if (IsSwitch(argument, "/noprint"))
                        options.CanPrint = false;
                    else if (IsSwitch(argument, "/nofullqualityprint"))
                        options.CanFullQualityPrint = false;
                    else if (IsSwitch(argument, "/nocopy"))
                        options.CanCopyContent = false;
                    else if (IsSwitch(argument, "/noaccessibilitycopy"))
                        options.CanAccessibilityCopyContent = false;
                    else if (IsSwitch(argument, "/noeditcontent"))
                        options.CanEditContent = false;
                    else if (IsSwitch(argument, "/noeditannot"))
                        options.CanEditAnnotations = false;
                    else if (IsSwitch(argument, "/nofillform"))
                        options.CanFillFormFields = false;
                    else if (IsSwitch(argument, "/noassembly"))
                        options.CanAssembleDocument = false;
                    else if (IsSwitch(argument, "/removesecurity"))
                        removeSecurity = true;
                    else if (IsSwitch(argument, "/info"))
                        showInfo = true;
                    else
                        throw new ArgumentException("Unknown argument: " + argument);
                }

                if (srcPdfFile == null)
                    throw new ArgumentException("The source PDF document is not specified.");
                if (outFile == null)
                    outFile = Path.GetFullPath(removeSecurity ? "UnsecuredDocument.pdf" : "SecuredDocument.pdf");
                if (removeSecurity && String.IsNullOrEmpty(options.OwnerPassword) && String.IsNullOrEmpty(options.UserPassword))
                    throw new ArgumentException("The owner or the user password is required to remove the security.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                ShowUsage();
                return 1;
            }

            try
            {
                PdfSecurityManager securityManager = new PdfSecurityManager(options);
                // securityManager.LicenseKey = "your license key";

                if (showInfo)
                {
                    // the password is needed only for the signature information of a document with an open password
                    string password = !String.IsNullOrEmpty(options.OwnerPassword) ? options.OwnerPassword : options.UserPassword;
                    PrintSecurityInfo(srcPdfFile, securityManager.GetSecurityInfo(srcPdfFile, password));
                }
                else if (removeSecurity)
                {
                    // the owner password gives full rights; the user password also works when it is the only one known
                    string password = !String.IsNullOrEmpty(options.OwnerPassword) ? options.OwnerPassword : options.UserPassword;
                    securityManager.SaveUnSecuredPdfToFile(srcPdfFile, outFile, password);
                    Console.WriteLine("Security removed: " + outFile);
                }
                else
                {
                    securityManager.SaveSecuredPdfToFile(srcPdfFile, outFile);
                    Console.WriteLine("Secured document: " + outFile);
                }
                return 0;
            }
            catch (Exception ex)
            {
                // SecurePdfException for documents that cannot be read or a wrong password
                Console.WriteLine("Error: " + ex.Message);
                return 2;
            }
        }

        // no arguments: protect the bundled document, then remove the protection again
        static int RunDemo()
        {
            ShowUsage();
            Console.WriteLine();
            Console.WriteLine("No arguments given: protecting report.pdf, then removing the protection.");

            string sampleFolder = AppDomain.CurrentDomain.BaseDirectory;
            string source = Path.Combine(sampleFolder, "report.pdf");
            string secured = Path.Combine(sampleFolder, "SecuredDocument.pdf");
            string unsecured = Path.Combine(sampleFolder, "UnsecuredDocument.pdf");

            try
            {
                PdfSecurityOptions options = new PdfSecurityOptions();
                // "user" opens the document with the permissions below; "owner" opens it with all the rights
                options.UserPassword = "user";
                options.OwnerPassword = "owner";
                options.CanPrint = false;
                options.CanCopyContent = false;
                options.CanEditContent = false;
                options.KeySize = EncryptionKeySize.EncryptKey256Bit;
                options.Algorithm = EncryptionAlgorithm.AES;

                PdfSecurityManager securityManager = new PdfSecurityManager(options);
                // securityManager.LicenseKey = "your license key";
                securityManager.SaveSecuredPdfToFile(source, secured);
                Console.WriteLine("Secured document: {0} (AES 256 bit, passwords user/owner, no printing, no copying)", secured);

                // read the security settings back, without changing the document
                PrintSecurityInfo(secured, new PdfSecurityManager().GetSecurityInfo(secured));

                // removing the security needs one of the passwords
                new PdfSecurityManager().SaveUnSecuredPdfToFile(secured, unsecured, "owner");
                Console.WriteLine("Security removed: " + unsecured);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 2;
            }
        }

        static void PrintSecurityInfo(string file, PdfSecurityInfo info)
        {
            Console.WriteLine("Security of " + file + ":");
            if (info.IsEncrypted)
                Console.WriteLine("  encrypted: {0}, {1} bit, revision {2}", info.Algorithm, info.KeyLength, info.Revision);
            else
                Console.WriteLine("  not encrypted");
            Console.WriteLine("  open password: {0}, owner password needed for full rights: {1}",
                info.IsUserPasswordRequired ? "yes" : "no", info.IsOwnerPasswordRequired ? "yes" : "no");
            Console.WriteLine("  print: {0}, high quality print: {1}, copy: {2}, accessibility copy: {3}",
                info.CanPrint, info.CanFullQualityPrint, info.CanCopyContent, info.CanAccessibilityCopyContent);
            Console.WriteLine("  edit content: {0}, edit annotations: {1}, fill forms: {2}, assemble: {3}",
                info.CanEditContent, info.CanEditAnnotations, info.CanFillFormFields, info.CanAssembleDocument);
            if (info.IsOpened)
                Console.WriteLine("  signatures: {0}, certified: {1}", info.SignatureCount, info.IsCertified ? info.CertificationLevel.ToString() : "no");
            else
                Console.WriteLine("  signatures: unknown (give the password to read them)");
        }

        static bool IsSwitch(string argument, string name)
        {
            return argument.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        static bool TryGetValue(string argument, string name, out string value)
        {
            value = null;
            if (!argument.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                return false;
            value = argument.Substring(name.Length).Trim().Trim('"');
            if (value.Length == 0)
                throw new ArgumentException("Missing value for " + name);
            return true;
        }

        static EncryptionKeySize ParseKeySize(string value)
        {
            switch (value)
            {
                case "40": return EncryptionKeySize.EncryptKey40Bit;
                case "128": return EncryptionKeySize.EncryptKey128Bit;
                case "256": return EncryptionKeySize.EncryptKey256Bit;
                default: throw new ArgumentException("The key size must be 40, 128 or 256.");
            }
        }

        static EncryptionAlgorithm ParseAlgorithm(string value)
        {
            switch (value.ToUpperInvariant())
            {
                case "AES": return EncryptionAlgorithm.AES;
                case "RC4": return EncryptionAlgorithm.RC4;
                default: throw new ArgumentException("The encryption algorithm must be RC4 or AES.");
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: PdfSecurityCommandLine /pdf:source_file [/out:file]");
            Console.WriteLine("           [/userpswd:password] [/ownerpswd:password] [/keysize:40|128|256] [/algo:RC4|AES]");
            Console.WriteLine("           [/noprint] [/nofullqualityprint] [/nocopy] [/noaccessibilitycopy]");
            Console.WriteLine("           [/noeditcontent] [/noeditannot] [/nofillform] [/noassembly] [/removesecurity] [/info]");
            Console.WriteLine();
            Console.WriteLine("With /removesecurity the owner or the user password of the source document is required.");
            Console.WriteLine("/info shows the security settings of the source document and changes nothing.");
            Console.WriteLine("Protect: PdfSecurityCommandLine /pdf:report.pdf /noprint /keysize:256 /ownerpswd:owner /userpswd:user /out:Secured.pdf");
            Console.WriteLine("Remove:  PdfSecurityCommandLine /pdf:Secured.pdf /ownerpswd:owner /removesecurity /out:Unsecured.pdf");
            Console.WriteLine("Show:    PdfSecurityCommandLine /pdf:Secured.pdf /info");
        }
    }
}
