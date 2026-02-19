#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Popup window for selecting ScriptableObjects.
    /// </summary>
    public class SOSelectorPopup : AssetSelectorPopup<ScriptableObject>
    {
        public static void Show(Rect buttonRect, AssetInfo<ScriptableObject>[] assets, bool allowNull, bool showSearch, Action<ScriptableObject> onAssetSelected)
        {
            ShowPopup<SOSelectorPopup>(buttonRect, assets, allowNull, showSearch, onAssetSelected);
        }
    }
}
#endif