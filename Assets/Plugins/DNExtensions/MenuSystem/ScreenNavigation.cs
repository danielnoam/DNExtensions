using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions.Utilities;
using DNExtensions.Utilities.CustomFields;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DNExtensions.MenuSystem
{
    
    [DisallowMultipleComponent]
    public class ScreenNavigation : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoSelectOnEnable = true;
        [SerializeField] private bool autoSelectOnNavigate = true;
        [SerializeField] private bool rememberPreviousSelection = true;
        [SerializeField] private bool enableOnPointerEnterSelection = true;
        [SerializeField] private bool enableOnPointerExitDeselection = true;
        [SerializeField] private OptionalField<Selectable> overrideDefaultSelectable = new OptionalField<Selectable>(false,true);


        private List<Selectable> _selectables;
        private GameObject _lastSelectedObject;
        private Selectable _defaultSelectable;


        private void Awake()
        {
            SetUpSelectables();
        }
        

        private void Update()
        {
            if (!autoSelectOnNavigate || !EventSystem.current || EventSystem.current.currentSelectedGameObject) return;

            if (IsNavigationInputPressed())
            {
                SelectDefault();
            }
        }

        private void OnEnable()
        {
            if (autoSelectOnEnable && EventSystem.current  && !EventSystem.current.currentSelectedGameObject)
            {
                SelectDefault();
            }
        }

        private void OnDisable()
        {
            if (rememberPreviousSelection && EventSystem.current)
            {
                var currentSelected = EventSystem.current.currentSelectedGameObject;
                if (currentSelected && _selectables.Any(s => s && s.gameObject == currentSelected))
                {
                    _lastSelectedObject = currentSelected;
                }
            }
        }
        
        public void SetUpSelectables()
        {
            _selectables = new List<Selectable>(GetComponentsInChildren<Selectable>(true));

            if (_selectables.Count == 0)
            {
                Debug.LogWarning($"ScreenNavigation on {gameObject.name} found no Selectables.");
                return;
            }

            _defaultSelectable = overrideDefaultSelectable ? overrideDefaultSelectable.Value : _selectables[0];

            if (enableOnPointerEnterSelection)
            {
                _selectables?.EnableOnPointerEnterSelection();
            }

            if (enableOnPointerExitDeselection)
            {
                _selectables?.EnableOnPointerExitDeselection();
            }
        }

        private void SelectDefault()
        {
            if (rememberPreviousSelection && _lastSelectedObject && _lastSelectedObject.activeInHierarchy)
            {
                if (_lastSelectedObject.TryGetComponent(out Selectable selectable))
                {
                    selectable.SetSelected();
                    return;
                }
            }

            
            if (_defaultSelectable)
            {
                _defaultSelectable.SetSelected();
                return;
            }
            
            
            foreach (var selectable in _selectables)
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
        
    }
}