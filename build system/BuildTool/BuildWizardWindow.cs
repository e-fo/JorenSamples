using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System.Xml.Linq;
using Paeezan.Utility;

namespace Paeezan.BSClient.Editor
{
    public class BuildWizardWindow : EditorWindow
    {
        [MenuItem("Battleship/Build Wizard")]
        public static void Open()
        {
            var window = GetWindow<BuildWizardWindow>();
            window.Show();
            BuildWizardPlayerBuild.SetBundleVersion();
        }

        private Vector2 _scrollPosition;

        private void OnGUI()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            ProfileList();
            GUILayout.Space(10);
            ProfilesGeneralButtons();
            GUILayout.Space(30);
            Options();
            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            BuildButtons();
            BuildAndRunOptions();
        }



        #region Window Sections

        #region Profile

        private void ProfileList()
        {
            var needRefresh = false;
            
            GUILayout.Label("Available Profiles", GetTitleStyle());
            foreach (var profile in BuildWizardProfiles.Profiles) {
                if (profile == null) {
                    needRefresh = true;
                    continue;
                }
                
                ProfileInfo(profile);
                ProfileBuildName(profile);
                ProfileButtons(profile);
                
                if (string.IsNullOrEmpty(profile.buildName)) {
                    EditorGUILayout.HelpBox("Fill above field with short name for build file name", MessageType.Warning);
                }
            }

            if (needRefresh) {
                BuildWizardProfiles.RefreshProfileList();
            }
        }

