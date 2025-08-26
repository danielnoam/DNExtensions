using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.InputSystem;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine.Events;

namespace  DNExtensions.MenuSystem
{
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]

    public class MenuPage : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Will select the previous selectable or if there is non, the first available one")]
        [SerializeField] private bool autoSetSelectableOnPageSelect = true;
        [SerializeField, ShowIf("autoSetSelectableOnPageSelect")] private bool savePreviousSelectableOnPageDeselect = true;
        [SerializeField] private Selectable[] selectables = Array.Empty<Selectable>();
        [SerializeField] private AnimatedPageObject[] animatedObjects = Array.Empty<AnimatedPageObject>();

        [Space(10)] 
        [ReadOnly] public bool canSelect;
        [ReadOnly] public Selectable currentSelectable;
        [ReadOnly] public Selectable previousSelectable;
        [SerializeField, ReadOnly] private MenuController controller;
        [SerializeField, ReadOnly] private MenuCategory category;
        [SerializeField, HideInInspector] private AudioSource audioSource;

        private void OnValidate()
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
            if (!controller) controller = GetComponentInParent<MenuController>();
            if (!category) category = GetComponentInParent<MenuCategory>();

            if (selectables.Length == 0)
            {
                AddAllSelectablesInPage();
            }
        }

        private void Awake()
        {
            if (!controller) controller = GetComponentInParent<MenuController>();
            if (!category) category = GetComponentInParent<MenuCategory>();
            
            if (!category || !controller)
            {
                Debug.LogError("No menu controllers found!");
                return;
            }


            if (selectables.Length > 0)
            {
                foreach (Selectable selectable in selectables)
                {
                    SetupSelectable(selectable);
                }

            }
        }


        public void OnPageSelected(bool animate = true)
        {
            canSelect = true;

            var delay = 0f;
            var totalAnimationTime = 0f;

            if (animatedObjects.Length > 0 && animate)
            {
                totalAnimationTime = animatedObjects.Max(animatedObject => animatedObject.duration);

                foreach (var animatedObject in animatedObjects)
                {
                    animatedObject.Animate(delay);
                    delay += 0.1f;
                }
            }

            if (autoSetSelectableOnPageSelect)
                StartCoroutine(SelectFirstAvailableSelectableCar(0.05f + delay + totalAnimationTime));
        }

        public float OnPageDeselected(bool animate = true)
        {
            if (savePreviousSelectableOnPageDeselect && currentSelectable) 
            {
                previousSelectable = currentSelectable;
            } 
            else if (!savePreviousSelectableOnPageDeselect)
            {
                previousSelectable = null;
            }
            
            currentSelectable = null;
            canSelect = false;

            if (animatedObjects.Length > 0 && animate)
            {
                var totalAnimationTime = animatedObjects.Max(animatedObject => animatedObject.duration);
                var delay = 0f;

                foreach (var animatedObject in animatedObjects)
                {
                    animatedObject.Reverse(delay);
                    delay += 0.1f;
                }

                return totalAnimationTime + delay;
            }

            return 0f;
        }

        public void OnNavigate(InputAction.CallbackContext context)
        {
            if (!canSelect || currentSelectable) return;
            SelectFirstAvailableSelectable();
        }


        private void OnSelectableSelected(BaseEventData eventData)
        {
            if (!canSelect || !eventData.selectedObject.activeSelf) return;

            currentSelectable = eventData.selectedObject.GetComponent<Selectable>();
        }

        private void OnSelectableDeselected(BaseEventData eventData)
        {
            if (!canSelect || !eventData.selectedObject.activeSelf || currentSelectable == null) return;

            previousSelectable = currentSelectable;
            currentSelectable = null;
        }

        private void OnSelectablePointerEnter(BaseEventData eventData)
        {
            if (!canSelect) return;

            if (eventData is PointerEventData pointerEventData)
            {
                pointerEventData.selectedObject = pointerEventData.pointerEnter;
            }
        }

        private void OnSelectablePointerExit(BaseEventData eventData)
        {
            if (!canSelect) return;

            if (eventData is PointerEventData pointerEventData)
            {
                pointerEventData.selectedObject = null;
            }
        }
        


        private void SetupSelectable(Selectable selectable)
        {
            // Add events
            var eventTrigger = selectable.GetComponent<EventTrigger>() ??
                               selectable.gameObject.AddComponent<EventTrigger>();
            AddEventTriggerEntry(eventTrigger, EventTriggerType.Select, OnSelectableSelected);
            AddEventTriggerEntry(eventTrigger, EventTriggerType.Deselect, OnSelectableDeselected);
            AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerEnter, OnSelectablePointerEnter);
            AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerExit, OnSelectablePointerExit);

        }


        private void AddEventTriggerEntry(EventTrigger eventTrigger, EventTriggerType type,
            UnityAction<BaseEventData> callback)
        {
            var existingEntry = eventTrigger.triggers.FirstOrDefault(entry => entry.eventID == type);

            if (existingEntry != null)
            {
                existingEntry.callback.AddListener(callback);
            }
            else
            {
                var newEntry = new EventTrigger.Entry
                {
                    eventID = type,
                    callback = new EventTrigger.TriggerEvent()
                };
                newEntry.callback.AddListener(callback);
                eventTrigger.triggers.Add(newEntry);
            }
        }


        private IEnumerator SelectFirstAvailableSelectableCar(float delay = 0)
        {
            yield return new WaitForSecondsRealtime(delay);
            SelectFirstAvailableSelectable();
        }

        private void SelectFirstAvailableSelectable()
        {
            if (selectables.Length == 0) return;

            if (currentSelectable && currentSelectable.isActiveAndEnabled)
            {
                EventSystem.current.SetSelectedGameObject(currentSelectable.gameObject);

            }
            else if (previousSelectable && previousSelectable.isActiveAndEnabled)
            {
                EventSystem.current.SetSelectedGameObject(previousSelectable.gameObject);

            }
            else
            {
                foreach (var selectable in selectables)
                {
                    if (!selectable.gameObject.activeSelf || !selectable.interactable)
                        continue;
                    EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                    return;
                }
            }
        }





        #region Editor -----------------------------------------------------------------------------------------------------

        [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
        private void AddAllSelectablesInPage()
        {
            AddSelectablesRecursively(transform);
            selectables = selectables.Where(selectable => selectable).ToArray();
        }

        private void AddSelectablesRecursively(Transform parent)
        {
            if (parent.TryGetComponent(out Selectable selectable) && !selectables.Contains(selectable))
            {
                selectables = selectables.Append(selectable).ToArray();
            }

            // Recursively check all children
            foreach (Transform child in parent)
            {
                AddSelectablesRecursively(child);
            }
        }


        #endregion Editor -----------------------------------------------------------------------------------------------------

    }
}