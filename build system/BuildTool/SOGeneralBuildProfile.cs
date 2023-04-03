using System.Collections.Generic;
using Paeezan.Utility;
using UnityEditor;
using UnityEngine;

namespace Paeezan.BSClient.Editor {
    [CreateAssetMenu(fileName = "SOBuildProfile_General", menuName = "ScriptableObjects/Data/General BuildProfile")]
    public class SOGeneralBuildProfile : ScriptableObject {
        public List<string> scriptingDefineSymbols;

        [Header("Android Manifest")]
        public TextAsset baseAndroidManifest;
        public TextAsset androidManifest;

        public static SOGeneralBuildProfile GetProfileAsset() =>
            ScriptableObjectTools.GetSingletonScriptableObject<SOGeneralBuildProfile>(
                "Assets/Editor/BuildWizard/Profiles/SOBuildProfile_General");
        
        public void SetBuildFlags(List<string> buildTargetSymbols)
        {
            var buildFlags = "";
            foreach(var flag in buildTargetSymbols)
            {
                buildFlags += flag + ";";
            }
            foreach(var flag in scriptingDefineSymbols)
            {
                buildFlags += flag + ";";
            }

            if (buildFlags != "")
            {
                buildFlags = buildFlags[..^1];
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, buildFlags);
            }
        }
    }
}