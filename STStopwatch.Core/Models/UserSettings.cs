using System;
using System.Runtime.Serialization;

namespace STStopwatch.Core.Models
{
    [DataContract]
    public class UserSettings
    {
        [DataMember]
        public int CurrentSessionNumber { get; set; }

        [DataMember]
        public bool IsAlwaysOnTop { get; set; }

        [DataMember]
        public bool IsMinimalMode { get; set; }

        [DataMember]
        public bool IsSuperMinimalMode { get; set; }

        [DataMember]
        public bool IsAutoFreeAreaEnabled { get; set; }

        [DataMember]
        public bool StartInSuperMinimalMode { get; set; }

        [DataMember]
        public bool IsGuideCompanionEnabled { get; set; }

        [DataMember]
        public string ThemeName { get; set; }

        [DataMember]
        public bool HasDismissedGettingStarted { get; set; }

        [DataMember]
        public double WindowLeft { get; set; }

        [DataMember]
        public double WindowTop { get; set; }

        [DataMember]
        public double GlobalTargetDurationSeconds { get; set; }

        [DataMember]
        public string CustomSessionLabelTemplate { get; set; }

        [DataMember]
        public double[] SessionTargetsInSeconds { get; set; }

        public UserSettings()
        {
            CurrentSessionNumber = 1;
            IsAlwaysOnTop = false;
            IsMinimalMode = false;
            IsSuperMinimalMode = false;
            IsAutoFreeAreaEnabled = false;
            StartInSuperMinimalMode = false;
            IsGuideCompanionEnabled = false;
            ThemeName = "Light";
            HasDismissedGettingStarted = false;
            WindowLeft = -1;
            WindowTop = -1;
            GlobalTargetDurationSeconds = 3600.0; // 1 hour default
            CustomSessionLabelTemplate = "Session {0:D2}";

            SessionTargetsInSeconds = new double[12];
            for (int i = 0; i < 12; i++)
            {
                SessionTargetsInSeconds[i] = 3600.0;
            }
        }
    }
}
