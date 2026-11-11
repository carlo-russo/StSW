using System.Collections.Generic;
using STStopwatch.Core.Models;

namespace STStopwatch.Core.Services
{
    public interface IStorageService
    {
        UserSettings LoadSettings();
        void SaveSettings(UserSettings settings);
        List<SessionRecord> LoadRecords();
        void SaveRecord(SessionRecord record);
        void DeleteRecord(SessionRecord record);
        void DeleteAllRecords();
    }
}
