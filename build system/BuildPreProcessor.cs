using UnityEditor;
using UnityEngine;

public static class BuildPreProcessor
{
    

    #region Android
    public static void PrepareForBazaar(TextAsset baseManifest, TextAsset mainManifest)
    {
        // ManifestHelper manifest = new ManifestHelper(BASE_ANDROID_MANIFEST_PATH);
        ManifestHelper manifest = new ManifestHelper(baseManifest);
        manifest.AddMetaData("billing.service","bazaar.BazaarIabService");
        manifest.AddActivity("com.bazaar.BazaarIABProxyActivity","@android:style/Theme.Black.NoTitleBar.Fullscreen",exported:false,portrait:true);
        manifest.AddPermission("com.farsitel.bazaar.permission.PAY_THROUGH_BAZAAR");
        manifest.AddToQuerySection_Package("com.farsitel.bazaar");
        manifest.AddToQuerySection_Intent("ir.cafebazaar.pardakht.InAppBillingService.BIND");

        manifest.AddMetaData("metrix_storeName","Bazaar");

        manifest.Save(mainManifest);

        AssetDatabase.Refresh();
    }

    public static void PrepareForMyket(TextAsset baseManifest, TextAsset mainManifest)
    {
        // ManifestHelper manifest = new ManifestHelper(BASE_ANDROID_MANIFEST_PATH);
        ManifestHelper manifest = new ManifestHelper(baseManifest);
        manifest.AddMetaData("billing.service","myket.MyketIabService");
        manifest.AddActivity("com.myket.MyketIABProxyActivity", "@android:style/Theme.Translucent.NoTitleBar.Fullscreen", false);
        manifest.AddPermission("ir.mservices.market.BILLING");
        manifest.AddToQuerySection_Package("ir.mservices.market");
        manifest.AddToQuerySection_Intent("ir.mservices.market.InAppBillingService.BIND");
        manifest.AddMetaData("metrix_storeName","Myket");

        manifest.AddReciever("com.myket.util.IABReceiver",exported:true, new string[]
        {
            "ir.mservices.market.ping",
            "ir.mservices.market.purchase",
            "ir.mservices.market.getPurchase",
            "ir.mservices.market.billingSupport",
            "ir.mservices.market.skuDetail",
            "ir.mservices.market.consume",
        });

        manifest.Save(mainManifest);

        AssetDatabase.Refresh();
    }
    
    public static void PrepareForGooglePlay(TextAsset baseManifest, TextAsset mainManifest)
    {
        // ManifestHelper manifest = new ManifestHelper(BASE_ANDROID_MANIFEST_PATH);
        ManifestHelper manifest = new ManifestHelper(baseManifest);

        manifest.AddMetaData("metrix_storeName","GooglePlay");

        manifest.Save(mainManifest);

        AssetDatabase.Refresh();
    }
    #endregion
}
