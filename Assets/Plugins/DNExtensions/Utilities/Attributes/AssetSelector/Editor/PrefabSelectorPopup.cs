#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Popup window for selecting prefabs.
    /// </summary>
    public class PrefabSelectorPopup : AssetSelectorPopup<GameObject>
    {
        public static void Show(Rect buttonRect, AssetInfo<GameObject>[] assets, bool allowNull, bool showSearch, Action<GameObject> onAssetSelected)
        {
            ShowPopup<PrefabSelectorPopup>(buttonRect, assets, allowNull, showSearch, onAssetSelected);
        }
    }
}
#endif