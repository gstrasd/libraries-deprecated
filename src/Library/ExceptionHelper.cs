using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library.Resources;

namespace Library
{
    public static class ExceptionHelper
    {
        private static readonly JsonStringResource _messages = new JsonStringResource("Exceptions.json");

        public static void ThrowDisposed(string name)
        {
            throw new ObjectDisposedException(_messages.Format("ObjectDisposedException", name));
        }

        public static void ThrowMustBePositive(string name)
        {
            throw new ArgumentOutOfRangeException(name, _messages["MustBePositive"]);
        }

        public static void ThrowMustBeGreaterThanZero(string name)
        {
            throw new ArgumentOutOfRangeException(name, _messages["MustBeGreaterThanZero"]);
        }
    }
}
