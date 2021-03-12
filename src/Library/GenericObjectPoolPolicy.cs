using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace Library
{
    public class GenericObjectPoolPolicy<T> : PooledObjectPolicy<T>
    {
        private readonly Func<T> _create;
        private readonly Func<T, bool> _return;

        public GenericObjectPoolPolicy(Func<T> create, Func<T, bool> @return)
        {
            _create = create;
            _return = @return;
        }

        public override T Create()
        {
            var obj = _create();
            Created?.Invoke(obj);
            return obj;
        }

        public override bool Return(T obj) => _return(obj);

        internal event Action<T> Created;
    }
}
