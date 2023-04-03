using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Paeezan.Unity.Editor;
using Paeezan.Utility;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Paeezan.BSClient.Editor
{
    public static class UPMInfoCache
    {
        public static PackageCollection AllPackagesCollection {
            get {
                _cacheSO ??= ScriptableObjectTools.GetSingletonScriptableObject<UPMInfoCacheSO>();
                return _cacheSO.packages;
            }
        }

        public static StatusCode Status = StatusCode.Failure;
        static ListRequest _req;
        static UPMInfoCacheSO _cacheSO = null;

        public static void UpdatePackages()
        {
            if (Status == StatusCode.InProgress) return;

            _req = Client.List(offlineMode:true, includeIndirectDependencies:false);
            EditorApplication.update += EditorApplication_OnUpdated;
            Status = StatusCode.InProgress;
        }

        static void EditorApplication_OnUpdated()
        {
            // Debug.Log($"{nameof(UPMInfoCache)}: StatusCode -> {Status}");
            if (_req.IsCompleted)
            {
                if (_req.Status == StatusCode.Success)
                {
                    Status = StatusCode.Success;
                    _cacheSO.packages = _req.Result;
                    EditorUtility.SetDirty(_cacheSO);
                    // Debug.Log("Package List Updated Successfully");
                } else if (_req.Status >= StatusCode.Failure)
                {
                    Status = StatusCode.Failure;
                    Debug.LogError(_req.Error.message);
                }

                EditorApplication.update -= EditorApplication_OnUpdated;
            }
        }

        public static List<string> GetAllPackagesList()
        {
            UpdatePackages();
            var result = AllPackagesCollection?.Select(x => x.GetRelativeId()).ToList();

            if (result?.Count == 0 && Status == StatusCode.Success) {
                result = AllPackagesCollection.Select(x => x.GetRelativeId()).ToList();
            }

            return result;
        }
    }
}