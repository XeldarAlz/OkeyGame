using System;

namespace Runtime.Core.SignalCenter
{
    public interface ISignalCenter
    {
        void Subscribe<TSignal>(Action<TSignal> callback);
        void Unsubscribe<TSignal>(Action<TSignal> callback);
        void Fire<TSignal>(TSignal signal);
    }
}