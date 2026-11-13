using System;
using System.Collections.ObjectModel;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;
using STStopwatch.Core.Common;
using STStopwatch.Core.Models;
using STStopwatch.Core.Services;

namespace STStopwatch.Core.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ITimerEngine _timerEngine;
        private readonly IStorageService _storageService;
        private readonly ITrayService _trayService;
        private readonly DispatcherTimer _uiTimer;

        private SessionModel[] _sessions;
        private int _currentSessionIndex; // 0 to 11
        private TimeSpan _currentSessionElapsed;
        private TimeSpan _blockCompletedElapsed;
        private TimeSpan _globalTargetDuration;
        private string _sessionLabelText;
        private string _targetFinishTimeText;
        private double _progress;
        private bool _isMinimalMode;
        private bool _isSuperMinimalMode;
        private bool _isAutoFreeAreaEnabled;
        private bool _startInSuperMinimalMode;
        private bool _isGuideCompanionEnabled;
        private bool _isAlwaysOnTop;
        private bool _isStatisticsOpen;
        private bool _isSettingsOpen;
        private string _themeName;
        private bool _showGettingStarted;
        private DateTime _sessionStartTime;
        private DateTime _todayDurationDate;
        private TimeSpan _todayRecordedDuration;
        private bool _targetNotificationFired;

        private ObservableCollection<RingSessionSegment> _completedSegments;

        public MainViewModel(ITimerEngine timerEngine, IStorageService storageService, ITrayService trayService)
        {
            _timerEngine = timerEngine;
            _storageService = storageService;
            _trayService = trayService;

            _sessions = SessionModel.CreateDefault12Sessions();
            _completedSegments = new ObservableCollection<RingSessionSegment>();
            StatisticsVM = new StatisticsViewModel(_storageService);

            // Load settings
            UserSettings settings = _storageService.LoadSettings();
            _isAlwaysOnTop = settings.IsAlwaysOnTop;
            _isSuperMinimalMode = settings.StartInSuperMinimalMode || settings.IsSuperMinimalMode;
            _isMinimalMode = settings.IsMinimalMode || _isSuperMinimalMode;
            _isAutoFreeAreaEnabled = settings.IsAutoFreeAreaEnabled;
            _startInSuperMinimalMode = settings.StartInSuperMinimalMode;
            _isGuideCompanionEnabled = settings.IsGuideCompanionEnabled;
            _themeName = string.IsNullOrEmpty(settings.ThemeName) ? "Light" : settings.ThemeName;
            _showGettingStarted = !settings.HasDismissedGettingStarted;

            // Global constant target duration
            if (settings.GlobalTargetDurationSeconds >= 60.0)
            {
                _globalTargetDuration = TimeSpan.FromSeconds(settings.GlobalTargetDurationSeconds);
            }
            else
            {
                _globalTargetDuration = TimeSpan.FromHours(1); // Default 1 hour
            }

            int sessNum = settings.CurrentSessionNumber;
            if (sessNum < 1 || sessNum > 12) sessNum = 1;
            _currentSessionIndex = sessNum - 1;

            _sessionLabelText = string.Format("Session {0:D2}", CurrentSessionNumber);
            _sessionStartTime = DateTime.Now;
            RefreshTodayRecordedDuration();

            // Setup UI Dispatcher Timer (~30 FPS)
            _uiTimer = new DispatcherTimer(DispatcherPriority.Render);
            _uiTimer.Interval = TimeSpan.FromMilliseconds(33);
            _uiTimer.Tick += OnUiTimerTick;
            _uiTimer.Start();

            // Commands
            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            CompleteSessionCommand = new RelayCommand(ExecuteCompleteSession, CanExecuteCompleteSession);
            NextSessionCommand = new RelayCommand(ExecuteNextSession);
            ToggleMinimalModeCommand = new RelayCommand(ExecuteToggleMinimalMode);
            ToggleSuperMinimalModeCommand = new RelayCommand(ExecuteToggleSuperMinimalMode);
            ToggleAutoFreeAreaCommand = new RelayCommand(ExecuteToggleAutoFreeArea);
            ToggleAlwaysOnTopCommand = new RelayCommand(ExecuteToggleAlwaysOnTop);
            ToggleStatisticsCommand = new RelayCommand(ExecuteToggleStatistics);
            ToggleSettingsCommand = new RelayCommand(() => IsSettingsOpen = !IsSettingsOpen);
            CloseCommand = new RelayCommand(ExecuteClose);
            SelectSessionCommand = new RelayCommand(ExecuteSelectSession);
            DismissGettingStartedCommand = new RelayCommand(ExecuteDismissGettingStarted);

            UpdateTimerDisplay();
        }

        public StatisticsViewModel StatisticsVM { get; private set; }

        public bool ShowGettingStarted
        {
            get { return _showGettingStarted; }
            private set { SetProperty(ref _showGettingStarted, value, "ShowGettingStarted"); }
        }

        public ICommand DismissGettingStartedCommand { get; private set; }

        public SessionModel[] Sessions
        {
            get { return _sessions; }
        }

        public ObservableCollection<RingSessionSegment> CompletedSegments
        {
            get { return _completedSegments; }
        }

        public SessionModel CurrentSession
        {
            get { return _sessions[_currentSessionIndex]; }
        }

        public int CurrentSessionNumber
        {
            get { return _currentSessionIndex + 1; }
        }

        public string CurrentSessionPillText
        {
            get { return string.Format("{0:D2} / 12", CurrentSessionNumber); }
        }

        public string CurrentSessionColorHex
        {
            get { return CurrentSession.ThemeColorHex; }
        }

        public string SessionLabelText
        {
            get { return _sessionLabelText; }
            set
            {
                if (SetProperty(ref _sessionLabelText, value, "SessionLabelText"))
                {
                    SaveSettings();
                }
            }
        }

        public TimeSpan TargetDuration
        {
            get { return _globalTargetDuration; }
            set
            {
                if (SetProperty(ref _globalTargetDuration, value, "TargetDuration"))
                {
                    OnPropertyChanged("FormattedTarget");
                    SaveSettings();
                    UpdateTimerDisplay();
                }
            }
        }

        public string TargetFinishTimeText
        {
            get { return _targetFinishTimeText; }
            private set { SetProperty(ref _targetFinishTimeText, value, "TargetFinishTimeText"); }
        }

        public TimeSpan Elapsed
        {
            get { return _currentSessionElapsed; }
            private set
            {
                if (SetProperty(ref _currentSessionElapsed, value, "Elapsed"))
                {
                    OnPropertyChanged("FormattedElapsed");
                }
            }
        }

        public TimeSpan TotalElapsed
        {
            get { return _blockCompletedElapsed + _currentSessionElapsed; }
        }

        public string FormattedElapsed
        {
            get
            {
                int hours = (int)_currentSessionElapsed.TotalHours;
                return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, _currentSessionElapsed.Minutes, _currentSessionElapsed.Seconds);
            }
        }

        public string TodayTotalDurationText
        {
            get
            {
                TimeSpan total = _todayRecordedDuration;
                if (_sessionStartTime.Date == DateTime.Today)
                {
                    total += _currentSessionElapsed;
                }

                int hours = (int)total.TotalHours;
                return string.Format("{0:D2}:{1:D2}", hours, total.Minutes);
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
            get { return !_timerEngine.IsRunning && TotalElapsed > TimeSpan.Zero; }
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
                    if (!value)
                    {
                        if (_isSuperMinimalMode)
                        {
                            _isSuperMinimalMode = false;
                            OnPropertyChanged("IsSuperMinimalMode");
                        }
                    }
                    if (value && _isStatisticsOpen)
                    {
                        IsStatisticsOpen = false;
                    }
                    _trayService.UpdateState(_isMinimalMode, _isAlwaysOnTop);
                    SaveSettings();
                }
            }
        }

        public bool IsSuperMinimalMode
        {
            get { return _isSuperMinimalMode; }
            set
            {
                if (SetProperty(ref _isSuperMinimalMode, value, "IsSuperMinimalMode"))
                {
                    if (value)
                    {
                        if (!_isMinimalMode)
                        {
                            _isMinimalMode = true;
                            OnPropertyChanged("IsMinimalMode");
                        }
                        if (_isStatisticsOpen)
                        {
                            IsStatisticsOpen = false;
                        }
                    }
                    _trayService.UpdateState(_isMinimalMode, _isAlwaysOnTop);
                    SaveSettings();
                }
            }
        }

        public bool IsAutoFreeAreaEnabled
        {
            get { return _isAutoFreeAreaEnabled; }
            set
            {
                if (SetProperty(ref _isAutoFreeAreaEnabled, value, "IsAutoFreeAreaEnabled"))
                {
                    SaveSettings();
                }
            }
        }

        public bool StartInSuperMinimalMode
        {
            get { return _startInSuperMinimalMode; }
            set
            {
                if (SetProperty(ref _startInSuperMinimalMode, value, "StartInSuperMinimalMode"))
                    SaveSettings();
            }
        }

        public bool IsGuideCompanionEnabled
        {
            get { return _isGuideCompanionEnabled; }
            set
            {
                if (SetProperty(ref _isGuideCompanionEnabled, value, "IsGuideCompanionEnabled"))
                    SaveSettings();
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
                        IsSettingsOpen = false;
                        StatisticsVM.Refresh();
                    }
                }
            }
        }

        public bool IsSettingsOpen
        {
            get { return _isSettingsOpen; }
            set
            {
                if (value) IsStatisticsOpen = false;
                SetProperty(ref _isSettingsOpen, value, "IsSettingsOpen");
            }
        }

        public string ThemeName
        {
            get { return _themeName; }
            set
            {
                if (SetProperty(ref _themeName, value, "ThemeName"))
                {
                    OnPropertyChanged("IsDarkMode");
                    SaveSettings();
                }
            }
        }

        public bool IsDarkMode
        {
            get { return string.Equals(ThemeName, "Dark", StringComparison.OrdinalIgnoreCase); }
            set { ThemeName = value ? "Dark" : "Light"; }
        }

        public ICommand PlayPauseCommand { get; private set; }
        public ICommand CompleteSessionCommand { get; private set; }
        public ICommand NextSessionCommand { get; private set; }
        public ICommand ToggleMinimalModeCommand { get; private set; }
        public ICommand ToggleSuperMinimalModeCommand { get; private set; }
        public ICommand ToggleAutoFreeAreaCommand { get; private set; }
        public ICommand ToggleAlwaysOnTopCommand { get; private set; }
        public ICommand ToggleStatisticsCommand { get; private set; }
        public ICommand ToggleSettingsCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand SelectSessionCommand { get; private set; }

        public event EventHandler RequestClose;

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (_todayDurationDate != DateTime.Today)
            {
                RefreshTodayRecordedDuration();
            }
            Elapsed = _timerEngine.Elapsed;
            OnPropertyChanged("TotalElapsed");
            OnPropertyChanged("TodayTotalDurationText");

            double targetSeconds = TargetDuration.TotalSeconds;
            double totalSeconds = TotalElapsed.TotalSeconds;

            if (targetSeconds > 0)
            {
                Progress = totalSeconds / targetSeconds;
            }
            else
            {
                Progress = 0.0;
            }

            // Calculate Target Finish Time (when the duration will end)
            if (targetSeconds > 0)
            {
                if (!IsRunning && totalSeconds == 0 && _completedSegments.Count == 0)
                {
                    // Setting mode: finish time from now
                    DateTime finishTime = DateTime.Now + TargetDuration;
                    TargetFinishTimeText = finishTime.ToString("HH:mm:ss");
                }
                else
                {
                    // Active mode: finish time based on remaining time
                    double remainingSeconds = Math.Max(0.0, targetSeconds - totalSeconds);
                    DateTime finishTime = DateTime.Now.AddSeconds(remainingSeconds);
                    if (remainingSeconds <= 0.5)
                    {
                        TargetFinishTimeText = "Goal Reached (" + DateTime.Now.ToString("HH:mm") + ")";
                    }
                    else
                    {
                        TargetFinishTimeText = finishTime.ToString("HH:mm:ss");
                    }
                }
            }
            else
            {
                TargetFinishTimeText = DateTime.Now.ToString("HH:mm:ss");
            }

            // Target reached alert
            if (IsRunning && !_targetNotificationFired && totalSeconds >= targetSeconds && targetSeconds > 0)
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
                    "Target Duration Completed",
                    string.Format("You reached the target duration of {0}!", FormattedTarget));
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
                if (_currentSessionElapsed == TimeSpan.Zero)
                {
                    _sessionStartTime = DateTime.Now;
                }
                _timerEngine.Start();
            }

            UpdateTimerDisplay();
            NotifyStateChanged();
        }

        private bool CanExecuteCompleteSession()
        {
            return TotalElapsed > TimeSpan.Zero || _timerEngine.IsRunning || _completedSegments.Count > 0;
        }

        private void ExecuteCompleteSession()
        {
            // Completed / End Button (middle button)
            _currentSessionElapsed = _timerEngine.Elapsed;
            RecordCurrentSessionIfAny(true);

            _timerEngine.Reset();
            _currentSessionElapsed = TimeSpan.Zero;
            _targetNotificationFired = false;
            _blockCompletedElapsed = TimeSpan.Zero;
            _completedSegments.Clear();

            UpdateTimerDisplay();
            NotifyStateChanged();
        }

        private void ExecuteNextSession()
        {
            _currentSessionElapsed = _timerEngine.Elapsed;

            // 1. Record completed session
            RecordCurrentSessionIfAny(false);

            // 2. Add slice of completed session to duration ring
            double targetSec = TargetDuration.TotalSeconds;
            if (targetSec > 0)
            {
                double startRatio = _blockCompletedElapsed.TotalSeconds / targetSec;
                double endRatio = (_blockCompletedElapsed + _currentSessionElapsed).TotalSeconds / targetSec;

                double startAngle = Math.Min(1.0, startRatio) * 360.0;
                double endAngle = Math.Min(1.0, endRatio) * 360.0;

                if (endAngle > startAngle)
                {
                    _completedSegments.Add(new RingSessionSegment(
                        CurrentSessionNumber,
                        startAngle,
                        endAngle,
                        CurrentSessionColorHex
                    ));
                }
            }

            _blockCompletedElapsed += _currentSessionElapsed;

            // 3. Reset current session timer
            _timerEngine.Reset();
            _currentSessionElapsed = TimeSpan.Zero;
            _sessionStartTime = DateTime.Now;

            // 4. Advance session index (01 -> 12, then loops to 01)
            _currentSessionIndex = (_currentSessionIndex + 1) % 12;

            // 5. Update session label consistently
            AdvanceSessionLabelText();

            // 6. Pressing next also AUTOMATICALLY CONTINUES the stopwatch
            _timerEngine.Start();

            OnPropertyChanged("CurrentSession");
            OnPropertyChanged("CurrentSessionNumber");
            OnPropertyChanged("CurrentSessionPillText");
            OnPropertyChanged("CurrentSessionColorHex");
            OnPropertyChanged("TotalElapsed");
            OnPropertyChanged("CompletedSegments");

            SaveSettings();
            UpdateTimerDisplay();
            NotifyStateChanged();
        }

        private void AdvanceSessionLabelText()
        {
            if (string.IsNullOrEmpty(_sessionLabelText))
            {
                SessionLabelText = string.Format("Session {0:D2}", CurrentSessionNumber);
                return;
            }

            // Check if text ends with digits
            var match = Regex.Match(_sessionLabelText, @"^(.*?)(\d+)$");
            if (match.Success)
            {
                string prefix = match.Groups[1].Value;
                int digitsCount = match.Groups[2].Value.Length;
                string newNumberStr = CurrentSessionNumber.ToString("D" + digitsCount);
                SessionLabelText = prefix + newNumberStr;
            }
            // If user typed a custom label without numbers, preserve it as is!
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
                        RecordCurrentSessionIfAny(false);
                        _timerEngine.Reset();
                        _currentSessionIndex = sessionNum - 1;
                        _sessionStartTime = DateTime.Now;

                        AdvanceSessionLabelText();

                        OnPropertyChanged("CurrentSession");
                        OnPropertyChanged("CurrentSessionNumber");
                        OnPropertyChanged("CurrentSessionPillText");
                        OnPropertyChanged("CurrentSessionColorHex");

                        SaveSettings();
                        UpdateTimerDisplay();
                        NotifyStateChanged();
                    }
                }
            }
        }

        private void RecordCurrentSessionIfAny(bool isCompleted)
        {
            TimeSpan currentElapsed = _timerEngine.Elapsed;
            if (currentElapsed > TimeSpan.Zero)
            {
                var record = new SessionRecord();
                record.SessionNumber = CurrentSessionNumber;
                record.SessionLabel = SessionLabelText;
                record.StartTime = _sessionStartTime;
                record.EndTime = DateTime.Now;
                record.TargetDurationSeconds = TargetDuration.TotalSeconds;
                record.ElapsedSeconds = currentElapsed.TotalSeconds;
                record.IsCompleted = isCompleted || (TotalElapsed >= TargetDuration);
                record.ThemeColorHex = CurrentSession.ThemeColorHex;

                _storageService.SaveRecord(record);
                RefreshTodayRecordedDuration();
                OnPropertyChanged("TodayTotalDurationText");
                StatisticsVM.Refresh();
            }
        }

        private void RefreshTodayRecordedDuration()
        {
            _todayDurationDate = DateTime.Today;
            _todayRecordedDuration = TimeSpan.Zero;
            var records = _storageService.LoadRecords();
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].StartTime.Date == _todayDurationDate)
                {
                    _todayRecordedDuration += records[i].ElapsedDuration;
                }
            }
        }

        private void ExecuteToggleMinimalMode()
        {
            if (IsSuperMinimalMode)
            {
                IsSuperMinimalMode = false;
                IsMinimalMode = false;
            }
            else
            {
                IsMinimalMode = !IsMinimalMode;
            }
        }

        private void ExecuteToggleSuperMinimalMode()
        {
            IsSuperMinimalMode = !IsSuperMinimalMode;
        }

        private void ExecuteToggleAutoFreeArea()
        {
            IsAutoFreeAreaEnabled = !IsAutoFreeAreaEnabled;
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
            RecordCurrentSessionIfAny(false);
            SaveSettings();
            if (RequestClose != null)
            {
                RequestClose(this, EventArgs.Empty);
            }
        }

        private void ExecuteDismissGettingStarted()
        {
            ShowGettingStarted = false;
            var settings = _storageService.LoadSettings();
            settings.HasDismissedGettingStarted = true;
            _storageService.SaveSettings(settings);
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
            settings.IsSuperMinimalMode = IsSuperMinimalMode;
            settings.IsAutoFreeAreaEnabled = IsAutoFreeAreaEnabled;
            settings.StartInSuperMinimalMode = StartInSuperMinimalMode;
            settings.IsGuideCompanionEnabled = IsGuideCompanionEnabled;
            settings.ThemeName = ThemeName;
            settings.HasDismissedGettingStarted = !ShowGettingStarted;
            settings.GlobalTargetDurationSeconds = TargetDuration.TotalSeconds;
            settings.CustomSessionLabelTemplate = _sessionLabelText;

            _storageService.SaveSettings(settings);
        }
    }
}
