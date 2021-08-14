using System;
using System.Collections.Generic;
using System.Text;

namespace Library
{
    public class DisposeWrapper : IDisposable
    {
        private readonly Action _dispose;

        public DisposeWrapper(Action dispose)
        {
            if (dispose == null) throw new ArgumentNullException(nameof(dispose));

            _dispose = dispose;
        }

        public void Dispose() => _dispose?.Invoke();
    }
}
