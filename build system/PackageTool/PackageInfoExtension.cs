using Paeezan.BSClient.Editor;
using UnityEditor.PackageManager;
using UnityEngine;

public static class PackageInfoExtension
{
    public static string GetRelativeId(this PackageInfo info)
    {
        string ret = info.packageId;

        if (info.packageId.Contains("file:"))
        {
            string path = ret.Substring(info.name.Length + "@file:".Length);
            ret = $"{info.name}@file:{EditorUtils.ConvertToRelativePath(path).Replace("Packages/", "")}";
        }
        return ret;
    }
}