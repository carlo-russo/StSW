using System;
using System.Diagnostics;

namespace STStopwatch.Services
{
    public class HighResolutionTimerEngine : ITimerEngine
    {
        private readonly object _syncLock = new object();
        private long _startTimestamp;
        private TimeSpan _baseElapsed;
        private bool _isRunning;

        public HighResolutionTimerEngine()
        {
            _baseElapsed = TimeSpan.Zero;
            _isRunning = false;
        }

        public bool IsRunning
        {
            get
            {
                lock (_syncLock)
                {
                    return _isRunning;
                }
            }
        }

        public TimeSpan Elapsed
        {
            get
            {
                lock (_syncLock)
                {
                    if (!_isRunning)
                    {
                        return _baseElapsed;
                    }

                    long currentTimestamp = Stopwatch.GetTimestamp();
                    long diffTicks = currentTimestamp - _startTimestamp;
                    double seconds = (double)diffTicks / Stopwatch.Frequency;
                    return _baseElapsed + TimeSpan.FromSeconds(seconds);
                }
            }
        }

        public void Start()
        {
            lock (_syncLock)
            {
                if (_isRunning) return;

                _startTimestamp = Stopwatch.GetTimestamp();
                _isRunning = true;
            }
        }

        public void Pause()
        {
            lock (_syncLock)
            {
                if (!_isRunning) return;

                long currentTimestamp = Stopwatch.GetTimestamp();
                long diffTicks = currentTimestamp - _startTimestamp;
                double seconds = (double)diffTicks / Stopwatch.Frequency;
                _baseElapsed = _baseElapsed + TimeSpan.FromSeconds(seconds);
                _isRunning = false;
            }
        }

        public void Reset()
        {
            lock (_syncLock)
            {
                _isRunning = false;
                _baseElapsed = TimeSpan.Zero;
                _startTimestamp = 0;
            }
        }
    }
}
