using System;
using ExpertPdf.Anywhere.Drawing;
using System.IO;
using ExpertPdf.Anywhere.PDFSecurity;

namespace PdfDigitalSignature
{
    /// <summary>
    /// Signs PDF documents with a digital certificate (PKCS#12 / .pfx file): from a file to a file, from memory to
    /// memory, and from a stream to a stream for a password protected document. Optionally the signatures get a
    /// time stamp from a time stamp authority (TSA). Then it certifies a document (a certification signature that
    /// allows form filling and signing) with an empty field signed by an approver, and checks every signature
    /// it made with ValidateSignatures.
    ///
    /// certificate.pfx is a self-signed TEST certificate made for this sample (password: expertpdf). Viewers show
    /// the signature as valid but the signer as not trusted. Sign your documents with a certificate issued for you
    /// by a certification authority.
    /// </summary>
    class Program
    {
        static string sampleFolder = AppDomain.CurrentDomain.BaseDirectory;
        static string certificateFile = Path.Combine(sampleFolder, "certificate.pfx");
        const string CertificatePassword = "expertpdf";

        // the signature box, in points from the top left corner of the page: the bottom right of an A4 page
        static RectangleF signatureBounds = new RectangleF(345, 700, 200, 60);

        static int Main(string[] args)
        {
            // an optional time stamp server, for example: PdfDigitalSignature /tsa:https://your-tsa-server/
            TimeStampAuthority tsa = null;
            foreach (string argument in args)
            {
                if (argument.StartsWith("/tsa:", StringComparison.OrdinalIgnoreCase))
                {
                    tsa = new TimeStampAuthority(new Uri(argument.Substring(5)));
                    // tsa.UserName = "user";   // when the server requires a login
                    // tsa.Password = "password";
                    Console.WriteLine("Time stamps from " + tsa.Server);
                }
                else
                {
                    Console.WriteLine("Usage: PdfDigitalSignature [/tsa:time_stamp_server_url]");
                    return 1;
                }
            }

            try
            {
                SignFileToFile(tsa);
                SignBytes(tsa);
                SignProtectedStream(tsa);
                CertifyAndApprove(tsa);
                ValidateSignedDocuments();

                Console.WriteLine();
                Console.WriteLine("The signed documents are in " + sampleFolder);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 2;
            }
        }

        // Sign a file and save the signed document in another file.
        static void SignFileToFile(TimeStampAuthority tsa)
        {
            PdfSecurityManager securityManager = new PdfSecurityManager();
            // securityManager.LicenseKey = "your license key";

            string signedFile = Path.Combine(sampleFolder, "SignedReport.pdf");
            securityManager.Sign(Path.Combine(sampleFolder, "report.pdf"), signedFile,
                certificateFile, CertificatePassword,
                "Approval",                      // the name of the signature field
                "I approve this report",         // reason
                "Head office",                     // location
                "signer@example.com",            // contact info
                0,                               // the page of the signature box: the first page (index 0)
                signatureBounds, tsa);
            Console.WriteLine("SignedReport.pdf: report.pdf signed on page 1");
        }

        // Sign a document you have in memory and get the signed document as bytes.
        static void SignBytes(TimeStampAuthority tsa)
        {
            byte[] invoiceBytes = File.ReadAllBytes(Path.Combine(sampleFolder, "invoice.pdf"));

            byte[] signedBytes = new PdfSecurityManager().Sign(invoiceBytes,
                certificateFile, CertificatePassword, "Issuer", "Invoice issued", "Head office", "billing@example.com",
                0, signatureBounds, tsa);

            File.WriteAllBytes(Path.Combine(sampleFolder, "SignedInvoice.pdf"), signedBytes);
            Console.WriteLine("SignedInvoice.pdf: invoice.pdf signed in memory");
        }

        // Sign a password protected document read from a stream: SourcePdfPassword opens it.
        static void SignProtectedStream(TimeStampAuthority tsa)
        {
            // first protect the report: open password "user", owner password "owner"
            PdfSecurityOptions options = new PdfSecurityOptions();
            options.UserPassword = "user";
            options.OwnerPassword = "owner";
            byte[] protectedBytes = new PdfSecurityManager(options).GetSecuredPdfBytes(Path.Combine(sampleFolder, "report.pdf"));

            PdfSecurityManager securityManager = new PdfSecurityManager();
            securityManager.SourcePdfPassword = "owner";

            using (MemoryStream source = new MemoryStream(protectedBytes))
            using (FileStream destination = File.Create(Path.Combine(sampleFolder, "SignedProtected.pdf")))
            {
                // the signature goes on the last page (index 5) of the 6 pages report
                securityManager.Sign(source, destination, certificateFile, CertificatePassword,
                    "Approval", "Approved for distribution", "Head office", "signer@example.com",
                    5, signatureBounds, tsa);
            }
            Console.WriteLine("SignedProtected.pdf: the protected report signed on page 6");
        }

