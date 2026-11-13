using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using STStopwatch.Core.Services;
using STStopwatch.Core.ViewModels;
using STStopwatch.Services;
using Point = System.Windows.Point;

namespace STStopwatch
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _superMiniTapTimer;
        private int _superMiniTapCount;

        private DispatcherTimer _autoFreeRegionWatcherTimer;
        private FreeRegionFinderService _freeRegionService = new FreeRegionFinderService();
        private bool _isSmoothMoving;
        private bool _isEvaluatingRegion;
        private GuideCompanionWindow _guideCompanion;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;

                // Restore window dimensions according to mode
                ApplyWindowModeDimensions(vm.IsMinimalMode, vm.IsSuperMinimalMode);
                ApplyTheme(vm.ThemeName);
                if (vm.IsStatisticsOpen)
                {
                    ApplyStatisticsWindowWidth(true);
                }
                else if (vm.IsSettingsOpen) ApplyStatisticsWindowWidth(true);

                // Restore position
                var storage = new JsonStorageService();
                var settings = storage.LoadSettings();
                if (settings.WindowLeft > -1000 && settings.WindowTop > -1000 && settings.WindowLeft < 50000)
                {
                    // Ensure it is on screen
                    if (settings.WindowLeft + 100 < SystemParameters.VirtualScreenWidth &&
                        settings.WindowTop + 100 < SystemParameters.VirtualScreenHeight)
                    {
                        Left = Math.Max(0, settings.WindowLeft);
                        Top = Math.Max(0, settings.WindowTop);
                    }
                }
                SyncGuideCompanion(vm.IsGuideCompanionEnabled);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                source.AddHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;

            if (msg == WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xFFF0;
                if (command == SC_MAXIMIZE)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
            }
            return IntPtr.Zero;
        }

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsMinimalMode" || e.PropertyName == "IsSuperMinimalMode")
            {
                var vm = DataContext as MainViewModel;
                if (vm != null)
                {
                    ApplyWindowModeDimensions(vm.IsMinimalMode, vm.IsSuperMinimalMode);
                    ApplyTheme(vm.ThemeName);
                }
            }
            if (e.PropertyName == "IsStatisticsOpen")
            {
                var vm = DataContext as MainViewModel;
                if (vm != null && !vm.IsMinimalMode && !vm.IsSuperMinimalMode)
                {
                    ApplyStatisticsWindowWidth(vm.IsStatisticsOpen);
                    ApplyTheme(vm.ThemeName);
                }
            }
            if (e.PropertyName == "IsSettingsOpen")
            {
                var vm = DataContext as MainViewModel;
                if (vm != null && !vm.IsMinimalMode && !vm.IsSuperMinimalMode)
                {
                    ApplyStatisticsWindowWidth(vm.IsSettingsOpen || vm.IsStatisticsOpen);
                    ApplyTheme(vm.ThemeName);
                }
            }
            if (e.PropertyName == "ThemeName")
            {
                var vm = DataContext as MainViewModel;
                if (vm != null) ApplyTheme(vm.ThemeName);
            }
            if (e.PropertyName == "IsSuperMinimalMode" || e.PropertyName == "IsAutoFreeAreaEnabled")
            {
                UpdateAutoFreeRegionWatcher();
            }
            if (e.PropertyName == "IsGuideCompanionEnabled")
            {
                var vm = DataContext as MainViewModel;
                SyncGuideCompanion(vm != null && vm.IsGuideCompanionEnabled);
            }
        }

        private void SyncGuideCompanion(bool enabled)
        {
            if (enabled)
            {
                if (_guideCompanion == null)
                {
                    _guideCompanion = new GuideCompanionWindow();
                    PositionGuideCompanionBesideTimer();
                    _guideCompanion.Show();
                }
            }
            else if (_guideCompanion != null)
            {
                _guideCompanion.Close();
                _guideCompanion = null;
            }
        }

        private void PositionGuideCompanionBesideTimer()
        {
            double companionWidth = _guideCompanion.Width;
            double companionHeight = _guideCompanion.Height;
            double right = Left + ActualWidth;
            double leftCandidate = Left - companionWidth;
            bool fitsRight = right + companionWidth <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
            bool fitsLeft = leftCandidate >= SystemParameters.VirtualScreenLeft;
            double targetLeft = fitsRight || !fitsLeft ? right - 4 : leftCandidate + 4;
            double targetTop = Top + Math.Max(0, (ActualHeight - companionHeight) / 2);
            targetLeft = Math.Max(SystemParameters.VirtualScreenLeft,
                Math.Min(targetLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - companionWidth));
            targetTop = Math.Max(SystemParameters.VirtualScreenTop,
                Math.Min(targetTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - companionHeight));

            _guideCompanion.Left = targetLeft;
            _guideCompanion.Top = targetTop;
        }

        private void ApplyTheme(string themeName)
        {
            var settingsPanel = FindName("SettingsPanel") as System.Windows.Controls.Border;
            bool dark = string.Equals(themeName, "Dark", StringComparison.OrdinalIgnoreCase);
            var vm = DataContext as MainViewModel;
            WindowRootBorder.Background = dark ? BrushFromRgb(31, 36, 48) :
                (vm != null && vm.IsSuperMinimalMode ? Brushes.Transparent : (vm != null && vm.IsMinimalMode ? Brushes.White : BrushFromRgb(247, 250, 254)));
            WindowRootBorder.BorderBrush = dark ? BrushFromRgb(70, 80, 99) : BrushFromRgb(184, 203, 226);
            HeaderBar.Background = dark ? BrushFromRgb(40, 47, 62) : BrushFromRgb(240, 244, 248);
            HeaderBar.BorderBrush = dark ? BrushFromRgb(70, 80, 99) : BrushFromRgb(199, 213, 230);
            if (settingsPanel != null)
            {
                settingsPanel.Background = dark ? BrushFromRgb(40, 47, 62) : BrushFromRgb(247, 250, 254);
                settingsPanel.BorderBrush = dark ? BrushFromRgb(70, 80, 99) : BrushFromRgb(199, 213, 230);
            }

            foreach (DependencyObject element in WalkVisualTree(WindowRootBorder))
                ApplyThemeToElement(element, dark);
        }

        private static SolidColorBrush BrushFromRgb(byte red, byte green, byte blue)
        {
            return new SolidColorBrush(Color.FromRgb(red, green, blue));
        }

        private static System.Collections.Generic.IEnumerable<DependencyObject> WalkVisualTree(DependencyObject parent)
        {
            if (parent == null) yield break;
            var visual = parent as Visual;
            var visual3D = parent as System.Windows.Media.Media3D.Visual3D;
            if (visual == null && visual3D == null) yield break;
            yield return parent;
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                foreach (DependencyObject child in WalkVisualTree(VisualTreeHelper.GetChild(parent, i)))
                    yield return child;
            }
        }

        private static void ApplyThemeToElement(DependencyObject element, bool dark)
        {
            var control = element as System.Windows.Controls.Control;
            if (control != null)
            {
                if (control.ReadLocalValue(System.Windows.Controls.Control.BackgroundProperty) != DependencyProperty.UnsetValue)
                    control.Background = MapThemeBrush(control.Background, dark, false);
                if (control.ReadLocalValue(System.Windows.Controls.Control.ForegroundProperty) != DependencyProperty.UnsetValue)
                    control.Foreground = MapThemeBrush(control.Foreground, dark, true);
                if (control.ReadLocalValue(System.Windows.Controls.Control.BorderBrushProperty) != DependencyProperty.UnsetValue)
                    control.BorderBrush = MapThemeBrush(control.BorderBrush, dark, false);
            }

            var border = element as System.Windows.Controls.Border;
            if (border != null && border.TemplatedParent == null)
            {
                border.Background = MapThemeBrush(border.Background, dark, false);
                border.BorderBrush = MapThemeBrush(border.BorderBrush, dark, false);
            }

            var panel = element as System.Windows.Controls.Panel;
            if (panel != null && panel.ReadLocalValue(System.Windows.Controls.Panel.BackgroundProperty) != DependencyProperty.UnsetValue)
                panel.Background = MapThemeBrush(panel.Background, dark, false);

            var text = element as System.Windows.Controls.TextBlock;
            if (text != null)
            {
                if (text.ReadLocalValue(System.Windows.Controls.TextBlock.ForegroundProperty) != DependencyProperty.UnsetValue)
                    text.Foreground = MapThemeBrush(text.Foreground, dark, true);
                foreach (System.Windows.Documents.Inline inline in text.Inlines)
                {
                    var run = inline as System.Windows.Documents.Run;
                    if (run != null) run.Foreground = MapThemeBrush(run.Foreground, dark, true);
                }
            }

            var shape = element as System.Windows.Shapes.Shape;
            if (shape != null)
            {
                shape.Fill = MapThemeBrush(shape.Fill, dark, false);
                shape.Stroke = MapThemeBrush(shape.Stroke, dark, false);
            }
        }

        private static Brush MapThemeBrush(Brush brush, bool dark, bool isText)
        {
            var solid = brush as SolidColorBrush;
            if (solid == null || solid.Color.A == 0) return brush;
            string key = string.Format("#{0:X2}{1:X2}{2:X2}", solid.Color.R, solid.Color.G, solid.Color.B);
            string mapped = null;
            if (isText)
            {
                if (dark)
                {
                    switch (key)
                    {
                        case "#111111": mapped = "#F1F5F9"; break;
                        case "#172B4D": mapped = "#E2E8F0"; break;
                        case "#43546B": mapped = "#CBD5E1"; break;
                        case "#475569": mapped = "#CBD5E1"; break;
                        case "#64748B": mapped = "#AAB8CA"; break;
                        case "#0052FF": mapped = "#7FA5FF"; break;
                        case "#087F70": mapped = "#4FD1B5"; break;
                        case "#C45135": mapped = "#FF9A76"; break;
                    }
                }
                else
                {
                    switch (key)
                    {
                        case "#F1F5F9": mapped = "#111111"; break;
                        case "#E2E8F0": mapped = "#172B4D"; break;
                        case "#CBD5E1": mapped = "#43546B"; break;
                        case "#AAB8CA": mapped = "#64748B"; break;
                        case "#7FA5FF": mapped = "#0052FF"; break;
                        case "#4FD1B5": mapped = "#087F70"; break;
                        case "#FF9A76": mapped = "#C45135"; break;
                    }
                }
            }
            else if (dark)
            {
                switch (key)
                {
                    case "#F7FAFE": mapped = "#1F2430"; break;
                    case "#F0F4F8": mapped = "#282F3E"; break;
                    case "#EDF4FC": mapped = "#293345"; break;
                    case "#F8FAFC": mapped = "#252B38"; break;
                    case "#FFFFFF": mapped = "#313949"; break;
                    case "#F0EDFF": mapped = "#332E50"; break;
                    case "#E8FAF5": mapped = "#203D37"; break;
                    case "#FFF1E9": mapped = "#49352E"; break;
                    case "#DBEAFE": mapped = "#263B57"; break;
                    case "#C7D5E6": mapped = "#4B586D"; break;
                    case "#B8CBE2": mapped = "#4B586D"; break;
                    case "#A9C4E3": mapped = "#4B586D"; break;
                    case "#E2E8F0": mapped = "#4B586D"; break;
                    case "#D1C9FF": mapped = "#595081"; break;
                    case "#B6EBDD": mapped = "#315D54"; break;
                    case "#FFD3C2": mapped = "#634639"; break;
                }
            }
            else
            {
                switch (key)
                {
                    case "#1F2430": mapped = "#F7FAFE"; break;
                    case "#282F3E": mapped = "#F0F4F8"; break;
                    case "#293345": mapped = "#EDF4FC"; break;
                    case "#252B38": mapped = "#F8FAFC"; break;
                    case "#313949": mapped = "#FFFFFF"; break;
                    case "#332E50": mapped = "#F0EDFF"; break;
                    case "#203D37": mapped = "#E8FAF5"; break;
                    case "#49352E": mapped = "#FFF1E9"; break;
                    case "#263B57": mapped = "#DBEAFE"; break;
                    case "#4B586D": mapped = "#C7D5E6"; break;
                    case "#595081": mapped = "#D1C9FF"; break;
                    case "#315D54": mapped = "#B6EBDD"; break;
                    case "#634639": mapped = "#FFD3C2"; break;
                }
            }
            if (mapped == null) return brush;
            var color = (Color)ColorConverter.ConvertFromString(mapped);
            return new SolidColorBrush(Color.FromArgb(solid.Color.A, color.R, color.G, color.B));
        }

        private void ApplyStatisticsWindowWidth(bool isOpen)
        {
            if (isOpen)
            {
                MinWidth = 480;
                MaxWidth = 520;
                Width = 500;
            }
            else
            {
                MinWidth = 280;
                MaxWidth = 380;
                Width = 380;
            }
        }

        private void ApplyWindowModeDimensions(bool isMinimal, bool isSuperMinimal)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }

            if (isSuperMinimal)
            {
                MinWidth = 180;
                MinHeight = 180;
                MaxWidth = 200;
                MaxHeight = 200;
                Width = 200;
                Height = 200;

                WindowRootBorder.Margin = new Thickness(6);
                WindowRootBorder.BorderThickness = new Thickness(0);
                WindowRootBorder.Background = Brushes.Transparent;
                WindowRootBorder.CornerRadius = new CornerRadius(100);
                WindowRootBorder.Effect = null;

                RingArea.Margin = new Thickness(0);
                ProgressRing.Height = 190;
                HeaderBar.Visibility = Visibility.Collapsed;
                MinimalTopStrip.Visibility = Visibility.Collapsed;
                ControlsBar.Visibility = Visibility.Collapsed;

                ProgressRing.ApplyModeStyles();
            }
            else if (isMinimal)
            {
                MinWidth = 180;
                MinHeight = 300;
                MaxWidth = 202;
                MaxHeight = 300;
                Width = 202;
                Height = 300;

                WindowRootBorder.Margin = new Thickness(6);
                WindowRootBorder.BorderThickness = new Thickness(1);
                WindowRootBorder.Background = Brushes.White;
                WindowRootBorder.CornerRadius = new CornerRadius(14);
                WindowRootBorder.Effect = new DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 3,
                    Direction = 270,
                    Opacity = 0.18,
                    Color = Colors.Black
                };

                RingArea.Margin = new Thickness(0, 1, 0, 52);
                ProgressRing.Height = 190;
                HeaderBar.Visibility = Visibility.Collapsed;
                MinimalTopStrip.Visibility = Visibility.Visible;
                ControlsBar.Visibility = Visibility.Visible;
                ControlsBar.Padding = new Thickness(0);
                ControlsBar.Margin = new Thickness(0, 0, 0, 4);
                ButtonGrid.Width = 170;
                ButtonGrid.Height = 60;
                ButtonGrid.RowDefinitions[0].Height = new GridLength(30);
                ButtonGrid.RowDefinitions[1].Height = new GridLength(30);

                // Equal reduced sizes for all 3 buttons in minimized view
                PlayPauseButton.Width = 44;
                PlayPauseButton.Height = 30;
                CompleteSessionButton.Width = 44;
                CompleteSessionButton.Height = 30;
                NextSessionButton.Width = 44;
                NextSessionButton.Height = 30;
                ButtonGrid.LayoutTransform = Transform.Identity;

                ProgressRing.ApplyModeStyles();
            }
            else
            {
                MinWidth = 280;
                MinHeight = 360;
                MaxWidth = 380;
                MaxHeight = 530;
                Width = 380;
                Height = 530;

                WindowRootBorder.Margin = new Thickness(14);
                WindowRootBorder.BorderThickness = new Thickness(1);
                WindowRootBorder.Background = Brushes.White;
                WindowRootBorder.CornerRadius = new CornerRadius(16);
                WindowRootBorder.Effect = new DropShadowEffect
                {
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Direction = 270,
                    Opacity = 0.18,
                    Color = Colors.Black
                };

                RingArea.Margin = new Thickness(12, 8, 12, 4);
                ProgressRing.Height = 300;
                HeaderBar.Visibility = Visibility.Visible;
                MinimalTopStrip.Visibility = Visibility.Collapsed;
                ControlsBar.Visibility = Visibility.Visible;
                ControlsBar.Padding = new Thickness(0);
                ControlsBar.Margin = new Thickness(0, 0, 0, 56);
                ButtonGrid.Width = 300;
                ButtonGrid.Height = 76;
                ButtonGrid.RowDefinitions[0].Height = new GridLength(36);
                ButtonGrid.RowDefinitions[1].Height = new GridLength(40);

                PlayPauseButton.Width = 58;
                PlayPauseButton.Height = 36;
                CompleteSessionButton.Width = 60;
                CompleteSessionButton.Height = 38;
                NextSessionButton.Width = 58;
                NextSessionButton.Height = 36;
                ButtonGrid.LayoutTransform = Transform.Identity;

                ProgressRing.ApplyModeStyles();
            }

            UpdateAutoFreeRegionWatcher();
        }

        private void OnWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var vm = DataContext as MainViewModel;

                // Ctrl + Left Click toggles Super Minimized View.
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    if (vm != null)
                    {
                        if (vm.IsSuperMinimalMode)
                        {
                            // Exits super minimized view -> returns to minimized view
                            vm.IsSuperMinimalMode = false;
                        }
                        else
                        {
                            // Ctrl click returns / enters super minimized view
                            vm.IsSuperMinimalMode = true;
                        }
                    }
                    e.Handled = true;
                    return;
                }

                if (vm != null && vm.IsSuperMinimalMode)
                {
                    double startLeft = Left;
                    double startTop = Top;

                    try
                    {
                        DragMove();
                    }
                    catch
                    {
                    }

                    // Check if it was a drag or a tap
                    double moved = Math.Abs(Left - startLeft) + Math.Abs(Top - startTop);
                    if (moved <= 3.0)
                    {
                        // Stationary tap / click in Super Minimized mode
                        _superMiniTapCount++;
                        if (_superMiniTapTimer == null)
                        {
                            _superMiniTapTimer = new DispatcherTimer();
                            _superMiniTapTimer.Tick += OnSuperMiniTapTimerTick;
                        }

                        if (_superMiniTapCount == 1)
                        {
                            // Tap 1: Wait up to 320ms to see if tap 2 arrives
                            _superMiniTapTimer.Stop();
                            _superMiniTapTimer.Interval = TimeSpan.FromMilliseconds(320);
                            _superMiniTapTimer.Start();
                        }
                        else if (_superMiniTapCount == 2)
                        {
                            // Tap 2: Wait up to 280ms to see if tap 3 arrives
                            _superMiniTapTimer.Stop();
                            _superMiniTapTimer.Interval = TimeSpan.FromMilliseconds(280);
                            _superMiniTapTimer.Start();
                        }
                        else if (_superMiniTapCount >= 3)
                        {
                            // Triple tap: Pause / resume!
                            if (_superMiniTapTimer != null)
                            {
                                _superMiniTapTimer.Stop();
                            }
                            _superMiniTapCount = 0;
                            if (vm.PlayPauseCommand != null && vm.PlayPauseCommand.CanExecute(null))
                            {
                                vm.PlayPauseCommand.Execute(null);
                            }
                        }
                    }

                    e.Handled = true;
                    return;
                }

                try
                {
                    DragMove();
                }
                catch
                {
                }
            }
        }

        private void OnWindowPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null && vm.IsSuperMinimalMode && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                ProgressRing.ToggleSuperMiniGlow();
                e.Handled = true;
            }
        }

        private void OnSuperMiniTapTimerTick(object sender, EventArgs e)
        {
            if (_superMiniTapTimer != null)
            {
                _superMiniTapTimer.Stop();
            }

            var vm = DataContext as MainViewModel;
            if (vm != null && vm.IsSuperMinimalMode)
            {
                if (_superMiniTapCount == 2)
                {
                    // Double tap confirmed: Move timer to next session!
                    if (vm.NextSessionCommand != null && vm.NextSessionCommand.CanExecute(null))
                    {
                        vm.NextSessionCommand.Execute(null);
                    }
                }
                // Single tap (_superMiniTapCount == 1): Does nothing (pause is now triple tap)
            }
            _superMiniTapCount = 0;
        }

        private void AutoPositionToFreeArea()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                Point freePoint = _freeRegionService.FindMostFreeRegion(Width, Height, helper.Handle);

                SmoothMoveTo(freePoint.X, freePoint.Y);
            }
            catch
            {
            }
        }

        private void UpdateAutoFreeRegionWatcher()
        {
            var vm = DataContext as MainViewModel;
            bool shouldWatch = vm != null && vm.IsSuperMinimalMode && vm.IsAutoFreeAreaEnabled;

            if (shouldWatch)
            {
                // Hide widget from its own screen captures so the stopwatch circle/text
                // is not mistakenly detected as clutter (Windows 10 2004+).
                try
                {
                    var hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        FreeRegionFinderService.SetWindowDisplayAffinity(hwnd, FreeRegionFinderService.WDA_EXCLUDEFROMCAPTURE);
                    }
                }
                catch { /* Older OS — collar-based detection is the fallback */ }

                if (_autoFreeRegionWatcherTimer == null)
                {
                    _autoFreeRegionWatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
                    _autoFreeRegionWatcherTimer.Interval = TimeSpan.FromMilliseconds(1000);
                    _autoFreeRegionWatcherTimer.Tick += OnAutoFreeRegionWatcherTick;
                }
                if (!_autoFreeRegionWatcherTimer.IsEnabled)
                {
                    _freeRegionService.ResetCooldown();
                    _autoFreeRegionWatcherTimer.Start();
                }

                // Initial relocation upon entering
                Dispatcher.BeginInvoke(new Action(AutoPositionToFreeArea), DispatcherPriority.Background);
            }
            else
            {
                if (_autoFreeRegionWatcherTimer != null && _autoFreeRegionWatcherTimer.IsEnabled)
                {
                    _autoFreeRegionWatcherTimer.Stop();
                }

                // Restore normal window capture visibility
                try
                {
                    var hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        FreeRegionFinderService.SetWindowDisplayAffinity(hwnd, FreeRegionFinderService.WDA_NONE);
                    }
                }
                catch { }
            }
        }

        private void OnAutoFreeRegionWatcherTick(object sender, EventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null || !vm.IsSuperMinimalMode || !vm.IsAutoFreeAreaEnabled)
            {
                if (_autoFreeRegionWatcherTimer != null) _autoFreeRegionWatcherTimer.Stop();
                return;
            }

            if (_isSmoothMoving || _isEvaluatingRegion || Mouse.LeftButton == MouseButtonState.Pressed)
            {
                return;
            }

            _isEvaluatingRegion = true;
            IntPtr helperHandle = new WindowInteropHelper(this).Handle;
            double curLeft = Left;
            double curTop = Top;
            double curWidth = Width;
            double curHeight = Height;

            Task.Run(() =>
            {
                try
                {
                    Point? target = _freeRegionService.CheckAndFindTargetIfCluttered(
                        curLeft, curTop, curWidth, curHeight, helperHandle);

                    if (target.HasValue)
                    {
                        Point pt = target.Value;
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (vm.IsSuperMinimalMode && vm.IsAutoFreeAreaEnabled && !_isSmoothMoving && Mouse.LeftButton != MouseButtonState.Pressed)
                            {
                                SmoothMoveTo(pt.X, pt.Y);
                            }
                        }), DispatcherPriority.Background);
                    }
                }
                catch
                {
                }
                finally
                {
                    _isEvaluatingRegion = false;
                }
            });
        }

        private void SmoothMoveTo(double targetLeft, double targetTop)
        {
            try
            {
                _isSmoothMoving = true;
                // Ensure target is safely within virtual screen bounds
                targetLeft = Math.Max(0, Math.Min(targetLeft, SystemParameters.VirtualScreenWidth - Width));
                targetTop = Math.Max(0, Math.Min(targetTop, SystemParameters.VirtualScreenHeight - Height));

                var duration = new Duration(TimeSpan.FromMilliseconds(550));
                var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

                var animLeft = new DoubleAnimation(Left, targetLeft, duration) { EasingFunction = ease };
                var animTop = new DoubleAnimation(Top, targetTop, duration) { EasingFunction = ease };

                animLeft.Completed += delegate
                {
                    BeginAnimation(Window.LeftProperty, null);
                    BeginAnimation(Window.TopProperty, null);
                    Left = targetLeft;
                    Top = targetTop;
                    _isSmoothMoving = false;
                };

                BeginAnimation(Window.LeftProperty, animLeft);
                BeginAnimation(Window.TopProperty, animTop);
            }
            catch
            {
                Left = targetLeft;
                Top = targetTop;
                _isSmoothMoving = false;
            }
        }

        private void OnMinimizeWindowClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (_guideCompanion != null)
            {
                _guideCompanion.Close();
                _guideCompanion = null;
            }
            if (_autoFreeRegionWatcherTimer != null)
            {
                _autoFreeRegionWatcherTimer.Stop();
                _autoFreeRegionWatcherTimer = null;
            }

            if (_superMiniTapTimer != null)
            {
                _superMiniTapTimer.Stop();
                _superMiniTapTimer = null;
            }

            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                var storage = new JsonStorageService();
                var settings = storage.LoadSettings();
                settings.WindowLeft = Left;
                settings.WindowTop = Top;
                settings.IsMinimalMode = vm.IsMinimalMode;
                settings.IsSuperMinimalMode = vm.IsSuperMinimalMode;
                settings.IsAutoFreeAreaEnabled = vm.IsAutoFreeAreaEnabled;
                settings.IsAlwaysOnTop = vm.IsAlwaysOnTop;
                storage.SaveSettings(settings);
            }
        }
    }
}
