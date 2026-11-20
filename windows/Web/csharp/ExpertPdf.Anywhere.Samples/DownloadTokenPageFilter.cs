using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ExpertPdf.Samples
{
    // When a sample handler returns a file download (FileResult), echo the
    // client's one-time "downloadToken" form value back as a cookie on that
    // same response. A file download does NOT navigate the page and fires no
    // completion event, so the page's sample-form.js can't otherwise tell when
    // the work is done — it polls for this cookie to know the file has been
    // delivered and restores the "Converting …" button. Registered once as a
    // global page filter (Startup), so every convert handler gets it for free
    // without per-page edits.
    public sealed class DownloadTokenPageFilter : IAsyncPageFilter
    {
        public const string TokenName = "downloadToken";

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
            => Task.CompletedTask;

        public async Task OnPageHandlerExecutionAsync(
            PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            // Run the handler. The result is set but NOT yet executed, so a
            // cookie appended now is still sent with the file response headers.
            PageHandlerExecutedContext executed = await next();

            HttpContext http = executed.HttpContext;
            if (executed.Result is FileResult &&
                http.Request.HasFormContentType &&
                http.Request.Form.TryGetValue(TokenName, out var token) &&
                !string.IsNullOrEmpty(token))
            {
                http.Response.Cookies.Append(TokenName, token, new CookieOptions
                {
                    HttpOnly = false,                       // the page's JS must read it
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    MaxAge = TimeSpan.FromSeconds(30),      // transient hygiene
                });
            }
        }
    }
}
