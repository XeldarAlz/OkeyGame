using System;
using System.Collections.Generic;

namespace Runtime.Core.Signals
{
    public sealed class SignalCenter : ISignalCenter
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<TSignal>(Action<TSignal> callback)
        {
            Type signalType = typeof(TSignal);
            
            if (!_subscribers.TryGetValue(signalType, out List<Delegate> callbacks))
            {
                callbacks = new List<Delegate>();
                _subscribers[signalType] = callbacks;
            }
            
            callbacks.Add(callback);
        }

        public void Unsubscribe<TSignal>(Action<TSignal> callback)
        {
            Type signalType = typeof(TSignal);
            
            if (_subscribers.TryGetValue(signalType, out List<Delegate> callbacks))
            {
                callbacks.Remove(callback);
                
                if (callbacks.Count == 0)
                {
                    _subscribers.Remove(signalType);
                }
            }
        }

        public void Fire<TSignal>(TSignal signal)
        {
            Type signalType = typeof(TSignal);
            
            if (_subscribers.TryGetValue(signalType, out List<Delegate> callbacks))
            {
                // Create a copy of the list to avoid issues if callbacks modify the list
                Delegate[] callbacksCopy = callbacks.ToArray();
                
                for (int index = 0; index < callbacksCopy.Length; index++)
                {
                    ((Action<TSignal>)callbacksCopy[index]).Invoke(signal);
                }
            }
        }
    }
}
