using System;
using System.Windows.Media;
using STStopwatch.Common;

namespace STStopwatch.Models
{
    public class SessionModel : ObservableObject
    {
        private int _sessionNumber;
        private string _themeColorHex;
        private TimeSpan _targetDuration;
        private SolidColorBrush _themeBrush;

        public SessionModel(int sessionNumber, string themeColorHex, TimeSpan defaultTarget)
        {
            _sessionNumber = sessionNumber;
            _themeColorHex = themeColorHex;
            _targetDuration = defaultTarget;
            UpdateBrush();
        }

        public int SessionNumber
        {
            get { return _sessionNumber; }
            set
            {
                if (SetProperty(ref _sessionNumber, value, "SessionNumber"))
                {
                    OnPropertyChanged("ShortNumber");
                    OnPropertyChanged("DisplayLabel");
                }
            }
        }

        public string ShortNumber
        {
            get { return _sessionNumber.ToString("D2"); }
        }

        public string DisplayLabel
        {
            get { return string.Format("Session {0:D2}", _sessionNumber); }
        }

        public string ThemeColorHex
        {
            get { return _themeColorHex; }
            set
            {
                if (SetProperty(ref _themeColorHex, value, "ThemeColorHex"))
                {
                    UpdateBrush();
                }
            }
        }

        public SolidColorBrush ThemeBrush
        {
            get { return _themeBrush; }
            private set { SetProperty(ref _themeBrush, value, "ThemeBrush"); }
        }

        public TimeSpan TargetDuration
        {
            get { return _targetDuration; }
            set { SetProperty(ref _targetDuration, value, "TargetDuration"); }
        }

        private void UpdateBrush()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(_themeColorHex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                ThemeBrush = brush;
            }
            catch
            {
                var brush = new SolidColorBrush(Color.FromRgb(0, 82, 255));
                brush.Freeze();
                ThemeBrush = brush;
            }
        }

        public static SessionModel[] CreateDefault12Sessions()
        {
            // 12 Distinctive StartToday aesthetic colours
            string[] colors = new string[]
            {
                "#0052FF", // 01: StartToday Cobalt Blue (Vivid accent)
                "#00A389", // 02: Persian Teal
                "#6366F1", // 03: Radiant Indigo
                "#FF5C35", // 04: Coral Tangerine
                "#059669", // 05: Emerald Forest
                "#7C3AED", // 06: Royal Violet
                "#D97706", // 07: Solar Amber
                "#0284C7", // 08: Electric Azure
                "#E11D48", // 09: Crimson Rose
                "#475569", // 10: Slate Titanium
                "#16A34A", // 11: Vibrant Spring
                "#C026D3"  // 12: Vivid Fuchsia
            };

            var sessions = new SessionModel[12];
            for (int i = 0; i < 12; i++)
            {
                sessions[i] = new SessionModel(i + 1, colors[i], TimeSpan.FromHours(1));
            }
            return sessions;
        }
    }
}
