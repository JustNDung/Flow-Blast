using System;
using System.Collections.Generic;

namespace MessageDispatcher
{
    public static class MessageDispatcher
    {
        private static readonly Dictionary<Type, Delegate> Subscribers =
            new();

        public static void Subscribe<T>(Action<T> callback)
            where T : IMessage
        {
            var type = typeof(T);

            if (Subscribers.TryGetValue(type, out var existing))
            {
                Subscribers[type] = Delegate.Combine(existing, callback);
            }
            else
            {
                Subscribers.Add(type, callback);
            }
        }

        public static void Unsubscribe<T>(Action<T> callback)
            where T : IMessage
        {
            var type = typeof(T);

            if (!Subscribers.TryGetValue(type, out var existing))
                return;

            var current = Delegate.Remove(existing, callback);

            if (current == null)
                Subscribers.Remove(type);
            else
                Subscribers[type] = current;
        }

        public static void Publish<T>(T message)
            where T : IMessage
        {
            var type = typeof(T);

            if (!Subscribers.TryGetValue(type, out var callback))
                return;

            ((Action<T>)callback)?.Invoke(message);
        }

        public static void Clear()
        {
            Subscribers.Clear();
        }
    }
}