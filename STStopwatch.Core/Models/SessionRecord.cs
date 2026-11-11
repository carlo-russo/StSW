using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace STStopwatch.Core.Models
{
    [DataContract]
    public class SessionRecord : INotifyPropertyChanged
    {
        private bool _isDayViewSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsDayViewSelected
        {
            get { return _isDayViewSelected; }
            set
            {
                if (_isDayViewSelected == value) return;
                _isDayViewSelected = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDayViewSelected"));
                }
            }
        }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public int SessionNumber { get; set; }

        [DataMember]
        public string SessionLabel { get; set; }

        [DataMember]
        public DateTime StartTime { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }

        [DataMember]
        public double TargetDurationSeconds { get; set; }

        [DataMember]
        public double ElapsedSeconds { get; set; }

        [DataMember]
        public bool IsCompleted { get; set; }

        [DataMember]
        public string ThemeColorHex { get; set; }

        public SessionRecord()
        {
            Id = Guid.NewGuid().ToString();
            StartTime = DateTime.Now;
            EndTime = DateTime.Now;
            ThemeColorHex = "#0052FF";
        }

        public TimeSpan TargetDuration
        {
            get { return TimeSpan.FromSeconds(TargetDurationSeconds); }
        }

        public TimeSpan ElapsedDuration
        {
            get { return TimeSpan.FromSeconds(ElapsedSeconds); }
        }

        public string FormattedDate
        {
            get { return StartTime.ToString("yyyy-MM-dd"); }
        }

        public string FormattedTimeRange
        {
            get
            {
                return string.Format("{0} - {1}", StartTime.ToString("HH:mm"), EndTime.ToString("HH:mm"));
            }
        }

        public string FormattedTarget
        {
            get
            {
                TimeSpan ts = TargetDuration;
                int hours = (int)ts.TotalHours;
                return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, ts.Minutes, ts.Seconds);
            }
        }

        public string FormattedElapsed
        {
            get
            {
                TimeSpan ts = ElapsedDuration;
                int hours = (int)ts.TotalHours;
                return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, ts.Minutes, ts.Seconds);
            }
        }

        public int CompletionPercentage
        {
            get
            {
                if (TargetDurationSeconds <= 0) return 100;
                double pct = (ElapsedSeconds / TargetDurationSeconds) * 100.0;
                return (int)Math.Min(999.0, Math.Max(0.0, Math.Round(pct)));
            }
        }

        public string CompletionStatusText
        {
            get
            {
                if (IsCompleted || ElapsedSeconds >= TargetDurationSeconds)
                {
                    return "Completed";
                }
                return string.Format("{0}% Done", CompletionPercentage);
            }
        }
    }
}
