using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Paeezan.BSClient.Editor {
    public static class BuildWizardLog {
        private static List<(string message, LogType type)> _logs;

        private static string _fullLog;
        private static string _modifiedLog;
        private static string _summaryLog;
        private static int _counter;
        
        public static void Reset()
        {
            _logs = new List<(string, LogType)>();
            _fullLog = "Begin\n";
            _modifiedLog = "Begin\n";
            _summaryLog = "Begin\n";
            _counter = 0;
            
            WriteToFile();
        }

        public static void Add(string message, LogType type) => Add(message, "", type);

        public static void Add(string message, string trace, LogType type)
        {
            _logs.Add((message, type));
            
            _fullLog += $"[{_counter:0000}][{type}]\n{message}\n{trace}\n\n";
            if (type != LogType.Warning) {
                _modifiedLog += $"[{_counter:0000}][{type}]\n{message}\n{trace}\n\n";
                _summaryLog += $"[{_counter:0000}][{type}] {message}\n";
            }
            
            WriteToFile();
            
            _counter++;
        }

        private static void WriteToFile()
        {
            var logFolder = $"{BuildNamingTool.BuildFolder}\\BuildLog_DoNotShip\\";
            if (!Directory.Exists(logFolder)) {
                Directory.CreateDirectory(logFolder);
            }
            var logPath = logFolder + BuildNamingTool.DateWithDailyVersion;
            File.WriteAllText(logPath + "_FullLog.txt", _fullLog);
            File.WriteAllText(logPath + "_ModifiedLog.txt", _modifiedLog);
            File.WriteAllText(logPath + "_SummaryLog.txt", _summaryLog);
        }

        public static List<(string, LogType)> GetLogs() => _logs;
    }
}