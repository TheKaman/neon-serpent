// ConfigureAdMob.cs
// Verifies that GoogleMobileAdsSettings asset exists and the Android App ID is set correctly.
// The asset is pre-created at Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset.
//
// HOW TO RUN:
//   NeonSerpent → Configure AdMob Settings

using UnityEditor;
using UnityEngine;

namespace NeonSerpent.Editor
{
    public static class ConfigureAdMob
    {
        private const string k_AppIdAndroid = "ca-app-pub-7408967267429686~6774004553";
        private const string k_AssetPath    = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string k_TypeName     = "GoogleMobileAds.Editor.GoogleMobileAdsSettings";

        [MenuItem("NeonSerpent/Configure AdMob Settings")]
        public static void Configure()
        {
            var settingsType = GetSettingsType();
            if (settingsType == null)
            {
                EditorUtility.DisplayDialog("AdMob Setup — Type Missing",
                    "Could not find GoogleMobileAdsSettings type.\n" +
                    "Make sure the Google Mobile Ads Unity Plugin is fully imported and compiled.\n\n" +
                    "Expected Android App ID:\n" + k_AppIdAndroid,
                    "OK");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath(k_AssetPath, settingsType);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("AdMob Setup — Asset Missing",
                    "Settings asset not found at:\n" + k_AssetPath + "\n\n" +
                    "Re-import the Google Mobile Ads Unity Plugin, then run this again.",
                    "OK");
                return;
            }

            // Read the current App ID via SerializedObject so we avoid the internal class restriction.
            var so  = new SerializedObject(asset);
            var prop = so.FindProperty("adMobAndroidAppId");
            if (prop == null)
            {
                EditorUtility.DisplayDialog("AdMob Setup — Field Not Found",
                    "Could not read adMobAndroidAppId from the settings asset.\n\n" +
                    "Open the asset in the Inspector and verify the Android App ID is:\n" +
                    k_AppIdAndroid,
                    "OK");
                return;
            }

            string currentId = prop.stringValue;
            if (currentId == k_AppIdAndroid)
            {
                EditorUtility.DisplayDialog("AdMob — Already Configured",
                    "App ID is correctly set:\n\n" +
                    k_AppIdAndroid + "\n\n" +
                    "No changes needed.\n\n" +
                    "Next step if you have not already done it:\n" +
                    "Assets → External Dependency Manager → Android Resolver → Resolve All",
                    "OK");
            }
            else
            {
                // ID is wrong or blank — fix it in place.
                prop.stringValue = k_AppIdAndroid;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                Debug.Log($"[ConfigureAdMob] App ID corrected to: {k_AppIdAndroid}");
                EditorUtility.DisplayDialog("AdMob — App ID Updated",
                    "The App ID was incorrect or blank and has been fixed:\n\n" +
                    k_AppIdAndroid + "\n\n" +
                    "Next: Assets → External Dependency Manager → Android Resolver → Resolve All",
                    "OK");
            }
        }

        /// <summary>
        /// Walks all loaded assemblies to find the internal GoogleMobileAdsSettings type,
        /// avoiding a direct compile-time reference to the internal class.
        /// </summary>
        private static System.Type GetSettingsType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType(k_TypeName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
