using System;
using System.Collections.Generic;

namespace STStopwatch.Models
{
    public class DaySummaryModel
    {
        public DateTime Date { get; set; }
        public string FormattedDate { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public string FormattedTotalDuration { get; set; }
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public string FormattedAverageDuration { get; set; }
        public List<SessionRecord> Records { get; set; }

        public DaySummaryModel()
        {
            Records = new List<SessionRecord>();
            FormattedTotalDuration = "00:00:00";
            FormattedAverageDuration = "00:00:00";
        }
    }

    public class MonthDayRowModel
    {
        public DateTime Date { get; set; }
        public string DayString { get; set; }
        public int SessionCount { get; set; }
        public TimeSpan DayTotalDuration { get; set; }
        public string FormattedDayTotal { get; set; }
        public int WeekNumber { get; set; }
        public string WeekLabel { get; set; }
        public TimeSpan WeekTotalDuration { get; set; }
        public string FormattedWeekTotal { get; set; }
        public List<SessionRecord> Records { get; set; }

        public MonthDayRowModel()
        {
            Records = new List<SessionRecord>();
            FormattedDayTotal = "00:00:00";
            FormattedWeekTotal = "00:00:00";
        }
    }

    public class WeekTotalItem
    {
        public int WeekNumber { get; set; }
        public string WeekLabel { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public string FormattedTotalDuration { get; set; }
        public int SessionCount { get; set; }
    }

    public class MonthSummaryModel
    {
        public DateTime SelectedMonth { get; set; }
        public string MonthName { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public string FormattedTotalDuration { get; set; }
        public int TotalSessions { get; set; }
        public int ActiveDaysCount { get; set; }
        public TimeSpan DailyAverage { get; set; }
        public string FormattedDailyAverage { get; set; }
        public List<MonthDayRowModel> DayRows { get; set; }
        public List<WeekTotalItem> WeekTotals { get; set; }

        public MonthSummaryModel()
        {
            DayRows = new List<MonthDayRowModel>();
            WeekTotals = new List<WeekTotalItem>();
            FormattedTotalDuration = "00:00:00";
            FormattedDailyAverage = "00:00:00";
        }
    }
}
