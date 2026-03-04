using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Adds a Carousel View entry to the GameObject > UI (Canvas) menu, creating a ready-to-use
    /// hierarchy with a viewport for clipping, a content rect that defines the item slot size,
    /// and an indicators strip snapped to the bottom.
    /// </summary>
    internal static class CarouselViewCreator
    {
        private const string MenuPath = "GameObject/UI (Canvas)/Carousel View";
        private const int MenuPriority = 2062;
        private const float IndicatorHeight = 150f;

        [MenuItem(MenuPath, false, MenuPriority)]
        private static void Create(MenuCommand menuCommand)
        {
            GameObject parent = menuCommand.context as GameObject;
            EnsureCanvasExists(ref parent);

            GameObject root = CreateUIObject("Carousel View", parent);
            SetAnchorsAndSize(root.GetComponent<RectTransform>(),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CarouselView carousel = root.AddComponent<CarouselView>();
            carousel.spacing = 10f;
            
            GameObject viewportGo = CreateUIObject("Viewport", root);
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(0f, IndicatorHeight);
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage     = viewportGo.AddComponent<Image>();
            viewportImage.sprite    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
            viewportImage.type      = Image.Type.Sliced;
            viewportImage.color     = Color.white;

            Mask viewportMask            = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            GameObject contentGo = CreateUIObject("Content", viewportGo);
            RectTransform contentRect = contentGo.GetComponent<RectTransform>();
            SetAnchorsAndSize(contentRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            
            GameObject indicatorsGo = CreateUIObject("Indicators", root);
            RectTransform indicatorsRect = indicatorsGo.GetComponent<RectTransform>();
            indicatorsRect.anchorMin = new Vector2(0f, 0f);
            indicatorsRect.anchorMax = new Vector2(1f, 0f);
            indicatorsRect.offsetMin = Vector2.zero;
            indicatorsRect.offsetMax = new Vector2(0f, IndicatorHeight);

            HorizontalLayoutGroup layout  = indicatorsGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment         = TextAnchor.MiddleCenter;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = true;
            
            SerializedObject so = new SerializedObject(carousel);
            so.FindProperty("viewport").objectReferenceValue           = viewportRect;
            so.FindProperty("content").objectReferenceValue            = contentRect;
            so.FindProperty("indicatorContainer").objectReferenceValue = indicatorsRect;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create Carousel View");
            Selection.activeGameObject = root;
        }

        private static GameObject CreateUIObject(string name, GameObject parent)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<RectTransform>();
            GameObjectUtility.SetParentAndAlign(go, parent);
            return go;
        }

        private static void SetAnchorsAndSize(RectTransform rect,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rect.anchorMin        = anchorMin;
            rect.anchorMax        = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta        = sizeDelta;
        }
        
        private static void EnsureCanvasExists(ref GameObject parent)
        {
            if (parent != null && parent.GetComponentInParent<Canvas>() != null)
                return;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
            }

            parent = canvas.gameObject;
        }
    }
}
