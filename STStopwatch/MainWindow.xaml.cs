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
using STStopwatch.Services;
using STStopwatch.ViewModels;
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
                if (vm.IsStatisticsOpen)
                {
                    ApplyStatisticsWindowWidth(true);
                }

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
                }
            }
            if (e.PropertyName == "IsStatisticsOpen")
            {
                var vm = DataContext as MainViewModel;
                if (vm != null && !vm.IsMinimalMode && !vm.IsSuperMinimalMode)
                {
                    ApplyStatisticsWindowWidth(vm.IsStatisticsOpen);
                }
            }
            if (e.PropertyName == "IsSuperMinimalMode" || e.PropertyName == "IsAutoFreeAreaEnabled")
            {
                UpdateAutoFreeRegionWatcher();
            }
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
                ControlsBar.Margin = new Thickness(0, 0, 0, 20);
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

                // Shift + Left Click toggles Super Minimized View!
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
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
                            // Shift click returns / enters super minimized view
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
