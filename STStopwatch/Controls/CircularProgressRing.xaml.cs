using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using STStopwatch.Models;

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

        public static readonly DependencyProperty TotalElapsedProperty =
            DependencyProperty.Register("TotalElapsed", typeof(TimeSpan), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(TimeSpan.Zero, OnTotalElapsedChanged));

        public static readonly DependencyProperty TargetDurationProperty =
            DependencyProperty.Register("TargetDuration", typeof(TimeSpan), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(TimeSpan.FromHours(1), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTargetDurationChanged));

        public static readonly DependencyProperty TargetFinishTimeProperty =
            DependencyProperty.Register("TargetFinishTime", typeof(string), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata("12:00:00", OnTargetFinishTimeChanged));

        public static readonly DependencyProperty SessionLabelProperty =
            DependencyProperty.Register("SessionLabel", typeof(string), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata("Session 01", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSessionLabelChanged));

        public static readonly DependencyProperty DayDurationTextProperty =
            DependencyProperty.Register("DayDurationText", typeof(string), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata("00:00", OnDayDurationTextChanged));

        public static readonly DependencyProperty RingBrushProperty =
            DependencyProperty.Register("RingBrush", typeof(Brush), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 82, 255)), OnRingBrushChanged));

        public static readonly DependencyProperty IsMinimalModeProperty =
            DependencyProperty.Register("IsMinimalMode", typeof(bool), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(false, OnIsMinimalModeChanged));

        public static readonly DependencyProperty IsSuperMinimalModeProperty =
            DependencyProperty.Register("IsSuperMinimalMode", typeof(bool), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(false, OnIsSuperMinimalModeChanged));

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register("IsRunning", typeof(bool), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(false, OnIsRunningChanged));

        public static readonly DependencyProperty CompletedSegmentsProperty =
            DependencyProperty.Register("CompletedSegments", typeof(IList<RingSessionSegment>), typeof(CircularProgressRing),
                new FrameworkPropertyMetadata(null, OnCompletedSegmentsChanged));

        private bool _isDraggingThumb;
        private bool _isGlowEnabled = true;
        private Point _center;
        private double _radius;
        private const double MaxDurationHours = 16.0;

        public event EventHandler<TimeSpan> TargetDurationAdjusted;

        public CircularProgressRing()
        {
            InitializeComponent();
            Loaded += delegate { ApplyModeStyles(); };
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

        public TimeSpan TotalElapsed
        {
            get { return (TimeSpan)GetValue(TotalElapsedProperty); }
            set { SetValue(TotalElapsedProperty, value); }
        }

        public TimeSpan TargetDuration
        {
            get { return (TimeSpan)GetValue(TargetDurationProperty); }
            set { SetValue(TargetDurationProperty, value); }
        }

        public string TargetFinishTime
        {
            get { return (string)GetValue(TargetFinishTimeProperty); }
            set { SetValue(TargetFinishTimeProperty, value); }
        }

        public string SessionLabel
        {
            get { return (string)GetValue(SessionLabelProperty); }
            set { SetValue(SessionLabelProperty, value); }
        }

        public string DayDurationText
        {
            get { return (string)GetValue(DayDurationTextProperty); }
            set { SetValue(DayDurationTextProperty, value); }
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

        public bool IsSuperMinimalMode
        {
            get { return (bool)GetValue(IsSuperMinimalModeProperty); }
            set { SetValue(IsSuperMinimalModeProperty, value); }
        }

        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            set { SetValue(IsRunningProperty, value); }
        }

        public IList<RingSessionSegment> CompletedSegments
        {
            get { return (IList<RingSessionSegment>)GetValue(CompletedSegmentsProperty); }
            set { SetValue(CompletedSegmentsProperty, value); }
        }

        public bool IsAtBeginning
        {
            get
            {
                bool hasSegments = CompletedSegments != null && CompletedSegments.Count > 0;
                return !IsRunning && TotalElapsed == TimeSpan.Zero && !hasSegments;
            }
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null) ring.UpdateRingAndNode();
        }

        private static void OnElapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                TimeSpan ts = (TimeSpan)e.NewValue;
                int hours = (int)ts.TotalHours;
                ring.ElapsedHoursMinutesRun.Text = hours > 0
                    ? string.Format("{0:D2}:{1:D2}:", hours, ts.Minutes)
                    : string.Format("{0:D2}:", ts.Minutes);
                ring.ElapsedSecondsRun.Text = string.Format("{0:D2}", ts.Seconds);
                ring.ElapsedMillisecondsRun.Text = string.Format(".{0:D1}", ts.Milliseconds / 100);
                ring.UpdateRingAndNode();
            }
        }

        private static void OnTotalElapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null) ring.UpdateRingAndNode();
        }

        private static void OnTargetDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null) ring.UpdateRingAndNode();
        }

        private static void OnTargetFinishTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                ring.FinishDisplay.Text = (string)e.NewValue;
            }
        }

        private static void OnSessionLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                string newVal = (string)e.NewValue;
                if (ring.SessionLabelBox.Text != newVal)
                {
                    ring.SessionLabelBox.Text = newVal;
                }
            }
        }

        private static void OnDayDurationTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null && ring.DayDurationDisplay != null)
            {
                ring.DayDurationDisplay.Text = (string)e.NewValue;
                ring.SuperMiniDayDurationDisplay.Text = (string)e.NewValue;
            }
        }

        private Storyboard _glowPulseStoryboard;

        private static void OnRingBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                ring.UpdateColorsForTheme();
            }
        }

        private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null) ring.UpdateRingAndNode();
        }

        private static void OnCompletedSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null) ring.UpdateRingAndNode();
        }

        private static void OnIsMinimalModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                ring.ApplyModeStyles();
            }
        }

        private static void OnIsSuperMinimalModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = d as CircularProgressRing;
            if (ring != null)
            {
                ring.ApplyModeStyles();
            }
        }

        public void ApplyModeStyles()
        {
            if (IsSuperMinimalMode)
            {
                RingCanvas.Width = 190;
                RingCanvas.Height = 190;
                CompletedSegmentsCanvas.Width = 190;
                CompletedSegmentsCanvas.Height = 190;
                ElapsedDisplay.FontSize = 23;
                DayDurationDisplay.FontSize = 8;
                DayDurationDisplay.Visibility = Visibility.Collapsed;
                SuperMiniDayDurationDisplay.Visibility = Visibility.Visible;
                SessionLabelBox.FontSize = 11;
                SessionLabelBox.IsHitTestVisible = false;
                SessionLabelBox.Focusable = false;
                FinishTimePanel.Visibility = Visibility.Collapsed;
                TrackPath.StrokeThickness = 7;
                ProgressArcPath.StrokeThickness = 7;
                AdjustmentThumb.Visibility = Visibility.Collapsed;

                SuperMiniBackgroundEllipse.Visibility = Visibility.Visible;
                EdgeGlowEllipse.Visibility = _isGlowEnabled ? Visibility.Visible : Visibility.Collapsed;
                SuperMiniLogoBadge.Visibility = Visibility.Visible;

                UpdateColorsForTheme();
                if (_isGlowEnabled) StartGlowPulse();
            }
            else if (IsMinimalMode)
            {
                RingCanvas.Width = 190;
                RingCanvas.Height = 190;
                CompletedSegmentsCanvas.Width = 190;
                CompletedSegmentsCanvas.Height = 190;
                ElapsedDisplay.FontSize = 25;
                DayDurationDisplay.FontSize = 10;
                DayDurationDisplay.Visibility = Visibility.Visible;
                SuperMiniDayDurationDisplay.Visibility = Visibility.Collapsed;
                SessionLabelBox.FontSize = 12;
                SessionLabelBox.IsHitTestVisible = true;
                SessionLabelBox.Focusable = true;
                FinishTimePanel.Visibility = Visibility.Collapsed;
                TrackPath.StrokeThickness = 8;
                ProgressArcPath.StrokeThickness = 8;
                AdjustmentThumb.Visibility = Visibility.Visible;
                AdjustmentThumb.Width = 16;
                AdjustmentThumb.Height = 16;

                SuperMiniBackgroundEllipse.Visibility = Visibility.Collapsed;
                EdgeGlowEllipse.Visibility = Visibility.Collapsed;
                SuperMiniLogoBadge.Visibility = Visibility.Collapsed;

                UpdateColorsForTheme();
                StopGlowPulse();
            }
            else
            {
                RingCanvas.Width = 280;
                RingCanvas.Height = 280;
                CompletedSegmentsCanvas.Width = 280;
                CompletedSegmentsCanvas.Height = 280;
                ElapsedDisplay.FontSize = 40;
                DayDurationDisplay.FontSize = 12;
                DayDurationDisplay.Visibility = Visibility.Visible;
                SuperMiniDayDurationDisplay.Visibility = Visibility.Collapsed;
                SessionLabelBox.FontSize = 17;
                SessionLabelBox.IsHitTestVisible = true;
                SessionLabelBox.Focusable = true;
                FinishTimePanel.Visibility = Visibility.Visible;
                TrackPath.StrokeThickness = 10;
                ProgressArcPath.StrokeThickness = 10;
                AdjustmentThumb.Visibility = Visibility.Visible;
                AdjustmentThumb.Width = 22;
                AdjustmentThumb.Height = 22;

                SuperMiniBackgroundEllipse.Visibility = Visibility.Collapsed;
                EdgeGlowEllipse.Visibility = Visibility.Collapsed;
                SuperMiniLogoBadge.Visibility = Visibility.Collapsed;

                UpdateColorsForTheme();
                StopGlowPulse();
            }
            ApplyElapsedMillisecondsStyle();
            UpdateLayoutGeometry();
        }

        private void ApplyElapsedMillisecondsStyle()
        {
            ElapsedMillisecondsRun.FontSize = ElapsedDisplay.FontSize * 0.55;
            ElapsedMillisecondsRun.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
        }

        private void UpdateColorsForTheme()
        {
            var brush = RingBrush;
            if (brush == null) brush = new SolidColorBrush(Color.FromRgb(0, 82, 255));

            ProgressArcPath.Stroke = brush;
            ThumbInnerCore.Fill = brush;
            SuperMiniLogoBadge.Background = brush;

            var solidBrush = brush as SolidColorBrush;
            Color themeColor = solidBrush != null ? solidBrush.Color : Color.FromRgb(0, 82, 255);

            EdgeGlowEllipse.Stroke = brush;
            EdgeGlowEffect.Color = themeColor;
        }

        private void StartGlowPulse()
        {
            StopGlowPulse();
            if (!_isGlowEnabled || !IsSuperMinimalMode) return;

            // Periodic subtle edge glow pulsing
            var glowAnim = new DoubleAnimation
            {
                From = 0.35,
                To = 0.85,
                Duration = new Duration(TimeSpan.FromSeconds(1.6)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            _glowPulseStoryboard = new Storyboard();
            _glowPulseStoryboard.Children.Add(glowAnim);
            Storyboard.SetTarget(glowAnim, EdgeGlowEllipse);
            Storyboard.SetTargetProperty(glowAnim, new PropertyPath("Opacity"));
            _glowPulseStoryboard.Begin();
        }

        private void StopGlowPulse()
        {
            if (_glowPulseStoryboard != null)
            {
                _glowPulseStoryboard.Stop();
                _glowPulseStoryboard = null;
            }
        }

        public void ToggleSuperMiniGlow()
        {
            _isGlowEnabled = !_isGlowEnabled;
            EdgeGlowEllipse.Visibility = IsSuperMinimalMode && _isGlowEnabled
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (IsSuperMinimalMode && _isGlowEnabled)
            {
                StartGlowPulse();
            }
            else
            {
                StopGlowPulse();
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

            if (IsSuperMinimalMode)
            {
                _radius = (size - strokeThickness - 22.0) / 2.0;

                double bgSize = _radius * 2.0 + strokeThickness + 6.0;
                SuperMiniBackgroundEllipse.Width = bgSize;
                SuperMiniBackgroundEllipse.Height = bgSize;
                Canvas.SetLeft(SuperMiniBackgroundEllipse, _center.X - bgSize / 2.0);
                Canvas.SetTop(SuperMiniBackgroundEllipse, _center.Y - bgSize / 2.0);

                double glowSize = _radius * 2.0 + strokeThickness + 10.0;
                EdgeGlowEllipse.Width = glowSize;
                EdgeGlowEllipse.Height = glowSize;
                Canvas.SetLeft(EdgeGlowEllipse, _center.X - glowSize / 2.0);
                Canvas.SetTop(EdgeGlowEllipse, _center.Y - glowSize / 2.0);

            }
            else if (IsMinimalMode)
            {
                // In minimal mode, outer boundary of ring and thumb almost touches the canvas border (188px on 190px canvas)
                _radius = (size - 18.0) / 2.0;
            }
            else
            {
                _radius = (size - strokeThickness - 16.0) / 2.0;
            }

            var trackGeom = new EllipseGeometry(_center, _radius, _radius);
            TrackPath.Data = trackGeom;

            UpdateRingAndNode();
        }

        private void UpdateRingAndNode()
        {
            if (_radius <= 0) return;

            CompletedSegmentsCanvas.Children.Clear();

            if (IsAtBeginning)
            {
                // ==========================================
                // 1. SETTING MODE (At the beginning, stopped at zero)
                // ==========================================
                AdjustmentThumb.Cursor = Cursors.Hand;
                AdjustmentThumb.ToolTip = "Drag node to set target duration (0 - 16h)";

                double totalSec = TargetDuration.TotalSeconds;
                double maxSec = MaxDurationHours * 3600.0;
                double fraction = Math.Max(0.0, Math.Min(1.0, totalSec / maxSec));
                double angleDeg = fraction * 360.0;

                SetThumbAngle(angleDeg);
                ProgressArcPath.Data = null;

                if (angleDeg > 1.0 && angleDeg < 359.0)
                {
                    TargetGuidePath.Data = CreateArcGeometry(0.0, angleDeg);
                }
                else
                {
                    TargetGuidePath.Data = null;
                }
            }
            else
            {
                // ==========================================
                // 2. ACTIVE SESSION MODE (Sessions of One Duration)
                // ==========================================
                // The ring represents the one target duration!
                // 1 full revolution = TargetDuration.
                // Slices represent the sessions of that one duration.
                AdjustmentThumb.Cursor = Cursors.Arrow;
                AdjustmentThumb.ToolTip = null;
                TargetGuidePath.Data = null;

                double targetSec = TargetDuration.TotalSeconds;
                double currentStartAngle = 0.0;

                // 2a. Draw all completed sessions in this duration block
                if (CompletedSegments != null && targetSec > 0)
                {
                    for (int i = 0; i < CompletedSegments.Count; i++)
                    {
                        var seg = CompletedSegments[i];
                        if (seg.EndAngleDeg > seg.StartAngleDeg)
                        {
                            var segPath = new Path
                            {
                                Stroke = seg.SegmentBrush,
                                StrokeThickness = ProgressArcPath.StrokeThickness,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                Data = CreateArcGeometry(seg.StartAngleDeg, seg.EndAngleDeg)
                            };
                            CompletedSegmentsCanvas.Children.Add(segPath);
                            currentStartAngle = Math.Max(currentStartAngle, seg.EndAngleDeg);
                        }
                    }
                }

                // 2b. Draw active session arc up to current total angle
                double totalSecElapsed = TotalElapsed.TotalSeconds;
                double totalRatio = targetSec > 0 ? (totalSecElapsed / targetSec) : 0.0;
                double clampedTotalRatio = Math.Max(0.0, Math.Min(1.0, totalRatio));
                double currentTotalAngle = clampedTotalRatio * 360.0;

                if (currentTotalAngle > currentStartAngle)
                {
                    ProgressArcPath.Stroke = RingBrush;
                    ProgressArcPath.Data = CreateArcGeometry(currentStartAngle, currentTotalAngle);
                }
                else
                {
                    ProgressArcPath.Data = null;
                }

                // Position node right at the head of the current total progress!
                SetThumbAngle(currentTotalAngle);
            }
        }

        private Geometry CreateArcGeometry(double startAngleDeg, double endAngleDeg)
        {
            double sweep = endAngleDeg - startAngleDeg;
            if (sweep <= 0.05) return null;

            if (sweep >= 359.9)
            {
                return new EllipseGeometry(_center, _radius, _radius);
            }

            double startRad = (startAngleDeg - 90.0) * Math.PI / 180.0;
            double endRad = (endAngleDeg - 90.0) * Math.PI / 180.0;

            Point p1 = new Point(_center.X + _radius * Math.Cos(startRad), _center.Y + _radius * Math.Sin(startRad));
            Point p2 = new Point(_center.X + _radius * Math.Cos(endRad), _center.Y + _radius * Math.Sin(endRad));

            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(p1, false, false);
                ctx.ArcTo(p2, new Size(_radius, _radius), 0, sweep > 180.0, SweepDirection.Clockwise, true, false);
            }
            geom.Freeze();
            return geom;
        }

        private void SetThumbAngle(double angleDeg)
        {
            double rad = (angleDeg - 90.0) * Math.PI / 180.0;
            double thumbX = _center.X + _radius * Math.Cos(rad) - (AdjustmentThumb.Width / 2.0);
            double thumbY = _center.Y + _radius * Math.Sin(rad) - (AdjustmentThumb.Height / 2.0);

            Canvas.SetLeft(AdjustmentThumb, thumbX);
            Canvas.SetTop(AdjustmentThumb, thumbY);
        }

        private void OnThumbMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsAtBeginning) return;

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
            if (_isDraggingThumb && IsAtBeginning)
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
            if (!IsAtBeginning) return;

            double dx = pos.X - _center.X;
            double dy = pos.Y - _center.Y;

            double rad = Math.Atan2(dx, -dy);
            if (rad < 0) rad += 2 * Math.PI;

            double angleDeg = rad * 180.0 / Math.PI;
            double totalSeconds = (angleDeg / 360.0) * (MaxDurationHours * 3600.0);

            double totalMinutes = Math.Round(totalSeconds / 60.0);
            if (totalMinutes < 1) totalMinutes = 1;
            if (totalMinutes > (MaxDurationHours * 60)) totalMinutes = MaxDurationHours * 60;

            TimeSpan newTarget = TimeSpan.FromMinutes(totalMinutes);
            TargetDuration = newTarget;

            DateTime finish = DateTime.Now + newTarget;
            TargetFinishTime = finish.ToString("HH:mm:ss");

            int hours = (int)newTarget.TotalHours;
            int mins = newTarget.Minutes;
            TargetBubbleText.Text = string.Format("Target: {0:D2}h {1:D2}m", hours, mins);

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
            if (!IsAtBeginning) return;

            int deltaMinutes = e.Delta > 0 ? 1 : -1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                deltaMinutes *= 5;
            }

            double currentMins = TargetDuration.TotalMinutes;
            double newMins = Math.Max(1, Math.Min(MaxDurationHours * 60, currentMins + deltaMinutes));
            TimeSpan newTarget = TimeSpan.FromMinutes(newMins);
            TargetDuration = newTarget;

            DateTime finish = DateTime.Now + newTarget;
            TargetFinishTime = finish.ToString("HH:mm:ss");

            if (TargetDurationAdjusted != null)
            {
                TargetDurationAdjusted(this, newTarget);
            }
            e.Handled = true;
        }
    }
}
