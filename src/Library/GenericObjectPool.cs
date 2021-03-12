using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace Library
{
    public class GenericObjectPool<T> : DefaultObjectPool<T>, IDisposable where T : class
    {
        private readonly List<T> _disposablePool = new List<T>();
        private readonly bool _disposable;
        private bool _disposed;

        public GenericObjectPool(GenericObjectPoolPolicy<T> policy) : base(policy)
        {
            _disposable = typeof(IDisposable).IsAssignableFrom(typeof(T));
            policy.Created += obj =>
            {
                if (_disposable)_disposablePool.Add(obj);
                CreatedAsync?.Invoke(obj).Start();
            };
        }

        ~GenericObjectPool()
        {
            Dispose(false);
        }

        public event Func<T, Task> CreatedAsync;
        public event Func<T, Task> GetAsync;
        public event Func<T, Task> ReturnedAsync;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposable || _disposed) return;

            if (disposing)
            {
                _disposablePool.ForEach(i => ((IDisposable) i)?.Dispose());
            }

            _disposed = true;
        }

        public override T Get()
        {
            var obj = base.Get();
            GetAsync?.Invoke(obj).Start();
            return obj;
        }

        public override void Return(T obj)
        {
            base.Return(obj);
            ReturnedAsync?.Invoke(obj).Start();
        }
    }
}