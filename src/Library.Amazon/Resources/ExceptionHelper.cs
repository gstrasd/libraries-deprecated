using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Library.Http;
using Library.Resources;

namespace Library.Amazon.Resources
{
    internal static class ExceptionHelper
    {
        private static readonly JsonStringResource _messages = new JsonStringResource("ExceptionMessages.json");

        internal static void ThrowOnFailedHttpRequest(HttpStatusCode code, string key, params object[] args)
        {
            if (code.IsSuccess()) return;

            var values = new object[] { (int)code, code.GetDescription() }.Concat(args).ToArray();

            throw new HttpRequestException(String.Format(_messages[key], values));
        }
    }
}
