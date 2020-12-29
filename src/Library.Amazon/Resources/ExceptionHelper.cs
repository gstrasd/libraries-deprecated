using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Library.Http;

namespace Library.Amazon.Resources
{
    internal static class ExceptionHelper
    {
        internal static void ThrowOnFailedHttpRequest(HttpStatusCode code, string key, params object[] args)
        {
            if (code.IsSuccess()) return;

            var values = new object[] { (int)code, code.GetDescription() }.Concat(args).ToArray();
            var message = StringResources.ExceptionMessages[key];

            throw new HttpRequestException(String.Format(message, values));
        }

        internal static void ThrowDisposed(string name)
        {
            var message = StringResources.ExceptionMessages["ObjectDisposedException"];
            throw new ObjectDisposedException(String.Format(message, name));
        }
    }
}
