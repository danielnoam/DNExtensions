using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Utility methods for working with UI Selectables and EventTriggers
    /// </summary>
    public static class SelectableUtilities
    {

        /// <summary>
        /// Gets the first interactable Selectable in children
        /// </summary>
        public static Selectable GetFirstInteractableSelectable(this GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<Selectable>()
                .FirstOrDefault(s => s.isActiveAndEnabled && s.interactable);
        }

        /// <summary>
        /// Adds an event listener to a Selectable for the specified EventTriggerType
        /// </summary>
        public static void AddEventListener(this Selectable selectable, EventTriggerType eventType, UnityAction<BaseEventData> callback)
        {
            var eventTrigger = selectable.GetOrAddComponent<EventTrigger>();
            eventTrigger.AddEventListener(eventType, callback);
        }

        /// <summary>
        /// Adds an event listener to an EventTrigger for the specified EventTriggerType
        /// </summary>
        public static void AddEventListener(this EventTrigger eventTrigger, EventTriggerType eventType, UnityAction<BaseEventData> callback)
        {
            var entry = eventTrigger.triggers.FirstOrDefault(e => e.eventID == eventType);

            if (entry != null)
            {
                entry.callback.AddListener(callback);
            }
            else
            {
                entry = new EventTrigger.Entry { eventID = eventType };
                entry.callback.AddListener(callback);
                eventTrigger.triggers.Add(entry);
            }
        }

        /// <summary>
        /// Removes an event listener from a Selectable for the specified EventTriggerType
        /// </summary>
        public static void RemoveEventListener(this Selectable selectable, EventTriggerType eventType, UnityAction<BaseEventData> callback)
        {
            var eventTrigger = selectable.GetComponent<EventTrigger>();
            if (!eventTrigger) return;

            var entry = eventTrigger.triggers.FirstOrDefault(e => e.eventID == eventType);
            entry?.callback.RemoveListener(callback);
        }

        /// <summary>
        /// Adds Select event listener to a Selectable
        /// </summary>
        public static void OnSelect(this Selectable selectable, UnityAction<BaseEventData> callback)
        {
            selectable.AddEventListener(EventTriggerType.Select, callback);
        }

        /// <summary>
        /// Adds Deselect event listener to a Selectable
        /// </summary>
        public static void OnDeselect(this Selectable selectable, UnityAction<BaseEventData> callback)
        {
            selectable.AddEventListener(EventTriggerType.Deselect, callback);
        }

        /// <summary>
        /// Adds Submit event listener to a Selectable
        /// </summary>
        public static void OnSubmit(this Selectable selectable, UnityAction<BaseEventData> callback)
        {
            selectable.AddEventListener(EventTriggerType.Submit, callback);
        }

        /// <summary>
        /// Adds PointerEnter event listener to a Selectable
        /// </summary>
        public static void OnPointerEnter(this Selectable selectable, UnityAction<BaseEventData> callback)
        {
            selectable.AddEventListener(EventTriggerType.PointerEnter, callback);
        }

        /// <summary>
        /// Adds PointerExit event listener to a Selectable
        /// </summary>
        public static void OnPointerExit(this Selectable selectable, UnityAction<BaseEventData> callback)
        {
            selectable.AddEventListener(EventTriggerType.PointerExit, callback);
        }

        /// <summary>
        /// Adds PointerClick event listener to a Selectable
        /// </summary>
        public static void OnPointerClick(this Selectable selectable, UnityAction<BaseEventData> callback)
        {
            selectable.AddEventListener(EventTriggerType.PointerClick, callback);
        }

        /// <summary>
        /// Enables mouse hover selection for all Selectables
        /// </summary>
        public static void EnableMouseHoverSelection(this IEnumerable<Selectable> selectables)
        {
            foreach (var selectable in selectables)
            {
                selectable.EnableMouseHoverSelection();
            }
        }

        /// <summary>
        /// Enables mouse hover selection for a single Selectable
        /// </summary>
        public static void EnableMouseHoverSelection(this Selectable selectable)
        {
            selectable.OnPointerEnter(data =>
            {
                if (selectable.interactable && EventSystem.current)
                {
                    EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                }
            });
        }

        /// <summary>
        /// Sets up navigation for a list of Selectables in order
        /// </summary>
        public static void SetupLinearNavigation(this List<Selectable> selectables, bool wrap = true)
        {
            if (selectables == null || selectables.Count == 0) return;

            for (int i = 0; i < selectables.Count; i++)
            {
                var nav = selectables[i].navigation;
                nav.mode = Navigation.Mode.Explicit;

                if (i > 0)
                    nav.selectOnUp = selectables[i - 1];
                else if (wrap)
                    nav.selectOnUp = selectables[^1];

                if (i < selectables.Count - 1)
                    nav.selectOnDown = selectables[i + 1];
                else if (wrap)
                    nav.selectOnDown = selectables[0];

                selectables[i].navigation = nav;
            }
        }

        /// <summary>
        /// Sets up grid navigation for a list of Selectables
        /// </summary>
        public static void SetupGridNavigation(this List<Selectable> selectables, int columns, bool wrap = false)
        {
            if (selectables == null || selectables.Count == 0) return;

            int rows = Mathf.CeilToInt((float)selectables.Count / columns);

            for (int i = 0; i < selectables.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                var nav = selectables[i].navigation;
                nav.mode = Navigation.Mode.Explicit;

                int upIndex = i - columns;
                if (upIndex >= 0)
                    nav.selectOnUp = selectables[upIndex];
                else if (wrap)
                    nav.selectOnUp = selectables[i + columns * (rows - 1)];

                int downIndex = i + columns;
                if (downIndex < selectables.Count)
                    nav.selectOnDown = selectables[downIndex];
                else if (wrap && col < selectables.Count % columns)
                    nav.selectOnDown = selectables[col];

                int leftIndex = i - 1;
                if (col > 0)
                    nav.selectOnLeft = selectables[leftIndex];
                else if (wrap)
                    nav.selectOnLeft = selectables[i + columns - 1];

                int rightIndex = i + 1;
                if (col < columns - 1 && rightIndex < selectables.Count)
                    nav.selectOnRight = selectables[rightIndex];
                else if (wrap && row < rows - 1)
                    nav.selectOnRight = selectables[row * columns];

                selectables[i].navigation = nav;
            }
        }

        /// <summary>
        /// Clears all navigation from a Selectable
        /// </summary>
        public static void ClearNavigation(this Selectable selectable)
        {
            var nav = new Navigation { mode = Navigation.Mode.None };
            selectable.navigation = nav;
        }

        /// <summary>
        /// Checks if a Selectable is currently selected in the EventSystem
        /// </summary>
        public static bool IsSelected(this Selectable selectable)
        {
            return EventSystem.current && EventSystem.current.currentSelectedGameObject == selectable.gameObject;
        }

        /// <summary>
        /// Selects this Selectable in the EventSystem
        /// </summary>
        public static void SetSelected(this Selectable selectable)
        {
            if (EventSystem.current && selectable.isActiveAndEnabled && selectable.interactable)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }
    }
}