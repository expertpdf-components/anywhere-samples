using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpertPdf.Anywhere.PDFSecurity;

namespace ExpertPdf.Samples.Pages
{
    public class PdfSecurityModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty]
        public string TxtUserPassword { get; set; } = "user";

        [BindProperty]
        public string TxtOwnerPassword { get; set; } = "owner";

        [BindProperty]
        public string DdlEncryption { get; set; } = "AES256";

        [BindProperty]
        public bool ChkEncryptMetadata { get; set; } = true;

        [BindProperty]
        public bool ChkCanPrint { get; set; } = true;

        [BindProperty]
        public bool ChkCanFullQualityPrint { get; set; } = true;

        [BindProperty]
        public bool ChkCanCopyContent { get; set; }

        [BindProperty]
        public bool ChkCanEditContent { get; set; }

        [BindProperty]
        public bool ChkCanEditAnnotations { get; set; }

        [BindProperty]
        public bool ChkCanFillFormFields { get; set; } = true;

        [BindProperty]
        public bool ChkCanAssembleDocument { get; set; }

        public List<SelectListItem> Encryptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "AES256", Text = "AES 256-bit (recommended)" },
            new SelectListItem { Value = "AESGCM", Text = "AES-GCM 256-bit (PDF 2.0)" },
            new SelectListItem { Value = "AES128", Text = "AES 128-bit" },
            new SelectListItem { Value = "RC4128", Text = "RC4 128-bit" },
            new SelectListItem { Value = "RC440", Text = "RC4 40-bit" },
        };

        public IActionResult OnPost()
        {
            string pdfFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "doc2.pdf");

            // the security options
            PdfSecurityOptions options = new PdfSecurityOptions();
            options.UserPassword = TxtUserPassword;
            options.OwnerPassword = TxtOwnerPassword;

            // the encryption algorithm and key size
            switch (DdlEncryption)
            {
                case "AESGCM":
                    options.Algorithm = EncryptionAlgorithm.AESGCM;
                    options.KeySize = EncryptionKeySize.EncryptKey256Bit;
                    break;
                case "AES128":
                    options.Algorithm = EncryptionAlgorithm.AES;
                    options.KeySize = EncryptionKeySize.EncryptKey128Bit;
                    break;
                case "RC4128":
                    options.Algorithm = EncryptionAlgorithm.RC4;
                    options.KeySize = EncryptionKeySize.EncryptKey128Bit;
                    break;
                case "RC440":
                    options.Algorithm = EncryptionAlgorithm.RC4;
                    options.KeySize = EncryptionKeySize.EncryptKey40Bit;
                    break;
                default:
                    options.Algorithm = EncryptionAlgorithm.AES;
                    options.KeySize = EncryptionKeySize.EncryptKey256Bit;
                    break;
            }
            options.EncryptMetadata = ChkEncryptMetadata;

            // what the user password allows
            options.CanPrint = ChkCanPrint;
            options.CanFullQualityPrint = ChkCanFullQualityPrint;
            options.CanCopyContent = ChkCanCopyContent;
            options.CanEditContent = ChkCanEditContent;
            options.CanEditAnnotations = ChkCanEditAnnotations;
            options.CanFillFormFields = ChkCanFillFormFields;
            options.CanAssembleDocument = ChkCanAssembleDocument;

            // secure the document
            PdfSecurityManager securityManager = new PdfSecurityManager(options);
            byte[] pdf = securityManager.GetSecuredPdfBytes(pdfFile);

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;
        }
    }
}
