using UnityEditor.AddressableAssets.Settings;

namespace Paeezan.BSClient.Editor {
    public static class BuildWizardAddressable {
        public static void Build()
        {
            AddressableAssetSettings.BuildPlayerContent();
        }
    }
}