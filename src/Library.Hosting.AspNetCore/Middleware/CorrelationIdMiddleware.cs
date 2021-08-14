using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Library.Hosting.AspNetCore.Middleware
{
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(HeaderName, out var value) || !Guid.TryParse(value, out var correlationId))
            {
                correlationId = Guid.NewGuid();
            }

            if (correlationId != default)
            {
                context.Items.Add("correlationId", correlationId);
                context.Response.Headers[HeaderName] = $"{correlationId:D}";
            }

            return _next.Invoke(context);
        }
    }
}
