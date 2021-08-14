using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Hosting.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Library.Serilog.Enrichers
{
    public class CorrelationIdEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly AsyncLocal<Guid> _asyncAccessor;
        private const string _propertyKey = "correlation-id";

        public bool Enabled { get; set; }

        public CorrelationIdEnricher(IHttpContextAccessor contextAccessor, AsyncLocal<Guid> asyncAccessor)
        {
            if (contextAccessor == null) throw new ArgumentNullException(nameof(contextAccessor));
            if (asyncAccessor == null) throw new ArgumentNullException(nameof(asyncAccessor));

            _contextAccessor = contextAccessor;
            _asyncAccessor = asyncAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (!Enabled) return;

            if (!logEvent.Properties.ContainsKey(_propertyKey))
            {
                var asyncValue = _asyncAccessor.Value;
                if (asyncValue != default)
                {
                    logEvent.AddOrUpdateProperty(new LogEventProperty(_propertyKey, new ScalarValue(asyncValue)));
                    return;
                }

                var contextValue = _contextAccessor.HttpContext?.Items["correlationId"];
                if (contextValue is Guid id && id != default)
                {
                    logEvent.AddOrUpdateProperty(new LogEventProperty(_propertyKey, new ScalarValue(id)));
                }
            }
        }
    }
}
