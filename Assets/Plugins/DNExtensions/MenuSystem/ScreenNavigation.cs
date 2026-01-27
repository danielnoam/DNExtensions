

namespace DNExtenstions.MenuSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using DNExtensions.Utilities;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem.UI;
    using UnityEngine.UI;

    [DisallowMultipleComponent]
    public class ScreenNavigation : MonoBehaviour
    {
        [Header("Selection Settings")]
        [SerializeField] private bool autoSelectOnEnable = true;
        [SerializeField] private bool autoSelectOnNavigate = true;
        [SerializeField] private bool rememberPreviousSelection = true;
        [SerializeField] private bool enableMouseHoverSelection = true;
        [SerializeField] private Selectable defaultSelectable;


        private List<Selectable> selectables;
        private GameObject lastSelectedObject;


        private void Awake()
        {
            selectables = new List<Selectable>(GetComponentsInChildren<Selectable>(true));

            if (selectables.Count == 0)
            {
                Debug.LogWarning($"ScreenNavigation on {gameObject.name} found no Selectables.");
                return;
            }

            if (!defaultSelectable)
            {
                defaultSelectable = selectables[0];
            }

            if (enableMouseHoverSelection)
            {
                SetupMouseHoverSelection();
            }
        }

        private void Update()
        {
            if (!autoSelectOnNavigate || !EventSystem.current) return;
            if (EventSystem.current.currentSelectedGameObject) return;

            if (IsNavigationInputPressed())
            {
                SelectDefault();
            }
        }

        private void OnEnable()
        {
            if (autoSelectOnEnable && selectables is { Count: > 0 })
            {
                StartCoroutine(SelectDefaultDelayed());
            }
        }

        private void OnDisable()
        {
            if (rememberPreviousSelection && EventSystem.current)
            {
                var currentSelected = EventSystem.current.currentSelectedGameObject;
                if (currentSelected && selectables.Any(s => s && s.gameObject == currentSelected))
                {
                    lastSelectedObject = currentSelected;
                }
            }
        }

        public void SelectDefault()
        {
            if (!EventSystem.current) return;

            if (rememberPreviousSelection && lastSelectedObject && lastSelectedObject.activeInHierarchy)
            {
                var selectable = lastSelectedObject.GetComponent<Selectable>();
                if (selectable && selectable.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(lastSelectedObject);
                    return;
                }
            }

            if (defaultSelectable && defaultSelectable.isActiveAndEnabled && defaultSelectable.interactable)
            {
                EventSystem.current.SetSelectedGameObject(defaultSelectable.gameObject);
                return;
            }

            SelectFirstAvailable();
        }

        private void SelectFirstAvailable()
        {
            if (!EventSystem.current) return;

            foreach (var selectable in selectables)
            {
                if (selectable && selectable.isActiveAndEnabled && selectable.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                    return;
                }
            }
        }

        private bool IsNavigationInputPressed()
        {
            var inputModule =
                EventSystem.current.currentInputModule as InputSystemUIInputModule;
            if (!inputModule) return false;

            var moveAction = inputModule.move?.action;
            if (moveAction == null) return false;

            return moveAction.ReadValue<Vector2>() != Vector2.zero;
        }

        private void SetupMouseHoverSelection()
        {
            foreach (var selectable in selectables)
            {
                if (!selectable) continue;

                var trigger = selectable.GetOrAddComponent<EventTrigger>();


                if (trigger.triggers.All(e => e.eventID != EventTriggerType.PointerEnter))
                {
                    var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    entry.callback.AddListener((data) =>
                    {
                        if (selectable && selectable.interactable && EventSystem.current)
                        {
                            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                        }
                    });
                    trigger.triggers.Add(entry);
                }
            }
        }

        private IEnumerator SelectDefaultDelayed()
        {
            yield return null;
            SelectDefault();
        }
    }
}