using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using STStopwatch.Models;

namespace STStopwatch.Services
{
    public class JsonStorageService : IStorageService
    {
        private readonly string _storageDirectory;
        private readonly string _settingsFilePath;
        private readonly string _recordsFilePath;
        private readonly object _fileLock = new object();

        public JsonStorageService(string customDirectory = null)
        {
            if (!string.IsNullOrEmpty(customDirectory))
            {
                _storageDirectory = customDirectory;
            }
            else
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _storageDirectory = Path.Combine(appData, "StartToday", "STStopwatch");
            }
            _settingsFilePath = Path.Combine(_storageDirectory, "settings.json");
            _recordsFilePath = Path.Combine(_storageDirectory, "sessions.json");

            try
            {
                if (!Directory.Exists(_storageDirectory))
                {
                    Directory.CreateDirectory(_storageDirectory);
                }
            }
            catch
            {
                // Fallback to local working directory if AppData is inaccessible
                _storageDirectory = AppDomain.CurrentDomain.BaseDirectory;
                _settingsFilePath = Path.Combine(_storageDirectory, "settings.json");
                _recordsFilePath = Path.Combine(_storageDirectory, "sessions.json");
            }
        }

        public UserSettings LoadSettings()
        {
            lock (_fileLock)
            {
                if (!File.Exists(_settingsFilePath))
                {
                    var defaults = new UserSettings();
                    SaveSettings(defaults);
                    return defaults;
                }

                try
                {
                    using (var fs = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(UserSettings));
                        var settings = serializer.ReadObject(fs) as UserSettings;
                        if (settings != null)
                        {
                            if (settings.SessionTargetsInSeconds == null || settings.SessionTargetsInSeconds.Length < 12)
                            {
                                var targets = new double[12];
                                for (int i = 0; i < 12; i++)
                                {
                                    if (settings.SessionTargetsInSeconds != null && i < settings.SessionTargetsInSeconds.Length)
                                    {
                                        targets[i] = settings.SessionTargetsInSeconds[i];
                                    }
                                    else
                                    {
                                        targets[i] = 3600.0;
                                    }
                                }
                                settings.SessionTargetsInSeconds = targets;
                            }
                            return settings;
                        }
                    }
                }
                catch
                {
                    // If corrupted, return new defaults
                }

                return new UserSettings();
            }
        }

        public void SaveSettings(UserSettings settings)
        {
            if (settings == null) return;

            lock (_fileLock)
            {
                try
                {
                    string tempPath = _settingsFilePath + ".tmp";
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(UserSettings));
                        serializer.WriteObject(fs, settings);
                    }

                    if (File.Exists(_settingsFilePath))
                    {
                        File.Delete(_settingsFilePath);
                    }
                    File.Move(tempPath, _settingsFilePath);
                }
                catch
                {
                    // Log or ignore safe failure
                }
            }
        }

        public List<SessionRecord> LoadRecords()
        {
            lock (_fileLock)
            {
                if (!File.Exists(_recordsFilePath))
                {
                    return new List<SessionRecord>();
                }

                try
                {
                    using (var fs = new FileStream(_recordsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(List<SessionRecord>));
                        var records = serializer.ReadObject(fs) as List<SessionRecord>;
                        if (records != null)
                        {
                            return records;
                        }
                    }
                }
                catch
                {
                    // Fallback to empty list
                }

                return new List<SessionRecord>();
            }
        }

        public void SaveRecord(SessionRecord record)
        {
            if (record == null) return;

            lock (_fileLock)
            {
                try
                {
                    List<SessionRecord> records = LoadRecords();
                    records.Add(record);

                    string tempPath = _recordsFilePath + ".tmp";
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(List<SessionRecord>));
                        serializer.WriteObject(fs, records);
                    }

                    if (File.Exists(_recordsFilePath))
                    {
                        File.Delete(_recordsFilePath);
                    }
                    File.Move(tempPath, _recordsFilePath);
                }
                catch
                {
                    // Fail safely
                }
            }
        }

        public void DeleteRecord(SessionRecord record)
        {
            if (record == null) return;

            lock (_fileLock)
            {
                try
                {
                    List<SessionRecord> records = LoadRecords();
                    int index = -1;

                    if (!string.IsNullOrEmpty(record.Id))
                    {
                        index = records.FindIndex(r => r.Id == record.Id);
                    }

                    // Older saved records may not have an ID. Match identifying
                    // fields and remove only the first matching session.
                    if (index < 0)
                    {
                        index = records.FindIndex(r =>
                            r.SessionNumber == record.SessionNumber &&
                            r.StartTime == record.StartTime &&
                            r.EndTime == record.EndTime &&
                            r.SessionLabel == record.SessionLabel);
                    }

                    if (index < 0) return;
                    records.RemoveAt(index);

                    string tempPath = _recordsFilePath + ".tmp";
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(List<SessionRecord>));
                        serializer.WriteObject(fs, records);
                    }

                    if (File.Exists(_recordsFilePath)) File.Delete(_recordsFilePath);
                    File.Move(tempPath, _recordsFilePath);
                }
                catch
                {
                    // Fail safely
                }
            }
        }

        public void DeleteAllRecords()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(_recordsFilePath))
                    {
                        File.Delete(_recordsFilePath);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
