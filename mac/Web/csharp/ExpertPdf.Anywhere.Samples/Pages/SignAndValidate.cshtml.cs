using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.Drawing;
using ExpertPdf.Anywhere.PDFSecurity;

namespace ExpertPdf.Samples.Pages
{
    public class SignAndValidateModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtReason { get; set; } = "Approved";

        [BindProperty]
        public string TxtLocation { get; set; } = "Bucharest";

        [BindProperty]
        public string DdlDigest { get; set; } = "SHA256";

        [BindProperty]
        public bool ChkBackground { get; set; } = true;

        [BindProperty]
        public bool ChkValidate { get; set; }

        public List<SelectListItem> Digests { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "SHA256", Text = "SHA-256" },
            new SelectListItem { Value = "SHA384", Text = "SHA-384" },
            new SelectListItem { Value = "SHA512", Text = "SHA-512" },
        };

        public IActionResult OnPost()
        {
            string filesFolder = Path.Combine(Startup.ServerPath, "wwwroot", "files");
            byte[] source = System.IO.File.ReadAllBytes(Path.Combine(filesFolder, "w-9.pdf"));
            string certFile = Path.Combine(filesFolder, "oss.pfx");

            PdfSecurityManager securityManager = new PdfSecurityManager();

            // the digest the signature is computed with
            securityManager.DigestAlgorithm = (SignatureDigest)Enum.Parse(typeof(SignatureDigest), DdlDigest);

            // an image behind the text of the visible signature, faint so the text stays readable
            if (ChkBackground)
            {
                securityManager.SignatureBackgroundImage =
                    System.IO.File.ReadAllBytes(Path.Combine(filesFolder, "logo.png"));
                securityManager.SignatureBackgroundOpacity = 0.25f;
            }

            // sign the first page, in a box at its top-right corner (points from the top-left of the page)
            byte[] signed = securityManager.Sign(source, certFile, "osspswd", "Signature1",
                TxtReason, TxtLocation, "contact@example.com", 0, new RectangleF(320, 36, 240, 70), null);

            if (!ChkValidate)
            {
                FileResult signedResult = new FileContentResult(signed, "application/pdf");
                signedResult.FileDownloadName = "Sample.pdf";
                return signedResult;
            }

            // check the signatures of the signed document; the sample certificate is self-signed, so it is not
            // trusted unless it is passed as a trusted root
            SignatureValidationResult[] results = new PdfSecurityManager().ValidateSignatures(signed);

            StringBuilder report = new StringBuilder();
            foreach (SignatureValidationResult result in results)
            {
                report.AppendLine("Signature: " + result.FieldName);
                report.AppendLine("  Status: " + result.Status);
                report.AppendLine("  Signed by: " + result.SignerName);
                report.AppendLine("  Signing time: " + result.SigningTime);
                report.AppendLine("  Reason: " + result.Reason + ", location: " + result.Location);
                report.AppendLine("  Digest: " + result.DigestAlgorithm + ", algorithm: " + result.SignatureAlgorithm);
                report.AppendLine("  Changed after signing: " + result.IsDocumentModified);
                report.AppendLine("  Covers the whole document: " + result.CoversWholeDocument);
                report.AppendLine("  Certificate chain trusted: " + result.IsCertificateChainTrusted);
                foreach (string message in result.Messages)
                {
                    report.AppendLine("  " + message);
                }
            }

            FileResult fileResult = new FileContentResult(Encoding.UTF8.GetBytes(report.ToString()), "text/plain");
            fileResult.FileDownloadName = "Validation.txt";
            return fileResult;
        }
    }
}
