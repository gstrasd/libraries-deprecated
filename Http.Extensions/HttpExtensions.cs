using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.Http.Extensions
{
    public static class HttpExtensions
    {
        public static bool IsInformational(this HttpStatusCode statusCode)
        {
            return statusCode >= HttpStatusCode.Continue && statusCode < HttpStatusCode.OK;
        }

        public static bool IsSuccess(this HttpStatusCode statusCode)
        {
            return statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.MultipleChoices;
        }

        public static bool IsRedirection(this HttpStatusCode statusCode)
        {
            return statusCode >= HttpStatusCode.MultipleChoices && statusCode < HttpStatusCode.BadRequest;
        }

        public static bool IsClientError(this HttpStatusCode statusCode)
        {
            return statusCode >= HttpStatusCode.BadRequest && statusCode < HttpStatusCode.InternalServerError;
        }

        public static bool IsServerError(this HttpStatusCode statusCode)
        {
            return statusCode >= HttpStatusCode.InternalServerError && (int)statusCode < 600;
        }
    }
}
