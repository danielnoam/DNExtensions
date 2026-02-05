

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
                selectables?.EnableMouseHoverSelection();
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

            if (rememberPreviousSelection && lastSelectedObject && lastSelectedObject.activeInHierarchy)
            {
                if (lastSelectedObject.TryGetComponent(out Selectable selectable))
                {
                    selectable.SetSelected();
                    return;
                }
            }

            if (defaultSelectable)
            {
                defaultSelectable.SetSelected();
                return;
            }

            SelectFirstAvailable();
        }

        private void SelectFirstAvailable()
        {
            foreach (var selectable in selectables)
            {
                if (selectable)
                {
                    selectable.SetSelected();
                    return;
                }
            }
        }

        private bool IsNavigationInputPressed()
        {
            var inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            if (!inputModule) return false;

            var moveAction = inputModule.move?.action;
            if (moveAction == null) return false;

            return moveAction.ReadValue<Vector2>() != Vector2.zero;
        }



        private IEnumerator SelectDefaultDelayed()
        {
            yield return null;
            SelectDefault();
        }
    }
}