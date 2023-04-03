using UnityEditor.PackageManager;
using UnityEngine;

namespace Paeezan.BSClient.Editor
{
    public class UPMInfoCacheSO : ScriptableObject
    {
        [SerializeField]
        public PackageCollection packages;
    }
}