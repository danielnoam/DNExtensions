using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    public static class AudioClipContextMenu
    {
        [MenuItem("Assets/Create/Audio Profile from Clip", true)]
        private static bool CreateAudioProfileFromClipValidate()
        {
            return Selection.activeObject is AudioClip;
        }

        [MenuItem("Assets/Create/Audio Profile from Clip")]
        private static void CreateAudioProfileFromClip()
        {
            AudioClip clip = (AudioClip)Selection.activeObject;
            string path = AssetDatabase.GetAssetPath(clip);
            path = System.IO.Path.ChangeExtension(path, null) + "_Profile.asset";

            SOAudioProfile profile = ScriptableObject.CreateInstance<SOAudioProfile>();
            profile.clips = new AudioClip[] { clip };

            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = profile;
        }
    }
}