using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class DigitalSignaturesModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // create a new pdf document
            Document doc = new Document();

            // add a new page to the document
            PdfPage page = doc.AddPage();

            // get image path 
            // the image will be used to display the digital signature over it
            string imgFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "logo.png");

            // get certificate path
            string certFile = Path.Combine(Startup.ServerPath, "wwwroot", "files", "oss.pfx");

            // define a rendering result object
            AddElementResult result;

            // create image element from file path 
            ImageElement img = new ImageElement(0, 0, imgFile);
            result = page.AddElement(img);

            // get the #PKCS12 certificate from file
            DigitalCertificatesCollection certificates =
                DigitalCertificatesStore.GetCertificates(certFile, "osspswd");
            DigitalCertificate certificate = certificates[0];

            // create the digital signature object
            DigitalSignatureElement signature =
                new DigitalSignatureElement(result.EndPageBounds, certificate);
            signature.Reason = "ExpertPdf";
            signature.ContactInfo = "ExpertPdf";
            signature.Location = "ExpertPdf";
            page.AddElement(signature);

            // save pdf document
            byte[] pdf = doc.Save();

            // close pdf document
            doc.Close();

            // return resulted pdf document
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "Sample.pdf";
            return fileResult;

        }
    }
}
