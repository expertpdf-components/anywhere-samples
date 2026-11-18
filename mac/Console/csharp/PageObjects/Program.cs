using System;
using System.Globalization;
using System.IO;
using ExpertPdf.Anywhere.PdfDocument;

namespace PageObjects
{
    /// <summary>
    /// Reads and changes what a page of an existing PDF document draws:
    ///   letter.pdf          a one page letter the sample builds (a logo, a title, a few lines, a DRAFT mark)
    ///   letter-changed.pdf  the same letter with a text replaced and recolored, the logo moved to the right
    ///                       and the DRAFT mark in a layer that is hidden when the document opens
    /// With a PDF file as argument, the sample lists the objects of its first page instead.
    /// </summary>
    internal static class Program
    {
        private static string s_appFolder = AppDomain.CurrentDomain.BaseDirectory;

        private static int Main(string[] args)
        {
            if (args.Length > 1 || (args.Length > 0 && (args[0] == "--help" || args[0] == "/?")))
            {
                Console.WriteLine("Usage: PageObjects [pdf file]");
                Console.WriteLine("  pdf file  a document whose first page objects are listed");
                return 0;
            }

            try
            {
                if (args.Length == 1)
                {
                    ListObjects(Path.GetFullPath(args[0]));
                    return 0;
                }

                string letter = Path.Combine(s_appFolder, "letter.pdf");
                CreateLetter(letter);
                ListObjects(letter);
                ChangeLetter(letter, Path.Combine(s_appFolder, "letter-changed.pdf"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }

            Console.WriteLine("The documents are in " + s_appFolder);
            return 0;
        }

        // A one page letter with a logo, a title, a few lines of text and a DRAFT mark.
        private static void CreateLetter(string fileName)
        {
            Document document = new Document();
            // document.LicenseKey = "your license key";
            PdfPage page = document.AddPage();

            PdfFont title = document.AddFont(StdFontBaseFamily.HelveticaBold);
            title.Size = 22;
            PdfFont body = document.AddFont(StdFontBaseFamily.Helvetica);
            body.Size = 12;
            PdfFont mark = document.AddFont(StdFontBaseFamily.HelveticaBold);
            mark.Size = 40;

            page.AddElement(new ImageElement(0, 0, 150, Path.Combine(s_appFolder, "logo.png")));
            page.AddElement(new TextElement(0, 80, "Quarterly Report - ExpertPdf", title));
            page.AddElement(new TextElement(0, 130, "Prepared by the ExpertPdf team for our partners.", body));
            page.AddElement(new TextElement(0, 152, "Revenue grew 12% over the previous quarter.", body));
            page.AddElement(new TextElement(0, 174, "Questions about this report: contact ExpertPdf support.", body));
            page.AddElement(new TextElement(0, 260, "DRAFT", mark, new PdfColor(200, 200, 200)));

            document.Save(fileName);
            document.Close();
            Console.WriteLine("  letter.pdf: created");
        }

        // Every object of the first page, in the order it is painted.
        private static void ListObjects(string fileName)
        {
            Document document = new Document(fileName);
            PageObjectModel model = document.Pages[0].ObjectModel;

            Console.WriteLine("  {0}: {1} objects on the first page", Path.GetFileName(fileName), model.Objects.Count);
            foreach (PageObject pageObject in model.Objects)
            {
                string line = "    " + pageObject.Kind + " at " + Format(pageObject.Bounds);
                TextObject text = pageObject as TextObject;
                ImageObject image = pageObject as ImageObject;
                if (text != null)
                    line += ": \"" + text.Text + "\" (" + text.FontName + ")";
                else if (image != null)
                    line += ": " + image.PixelWidth + " x " + image.PixelHeight + " pixels";
                Console.WriteLine(line);
            }

            document.Close();
        }

        // Replace a text, move the logo and put the DRAFT mark in a hidden layer.
        private static void ChangeLetter(string fileName, string outFile)
        {
            Document document = new Document(fileName);
            // document.LicenseKey = "your license key";
            PageObjectModel model = document.Pages[0].ObjectModel;

            Layer draft = null;
            foreach (TextObject text in model.GetTextObjects())
            {
                if (text.Text.Contains("ExpertPdf"))
                {
                    // the new text is written in the object's font, from the same starting point
                    text.ReplaceText(text.Text.Replace("ExpertPdf", "My Company"));
                    text.FillColor = new PdfColor(200, 30, 30);
                }
                else if (text.Text == "DRAFT")
                {
                    if (draft == null)
                        draft = document.Layers.Add("Draft", false);
                    text.Layer = draft;
                }
            }

            // the logo moves 330 points to the right
            foreach (ImageObject image in model.GetImageObjects())
                image.Translate(330, 0);

            document.Save(outFile);
            document.Close();
            Console.WriteLine("  letter-changed.pdf: text replaced, logo moved, DRAFT in a hidden layer");
        }

        private static string Format(ExpertPdf.Anywhere.Drawing.RectangleF r)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.#}, {1:0.#}) {2:0.#} x {3:0.#} pt",
                r.X, r.Y, r.Width, r.Height);
        }
    }
}
