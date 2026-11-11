using System;

namespace STStopwatch.Core.Models
{
    public static class RingProgressState
    {
        public static bool IsAtBeginning(bool isRunning, TimeSpan totalElapsed, int completedSegmentCount)
        {
            return !isRunning && totalElapsed == TimeSpan.Zero && completedSegmentCount == 0;
        }
    }
}
