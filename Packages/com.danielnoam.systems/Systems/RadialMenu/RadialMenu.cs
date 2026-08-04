using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.Systems.RadialMenu
{
    public class RadialMenu<T> : MonoBehaviour where T : class
    {
        [Header("Input Settings")]
        [SerializeField] protected float selectionDeadzone = 50f;
        [SerializeField] protected float maxRadius = 150f;
        [SerializeField, ReadOnly] protected Vector2 mousePositionFromCenter;

        [Header("Element Settings")]
        [SerializeField] protected Color normalColor = Color.white;
        [SerializeField] protected Color hoveredColor = new Color(1f, 0.647f, 0f);

        [Header("References")]
        [SerializeField] protected RadialMenuElement elementPrefab;
        [SerializeField] protected Transform elementsContainer;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected TextMeshProUGUI menuTitleText;
        [SerializeField] protected TextMeshProUGUI selectedItemText;

        private readonly Dictionary<RadialMenuElement, T> _elementToItem = new();
        private readonly List<RadialMenuElement> _menuElements = new();
        private RadialMenuElement _hoveredElement;
        private Vector2 _accumulatedMouseDelta;
        private int _currentSegmentIndex = -1;
        private bool _isOpen;

        public event Action<RadialMenuElement, T> OnElementSelected;
        public event Action<RadialMenuElement, T> OnElementHoverChanged;
        public event Action OnMenuOpened;
        public event Action OnMenuClosed;

        private void Awake()
        {
            CloseMenu();
        }

        private void Update()
        {
            if (_isOpen) UpdateHoverSelection();
        }

        private void OnDestroy()
        {
            ClearMenu();
        }

        private void HoverElement(int index)
        {
            if (index < 0 || index >= _menuElements.Count) return;

            RadialMenuElement element = _menuElements[index];
            if (_hoveredElement == element) return;

            UnhoverElement();
            _hoveredElement = element;
            _hoveredElement.SetHovered();
            if (selectedItemText) selectedItemText.text = element.Info;

            if (_elementToItem.TryGetValue(element, out T item))
            {
                OnElementHoverChanged?.Invoke(element, item);
            }
        }

        private void UnhoverElement()
        {
            if (!_hoveredElement) return;
            if (selectedItemText) selectedItemText.text = "";
            _hoveredElement.SetNormal();
            _hoveredElement = null;
            OnElementHoverChanged?.Invoke(null, null);
        }

        private void UpdateHoverSelection()
        {
            if (_menuElements.Count == 0) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            _accumulatedMouseDelta += mouseDelta;

            if (_accumulatedMouseDelta.magnitude > maxRadius)
                _accumulatedMouseDelta = _accumulatedMouseDelta.normalized * maxRadius;

            mousePositionFromCenter = _accumulatedMouseDelta;

            if (mousePositionFromCenter.magnitude < selectionDeadzone)
            {
                UnhoverElement();
                _currentSegmentIndex = -1;
                return;
            }

            float mouseAngle = Mathf.Atan2(mousePositionFromCenter.y, mousePositionFromCenter.x) * Mathf.Rad2Deg;
            int closestIndex = -1;
            float smallestAngleDiff = float.MaxValue;

            for (int i = 0; i < _menuElements.Count; i++)
            {
                Vector2 elementScreenPos = RectTransformUtility.WorldToScreenPoint(null, _menuElements[i].transform.position);
                Vector2 menuCenter = RectTransformUtility.WorldToScreenPoint(null, elementsContainer.position);
                Vector2 elementDirection = elementScreenPos - menuCenter;
                float elementAngle = Mathf.Atan2(elementDirection.y, elementDirection.x) * Mathf.Rad2Deg;
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(mouseAngle, elementAngle));

                if (angleDiff < smallestAngleDiff)
                {
                    smallestAngleDiff = angleDiff;
                    closestIndex = i;
                }
            }

            if (closestIndex != _currentSegmentIndex)
            {
                _currentSegmentIndex = closestIndex;
                HoverElement(closestIndex);
            }
        }

        private void ClearMenu()
        {
            foreach (var element in _menuElements)
            {
                if (element) Destroy(element.gameObject);
            }

            _menuElements.Clear();
            _elementToItem.Clear();
        }

        public void SetupMenu(T[] items, Action<RadialMenuElement, T> configureElement)
        {
            ClearMenu();

            foreach (var item in items)
            {
                RadialMenuElement newElement = Instantiate(elementPrefab, elementsContainer);
                newElement.SetUp(normalColor, hoveredColor);
                configureElement?.Invoke(newElement, item);
                _elementToItem[newElement] = item;
                _menuElements.Add(newElement);
            }
        }

        public void SetupMenu(List<T> items, Action<RadialMenuElement, T> configureElement)
        {
            SetupMenu(items.ToArray(), configureElement);
        }

        public bool TrySelectHovered()
        {
            if (!_hoveredElement) return false;
            if (!_elementToItem.TryGetValue(_hoveredElement, out T item)) return false;
            OnElementSelected?.Invoke(_hoveredElement, item);
            return true;
        }

        public void OpenMenu()
        {
            if (!_isOpen) OnMenuOpened?.Invoke();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            _isOpen = true;
        }

        public void CloseMenu()
        {
            if (_isOpen) OnMenuClosed?.Invoke();
            UnhoverElement();
            ClearMenu();
            _accumulatedMouseDelta = Vector2.zero;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            selectedItemText.text = "";
            _isOpen = false;
        }
    }
}
