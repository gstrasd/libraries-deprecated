using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library
{
    public class ObserverManager<T>
    {
        private readonly HashSet<IObserver<T>> _observers = new HashSet<IObserver<T>>();

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            _observers.Add(observer);

            return new DisposeWrapper(() => _observers.Remove(observer));
        }

        public Task NotifyAsync(T value)
        {
            var observers = _observers.ToArray();
            if (!observers.Any()) return Task.CompletedTask;

            return Task.WhenAll(observers.Select(o => Task.Run(() => o?.OnNext(value))));
        }

        public Task NotifyErrorAsync(Exception error)
        {
            var observers = _observers.ToArray();
            if (!observers.Any()) return Task.CompletedTask;

            return Task.WhenAll(observers.Select(o => Task.Run(() => o?.OnError(error))));
        }

        public Task NotifyCompleteAsync()
        {
            var observers = _observers.ToArray();
            if (!observers.Any()) return Task.CompletedTask;

            return Task.WhenAll(observers.Select(o => Task.Run(() => o?.OnCompleted())));
        }
    }
}