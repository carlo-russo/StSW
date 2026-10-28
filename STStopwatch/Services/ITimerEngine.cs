using System;

namespace STStopwatch.Services
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
