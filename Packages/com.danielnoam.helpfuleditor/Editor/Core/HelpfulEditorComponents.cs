using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Reading and writing a component's enabled state.
    ///
    /// There is no interface for this: Behaviour, Renderer and Collider each declare their own
    /// 'enabled' and share no base that has one, so it can only be found by name per type. That is
    /// subtle enough to be worth having in exactly one place — it was written three times before
    /// this, with the flags maintained separately in each.
    /// </summary>
    internal static class HelpfulEditorComponents
    {
        // FlattenHierarchy is what finds the property on the base that actually declares it, rather
        // than only on the concrete component type.
        private const BindingFlags EnabledFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;

        // Null is cached as readily as a hit: "this type has no enabled state" is the answer for
        // Transform and every plain MonoBehaviour without one, and it is the common case.
        private static readonly Dictionary<Type, PropertyInfo> EnabledProperties = new Dictionary<Type, PropertyInfo>();

        /// <summary>Whether the component has an enabled state to show or change at all.</summary>
        public static bool CanToggle(Component component) => EnabledPropertyOf(component) != null;

        /// <summary>
        /// Anything without an enabled state counts as enabled — Transform being the obvious case.
        /// It has no off state, so reporting it as disabled would draw it greyed for no reason.
        /// </summary>
        public static bool IsEnabled(Component component)
        {
            if (!component) return false;

            PropertyInfo property = EnabledPropertyOf(component);
            if (property == null) return true;

            try
            {
                return (bool)property.GetValue(component);
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>Flips the enabled state, recording undo. Returns false when the component has none.</summary>
        public static bool Toggle(Component component, string undoName = "Toggle Component Enabled")
        {
            if (!component) return false;

            PropertyInfo property = EnabledPropertyOf(component);
            if (property == null || !property.CanWrite) return false;

            try
            {
                bool enabled = (bool)property.GetValue(component);

                Undo.RecordObject(component, undoName);
                property.SetValue(component, !enabled);
                EditorUtility.SetDirty(component);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HelpfulEditor] Could not toggle {component.GetType().Name}.enabled: {e.Message}", component);
                return false;
            }
        }

        private static PropertyInfo EnabledPropertyOf(Component component)
        {
            if (!component) return null;

            Type type = component.GetType();

            if (EnabledProperties.TryGetValue(type, out PropertyInfo cached)) return cached;

            PropertyInfo property = HelpfulEditorMembers.Property(type, "enabled", EnabledFlags);

            // A different member that happens to be called "enabled" is not the one meant here.
            if (property != null && (property.PropertyType != typeof(bool) || !property.CanRead)) property = null;

            EnabledProperties[type] = property;
            return property;
        }
    }
}
