using System;
using System.Media;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using STStopwatch.Common;
using STStopwatch.Models;
using STStopwatch.Services;

namespace STStopwatch.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ITimerEngine _timerEngine;
        private readonly IStorageService _storageService;
        private readonly ITrayService _trayService;
        private readonly DispatcherTimer _uiTimer;

        private SessionModel[] _sessions;
        private int _currentSessionIndex; // 0 to 11
        private TimeSpan _elapsed;
        private double _progress;
        private bool _isMinimalMode;
        private bool _isAlwaysOnTop;
        private bool _isStatisticsOpen;
        private DateTime _sessionStartTime;
        private bool _targetNotificationFired;

        public MainViewModel(ITimerEngine timerEngine, IStorageService storageService, ITrayService trayService)
        {
            _timerEngine = timerEngine;
            _storageService = storageService;
            _trayService = trayService;

            _sessions = SessionModel.CreateDefault12Sessions();
            StatisticsVM = new StatisticsViewModel(_storageService);

            // Load settings
            UserSettings settings = _storageService.LoadSettings();
            _isAlwaysOnTop = settings.IsAlwaysOnTop;
            _isMinimalMode = settings.IsMinimalMode;

            // Load target durations per session
            if (settings.SessionTargetsInSeconds != null)
            {
                for (int i = 0; i < Math.Min(12, settings.SessionTargetsInSeconds.Length); i++)
                {
                    double sec = settings.SessionTargetsInSeconds[i];
                    if (sec > 0)
                    {
                        _sessions[i].TargetDuration = TimeSpan.FromSeconds(sec);
                    }
                }
            }

            int sessNum = settings.CurrentSessionNumber;
            if (sessNum < 1 || sessNum > 12) sessNum = 1;
            _currentSessionIndex = sessNum - 1;

            _sessionStartTime = DateTime.Now;

            // Setup UI Dispatcher Timer (~30 FPS)
            _uiTimer = new DispatcherTimer(DispatcherPriority.Render);
            _uiTimer.Interval = TimeSpan.FromMilliseconds(33);
            _uiTimer.Tick += OnUiTimerTick;
            _uiTimer.Start();

            // Commands
            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            ResetCommand = new RelayCommand(ExecuteReset, CanExecuteReset);
            NextSessionCommand = new RelayCommand(ExecuteNextSession);
            ToggleMinimalModeCommand = new RelayCommand(ExecuteToggleMinimalMode);
            ToggleAlwaysOnTopCommand = new RelayCommand(ExecuteToggleAlwaysOnTop);
            ToggleStatisticsCommand = new RelayCommand(ExecuteToggleStatistics);
            CloseCommand = new RelayCommand(ExecuteClose);
            SelectSessionCommand = new RelayCommand(ExecuteSelectSession);

            UpdateTimerDisplay();
        }

        public StatisticsViewModel StatisticsVM { get; private set; }

        public SessionModel[] Sessions
        {
            get { return _sessions; }
        }

        public SessionModel CurrentSession
        {
            get { return _sessions[_currentSessionIndex]; }
        }

        public int CurrentSessionNumber
        {
            get { return _currentSessionIndex + 1; }
        }

        public string CurrentSessionLabel
        {
            get { return string.Format("Session {0:D2}", CurrentSessionNumber); }
        }

        public string CurrentSessionPillText
        {
            get { return string.Format("{0:D2} / 12", CurrentSessionNumber); }
        }

        public SolidColorBrush CurrentSessionColor
        {
            get { return CurrentSession.ThemeBrush; }
        }

        public TimeSpan TargetDuration
        {
            get { return CurrentSession.TargetDuration; }
            set
            {
                if (CurrentSession.TargetDuration != value)
                {
                    CurrentSession.TargetDuration = value;
                    OnPropertyChanged("TargetDuration");
                    OnPropertyChanged("FormattedTarget");
                    SaveSettings();
                    UpdateTimerDisplay();
                }
            }
        }

        public TimeSpan Elapsed
        {
            get { return _elapsed; }
            private set
            {
                if (SetProperty(ref _elapsed, value, "Elapsed"))
                {
                    OnPropertyChanged("FormattedElapsed");
                }
            }
        }

        public string FormattedElapsed
        {
            get
            {
                int hours = (int)_elapsed.TotalHours;
                return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, _elapsed.Minutes, _elapsed.Seconds);
            }
        }

        public string FormattedTarget
        {
            get
            {
                int hours = (int)TargetDuration.TotalHours;
                return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, TargetDuration.Minutes, TargetDuration.Seconds);
            }
        }

        public double Progress
        {
            get { return _progress; }
            private set { SetProperty(ref _progress, value, "Progress"); }
        }

        public bool IsRunning
        {
            get { return _timerEngine.IsRunning; }
        }

        public bool IsPaused
        {
            get { return !_timerEngine.IsRunning && _elapsed > TimeSpan.Zero; }
        }

        public string PlayPauseButtonLabel
        {
            get
            {
                if (IsRunning) return "Pause";
                if (IsPaused) return "Resume";
                return "Start";
            }
        }

        public bool IsMinimalMode
        {
            get { return _isMinimalMode; }
            set
            {
                if (SetProperty(ref _isMinimalMode, value, "IsMinimalMode"))
                {
                    if (value && _isStatisticsOpen)
                    {
                        IsStatisticsOpen = false;
                    }
                    _trayService.UpdateState(_isMinimalMode, _isAlwaysOnTop);
                    SaveSettings();
                }
            }
        }

        public bool IsAlwaysOnTop
        {
            get { return _isAlwaysOnTop; }
            set
            {
                if (SetProperty(ref _isAlwaysOnTop, value, "IsAlwaysOnTop"))
                {
                    _trayService.UpdateState(_isMinimalMode, _isAlwaysOnTop);
                    SaveSettings();
                }
            }
        }

        public bool IsStatisticsOpen
        {
            get { return _isStatisticsOpen; }
            set
            {
                if (SetProperty(ref _isStatisticsOpen, value, "IsStatisticsOpen"))
                {
                    if (value)
                    {
                        StatisticsVM.Refresh();
                    }
                }
            }
        }

        public ICommand PlayPauseCommand { get; private set; }
        public ICommand ResetCommand { get; private set; }
        public ICommand NextSessionCommand { get; private set; }
        public ICommand ToggleMinimalModeCommand { get; private set; }
        public ICommand ToggleAlwaysOnTopCommand { get; private set; }
        public ICommand ToggleStatisticsCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand SelectSessionCommand { get; private set; }

        public event EventHandler RequestClose;

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            Elapsed = _timerEngine.Elapsed;

            double targetSeconds = TargetDuration.TotalSeconds;
            if (targetSeconds > 0)
            {
                Progress = _elapsed.TotalSeconds / targetSeconds;
            }
            else
            {
                Progress = 0.0;
            }

            // Check if target was reached for notification
            if (IsRunning && !_targetNotificationFired && _elapsed >= TargetDuration && TargetDuration > TimeSpan.Zero)
            {
                _targetNotificationFired = true;
                try
                {
                    SystemSounds.Asterisk.Play();
                }
                catch
                {
                }
                _trayService.ShowNotification(
                    "Target Reached",
                    string.Format("{0} completed its target duration of {1}!", CurrentSessionLabel, FormattedTarget));
            }
        }

        private void ExecutePlayPause()
        {
            if (_timerEngine.IsRunning)
            {
                _timerEngine.Pause();
            }
            else
            {
                if (_elapsed == TimeSpan.Zero)
                {
                    _sessionStartTime = DateTime.Now;
                    _targetNotificationFired = false;
                }
                _timerEngine.Start();
            }

            NotifyStateChanged();
        }

        private bool CanExecuteReset()
        {
            return _elapsed > TimeSpan.Zero || _timerEngine.IsRunning;
        }

        private void ExecuteReset()
        {
            _timerEngine.Reset();
            _targetNotificationFired = false;
            UpdateTimerDisplay();
            NotifyStateChanged();
        }

        private void ExecuteNextSession()
        {
            // Consistent policy: Record completed / accumulated session before advancing
            RecordCurrentSessionIfAny();

            // Reset current stopwatch
            _timerEngine.Reset();
            _targetNotificationFired = false;
            UpdateTimerDisplay();

            // Advance session up to session 12, then loop to 1
            _currentSessionIndex = (_currentSessionIndex + 1) % 12;

            _sessionStartTime = DateTime.Now;

            OnPropertyChanged("CurrentSession");
            OnPropertyChanged("CurrentSessionNumber");
            OnPropertyChanged("CurrentSessionLabel");
            OnPropertyChanged("CurrentSessionPillText");
            OnPropertyChanged("CurrentSessionColor");
            OnPropertyChanged("TargetDuration");
            OnPropertyChanged("FormattedTarget");

            SaveSettings();
            NotifyStateChanged();
        }

        private void ExecuteSelectSession(object param)
        {
            int sessionNum;
            if (param != null && int.TryParse(param.ToString(), out sessionNum))
            {
                if (sessionNum >= 1 && sessionNum <= 12)
                {
                    if (sessionNum - 1 != _currentSessionIndex)
                    {
                        RecordCurrentSessionIfAny();
                        _timerEngine.Reset();
                        _targetNotificationFired = false;
                        _currentSessionIndex = sessionNum - 1;
                        _sessionStartTime = DateTime.Now;

                        OnPropertyChanged("CurrentSession");
                        OnPropertyChanged("CurrentSessionNumber");
                        OnPropertyChanged("CurrentSessionLabel");
                        OnPropertyChanged("CurrentSessionPillText");
                        OnPropertyChanged("CurrentSessionColor");
                        OnPropertyChanged("TargetDuration");
                        OnPropertyChanged("FormattedTarget");

                        SaveSettings();
                        NotifyStateChanged();
                    }
                }
            }
        }

        private void RecordCurrentSessionIfAny()
        {
            TimeSpan currentElapsed = _timerEngine.Elapsed;
            if (currentElapsed.TotalSeconds >= 1.0)
            {
                var record = new SessionRecord();
                record.SessionNumber = CurrentSessionNumber;
                record.SessionLabel = CurrentSessionLabel;
                record.StartTime = _sessionStartTime;
                record.EndTime = DateTime.Now;
                record.TargetDurationSeconds = TargetDuration.TotalSeconds;
                record.ElapsedSeconds = currentElapsed.TotalSeconds;
                record.IsCompleted = currentElapsed >= TargetDuration;
                record.ThemeColorHex = CurrentSession.ThemeColorHex;

                _storageService.SaveRecord(record);
                StatisticsVM.Refresh();
            }
        }

        private void ExecuteToggleMinimalMode()
        {
            IsMinimalMode = !IsMinimalMode;
        }

        private void ExecuteToggleAlwaysOnTop()
        {
            IsAlwaysOnTop = !IsAlwaysOnTop;
        }

        private void ExecuteToggleStatistics()
        {
            IsStatisticsOpen = !IsStatisticsOpen;
        }

        private void ExecuteClose()
        {
            RecordCurrentSessionIfAny();
            SaveSettings();
            if (RequestClose != null)
            {
                RequestClose(this, EventArgs.Empty);
            }
        }

        private void NotifyStateChanged()
        {
            OnPropertyChanged("IsRunning");
            OnPropertyChanged("IsPaused");
            OnPropertyChanged("PlayPauseButtonLabel");
            CommandManager.InvalidateRequerySuggested();
        }

        private void SaveSettings()
        {
            var settings = new UserSettings();
            settings.CurrentSessionNumber = CurrentSessionNumber;
            settings.IsAlwaysOnTop = IsAlwaysOnTop;
            settings.IsMinimalMode = IsMinimalMode;

            var targets = new double[12];
            for (int i = 0; i < 12; i++)
            {
                targets[i] = _sessions[i].TargetDuration.TotalSeconds;
            }
            settings.SessionTargetsInSeconds = targets;

            _storageService.SaveSettings(settings);
        }
    }
}
