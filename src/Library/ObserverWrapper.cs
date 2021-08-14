using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library
{
    public class ObserverWrapper<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public ObserverWrapper(Action<T> onNext, Action<Exception> onError = default, Action onCompleted = default)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void OnCompleted() => _onCompleted?.Invoke();

        public void OnError(Exception error) => _onError?.Invoke(error);

        public void OnNext(T value) => _onNext?.Invoke(value);
    }
}