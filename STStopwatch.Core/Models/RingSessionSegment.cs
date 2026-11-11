namespace STStopwatch.Core.Models
{
    public class RingSessionSegment
    {
        public int SessionNumber { get; set; }
        public double StartAngleDeg { get; set; }
        public double EndAngleDeg { get; set; }
        public string ThemeColorHex { get; set; }

        public RingSessionSegment(int sessionNumber, double startAngleDeg, double endAngleDeg, string themeColorHex)
        {
            SessionNumber = sessionNumber;
            StartAngleDeg = startAngleDeg;
            EndAngleDeg = endAngleDeg;
            ThemeColorHex = themeColorHex;
        }
    }
}
