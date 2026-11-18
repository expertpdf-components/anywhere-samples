using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpertPdf.Samples.Pages
{
    public class ViewHttpCookiesModel : PageModel
    {
        public void OnGet()
        {
            string cookies = string.Empty;
            foreach (string key in Request.Cookies.Keys)
            {
                string cookie = Request.Cookies[key];
                cookies += "<br/>" + key + ": " + cookie;
            }

            ViewData.Add("cookiesValue", cookies);
        }
    }
}
