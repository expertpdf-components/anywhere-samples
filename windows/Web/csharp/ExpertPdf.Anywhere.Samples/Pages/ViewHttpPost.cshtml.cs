using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpertPdf.Samples.Pages
{
    // the converter posts plain form data, without the antiforgery token of the Razor Pages forms
    [IgnoreAntiforgeryToken]
    public class ViewHttpPostModel : PageModel
    {
        public string Method { get; private set; }

        public List<KeyValuePair<string, string>> Fields { get; } = new List<KeyValuePair<string, string>>();

        public void OnGet()
        {
            Method = Request.Method;
        }

        public void OnPost()
        {
            Method = Request.Method;
            foreach (string key in Request.Form.Keys)
                Fields.Add(new KeyValuePair<string, string>(key, Request.Form[key]));
        }
    }
}
