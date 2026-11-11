using System;

namespace STStopwatch.Core.Services
{
    public interface ITimerEngine
    {
        bool IsRunning { get; }
        TimeSpan Elapsed { get; }
        void Start();
        void Pause();
        void Reset();
    }
}
