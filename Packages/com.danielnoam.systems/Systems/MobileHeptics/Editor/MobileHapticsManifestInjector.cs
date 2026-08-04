using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace DNExtensions.Systems.MobileHaptics
{
    /// <summary>
    /// Injects the VIBRATE permission into the generated Android manifest at build time.
    /// Runs after Gradle project generation, so it never conflicts with Unity's manifest merging.
    /// </summary>
    public class MobileHapticsManifestInjector : IPostGenerateGradleAndroidProject
    {
        private const string VibratePermission = "android.permission.VIBRATE";
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!MobileHapticsSettings.Instance.addVibratePermission) return;

            string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"[MobileHaptics] Manifest not found at: {manifestPath}");
                return;
            }

            var doc = new XmlDocument();
            doc.Load(manifestPath);

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("android", AndroidNs);

            if (doc.SelectSingleNode($"//uses-permission[@android:name='{VibratePermission}']", nsmgr) != null)
                return;

            XmlNode manifest = doc.SelectSingleNode("/manifest");
            if (manifest == null)
            {
                Debug.LogError("[MobileHaptics] Could not find <manifest> root in generated manifest.");
                return;
            }

            var permission = doc.CreateElement("uses-permission");
            var nameAttr = doc.CreateAttribute("android", "name", AndroidNs);
            nameAttr.Value = VibratePermission;
            permission.Attributes.Append(nameAttr);
            manifest.InsertBefore(permission, manifest.FirstChild);

            doc.Save(manifestPath);
            Debug.Log("[MobileHaptics] Injected VIBRATE permission into generated manifest.");
        }
    }
}