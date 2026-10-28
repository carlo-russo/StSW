using System.Windows.Media;

namespace STStopwatch.Models
{
    public class RingSessionSegment
    {
        public int SessionNumber { get; set; }
        public double StartAngleDeg { get; set; }
        public double EndAngleDeg { get; set; }
        public Brush SegmentBrush { get; set; }

        public RingSessionSegment(int sessionNumber, double startAngleDeg, double endAngleDeg, Brush brush)
        {
            SessionNumber = sessionNumber;
            StartAngleDeg = startAngleDeg;
            EndAngleDeg = endAngleDeg;
            SegmentBrush = brush;
        }
    }
}
