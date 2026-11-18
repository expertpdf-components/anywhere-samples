using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpertPdf.Anywhere.PdfDocument;

namespace ExpertPdf.Samples.Pages
{
    public class GraphicElementsModel : PageModel
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

            // define a rendering result object
            AddElementResult result;

            // simple horizontal line element
            LineElement line1 = new LineElement(0, 0, 400, 0);
            line1.LineStyle.LineWidth = 3;
            result = page.AddElement(line1);

            // horizontal dotted red line element
            LineElement line2 = new LineElement(0, 30, 400, 30);
            line2.ForeColor = ExpertPdf.Anywhere.Drawing.Color.Red;
            line2.LineStyle.LineWidth = 2;
            line2.LineStyle.LineDashStyle = LineDashStyle.Dot;
            result = page.AddElement(line2);

            // horizontal dashed blue line element with complex styles
            LineElement line3 = new LineElement(0, 60, 400, 60);
            line3.ForeColor = ExpertPdf.Anywhere.Drawing.Color.Blue;
            line3.LineStyle.LineWidth = 2;
            line3.LineStyle.LineDashStyle = LineDashStyle.Dash;
            line3.LineStyle.LineCapStyle = LineCapStyle.RoundCap;
            line3.LineStyle.LineJoinStyle = LineJoinStyle.RoundJoin;
            result = page.AddElement(line3);

            // simple rectangle
            RectangleElement rect1 = new RectangleElement(0, 90, 400, 100);
            result = page.AddElement(rect1);

            // simple circle
            CircleElement circ1 = new CircleElement(50, 250, 50);
            circ1.ForeColor = ExpertPdf.Anywhere.Drawing.Color.Green;
            result = page.AddElement(circ1);

            // ellipse filled with gradient background color
            EllipseElement elli1 = new EllipseElement(300, 250, 100, 50);
            elli1.ForeColor = ExpertPdf.Anywhere.Drawing.Color.FromArgb(165, 42, 42);
            elli1.Gradient = new GradientColor(GradientDirection.ForwardDiagonal,
                ExpertPdf.Anywhere.Drawing.Color.FromArgb(173, 216, 230), ExpertPdf.Anywhere.Drawing.Color.FromArgb(0, 0, 139));
            elli1.LineStyle.LineWidth = 2;
            result = page.AddElement(elli1);

            // bezier curve
            BezierCurveElement bez1 = new BezierCurveElement(
                50, 350, 100, 400, 200, 350, 250, 370);
            result = page.AddElement(bez1);

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
