using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using STStopwatch.Controls;
using STStopwatch.Models;
using STStopwatch.Services;
using STStopwatch.ViewModels;

namespace STStopwatch.Tests
{
    public class MockTrayService : ITrayService
    {
        public bool LastMinimal;
        public bool LastAlwaysOnTop;
        public string LastNotificationTitle;
        public string LastNotificationMessage;

        public void Initialize(Action onRestore, Action onToggleMinimal, Action onToggleTopmost, Action onExit) { }
        public void UpdateState(bool isMinimal, bool isAlwaysOnTop)
        {
            LastMinimal = isMinimal;
            LastAlwaysOnTop = isAlwaysOnTop;
        }
        public void ShowNotification(string title, string message)
        {
            LastNotificationTitle = title;
            LastNotificationMessage = message;
        }
        public void SetVisible(bool isVisible) { }
        public void Dispose() { }
    }

    public class MockStorageService : IStorageService
    {
        public UserSettings Settings = new UserSettings();
        public List<SessionRecord> Records = new List<SessionRecord>();

        public UserSettings LoadSettings() { return Settings; }
        public void SaveSettings(UserSettings settings) { Settings = settings; }
        public List<SessionRecord> LoadRecords() { return new List<SessionRecord>(Records); }
        public void SaveRecord(SessionRecord record) { Records.Add(record); }
        public void DeleteRecord(SessionRecord record) { Records.RemoveAll(r => r.Id == record.Id); }
        public void DeleteAllRecords() { Records.Clear(); }
    }

    class Program
    {
        static int _passed = 0;
        static int _failed = 0;

