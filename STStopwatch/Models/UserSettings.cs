using System;
using System.Runtime.Serialization;

namespace STStopwatch.Models
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
        public double WindowLeft { get; set; }

        [DataMember]
        public double WindowTop { get; set; }

        [DataMember]
        public double[] SessionTargetsInSeconds { get; set; }

        public UserSettings()
        {
            CurrentSessionNumber = 1;
            IsAlwaysOnTop = false;
            IsMinimalMode = false;
            WindowLeft = -1;
            WindowTop = -1;

            // Default 1 hour (3600 seconds) for each of the 12 sessions
            SessionTargetsInSeconds = new double[12];
            for (int i = 0; i < 12; i++)
            {
                SessionTargetsInSeconds[i] = 3600.0;
            }
        }
    }
}
