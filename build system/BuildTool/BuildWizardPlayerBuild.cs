using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Paeezan.BSClient.Editor {
    public static class BuildWizardPlayerBuild {
        public static bool IsInProgress;

        public static void SetBundleVersion()
        {
            PlayerSettings.Android.bundleVersionCode = int.Parse(BuildNamingTool.DateWithDailyVersion);
        }

        public static void Build(List<SOBuildProfile> profiles)
        {
            BuildWizardLog.Reset();
            Application.logMessageReceived += BuildWizardLog.Add;
            BuildNamingTool.Reset();
            foreach (var profile in profiles) {
                BuildWizardAddressable.Build();
                if (profile != null && profile.active) {
                    Debug.Log("Start Building Profile " + profile.name);
                    Build(profile);
                }
            }

            
            Application.logMessageReceived -= BuildWizardLog.Add;
            Debug.ClearDeveloperConsole();
            foreach (var log in BuildWizardLog.GetLogs().ToArray()) {
                var message = log.Item2 + ": " + log.Item1;
                switch (log.Item2) {
                    case LogType.Error:
                        Debug.LogError(message);
                        break;
                    case LogType.Assert:
                        Debug.LogAssertion(message);
                        break;
                    case LogType.Warning:
                        Debug.LogWarning(message);
                        break;
                    case LogType.Log:
                        Debug.Log(message);
                        break;
                    case LogType.Exception:
                        Debug.LogError(message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static void Build(SOBuildProfile profile, bool dryRun=false, bool buildAndRun = false)
        {
            SetBundleVersion();
            
            _progress = 0;
            IsInProgress = true;

            ProgressBar(profile, "Check IAP preset");
            CheckIAPPresetExistence(profile);
            ProgressBar(profile, "Applying presets");
            ApplyPresets(profile, dryRun);
            ProgressBar(profile, "Excluding Files");
            RefreshAssetDataBase(ExcludeFiles(profile));
            ProgressBar(profile, "Including Files");
            RefreshAssetDataBase(IncludeFiles(profile));
            ProgressBar(profile, "Excluding Directories");
            RefreshAssetDataBase(ExcludeDirectories(profile));
            ProgressBar(profile, "Including Directories");
            RefreshAssetDataBase(IncludeDirectories(profile));
            ProgressBar(profile, "Excluding Packages");
            RefreshAssetDataBase(ExcludePackages(profile));
            ProgressBar(profile, "Including Packages");
            RefreshAssetDataBase(IncludePackages(profile));
            ProgressBar(profile, "Apply Manifest Modifications");
            profile.ApplyManifestModifications();
            ProgressBar(profile, "Set script define symbols");
            profile.ApplyScriptingDefineSymbol();
            ProgressBar(profile, "Build player");
            BuildPlayer(profile, dryRun, buildAndRun);
            ProgressBar(profile, "Reverting");
            RefreshAssetDataBase(RevertAllExcludedPackages());
            
            EndBuildProcess(true, $"Profile {profile.name} built!");
        }

        private static void CheckIAPPresetExistence(SOBuildProfile profile)
        {
            var iap = profile.ConfigurableAssets.FirstOrDefault(x => x.Item1 is SOIAPManager t);
            if (null == iap) {
                EndBuildProcess(false, 
                    $"You don't add {nameof(SOIAPManager)} asset file to configurable assets in {profile.name} build profile");
            }
        }

        private static void ApplyPresets(SOBuildProfile profile, bool dryRun)
        {
            var isAllApplicable = true;
            profile.ConfigurableAssets.ForEach(c => {
                isAllApplicable &= c.Item2.CanBeAppliedTo(c.Item1);
                if (!isAllApplicable) {
                    Debug.LogError(
                        $"The preset of {c.Item1.name} can't be applied, please select correct preset for this configurable asset.");
                }
            });
            if (!isAllApplicable) {
                EndBuildProcess(false, $"all preset for profile {profile.name} is not applicable");
            }

            profile.ConfigurableAssets.ForEach(c => {
                if (!dryRun) c.Item2.ApplyTo(c.Item1);
            });
        }

        private static List<string> ExcludeFiles(SOBuildProfile profile)
        {
            var result = new List<string>();
            foreach (var path in profile.excludingFiles) {
                if (File.Exists($"{Application.dataPath}/{path}")) {
                    File.Move($"{Application.dataPath}/{path}",
                        $"{Application.dataPath}/{path}~");
                    result.Add($"{Application.dataPath}/{path}");
                }
                else {
                    Debug.LogError(
                        $"can't exclude File \"{path}\" for profile {profile.name} because it doesn't exist.");
                }
            }
            return result;
        }
        
        private static List<string> IncludeFiles(SOBuildProfile profile)
        {
            var result = new List<string>();
            foreach (var path in profile.includingFiles) {
                if (File.Exists($"{Application.dataPath}/{path}~")) {
                    File.Move($"{Application.dataPath}/{path}~",
                        $"{Application.dataPath}/{path}");
                    result.Add($"{Application.dataPath}/{path}~");
                }
                else {
                    EndBuildProcess(false,
                        $"can't include File \"{path}\" for profile {profile.name} because it doesn't exist.");
                }
            }
            return result;
        }
        
        private static List<string> ExcludeDirectories(SOBuildProfile profile)
        {
            var result = new List<string>();
            foreach (var path in profile.excludingDirectories) {
                if (File.Exists($"{Application.dataPath}/{path}")) {
                    File.Move($"{Application.dataPath}/{path}",
                        $"{Application.dataPath}/{path}~");
                    result.Add($"{Application.dataPath}/{path}");
                }
                else {
                    Debug.LogError(
                        $"can't exclude Directory \"{path}\" for profile {profile.name} because it doesn't exist.");
                }
            }
            return result;
        }
        
        private static List<string> IncludeDirectories(SOBuildProfile profile)
        {
            var result = new List<string>();
            foreach (var path in profile.includingDirectories) {
                if (File.Exists($"{Application.dataPath}/{path}~")) {
                    File.Move($"{Application.dataPath}/{path}~",
                        $"{Application.dataPath}/{path}");
                    result.Add($"{Application.dataPath}/{path}~");
                }
                else {
                    EndBuildProcess(false,
                        $"can't include Directory \"{path}\" for profile {profile.name} because it doesn't exist.");
                }
            }
            return result;
        }

        private static List<string> ExcludePackages(SOBuildProfile profile)
        {
            var result = new List<string>();
            foreach (var package in profile.ExcludePackageList) {
                if (!package.IsEmbedded) {
                    EndBuildProcess(false, $"can't exclude package \"{package.PackageId}\" for profile {profile.name} because it's not embedded package.");
                }
                if (Directory.Exists(package.FullPath)) {
                    Directory.Move(package.FullPath, package.FullPath + "~");
                    result.Add(package.FullPath);
                }
            }

            return result;
        }

        private static List<string> IncludePackages(SOBuildProfile profile)
        {
            var result = new List<string>();
            foreach (var package in profile.IncludePackageList) {
                if (!package.IsEmbedded) {
                    EndBuildProcess(false, $"can't include package \"{package.PackageId}\" for profile {profile.name} because it's not embedded package.");
                }
                if (Directory.Exists(package.FullPath + "~")) {
                    Directory.Move(package.FullPath + "~", package.FullPath);
                    result.Add(package.FullPath + "~");
                }
                else if (!Directory.Exists(package.FullPath)) {
                    EndBuildProcess(false, $"There is no directory with path \"{package.FullPath}\" to include in project");
                }
            }

            return result;
        }

        private static void BuildPlayer(SOBuildProfile profile, bool dryRun, bool buildAndRun)
        {
            if (dryRun) {
                return;
            }
            var option = profile.GetBuildOption(buildAndRun);
            option.locationPathName = BuildNamingTool.GetTempBuildPath();
            var report = BuildPipeline.BuildPlayer(option);
            BuildSummary summary = report.summary;
            Debug.Log($"Build {profile.buildName} Result: " + summary.result);
            if (summary.result == BuildResult.Succeeded) {
                File.Move(summary.outputPath, BuildNamingTool.GetBuildPath(profile));
                Debug.Log($"Build {profile.buildName}\n{summary.totalSize} bytes\n{summary.totalErrors} Errors\n{summary.totalTime}s\n{summary.result}");
            }

            if (summary.result == BuildResult.Failed) {
                EndBuildProcess(false, $"Build {profile.buildName}\n{summary.totalSize} bytes\n{summary.totalErrors} Errors\n{summary.totalTime}s\n{summary.result}");
            }
        }

        private static List<string> RevertAllExcludedPackages()
        {
            //This is not a good solution because maybe for some reason we what to ignore some package folder with ~ but for now it's working
            var result = new List<string>();
            var dirs = Directory.GetDirectories(Application.dataPath[..^"Assets".Length] + "Packages/PackagesSource");
            foreach (var dir in dirs) {
                if (dir[^1..] == "~") {
                    Directory.Move(dir, dir[..^1]);
                    result.Add(dir);
                }
            }

            return result;
        }

        public static void RevertManifest()
        {
            var doc = XDocument.Load(AssetDatabase.GetAssetPath(SOGeneralBuildProfile.GetProfileAsset().baseAndroidManifest));
            doc.Save(AssetDatabase.GetAssetPath(SOGeneralBuildProfile.GetProfileAsset().androidManifest));
        }


        #region General Tools


        public static void EndBuildProcess(bool succeeded, string message)
        {
            RevertManifest();
            EditorUtility.ClearProgressBar();
            IsInProgress = false;
            if (succeeded) {
                Debug.Log("Build succeeded: " + message);
            }
            else
            {
                throw new Exception("Build Failed: " + message);
            }
        }


        private static float _progress;
        private static void ProgressBar(SOBuildProfile currentProfile, string message)
        {
            _progress += (1 - _progress) / 4f;
            Debug.Log($"Build Profile: {currentProfile.name}\n" + $"{(_progress * 100):00}% - {message}");
            EditorUtility.DisplayProgressBar($"Build Profile: {currentProfile.name}", $"{(_progress * 100):00}% - {message}", _progress);
        }

        private static void RefreshAssetDataBase(List<string> changedFolders)
        {
            AssetDatabase.ForceReserializeAssets(changedFolders);
            AssetDatabase.Refresh();
        }

        #endregion
    }
}