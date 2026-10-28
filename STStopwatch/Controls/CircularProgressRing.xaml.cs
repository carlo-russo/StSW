using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace STStopwatch.Controls
{
    public partial class CircularProgressRing : UserControl
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnProgressChanged));

        public static readonly DependencyProperty ElapsedProperty =
            DependencyProperty.Register("Elapsed", typeof(TimeSpan), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(TimeSpan.Zero, OnElapsedChanged));

        public static readonly DependencyProperty TargetDurationProperty =
            DependencyProperty.Register("TargetDuration", typeof(TimeSpan), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(TimeSpan.FromHours(1), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTargetDurationChanged));

        public static readonly DependencyProperty SessionLabelProperty =
            DependencyProperty.Register("SessionLabel", typeof(string), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata("Session 01", OnSessionLabelChanged));

        public static readonly DependencyProperty RingBrushProperty =
            DependencyProperty.Register("RingBrush", typeof(Brush), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 82, 255)), OnRingBrushChanged));

        public static readonly DependencyProperty IsMinimalModeProperty =
            DependencyProperty.Register("IsMinimalMode", typeof(bool), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(false, OnIsMinimalModeChanged));

        private bool _isDraggingThumb;
        private Point _center;
        private double _radius;
        private const double MaxDurationHours = 16.0;

        public event EventHandler<TimeSpan> TargetDurationAdjusted;

        public CircularProgressRing()
        {
            InitializeComponent();
            Loaded += delegate { UpdateLayoutGeometry(); };
        }

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public TimeSpan Elapsed
        {
            get { return (TimeSpan)GetValue(ElapsedProperty); }
            set { SetValue(ElapsedProperty, value); }
        }

        public TimeSpan TargetDuration
        {
            get { return (TimeSpan)GetValue(TargetDurationProperty); }
            set { SetValue(TargetDurationProperty, value); }
        }

        public string SessionLabel
        {
            get { return (string)GetValue(SessionLabelProperty); }
            set { SetValue(SessionLabelProperty, value); }
        }

        public Brush RingBrush
        {
            get { return (Brush)GetValue(RingBrushProperty); }
            set { SetValue(RingBrushProperty, value); }
        }

        public bool IsMinimalMode
        {
            get { return (bool)GetValue(IsMinimalModeProperty); }
            set { SetValue(IsMinimalModeProperty, value); }
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null) ring.UpdateProgressArc();
        }

        private static void OnElapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                TimeSpan ts = (TimeSpan)e.NewValue;
                int hours = (int)ts.TotalHours;
                ring.ElapsedDisplay.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, ts.Minutes, ts.Seconds);
            }
        }

        private static void OnTargetDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                TimeSpan ts = (TimeSpan)e.NewValue;
                int hours = (int)ts.TotalHours;
                ring.TargetDisplay.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, ts.Minutes, ts.Seconds);
                ring.UpdateThumbPosition();
            }
        }

        private static void OnSessionLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                ring.SessionLabelText.Text = (string)e.NewValue;
            }
        }

        private static void OnRingBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                var brush = e.NewValue as Brush;
                ring.ProgressArcPath.Stroke = brush;
                ring.ThumbInnerCore.Fill = brush;
            }
        }

        private static void OnIsMinimalModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                bool isMin = (bool)e.NewValue;
                if (isMin)
                {
                    ring.RingCanvas.Width = 240;
                    ring.RingCanvas.Height = 240;
                    ring.ElapsedDisplay.FontSize = 32;
                    ring.SessionLabelText.FontSize = 14;
                }
                else
                {
                    ring.RingCanvas.Width = 280;
                    ring.RingCanvas.Height = 280;
                    ring.ElapsedDisplay.FontSize = 40;
                    ring.SessionLabelText.FontSize = 16;
                }
                ring.UpdateLayoutGeometry();
            }
        }

        private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayoutGeometry();
        }

        private void UpdateLayoutGeometry()
        {
            double size = Math.Min(RingCanvas.ActualWidth, RingCanvas.ActualHeight);
            if (size <= 0) size = RingCanvas.Width;
            if (double.IsNaN(size) || size <= 0) size = 280;

            _center = new Point(size / 2.0, size / 2.0);
            double strokeThickness = ProgressArcPath.StrokeThickness;
            _radius = (size - strokeThickness - 16.0) / 2.0;

            // Draw Full Background Track
            var trackGeom = new EllipseGeometry(_center, _radius, _radius);
            TrackPath.Data = trackGeom;

            UpdateProgressArc();
            UpdateThumbPosition();
        }

        private void UpdateProgressArc()
        {
            if (_radius <= 0) return;

            double ratio = Math.Max(0.0, Math.Min(1.0, Progress));
            double angle = ratio * 360.0;

            if (angle <= 0.05)
            {
                ProgressArcPath.Data = null;
                return;
            }

            if (angle >= 359.9)
            {
                ProgressArcPath.Data = new EllipseGeometry(_center, _radius, _radius);
                return;
            }

            Point startPoint = new Point(_center.X, _center.Y - _radius);
            double rad = (angle - 90.0) * Math.PI / 180.0;
            Point endPoint = new Point(_center.X + _radius * Math.Cos(rad), _center.Y + _radius * Math.Sin(rad));

            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(startPoint, false, false);
                ctx.ArcTo(endPoint, new Size(_radius, _radius), 0, angle > 180.0, SweepDirection.Clockwise, true, false);
            }
            geom.Freeze();
            ProgressArcPath.Data = geom;
        }

        private void UpdateThumbPosition()
        {
            if (_radius <= 0) return;

            double totalSec = TargetDuration.TotalSeconds;
            double maxSec = MaxDurationHours * 3600.0;
            double fraction = Math.Max(0.0, Math.Min(1.0, totalSec / maxSec));
            double angleDeg = fraction * 360.0;

            double rad = (angleDeg - 90.0) * Math.PI / 180.0;
            double thumbX = _center.X + _radius * Math.Cos(rad) - (AdjustmentThumb.Width / 2.0);
            double thumbY = _center.Y + _radius * Math.Sin(rad) - (AdjustmentThumb.Height / 2.0);

            Canvas.SetLeft(AdjustmentThumb, thumbX);
            Canvas.SetTop(AdjustmentThumb, thumbY);

            // Also update guide line to target
            if (angleDeg > 1.0 && angleDeg < 359.0)
            {
                Point startPoint = new Point(_center.X, _center.Y - _radius);
                Point endPoint = new Point(_center.X + _radius * Math.Cos(rad), _center.Y + _radius * Math.Sin(rad));

                var guideGeom = new StreamGeometry();
                using (var ctx = guideGeom.Open())
                {
                    ctx.BeginFigure(startPoint, false, false);
                    ctx.ArcTo(endPoint, new Size(_radius, _radius), 0, angleDeg > 180.0, SweepDirection.Clockwise, true, false);
                }
                guideGeom.Freeze();
                TargetGuidePath.Data = guideGeom;
            }
            else
            {
                TargetGuidePath.Data = null;
            }
        }

        private void OnThumbMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDraggingThumb = true;
                AdjustmentThumb.CaptureMouse();
                TargetBubble.Visibility = Visibility.Visible;
                UpdateTargetFromMouse(e.GetPosition(RingCanvas));
                e.Handled = true;
            }
        }

        private void OnThumbMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingThumb)
            {
                UpdateTargetFromMouse(e.GetPosition(RingCanvas));
                e.Handled = true;
            }
        }

        private void OnThumbMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingThumb)
            {
                _isDraggingThumb = false;
                AdjustmentThumb.ReleaseMouseCapture();
                TargetBubble.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

        private void UpdateTargetFromMouse(Point pos)
        {
            double dx = pos.X - _center.X;
            double dy = pos.Y - _center.Y;

            // Angle clockwise from 12 o'clock
            double rad = Math.Atan2(dx, -dy);
            if (rad < 0) rad += 2 * Math.PI;

            double angleDeg = rad * 180.0 / Math.PI;
            double totalSeconds = (angleDeg / 360.0) * (MaxDurationHours * 3600.0);

            // Continuous duration adjustment: snap to nearest minute for sensible precision
            double totalMinutes = Math.Round(totalSeconds / 60.0);
            if (totalMinutes < 1) totalMinutes = 1; // Minimum 1 minute
            if (totalMinutes > (MaxDurationHours * 60)) totalMinutes = MaxDurationHours * 60; // Max 16 hours

            TimeSpan newTarget = TimeSpan.FromMinutes(totalMinutes);
            TargetDuration = newTarget;

            int hours = (int)newTarget.TotalHours;
            int mins = newTarget.Minutes;
            TargetBubbleText.Text = string.Format("Target: {0:D2}h {1:D2}m", hours, mins);

            // Position bubble near thumb
            double thumbX = Canvas.GetLeft(AdjustmentThumb);
            double thumbY = Canvas.GetTop(AdjustmentThumb);
            Canvas.SetLeft(TargetBubble, Math.Max(10, Math.Min(RingCanvas.ActualWidth - 110, thumbX - 40)));
            Canvas.SetTop(TargetBubble, Math.Max(0, thumbY - 32));

            if (TargetDurationAdjusted != null)
            {
                TargetDurationAdjusted(this, newTarget);
            }
        }

        private void OnControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int deltaMinutes = e.Delta > 0 ? 1 : -1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                deltaMinutes *= 5; // 5 min step with shift
            }

            double currentMins = TargetDuration.TotalMinutes;
            double newMins = Math.Max(1, Math.Min(MaxDurationHours * 60, currentMins + deltaMinutes));
            TimeSpan newTarget = TimeSpan.FromMinutes(newMins);
            TargetDuration = newTarget;

            if (TargetDurationAdjusted != null)
            {
                TargetDurationAdjusted(this, newTarget);
            }
            e.Handled = true;
        }
    }
}
