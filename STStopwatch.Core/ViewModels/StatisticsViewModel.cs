using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using STStopwatch.Core.Common;
using STStopwatch.Core.Models;
using STStopwatch.Core.Services;

namespace STStopwatch.Core.ViewModels
{
    public class StatisticsViewModel : ObservableObject
    {
        private readonly IStorageService _storageService;
        private DateTime _selectedDay;
        private DateTime _selectedMonth;
        private int _selectedViewIndex; // 0 = Day View, 1 = Month View

        private DaySummaryModel _daySummary;
        private MonthSummaryModel _monthSummary;

        public StatisticsViewModel(IStorageService storageService)
        {
            _storageService = storageService;
            _selectedDay = DateTime.Today;
            _selectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _selectedViewIndex = 0;

            PreviousDayCommand = new RelayCommand(ExecutePreviousDay);
            NextDayCommand = new RelayCommand(ExecuteNextDay);
            TodayCommand = new RelayCommand(ExecuteToday);

            PreviousMonthCommand = new RelayCommand(ExecutePreviousMonth);
            NextMonthCommand = new RelayCommand(ExecuteNextMonth);
            CurrentMonthCommand = new RelayCommand(ExecuteCurrentMonth);
            DeleteSessionCommand = new RelayCommand(ExecuteDeleteSession, CanDeleteSession);

            Refresh();
        }

        public int SelectedViewIndex
        {
            get { return _selectedViewIndex; }
            set
            {
                if (SetProperty(ref _selectedViewIndex, value, "SelectedViewIndex"))
                {
                    OnPropertyChanged("IsDayView");
                    OnPropertyChanged("IsMonthView");
                }
            }
        }

        public bool IsDayView
        {
            get { return _selectedViewIndex == 0; }
        }

        public bool IsMonthView
        {
            get { return _selectedViewIndex == 1; }
        }

        public DateTime SelectedDay
        {
            get { return _selectedDay; }
            set
            {
                if (SetProperty(ref _selectedDay, value, "SelectedDay"))
                {
                    UpdateDaySummary();
                }
            }
        }

        public DateTime SelectedMonth
        {
            get { return _selectedMonth; }
            set
            {
                if (SetProperty(ref _selectedMonth, value, "SelectedMonth"))
                {
                    UpdateMonthSummary();
                }
            }
        }

        public DaySummaryModel DaySummary
        {
            get { return _daySummary; }
            private set { SetProperty(ref _daySummary, value, "DaySummary"); }
        }

        public MonthSummaryModel MonthSummary
        {
            get { return _monthSummary; }
            private set { SetProperty(ref _monthSummary, value, "MonthSummary"); }
        }

        public ICommand PreviousDayCommand { get; private set; }
        public ICommand NextDayCommand { get; private set; }
        public ICommand TodayCommand { get; private set; }

        public ICommand PreviousMonthCommand { get; private set; }
        public ICommand NextMonthCommand { get; private set; }
        public ICommand CurrentMonthCommand { get; private set; }
        public ICommand DeleteSessionCommand { get; private set; }

        private bool CanDeleteSession(object parameter)
        {
            return parameter is SessionRecord;
        }

        private void ExecuteDeleteSession(object parameter)
        {
            var record = parameter as SessionRecord;
            if (record == null) return;

            _storageService.DeleteRecord(record);
            Refresh();
        }

        private void ExecutePreviousDay()
        {
            SelectedDay = SelectedDay.AddDays(-1);
        }

        private void ExecuteNextDay()
        {
            SelectedDay = SelectedDay.AddDays(1);
        }

        private void ExecuteToday()
        {
            SelectedDay = DateTime.Today;
        }

        private void ExecutePreviousMonth()
        {
            SelectedMonth = SelectedMonth.AddMonths(-1);
        }

        private void ExecuteNextMonth()
        {
            SelectedMonth = SelectedMonth.AddMonths(1);
        }

        private void ExecuteCurrentMonth()
        {
            SelectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        public void Refresh()
        {
            UpdateDaySummary();
            UpdateMonthSummary();
        }

        private void UpdateDaySummary()
        {
            var records = _storageService.LoadRecords();
            var dayRecords = new List<SessionRecord>();
            TimeSpan total = TimeSpan.Zero;
            int completedCount = 0;

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                if (r.StartTime.Date == _selectedDay.Date)
                {
                    dayRecords.Add(r);
                    total += r.ElapsedDuration;
                    if (r.IsCompleted || r.ElapsedSeconds >= r.TargetDurationSeconds)
                    {
                        completedCount++;
                    }
                }
            }

            TimeSpan avg = dayRecords.Count > 0
                ? TimeSpan.FromSeconds(total.TotalSeconds / dayRecords.Count)
                : TimeSpan.Zero;

            var summary = new DaySummaryModel();
            summary.Date = _selectedDay;
            summary.FormattedDate = _selectedDay.ToString("dddd, MMMM dd, yyyy");
            summary.TotalDuration = total;
            int totalHours = (int)total.TotalHours;
            summary.FormattedTotalDuration = string.Format("{0:D2}:{1:D2}:{2:D2}", totalHours, total.Minutes, total.Seconds);
            summary.TotalSessions = dayRecords.Count;
            summary.CompletedSessions = completedCount;
            summary.AverageDuration = avg;
            int avgHours = (int)avg.TotalHours;
            summary.FormattedAverageDuration = string.Format("{0:D2}:{1:D2}:{2:D2}", avgHours, avg.Minutes, avg.Seconds);
            summary.Records = dayRecords;

            DaySummary = summary;
        }

