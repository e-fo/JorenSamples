using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Paeezan.Utility;
using UnityEditor;
using UnityEngine;
using UnityEditor.Presets;
using UnityEngine.Events;

namespace Paeezan.BSClient.Editor {
    using Unity.DataStructure;

    [CreateAssetMenu(fileName = "SOBuildProfile_NAME", menuName = "ScriptableObjects/Data/BuildProfile")]
    public class SOBuildProfile : ScriptableObject {

        public string buildName;
        public BuildType type;
        [Header("Options")] 
        public bool active;
        public bool isAppBundle;
        public bool arm64;
        public bool developmentBuild;
        public bool deepProfiling;
        

        public List<SerializedTuple<ScriptableObject, Preset>> ConfigurableAssets;
        [Header("Packages")]
        public UnityPackage[] IncludePackageList;
        public UnityPackage[] ExcludePackageList;

        [Header("Directories")]
        public List<string> includingDirectories;
        public List<string> excludingDirectories;
        
        [Header("Files")]
        public List<string> includingFiles;
        public List<string> excludingFiles;

        [Space]
        public List<string> scriptingDefineSymbols;
        
        
        [Help("XML Example:\n" +
              "<root xmlns:android=\"http://schemas.android.com/apk/res/android\" xmlns:tools=\"http://schemas.android.com/tools\">\n" +
              "     <add route=\"application.meta-data\">\n" +
              "         <meta-data android:name=\"metrix_storeName\" android:value=\"GooglePlay\" />\n" +
              "     </add>\n" +
              "     <add>\n" +
              "         <intent>\n" +
              "             <action android:name=\"ir.mservices.market.InAppBillingService.BIND\" />\n" +
              "         </intent>\n" +
              "     </add>\n" +
              "</root>")]
        public TextAsset ManifestModifications;
        


        public BuildPlayerOptions GetBuildOption(bool run)
        {
            EditorUserBuildSettings.buildAppBundle = isAppBundle;
            
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 |
                                                         (arm64
                                                             ? AndroidArchitecture.ARM64
                                                             : AndroidArchitecture.None);

            var option = new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                target = BuildTarget.Android,
                options = BuildOptions.ShowBuiltPlayer
            };

            
            if (developmentBuild) {
                option.options |= BuildOptions.Development;
                if (run) {
                    option.options |= BuildOptions.ConnectWithProfiler;
                }
            }
            if (deepProfiling) {
                option.options |= BuildOptions.EnableDeepProfilingSupport;
            }
            if (run) {
                option.options |= BuildOptions.AutoRunPlayer |
                                  // BuildOptions.ConnectWithProfiler |
                                  BuildOptions.WaitForPlayerConnection;
            }

            return option;
        }
        
        public void ApplyManifestModifications()
        {
            if (ManifestModifications == null) {
                return;
            }
            var doc = XDocument.Load(AssetDatabase.GetAssetPath(SOGeneralBuildProfile.GetProfileAsset().baseAndroidManifest));
            XMLHelper.AddElements(doc, XDocument.Parse(ManifestModifications.text));
            doc.Save(AssetDatabase.GetAssetPath(SOGeneralBuildProfile.GetProfileAsset().androidManifest));
        }

        public void ApplyScriptingDefineSymbol()
        {
            SOGeneralBuildProfile.GetProfileAsset().SetBuildFlags(scriptingDefineSymbols);
        }
    }
    
    public enum BuildType
    {
        Test,
        Qabr,
        Stage
    }
}