        private void ProfileInfo(SOBuildProfile profile)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            profile.buildName = GUILayout.TextField(profile.buildName,
                new GUIStyle(GUI.skin.textField) {fixedWidth = 40});
            var toggleValue = GUILayout.Toggle(profile.active, profile.name);
            if (profile.active != toggleValue) {
                profile.active = toggleValue;
                EditorUtility.SetDirty(profile);
            }
            GUILayout.EndHorizontal();
        }

        private void ProfileBuildName(SOBuildProfile profile)
        {
            if (profile.active) {
                GUILayout.BeginHorizontal();
                GUILayout.Space(50);
                GUILayout.Label(BuildNamingTool.GetApplicationName(profile));
                GUILayout.EndHorizontal();
            }
        }
        private void ProfileButtons(SOBuildProfile profile)
        {                
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GetButtonStyle(false))) {
                EditorGUIUtility.PingObject(profile);
                Selection.activeObject = profile;
            }

            if (GUILayout.Button("Apply Manifest", GetButtonStyle(false))) {
                profile.ApplyManifestModifications();
            }
            if (GUILayout.Button("Apply SDS", GetButtonStyle(false))) {
                profile.ApplyScriptingDefineSymbol();
            }
                
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
        }

        
        private void ProfilesGeneralButtons()
        {
            HorizontalSpace(() => {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("New Profile", GetButtonStyle(false))) {
                    var profilesPath = "Assets/Editor/BuildWizard/Profiles/";
                    ScriptableObjectTools.CreateScriptableObject<SOBuildProfile>(profilesPath + "SOBuildProfile_NEW");
                }

                if (GUILayout.RepeatButton("Refresh Profile List")) {
                    BuildWizardProfiles.RefreshProfileList();
                }
                GUILayout.Space(20);
                if (GUILayout.RepeatButton("Revert Manifest")) {
                    BuildWizardPlayerBuild.RevertManifest();
                }

                GUILayout.FlexibleSpace();
            });
        }

        #endregion

        #region Options

        private void Options()
        {
            var opt = SOGeneralBuildProfile.GetProfileAsset();
            HorizontalSpace(() => {
                GUILayout.FlexibleSpace();
                GUILayout.Label("General Build Options", GetTitleStyle());
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Browse", GetButtonStyle(false))) {
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = opt;
                }
            }, 20, 20);
            ScriptingDefineSymbols(opt);
            GUILayout.Space(10);
            Keystore(opt);
            AppVersion();
        }

        private void ScriptingDefineSymbols(SOGeneralBuildProfile options)
        {
            // var target = new SerializedObject(options);
            // EditorGUILayout.PropertyField(target.FindProperty(nameof(options.scriptingDefineSymbols)));
            
            var symbols = options.scriptingDefineSymbols;
            
            HorizontalSpace(() => {
                GUILayout.Label("Scripting Define Symbols", GetSubTitleStyle());
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+", GetButtonStyle(false))) {
                    symbols.Add("");
                    EditorUtility.SetDirty(options);
                }
            }, 30, 50);

            if (symbols.Count == 0) {
                HorizontalSpace(() => EditorGUILayout.HelpBox("List is empty", MessageType.None), 50, 50);
            }
            for (var i = 0; i < symbols.Count; i++) {
                var index = i;
                HorizontalSpace(() => {
                    symbols[index] = EditorGUILayout.TextField(symbols[index]);
                    if (GUILayout.Button("X", GetButtonStyle(false))) {
                        symbols.RemoveAt(index);
                        EditorUtility.SetDirty(options);
                    }
                }, 50, 50);
            }

            if (symbols.Any(string.IsNullOrEmpty)) {
                EditorGUILayout.HelpBox("Don't leave any of scripting define symbols empty", MessageType.Error);
            }
        }

        private void Keystore(SOGeneralBuildProfile options)
        {
            SubTitle("Keystore");
            GUI.enabled = false;
            HorizontalSpace(() => {
                EditorGUILayout.PasswordField("Keystore Password", "ManYeParandamArezooDaram");
            });
            HorizontalSpace(() => {
                EditorGUILayout.PasswordField("Alias Password", "KeYaramBashi");
            });
            GUI.enabled = true;
            EditorGUILayout.HelpBox("This part is not implemented yet.", MessageType.Info);
        }
        private void AppVersion()
        {
            SubTitle("Version");
            HorizontalSpace(() => {
                PlayerSettings.bundleVersion = EditorGUILayout.TextField("App Version", Application.version);
                GUI.enabled = false;
                var v = EditorGUILayout.TextField("Bundle Version",
                    PlayerSettings.Android.bundleVersionCode.ToString());
                v = Regex.Replace(v, @"[\D-]", string.Empty);
                PlayerSettings.Android.bundleVersionCode = int.Parse(v);
                GUI.enabled = true;
            });
        }
        #endregion

        #region Build
        

        private void BuildButtons()
        {
            var normal = new GUIStyle(GUI.skin.button);
            // normal.fixedHeight = 20;
            normal.fixedWidth = 130;
            
            
            if (BuildWizardProfiles.Profiles.All(p => !p.active)) {
                EditorGUILayout.HelpBox("There is no active build profile", MessageType.Error);
            }
            if (BuildWizardProfiles.Profiles.Any(p => p.active && string.IsNullOrEmpty(p.buildName))){
                EditorGUILayout.HelpBox("One of active profiles doesn't have build file name", MessageType.Error);
            }
            
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Addressable", normal)) {
                BuildWizardAddressable.Build();
            }

            if (BuildWizardProfiles.Profiles.All(p => !p.active) ||
                BuildWizardProfiles.Profiles.Any(p => p.active && string.IsNullOrEmpty(p.buildName))) {
                GUI.enabled = false;
            }
            
            if (GUILayout.Button("Player", normal)) {
                BuildWizardPlayerBuild.Build(BuildWizardProfiles.Profiles);
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        
        [SerializeField] int _profileIdx = 0;

        private void BuildAndRunOptions()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            _profileIdx = EditorGUILayout.Popup(_profileIdx,
                BuildWizardProfiles.Profiles.Select(p => p.name).ToArray());
            var profile = BuildWizardProfiles.Profiles[_profileIdx];
            if (string.IsNullOrEmpty(profile.buildName)) {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Build And Run")) {
                BuildWizardPlayerBuild.Build(profile, false, true);
            }

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(profile.buildName)) {
                EditorGUILayout.HelpBox("Fill build file name to run", MessageType.Error);
            }
            else {
                GUILayout.Space(40);
            }

        }

        #endregion

        #endregion
        
        #region Style Tools

        private GUIStyle GetTitleStyle()
        {
            var result = new GUIStyle(GUI.skin.label);
            result.fontSize = 20;
            result.fixedHeight = 30;
            result.alignment = TextAnchor.MiddleCenter;

            return result;
        }

        private GUIStyle GetSubTitleStyle()
        {
            var result = new GUIStyle(GUI.skin.label);
            result.fontStyle = FontStyle.Bold;

            return result;
        }
        
        private GUIStyle GetButtonStyle(bool stretch)
        {
            var result = new GUIStyle(GUI.skin.button);
            result.stretchWidth = stretch;

            return result;
        }

        

        #endregion

        #region Other tools
        

        private void SubTitle(string text, float left = 30)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(left);
            GUILayout.Label(text, GetSubTitleStyle());
            GUILayout.EndHorizontal();
        }

        private void HorizontalSpace(Action content, float leftSpace = 30, float rightSpace = 30)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(leftSpace);
            content?.Invoke();
            GUILayout.Space(rightSpace);
            GUILayout.EndHorizontal();
        }

        #endregion
    }
}