using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using STStopwatch.Models;

namespace STStopwatch.Views
{
    public partial class DayViewControl : UserControl
    {
        public DayViewControl()
        {
            InitializeComponent();
        }
    }

    public class DayDurationRing : FrameworkElement
    {
        public static readonly DependencyProperty RecordsProperty =
            DependencyProperty.Register("Records", typeof(IList<SessionRecord>), typeof(DayDurationRing),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public IList<SessionRecord> Records
        {
            get { return (IList<SessionRecord>)GetValue(RecordsProperty); }
            set { SetValue(RecordsProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= 0) return;

            const double secondsPerDay = 24.0 * 60.0 * 60.0;
            double stroke = Math.Max(5.0, size * 0.09);
            double radius = (size - stroke) / 2.0;
            Point center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            Rect ringBounds = new Rect(center.X - radius, center.Y - radius, radius * 2.0, radius * 2.0);
            var ringGeometry = new EllipseGeometry(ringBounds);
            drawingContext.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(226, 232, 240)), stroke), ringGeometry);

            // Hour marks keep the ring anchored to a fixed 24-hour clock face.
            for (int hour = 0; hour < 24; hour++)
            {
                double angleRadians = (hour * 15.0 - 90.0) * Math.PI / 180.0;
                double outerRadius = radius + stroke / 2.0;
                double innerRadius = radius - stroke / 2.0;
                Point outer = new Point(center.X + outerRadius * Math.Cos(angleRadians), center.Y + outerRadius * Math.Sin(angleRadians));
                Point inner = new Point(center.X + innerRadius * Math.Cos(angleRadians), center.Y + innerRadius * Math.Sin(angleRadians));
                Color tickColor = hour % 6 == 0 ? Color.FromRgb(100, 116, 139) : Color.FromRgb(203, 213, 225);
                double tickWidth = hour % 6 == 0 ? 1.5 : 0.75;
                drawingContext.DrawLine(new Pen(new SolidColorBrush(tickColor), tickWidth), inner, outer);
            }

            TimeSpan total = TimeSpan.Zero;
            IList<SessionRecord> records = Records;
            if (records != null)
            {
                for (int i = 0; i < records.Count; i++)
                {
                    if (records[i] != null && records[i].ElapsedSeconds > 0)
                    {
                        total += records[i].ElapsedDuration;
                    }
                }
            }

            if (records != null && total.TotalSeconds > 0)
            {
                for (int i = 0; i < records.Count; i++)
                {
                    SessionRecord record = records[i];
                    if (record == null || record.ElapsedSeconds <= 0) continue;

                    double startSeconds = record.StartTime.TimeOfDay.TotalSeconds;
                    double availableSeconds = Math.Max(0.0, secondsPerDay - startSeconds);
                    double durationSeconds = Math.Min(record.ElapsedDuration.TotalSeconds, availableSeconds);
                    double sweep = durationSeconds / secondsPerDay * 360.0;
                    if (sweep <= 0.0) continue;

                    double startAngle = startSeconds / secondsPerDay * 360.0;
                    Color color = ParseColor(record.ThemeColorHex);
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    var pen = new Pen(brush, stroke)
                    {
                        StartLineCap = PenLineCap.Round,
                        EndLineCap = PenLineCap.Round
                    };

                    if (sweep >= 359.9)
                    {
                        drawingContext.DrawGeometry(null, pen, ringGeometry);
                    }
                    else
                    {
                        double startRadians = (startAngle - 90.0) * Math.PI / 180.0;
                        double endRadians = (startAngle + sweep - 90.0) * Math.PI / 180.0;
                        Point start = new Point(center.X + radius * Math.Cos(startRadians), center.Y + radius * Math.Sin(startRadians));
                        Point end = new Point(center.X + radius * Math.Cos(endRadians), center.Y + radius * Math.Sin(endRadians));
                        var geometry = new StreamGeometry();
                        using (StreamGeometryContext context = geometry.Open())
                        {
                            context.BeginFigure(start, false, false);
                            context.ArcTo(end, new Size(radius, radius), 0.0, sweep > 180.0, SweepDirection.Clockwise, true, false);
                        }
                        geometry.Freeze();
                        drawingContext.DrawGeometry(null, pen, geometry);
                    }

                }
            }

            string duration = string.Format("{0:D2}:{1:D2}", (int)total.TotalHours, total.Minutes);
            DrawCenteredText(drawingContext, "DAY TOTAL", center, -10.0, Math.Max(8.0, size * 0.105), Color.FromRgb(100, 116, 139), FontWeights.SemiBold);
            DrawCenteredText(drawingContext, duration, center, 7.0, Math.Max(10.0, size * 0.145), Color.FromRgb(17, 17, 17), FontWeights.Bold);
        }

        private static Color ParseColor(string colorText)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(colorText))
                {
                    return (Color)ColorConverter.ConvertFromString(colorText);
                }
            }
            catch
            {
            }

            return Color.FromRgb(0, 82, 255);
        }

        private void DrawCenteredText(DrawingContext drawingContext, string text, Point center, double verticalOffset, double fontSize, Color color, FontWeight weight)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                fontSize,
                new SolidColorBrush(color),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(formatted, new Point(center.X - formatted.Width / 2.0, center.Y + verticalOffset - formatted.Height / 2.0));
        }
    }
}
