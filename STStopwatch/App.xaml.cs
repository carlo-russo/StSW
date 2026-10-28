using System;
using System.Windows;
using STStopwatch.Services;
using STStopwatch.ViewModels;

namespace STStopwatch
{
    public partial class App : Application
    {
        private ITrayService _trayService;

        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            var timerEngine = new HighResolutionTimerEngine();
            var storageService = new JsonStorageService();
            _trayService = new TrayService();

            var mainVm = new MainViewModel(timerEngine, storageService, _trayService);

            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainVm;

            mainVm.RequestClose += delegate
            {
                mainWindow.Close();
            };

            // Setup System Tray
            _trayService.Initialize(
                onRestore: delegate
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        mainWindow.Show();
                        mainWindow.WindowState = WindowState.Normal;
                        mainWindow.Activate();
                    }));
                },
                onToggleMinimal: delegate
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        mainVm.IsMinimalMode = !mainVm.IsMinimalMode;
                        mainWindow.Show();
                        mainWindow.Activate();
                    }));
                },
                onToggleTopmost: delegate
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        mainVm.IsAlwaysOnTop = !mainVm.IsAlwaysOnTop;
                    }));
                },
                onExit: delegate
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        mainVm.CloseCommand.Execute(null);
                        Shutdown();
                    }));
                }
            );

            _trayService.UpdateState(mainVm.IsMinimalMode, mainVm.IsAlwaysOnTop);

            MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_trayService != null)
            {
                _trayService.Dispose();
                _trayService = null;
            }
            base.OnExit(e);
        }
    }
}
