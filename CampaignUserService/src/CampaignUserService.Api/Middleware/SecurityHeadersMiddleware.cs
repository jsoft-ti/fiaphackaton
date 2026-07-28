namespace CampaignUserService.Api.Middleware;

/// <summary>
/// Adds the standard set of defensive HTTP security headers to every
/// response (protection against clickjacking, MIME sniffing, XSS, and
/// leaking referrer information).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["X-XSS-Protection"] = "1; mode=block";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Content-Security-Policy"] =
                "default-src 'self'; frame-ancestors 'none'; base-uri 'self'";

            if (context.Request.IsHttps)
            {
                headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
            }

            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        });

        await next(context);
    }
}
