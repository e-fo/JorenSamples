using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Paeezan.BSClient.Editor {
    public static class BuildNamingTool {
        private static int _dailyVersionCounter;
        private static bool _dailyVersionSet;

        private static string _applicationName = "BTLSHP";


        public static void Reset()
        {
            _dailyVersionCounter = 0;
            _dailyVersionSet = false;
        }

        public static string BuildFolder {
            get {
                var slicedPath = Application.dataPath.Split('/');
                var removingCharacterCount = slicedPath.Last().Length + slicedPath[slicedPath.Length - 2].Length + 2;
                var buildFolder = Application.dataPath.Substring(0, Application.dataPath.Length - removingCharacterCount) +
                                  "/Builds_" + _applicationName;
                return buildFolder;
            }
        }

        public static string GetTempBuildPath()
        {
            return BuildFolder + "/TempBuild.apk";
        }
        public static string GetBuildPath(SOBuildProfile profile) {
            if (!Directory.Exists(BuildFolder)) {
                Directory.CreateDirectory(BuildFolder);
            }

            SetDailyVersion(BuildFolder);
            return BuildFolder + $"/{GetApplicationName(profile)}";;
        }

        public static string GetApplicationName(SOBuildProfile profile)
        {
            var applicationName = AddUnderScore(_applicationName) +
                                  AddUnderScore(DateWithDailyVersion) +
                                  AddUnderScore(profile.buildName) +
                                  ((profile.GetBuildOption(false).options & BuildOptions.Development) != 0
                                      ? "DEV_"
                                      : "") +
                                  ((profile.GetBuildOption(false).options & BuildOptions.EnableDeepProfilingSupport) != 0
                                      ? "DP_"
                                      : "") +
                                  profile.type +
                                  // $"({Application.version}-" + PlayerSettings.Android.bundleVersionCode + ")";
                                  $"({Application.version})";
            var applicationExtension = profile.isAppBundle ? "aab" : "apk";
            return $"{applicationName}.{applicationExtension}";
        }

        private static void SetDailyVersion(string buildPath) {
            if (_dailyVersionSet) {
                return;
            }

            _dailyVersionSet = true;
            var builds = Directory.GetFiles(buildPath);
            while (builds.Any(f => f.Contains(DateWithDailyVersion)) && _dailyVersionCounter < 100) {
                _dailyVersionCounter++;
            }

            if (_dailyVersionCounter > 99) {
                throw new Exception("You Exceeded 100 build per day! Go and rest a little bit and then come back and delete some of previous build you had today so I can select a version for naming");
            }
        }
        
        public static string DateWithDailyVersion => Today + _dailyVersionCounter.ToString("00");
        
        private static string Today {
            get {
                var pc = new PersianCalendar();
                return pc.GetYear(DateTime.Today).ToString().Substring(2) +
                       pc.GetMonth(DateTime.Today).ToString("00") +
                       pc.GetDayOfMonth(DateTime.Today).ToString("00");
            }
        }
        
        private static string AddUnderScore(string input, bool end = true) {
            return input.Trim() != "" 
                ? end 
                    ? $"{input}_" 
                    : $"_{input}" 
                : "";
        }
    }
}