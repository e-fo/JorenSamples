using System;
using UnityEngine;

namespace Paeezan.BSClient.Editor
{
    [Serializable]
    public class UnityPackage
    {
        public string PackageName => PackageId.Split('@')[0];
        public string PackagePath => "Packages/" + PackageId.Split('@')[1]["file:".Length..];
        public string FullPath => Application.dataPath[..^"Assets".Length] + PackagePath;
        public bool IsEmbedded => PackageId.Split('@')[1][.."file:".Length] == "file:";
        
        public string PackageId;
    }
}