

#if UNITY_EDITOR
using UnityEditor;


namespace DNExtensions.Utilities.PlayFromCamera
{
    public static class PlayFromCameraSettings
    {
        private const string PrefAlsoSetRotation = "DNExtensions_PlayFromCamera_AlsoSetRotation";
        private const string PrefPlayerSelectionMode = "DNExtensions_PlayFromCamera_PlayerSelectionMode";
        private const string PrefPlayerTag = "DNExtensions_PlayFromCamera_PlayerTag";
        private const string PrefPlayerObjectPath = "DNExtensions_PlayFromCamera_PlayerObjectPath";

        public static bool AlsoSetRotation
        {
            get => EditorPrefs.GetBool(PrefAlsoSetRotation, false);
            set => EditorPrefs.SetBool(PrefAlsoSetRotation, value);
        }

        public static PlayerSelectionMode PlayerSelectionMode
        {
            get => (PlayerSelectionMode)EditorPrefs.GetInt(PrefPlayerSelectionMode, 0);
            set => EditorPrefs.SetInt(PrefPlayerSelectionMode, (int)value);
        }

        public static string PlayerTag
        {
            get => EditorPrefs.GetString(PrefPlayerTag, "Player");
            set => EditorPrefs.SetString(PrefPlayerTag, value);
        }

        public static string PlayerObjectPath
        {
            get => EditorPrefs.GetString(PrefPlayerObjectPath, "");
            set => EditorPrefs.SetString(PrefPlayerObjectPath, value);
        }
    }

}

#endif