using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Paeezan.BSClient.Editor
{
    [CustomPropertyDrawer(typeof(UnityPackage))]
    public class UnityPackagePropertyDrawer : PropertyDrawer
    {
        private List<string> _options = new List<string>();
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var p = property.FindPropertyRelative(nameof(UnityPackage.PackageId));
            int index = GetIndexFromPackage(GetPackageOptions(p.stringValue), property);

            var maskRect = rect; maskRect.xMin = maskRect.xMax - 300;
            int newIdx = EditorGUI.Popup(maskRect, index, GetPackageOptions(p.stringValue));

            if (index != newIdx)
            {
                string selected = _options[newIdx];
                p.stringValue = selected.Replace("\\", "/");
            }
        }

        private string[] GetPackageOptions(string selected)
        {
            if (_options?.Count == 0) {
                _options = UPMInfoCache.GetAllPackagesList();
            }

            _options ??= new List<string>();
            if (!_options.Contains(selected)) _options.Add(selected);
            return _options.Select(x=>x.Replace('/', '\\')).ToArray();
        }

        private int GetIndexFromPackage(string[] options, SerializedProperty property)
        {
            int ret = 0;
            var p = property.FindPropertyRelative(nameof(UnityPackage.PackageId));
            string name = p.stringValue;
            ret = options.ToList().IndexOf(name.Replace('/', '\\'));
            return ret;
        }
    }
}