        // Certify a document: the author's certification signature states the changes allowed afterwards. Here form
        // filling and signing are allowed, so an approver can still sign the empty field added before certifying.
        static void CertifyAndApprove(TimeStampAuthority tsa)
        {
            string withField = Path.Combine(sampleFolder, "ReportWithApprovalField.pdf");
            string certified = Path.Combine(sampleFolder, "CertifiedReport.pdf");
            string approved = Path.Combine(sampleFolder, "CertifiedAndApprovedReport.pdf");

            // the field of the approver is added first: a certified document can be signed, not given new fields
            new PdfSecurityManager().AddSignatureField(Path.Combine(sampleFolder, "report.pdf"), withField,
                "Approval", 0, new RectangleF(40, 700, 200, 60));

            PdfSecurityManager author = new PdfSecurityManager();
            author.CertificationLevel = PdfCertificationLevel.FormFillingAllowed;
            author.Sign(withField, certified, certificateFile, CertificatePassword,
                "Author", "I am the author of this report", "Head office", "author@example.com",
                0, signatureBounds, tsa);
            Console.WriteLine("CertifiedReport.pdf: report.pdf certified (form filling and signing allowed)");

            // the approver signs the existing field: its position comes from the field
            new PdfSecurityManager().Sign(certified, approved, certificateFile, CertificatePassword,
                "Approval", "Approved", "Branch office", "approver@example.com",
                0, RectangleF.Empty, tsa);
            Console.WriteLine("CertifiedAndApprovedReport.pdf: the certified report signed by the approver");
        }

        // Check the signatures: integrity, signer, certificate and time stamp. The sample certificate is self-signed,
        // so the signer is unknown until you trust its certificate (TrustedRootCertificates).
        static void ValidateSignedDocuments()
        {
            Console.WriteLine();
            PdfSecurityManager securityManager = new PdfSecurityManager();
            foreach (string name in new string[] { "SignedReport.pdf", "SignedInvoice.pdf", "CertifiedAndApprovedReport.pdf" })
            {
                Console.WriteLine(name + ":");
                foreach (SignatureValidationResult result in securityManager.ValidateSignatures(Path.Combine(sampleFolder, name)))
                    PrintResult(result);
            }

            // the protected document is opened with its password
            securityManager.SourcePdfPassword = "user";
            Console.WriteLine("SignedProtected.pdf:");
            foreach (SignatureValidationResult result in securityManager.ValidateSignatures(Path.Combine(sampleFolder, "SignedProtected.pdf")))
                PrintResult(result);

            // trust the sample certificate: the signature becomes valid. CheckRevocation = true would also check
            // online that the certificates are not revoked.
            SignatureValidationOptions options = new SignatureValidationOptions();
            options.TrustedRootCertificates.Add(
                System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                    certificateFile, CertificatePassword));
            Console.WriteLine("SignedReport.pdf, trusting the sample certificate:");
            foreach (SignatureValidationResult result in new PdfSecurityManager().ValidateSignatures(Path.Combine(sampleFolder, "SignedReport.pdf"), options))
                PrintResult(result);

            // the security settings without changing the file
            PdfSecurityInfo info = new PdfSecurityManager().GetSecurityInfo(Path.Combine(sampleFolder, "CertifiedAndApprovedReport.pdf"));
            Console.WriteLine("CertifiedAndApprovedReport.pdf: {0} signatures, certified: {1} ({2})",
                info.SignatureCount, info.IsCertified, info.CertificationLevel);
        }

        static void PrintResult(SignatureValidationResult result)
        {
            Console.WriteLine("  {0}: {1}, signed by {2} on {3}", result.FieldName, result.Status, result.SignerName, result.SigningTime);
            Console.WriteLine("    modified after signing: {0}, certificate trusted: {1}, time stamp: {2}{3}",
                result.IsDocumentModified, result.IsCertificateChainTrusted, result.HasTimeStamp,
                result.IsCertification ? ", certification: " + result.CertificationLevel : "");
            foreach (string message in result.Messages)
                Console.WriteLine("    " + message);
        }
    }
}
