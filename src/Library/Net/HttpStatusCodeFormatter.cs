using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Library.Net
{
    public class HttpStatusCodeFormatter : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is HttpStatusCode statusCode)
            {
                switch (format?.Trim().ToUpper())
                {
                    case "F": return GetDescription(statusCode);
                    case "G":
                    case null: return $"{statusCode:D} ({GetDescription(statusCode)})";
                }
            }

            return arg is IFormattable formattable ? formattable.ToString(format, formatProvider) : arg?.ToString() ?? String.Empty;
        }

        private static string GetDescription(HttpStatusCode statusCode)
        {
            return
                (int) statusCode switch
                {
                    100 => "Continue",
                    101 => "Switching Protocols",
                    102 => "Processing",
                    103 => "Early Hints",

                    200 => "OK",
                    201 => "Created",
                    202 => "Accepted",
                    203 => "Non-Authoritative Information",
                    204 => "No Content",
                    205 => "Reset Content",
                    206 => "Partial Content",
                    207 => "Multi-Status",
                    208 => "Already Reported",
                    226 => "IM Used",

                    300 => "Multiple Choices",
                    301 => "Moved Permanently",
                    302 => "Found",
                    303 => "See Other",
                    304 => "Not Modified",
                    305 => "Use Proxy",
                    307 => "Temporary Redirect",
                    308 => "Permanent Redirect",

                    400 => "Bad Request",
                    401 => "Unauthorized",
                    402 => "Payment Required",
                    403 => "Forbidden",
                    404 => "Not Found",
                    405 => "Method Not Allowed",
                    406 => "Not Acceptable",
                    407 => "Proxy Authentication Required",
                    408 => "Request Timeout",
                    409 => "Conflict",
                    410 => "Gone",
                    411 => "Length Required",
                    412 => "Precondition Failed",
                    413 => "Request Entity Too Large",
                    414 => "Request-Uri Too Long",
                    415 => "Unsupported Media Type",
                    416 => "Requested Range Not Satisfiable",
                    417 => "Expectation Failed",
                    421 => "Misdirected Request",
                    422 => "Unprocessable Entity",
                    423 => "Locked",
                    424 => "Failed Dependency",
                    426 => "Upgrade Required", // RFC 2817
                    428 => "Precondition Required",
                    429 => "Too Many Requests",
                    431 => "Request Header Fields Too Large",
                    451 => "Unavailable For Legal Reasons",

                    500 => "Internal Server Error",
                    501 => "Not Implemented",
                    502 => "Bad Gateway",
                    503 => "Service Unavailable",
                    504 => "Gateway Timeout",
                    505 => "Http Version Not Supported",
                    506 => "Variant Also Negotiates",
                    507 => "Insufficient Storage",
                    508 => "Loop Detected",
                    510 => "Not Extended",
                    511 => "Network Authentication Required",

                    _ => String.Empty
                };
        }
    }
}