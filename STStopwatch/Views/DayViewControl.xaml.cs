using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using STStopwatch.Core.Models;

namespace STStopwatch.Views
{
    public partial class DayViewControl : UserControl
    {
        public DayViewControl()
        {
            InitializeComponent();
            DayDurationRingControl.SessionSelected += OnDayDurationSessionSelected;
        }

        private void OnDayDurationSessionSelected(SessionRecord selectedRecord)
        {
            var viewModel = DataContext as STStopwatch.Core.ViewModels.StatisticsViewModel;
            if (viewModel == null || viewModel.DaySummary == null) return;

            IList<SessionRecord> records = viewModel.DaySummary.Records;
            for (int i = 0; i < records.Count; i++)
            {
                records[i].IsDayViewSelected = object.ReferenceEquals(records[i], selectedRecord);
            }

            if (selectedRecord != null)
            {
                ScrollToRecord(selectedRecord);
            }
        }

        private void OnRecordRowMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as FrameworkElement;
            var record = border != null ? border.DataContext as SessionRecord : null;
            if (record != null)
            {
                OnDayDurationSessionSelected(record);
            }
        }

        private void ScrollToRecord(SessionRecord selectedRecord)
        {
            if (selectedRecord == null) return;

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                try
                {
                    var container = RecordsItemsControl.ItemContainerGenerator.ContainerFromItem(selectedRecord) as FrameworkElement;
                    if (container == null)
                    {
                        RecordsItemsControl.UpdateLayout();
                        container = RecordsItemsControl.ItemContainerGenerator.ContainerFromItem(selectedRecord) as FrameworkElement;
                    }

                    if (container != null)
                    {
                        if (RecordsContentPanel != null && RecordsScrollViewer != null && RecordsScrollViewer.ViewportHeight > 0)
                        {
                            GeneralTransform transform = container.TransformToAncestor(RecordsContentPanel);
                            Point pt = transform.Transform(new Point(0, 0));
                            double targetOffset = pt.Y - (RecordsScrollViewer.ViewportHeight - container.ActualHeight) / 2.0;
                            if (targetOffset < 0) targetOffset = 0;
                            if (targetOffset > RecordsScrollViewer.ScrollableHeight) targetOffset = RecordsScrollViewer.ScrollableHeight;
                            RecordsScrollViewer.ScrollToVerticalOffset(targetOffset);
                        }

                        container.BringIntoView();
                    }
                }
                catch
                {
                    try
                    {
                        var container = RecordsItemsControl.ItemContainerGenerator.ContainerFromItem(selectedRecord) as FrameworkElement;
                        if (container != null)
                        {
                            container.BringIntoView();
                        }
                    }
                    catch
                    {
                    }
                }
            }));
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

        public event Action<SessionRecord> SessionSelected;

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            Point point = e.GetPosition(this);
            double size = Math.Min(ActualWidth, ActualHeight);
            double stroke = Math.Max(5.0, size * 0.09);
            double radius = (size - stroke) / 2.0;
            Point center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            SessionRecord selected = null;
            if (Math.Abs(distance - radius) <= stroke / 2.0 + 8.0)
            {
                double angle = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 90.0 + 360.0) % 360.0;
                double angularTolerance = (stroke / 2.0 + 4.0) / radius * 180.0 / Math.PI;
                IList<SessionRecord> records = Records;
                if (records != null && records.Count > 0)
                {
                    // Pass 1: Check for exact hit within the segment angle bounds (topmost drawn first)
                    for (int i = records.Count - 1; i >= 0; i--)
                    {
                        SessionRecord record = records[i];
                        if (record == null || record.ElapsedSeconds <= 0) continue;

                        double startSeconds = record.StartTime.TimeOfDay.TotalSeconds;
                        double durationSeconds = Math.Min(record.ElapsedDuration.TotalSeconds, 24.0 * 60.0 * 60.0 - startSeconds);
                        if (durationSeconds <= 0) continue;

                        double startAngle = startSeconds / (24.0 * 60.0 * 60.0) * 360.0;
                        double sweep = durationSeconds / (24.0 * 60.0 * 60.0) * 360.0;
                        double offset = (angle - startAngle + 360.0) % 360.0;

                        if (offset >= 0.0 && offset <= sweep)
                        {
                            selected = record;
                            break;
                        }
                    }

                    // Pass 2: If no exact hit, find the nearest segment within angularTolerance
                    if (selected == null)
                    {
                        double bestDistance = double.MaxValue;
                        for (int i = records.Count - 1; i >= 0; i--)
                        {
                            SessionRecord record = records[i];
                            if (record == null || record.ElapsedSeconds <= 0) continue;

                            double startSeconds = record.StartTime.TimeOfDay.TotalSeconds;
                            double durationSeconds = Math.Min(record.ElapsedDuration.TotalSeconds, 24.0 * 60.0 * 60.0 - startSeconds);
                            if (durationSeconds <= 0) continue;

                            double startAngle = startSeconds / (24.0 * 60.0 * 60.0) * 360.0;
                            double sweep = durationSeconds / (24.0 * 60.0 * 60.0) * 360.0;
                            double offset = (angle - startAngle + 360.0) % 360.0;

                            double dist;
                            if (offset <= sweep)
                            {
                                dist = 0.0;
                            }
                            else
                            {
                                dist = Math.Min(offset - sweep, 360.0 - offset);
                            }

                            if (dist <= angularTolerance && dist < bestDistance)
                            {
                                bestDistance = dist;
                                selected = record;
                            }
                        }
                    }
                }
            }

            if (SessionSelected != null)
            {
                SessionSelected(selected);
            }

            e.Handled = true;
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
            DrawCenteredText(drawingContext, duration, center, 0.0, Math.Max(10.0, size * 0.145), Color.FromRgb(17, 17, 17), FontWeights.Bold);
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
