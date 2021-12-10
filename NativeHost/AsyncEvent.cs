using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace NativeHost
{
    public class AsyncEvent<TEventArgs>
    {
        private readonly object _lock = new();

        private ImmutableList<Func<object, TEventArgs, Task>> _callbacks;

        public AsyncEvent()
            : this(Enumerable.Empty<Func<object, TEventArgs, Task>>())
        {
        }

        public AsyncEvent(IEnumerable<Func<object, TEventArgs, Task>> delegates)
        {
            _callbacks = delegates.ToImmutableList();
        }

        public static AsyncEvent<TEventArgs> operator +(AsyncEvent<TEventArgs> e, Func<object, TEventArgs, Task> callback)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (callback == null) 
                throw new ArgumentNullException(nameof(callback));

            lock (e._lock)
            {
                e._callbacks = e._callbacks.Add(callback);
            }

            return e;
        }

        public static AsyncEvent<TEventArgs> operator -(AsyncEvent<TEventArgs> e, Func<object, TEventArgs, Task> callback)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (callback == null) 
                throw new ArgumentNullException(nameof(callback));

            lock (e._lock)
            {
                e._callbacks = e._callbacks.Remove(callback);
            }

            return e;
        }

        public Task InvokeAsync(object sender, TEventArgs eventArgs)
        {
            return Task.WhenAll(_callbacks.Select(x => x(sender, eventArgs)));
        }
    }
}