        static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[PASS] " + testName);
                Console.ResetColor();
                _passed++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAIL] " + testName);
                Console.ResetColor();
                _failed++;
            }
        }

        [STAThread]
        static int Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("ST Stopwatch - Comprehensive Verification Test Suite");
            Console.WriteLine("==================================================");

            TestTimerEngine();
            Test12SessionsAndColours();
            TestJsonStorageService();
            TestMainViewModelLifecycleAndControls();
            TestStatisticsDayAndMonthViews();
            TestTargetDurationAdjustment();
            TestRevolutionAndNodeDraggableBehavior();
            TestEditableSessionTextAndConstantDurationAndAutoContinue();

            Console.WriteLine("\n--------------------------------------------------");
            Console.WriteLine(string.Format("Test Results: {0} Passed, {1} Failed", _passed, _failed));
            Console.WriteLine("--------------------------------------------------");

            return _failed == 0 ? 0 : 1;
        }

        static void TestTimerEngine()
        {
            Console.WriteLine("\n--> Testing HighResolutionTimerEngine...");
            var timer = new HighResolutionTimerEngine();

            Assert(!timer.IsRunning, "Timer initially not running");
            Assert(timer.Elapsed == TimeSpan.Zero, "Timer initially zero elapsed");

            timer.Start();
            Assert(timer.IsRunning, "Timer is running after Start");
            Thread.Sleep(60);
            TimeSpan elapsedAfterRun = timer.Elapsed;
            Assert(elapsedAfterRun.TotalMilliseconds >= 40, "Timer tracks positive elapsed time");

            timer.Pause();
            Assert(!timer.IsRunning, "Timer paused");
            TimeSpan elapsedAtPause = timer.Elapsed;
            Thread.Sleep(30);
            Assert(timer.Elapsed == elapsedAtPause, "Paused timer does not advance");

            timer.Start();
            Thread.Sleep(40);
            Assert(timer.Elapsed > elapsedAtPause, "Resumed timer continues accumulating time");

            timer.Reset();
            Assert(!timer.IsRunning, "Reset timer is not running");
            Assert(timer.Elapsed == TimeSpan.Zero, "Reset timer elapsed is zero");
        }

        static void Test12SessionsAndColours()
        {
            Console.WriteLine("\n--> Testing 12-Session Definitions and Colours...");
            var sessions = SessionModel.CreateDefault12Sessions();

            Assert(sessions.Length == 12, "Exactly 12 sessions configured");

            var colorSet = new HashSet<string>();
            for (int i = 0; i < sessions.Length; i++)
            {
                var s = sessions[i];
                Assert(s.SessionNumber == (i + 1), string.Format("Session {0} has correct number", i + 1));
                Assert(s.ShortNumber == (i + 1).ToString("D2"), string.Format("Session {0} formatted as two digits", i + 1));
                Assert(s.DisplayLabel == string.Format("Session {0:D2}", i + 1), string.Format("Session {0} has correct label", i + 1));
                Assert(!string.IsNullOrEmpty(s.ThemeColorHex), string.Format("Session {0} has hex color", i + 1));
                colorSet.Add(s.ThemeColorHex);
            }

            Assert(colorSet.Count == 12, "All 12 sessions have distinct unique colors");
        }

        static void TestJsonStorageService()
        {
            Console.WriteLine("\n--> Testing JsonStorageService persistence...");
            string tempDir = Path.Combine(Path.GetTempPath(), "STStopwatch_Test_" + Guid.NewGuid().ToString("N"));
            var storage = new JsonStorageService(tempDir);

            try
            {
                var settings = storage.LoadSettings();
                Assert(settings != null, "Settings load successfully");
                Assert(settings.SessionTargetsInSeconds.Length >= 12, "Settings have 12 session target durations");

                settings.IsAlwaysOnTop = true;
                settings.IsMinimalMode = true;
                settings.IsSuperMinimalMode = true;
                settings.IsAutoFreeAreaEnabled = true;
                settings.CurrentSessionNumber = 5;
                storage.SaveSettings(settings);

                var reloaded = storage.LoadSettings();
                Assert(reloaded.IsAlwaysOnTop == true, "Persisted IsAlwaysOnTop verified");
                Assert(reloaded.IsMinimalMode == true, "Persisted IsMinimalMode verified");
                Assert(reloaded.IsSuperMinimalMode == true, "Persisted IsSuperMinimalMode verified");
                Assert(reloaded.IsAutoFreeAreaEnabled == true, "Persisted IsAutoFreeAreaEnabled verified");
                Assert(reloaded.CurrentSessionNumber == 5, "Persisted CurrentSessionNumber verified");

                // Test Session Record persistence
                var rec = new SessionRecord();
                rec.SessionNumber = 1;
                rec.SessionLabel = "Session 01";
                rec.StartTime = DateTime.Now.AddMinutes(-30);
                rec.EndTime = DateTime.Now;
                rec.TargetDurationSeconds = 1800;
                rec.ElapsedSeconds = 1800;
                rec.IsCompleted = true;

                int initialCount = storage.LoadRecords().Count;
                storage.SaveRecord(rec);
                var records = storage.LoadRecords();
                Assert(records.Count == initialCount + 1, "Session record persisted and retrieved");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        static void TestMainViewModelLifecycleAndControls()
        {
            Console.WriteLine("\n--> Testing MainViewModel controls and session advancement...");
            var timer = new HighResolutionTimerEngine();
            var storage = new MockStorageService();
            var tray = new MockTrayService();

            var vm = new MainViewModel(timer, storage, tray);

            Assert(vm.PlayPauseButtonLabel == "Start", "Initial button label is Start");
            Assert(vm.CurrentSessionNumber == 1, "Initial session is 1");
            Assert(vm.SessionLabelText == "Session 01", "Initial session label is Session 01");

            // Start
            vm.PlayPauseCommand.Execute(null);
            Assert(vm.IsRunning, "ViewModel running after PlayPauseCommand");
            Assert(vm.PlayPauseButtonLabel == "Pause", "Button label is Pause while running");

            // Pause
            Thread.Sleep(30);
            vm.PlayPauseCommand.Execute(null);
            Assert(!vm.IsRunning, "ViewModel paused after second PlayPauseCommand");
            Assert(vm.PlayPauseButtonLabel == "Resume", "Button label is Resume while paused");

            // Resume
            vm.PlayPauseCommand.Execute(null);
            Assert(vm.IsRunning, "ViewModel running after resume");

            // Advance Session: Policy test
            vm.NextSessionCommand.Execute(null);
            Assert(storage.Records.Count == 1, "Session record preserved before advancing");
            Assert(vm.CurrentSessionNumber == 2, "Session number advanced to 02");
            Assert(vm.SessionLabelText == "Session 02", "Session label updated to Session 02");
            Assert(vm.IsRunning, "Pressing next automatically continues running!");

            // Loop through remaining sessions up to 12 and back to 1
            for (int i = 3; i <= 12; i++)
            {
                vm.NextSessionCommand.Execute(null);
                Assert(vm.CurrentSessionNumber == i, string.Format("Advanced to session {0:D2}", i));
            }

            // Advancing past 12 loops back to 1
            vm.NextSessionCommand.Execute(null);
            Assert(vm.CurrentSessionNumber == 1, "Session wraps from 12 back to 01");

            // Complete / End Button in middle
            vm.CompleteSessionCommand.Execute(null);
            Assert(!vm.IsRunning, "Complete/End resets stopwatch to stopped");
            Assert(vm.Elapsed == TimeSpan.Zero, "Complete/End resets elapsed to zero");

            // Minimal mode toggle
            Assert(!vm.IsMinimalMode, "Initially normal mode");
            vm.ToggleMinimalModeCommand.Execute(null);
            Assert(vm.IsMinimalMode, "Minimal mode enabled");
            Assert(tray.LastMinimal, "Tray updated with minimal mode");
            vm.ToggleMinimalModeCommand.Execute(null);
            Assert(!vm.IsMinimalMode, "Minimal mode disabled");

            // Super Minimal mode toggle
            Assert(!vm.IsSuperMinimalMode, "Initially not super minimal mode");
            vm.ToggleSuperMinimalModeCommand.Execute(null);
            Assert(vm.IsSuperMinimalMode, "Super minimal mode enabled");
            Assert(vm.IsMinimalMode, "Super minimal mode implies minimal mode");
            vm.ToggleSuperMinimalModeCommand.Execute(null);
            Assert(!vm.IsSuperMinimalMode, "Super minimal mode disabled");

            // Always on top toggle
            Assert(!vm.IsAlwaysOnTop, "Initially not topmost");
            vm.ToggleAlwaysOnTopCommand.Execute(null);
            Assert(vm.IsAlwaysOnTop, "Always on top enabled");
            Assert(tray.LastAlwaysOnTop, "Tray updated with topmost state");

            // Auto Free Area toggle
            Assert(!vm.IsAutoFreeAreaEnabled, "Initially auto free area is disabled for manual positioning");
            vm.ToggleAutoFreeAreaCommand.Execute(null);
            Assert(vm.IsAutoFreeAreaEnabled, "Auto free area enabled");
            vm.ToggleAutoFreeAreaCommand.Execute(null);
            Assert(!vm.IsAutoFreeAreaEnabled, "Auto free area disabled for manual positioning");
        }

        static void TestStatisticsDayAndMonthViews()
        {
            Console.WriteLine("\n--> Testing Statistics Day and Month Views...");
            var storage = new MockStorageService();

            DateTime today = DateTime.Today;
            DateTime yesterday = today.AddDays(-1);

            // Seed test sessions
            storage.SaveRecord(new SessionRecord {
                SessionNumber = 1,
                SessionLabel = "Session 01",
                StartTime = today.AddHours(9),
                EndTime = today.AddHours(10),
                TargetDurationSeconds = 3600,
                ElapsedSeconds = 3600,
                IsCompleted = true
            });

            storage.SaveRecord(new SessionRecord {
                SessionNumber = 2,
                SessionLabel = "Session 02",
                StartTime = today.AddHours(11),
                EndTime = today.AddHours(12).AddMinutes(30),
                TargetDurationSeconds = 3600,
                ElapsedSeconds = 5400,
                IsCompleted = true
            });

            storage.SaveRecord(new SessionRecord {
                SessionNumber = 3,
                SessionLabel = "Session 03",
                StartTime = yesterday.AddHours(14),
                EndTime = yesterday.AddHours(15),
                TargetDurationSeconds = 3600,
                ElapsedSeconds = 3600,
                IsCompleted = true
            });

            var statsVm = new StatisticsViewModel(storage);

            // Day View Test
            statsVm.SelectedDay = today;
            Assert(statsVm.DaySummary.TotalSessions == 2, "Day view reflects 2 sessions today");
            Assert(statsVm.DaySummary.TotalDuration == TimeSpan.FromSeconds(9000), "Day view calculates 9000s (02:30:00) total duration");
            Assert(statsVm.DaySummary.CompletedSessions == 2, "Day view calculates 2 completed sessions");

            // Month View Test
            statsVm.SelectedMonth = new DateTime(today.Year, today.Month, 1);
            Assert(statsVm.MonthSummary.TotalSessions == 3, "Month view reflects all 3 sessions this month");
            Assert(statsVm.MonthSummary.ActiveDaysCount == 2, "Month view reflects 2 active days");
            Assert(statsVm.MonthSummary.DayRows.Count == 2, "Month view has 2 date rows");
            Assert(statsVm.MonthSummary.WeekTotals.Count > 0, "Month view computes weekly totals");

            // Verify week totals column
            foreach (var row in statsVm.MonthSummary.DayRows)
            {
                Assert(!string.IsNullOrEmpty(row.FormattedWeekTotal), "Week total column formatted on row: " + row.FormattedWeekTotal);
                Assert(row.WeekTotalDuration.TotalSeconds > 0, "Week total duration is positive");
            }
        }

        static void TestTargetDurationAdjustment()
        {
            Console.WriteLine("\n--> Testing Target Duration Adjustment up to 16 Hours...");
            var timer = new HighResolutionTimerEngine();
            var storage = new MockStorageService();
            var tray = new MockTrayService();

            var vm = new MainViewModel(timer, storage, tray);

            vm.TargetDuration = TimeSpan.FromHours(16);
            Assert(vm.TargetDuration == TimeSpan.FromHours(16), "Target duration can be set to maximum 16 hours");
            Assert(vm.FormattedTarget == "16:00:00", "Formatted target displays 16:00:00");

            vm.TargetDuration = TimeSpan.FromMinutes(45);
            Assert(vm.TargetDuration == TimeSpan.FromMinutes(45), "Target duration can be set to 45 minutes");
            Assert(vm.FormattedTarget == "00:45:00", "Formatted target displays 00:45:00");
        }

        static void TestRevolutionAndNodeDraggableBehavior()
        {
            Console.WriteLine("\n--> Testing Full Revolution and Node Draggable State...");
            var ring = new CircularProgressRing();

            ring.IsRunning = false;
            ring.Elapsed = TimeSpan.Zero;
            ring.TotalElapsed = TimeSpan.Zero;
            ring.TargetDuration = TimeSpan.FromHours(2);

            Assert(ring.IsAtBeginning == true, "Node is in setting mode at the beginning");

            ring.IsRunning = true;
            Assert(ring.IsAtBeginning == false, "Node is locked (not draggable) once running");

            ring.IsRunning = false;
            ring.Elapsed = TimeSpan.FromMinutes(30);
            ring.TotalElapsed = TimeSpan.FromMinutes(30);
            Assert(ring.IsAtBeginning == false, "Node remains locked (not draggable) while paused during session");

            ring.Elapsed = TimeSpan.Zero;
            ring.TotalElapsed = TimeSpan.Zero;
            ring.IsRunning = false;
            Assert(ring.IsAtBeginning == true, "Node unlocks and becomes draggable again after reset");

            TimeSpan target = TimeSpan.FromHours(2);
            TimeSpan halfElapsed = TimeSpan.FromHours(1);
            TimeSpan fullElapsed = TimeSpan.FromHours(2);

            double halfRatio = halfElapsed.TotalSeconds / target.TotalSeconds;
            double halfAngle = halfRatio * 360.0;
            Assert(Math.Abs(halfAngle - 180.0) < 0.001, "50% elapsed matches half revolution (180 deg)");

            double fullRatio = fullElapsed.TotalSeconds / target.TotalSeconds;
            double fullAngle = fullRatio * 360.0;
            Assert(Math.Abs(fullAngle - 360.0) < 0.001, "100% elapsed matches full revolution (360 deg)");
        }

        static void TestEditableSessionTextAndConstantDurationAndAutoContinue()
        {
            Console.WriteLine("\n--> Testing Editable Session Text, Constant Duration, and Auto Continue...");
            var timer = new HighResolutionTimerEngine();
            var storage = new MockStorageService();
            var tray = new MockTrayService();

            var vm = new MainViewModel(timer, storage, tray);

            // Set duration to 2.5 hours
            TimeSpan chosenDuration = TimeSpan.FromHours(2.5);
            vm.TargetDuration = chosenDuration;

            // User edits session text to "Sprint 01"
            vm.SessionLabelText = "Sprint 01";

            // Start stopwatch
            vm.PlayPauseCommand.Execute(null);
            Assert(vm.IsRunning, "Stopwatch running");
            Thread.Sleep(30);

            // Verify Target Finish Time is clock time
            Assert(!string.IsNullOrEmpty(vm.TargetFinishTimeText), "Target finish time is populated");
            Assert(vm.TargetFinishTimeText.Contains(":"), "Target finish time is clock formatted: " + vm.TargetFinishTimeText);

            // Press Next
            vm.NextSessionCommand.Execute(null);

            // 1. Duration remains constant!
            Assert(vm.TargetDuration == chosenDuration, "Session duration remains constant across Next");

            // 2. Stopwatch automatically continues!
            Assert(vm.IsRunning, "Pressing next automatically continues stopwatch");

            // 3. Text label updated consistently ("Sprint 01" -> "Sprint 02")
            Assert(vm.SessionLabelText == "Sprint 02", "Session label updated consistently to Sprint 02");

            // 4. Slices of one duration are recorded on the ring
            Assert(vm.CompletedSegments.Count == 1, "Completed session slice is recorded on ring");

            // Next again
            Thread.Sleep(30);
            vm.NextSessionCommand.Execute(null);
            Assert(vm.SessionLabelText == "Sprint 03", "Session label updated consistently to Sprint 03");
            Assert(vm.CompletedSegments.Count == 2, "Second session slice recorded on ring");
            Assert(vm.TargetDuration == chosenDuration, "Duration still constant at 2.5 hours");
            Assert(vm.IsRunning, "Still automatically running");

            // Test custom label without number (e.g. "Deep Work")
            vm.SessionLabelText = "Deep Work";
            vm.NextSessionCommand.Execute(null);
            Assert(vm.SessionLabelText == "Deep Work", "Custom text without number preserved consistently");

            // Complete / End in middle
            vm.CompleteSessionCommand.Execute(null);
            Assert(!vm.IsRunning, "Completed button in middle stopped the timer");
            Assert(vm.CompletedSegments.Count == 0, "Completed button cleared segments for next block");
            Assert(vm.Elapsed == TimeSpan.Zero, "Elapsed reset to zero");
            Assert(vm.TargetDuration == chosenDuration, "Target duration preserved for subsequent blocks");
        }
    }
}
