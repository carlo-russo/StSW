using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using STStopwatch.Services;
using STStopwatch.ViewModels;

namespace STStopwatch
{
    public partial class MainWindow : Window
    {
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
                ApplyWindowModeDimensions(vm.IsMinimalMode);

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

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsMinimalMode")
            {
                var vm = DataContext as MainViewModel;
                if (vm != null)
                {
                    ApplyWindowModeDimensions(vm.IsMinimalMode);
                }
            }
        }

        private void ApplyWindowModeDimensions(bool isMinimal)
        {
            if (isMinimal)
            {
                Width = 280;
                Height = 360;
            }
            else
            {
                Width = 380;
                Height = 530;
            }
        }

        private void OnWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                }
            }
        }

        private void OnMinimizeWindowClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                var storage = new JsonStorageService();
                var settings = storage.LoadSettings();
                settings.WindowLeft = Left;
                settings.WindowTop = Top;
                settings.IsMinimalMode = vm.IsMinimalMode;
                settings.IsAlwaysOnTop = vm.IsAlwaysOnTop;
                storage.SaveSettings(settings);
            }
        }
    }
}