        private void UpdateMonthSummary()
        {
            var allRecords = _storageService.LoadRecords();
            var monthRecords = new List<SessionRecord>();

            DateTime monthStart = new DateTime(_selectedMonth.Year, _selectedMonth.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1);

            for (int i = 0; i < allRecords.Count; i++)
            {
                var r = allRecords[i];
                if (r.StartTime >= monthStart && r.StartTime < monthEnd)
                {
                    monthRecords.Add(r);
                }
            }

            // Group by Date
            var byDate = new Dictionary<DateTime, List<SessionRecord>>();
            var weekTotalsDict = new Dictionary<int, TimeSpan>();
            var weekSessionsDict = new Dictionary<int, int>();

            var calendar = CultureInfo.CurrentCulture.Calendar;

            TimeSpan monthTotal = TimeSpan.Zero;

            for (int i = 0; i < monthRecords.Count; i++)
            {
                var r = monthRecords[i];
                DateTime dateKey = r.StartTime.Date;
                if (!byDate.ContainsKey(dateKey))
                {
                    byDate[dateKey] = new List<SessionRecord>();
                }
                byDate[dateKey].Add(r);
                monthTotal += r.ElapsedDuration;

                int weekNumber = calendar.GetWeekOfYear(dateKey, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                if (!weekTotalsDict.ContainsKey(weekNumber))
                {
                    weekTotalsDict[weekNumber] = TimeSpan.Zero;
                    weekSessionsDict[weekNumber] = 0;
                }
                weekTotalsDict[weekNumber] += r.ElapsedDuration;
                weekSessionsDict[weekNumber] += 1;
            }

            var dayRows = new List<MonthDayRowModel>();
            var sortedDates = new List<DateTime>(byDate.Keys);
            sortedDates.Sort();

            for (int i = 0; i < sortedDates.Count; i++)
            {
                DateTime dt = sortedDates[i];
                var list = byDate[dt];
                TimeSpan dayTotal = TimeSpan.Zero;
                for (int j = 0; j < list.Count; j++)
                {
                    dayTotal += list[j].ElapsedDuration;
                }

                int weekNumber = calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                TimeSpan weekTotal = weekTotalsDict[weekNumber];

                var row = new MonthDayRowModel();
                row.Date = dt;
                row.DayString = dt.ToString("yyyy-MM-dd (ddd)");
                row.SessionCount = list.Count;
                row.DayTotalDuration = dayTotal;
                int dayHours = (int)dayTotal.TotalHours;
                row.FormattedDayTotal = string.Format("{0:D2}:{1:D2}:{2:D2}", dayHours, dayTotal.Minutes, dayTotal.Seconds);
                row.WeekNumber = weekNumber;
                row.WeekLabel = string.Format("W{0}", weekNumber);
                row.WeekTotalDuration = weekTotal;
                int weekHours = (int)weekTotal.TotalHours;
                row.FormattedWeekTotal = string.Format("{0:D2}:{1:D2}:{2:D2}", weekHours, weekTotal.Minutes, weekTotal.Seconds);
                row.Records = list;

                dayRows.Add(row);
            }

            // Summary of weeks
            var weekSummaries = new List<WeekTotalItem>();
            var sortedWeeks = new List<int>(weekTotalsDict.Keys);
            sortedWeeks.Sort();

            for (int i = 0; i < sortedWeeks.Count; i++)
            {
                int w = sortedWeeks[i];
                TimeSpan wt = weekTotalsDict[w];
                int wtHours = (int)wt.TotalHours;

                var wi = new WeekTotalItem();
                wi.WeekNumber = w;
                wi.WeekLabel = string.Format("Week {0}", w);
                wi.TotalDuration = wt;
                wi.FormattedTotalDuration = string.Format("{0:D2}:{1:D2}:{2:D2}", wtHours, wt.Minutes, wt.Seconds);
                wi.SessionCount = weekSessionsDict[w];
                weekSummaries.Add(wi);
            }

            TimeSpan dailyAvg = dayRows.Count > 0
                ? TimeSpan.FromSeconds(monthTotal.TotalSeconds / dayRows.Count)
                : TimeSpan.Zero;

            var monthSummary = new MonthSummaryModel();
            monthSummary.SelectedMonth = _selectedMonth;
            monthSummary.MonthName = _selectedMonth.ToString("MMMM yyyy");
            monthSummary.TotalDuration = monthTotal;
            int totalHours = (int)monthTotal.TotalHours;
            monthSummary.FormattedTotalDuration = string.Format("{0:D2}:{1:D2}:{2:D2}", totalHours, monthTotal.Minutes, monthTotal.Seconds);
            monthSummary.TotalSessions = monthRecords.Count;
            monthSummary.ActiveDaysCount = dayRows.Count;
            monthSummary.DailyAverage = dailyAvg;
            int avgHours = (int)dailyAvg.TotalHours;
            monthSummary.FormattedDailyAverage = string.Format("{0:D2}:{1:D2}:{2:D2}", avgHours, dailyAvg.Minutes, dailyAvg.Seconds);
            monthSummary.DayRows = dayRows;
            monthSummary.WeekTotals = weekSummaries;

            MonthSummary = monthSummary;
        }
    }
}
