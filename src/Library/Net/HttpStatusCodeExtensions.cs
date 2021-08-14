using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Library.Net
{
    public static class HttpStatusCodeExtensions
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

        public static HttpStatusCodeClass GetStatusCodeClass(this HttpStatusCode statusCode)
        {
            if (statusCode.IsInformational()) return HttpStatusCodeClass.Informational;
            if (statusCode.IsSuccess()) return HttpStatusCodeClass.Success;
            if (statusCode.IsRedirection()) return HttpStatusCodeClass.Redirection;
            if (statusCode.IsClientError()) return HttpStatusCodeClass.ClientError;
            if (statusCode.IsServerError()) return HttpStatusCodeClass.ServerError;
            throw new ArgumentOutOfRangeException(nameof(statusCode), $"Argument must be a HTTP status code value between {(int)HttpStatusCode.Continue}");
        }

        public static void EnsureSuccess(this HttpStatusCode statusCode)
        {
            if (!statusCode.IsSuccess())
            {
                throw new HttpRequestException(String.Concat("Response status code does not indicate success: ", new HttpStatusCodeFormatter().Format("G", statusCode, default)));
            }
        }
    }
}
