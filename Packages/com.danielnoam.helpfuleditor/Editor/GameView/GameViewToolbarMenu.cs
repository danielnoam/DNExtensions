using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// The two things every Game View toolbar item's right-click menu needs: entries that grey out
    /// while the feature is mid-job rather than disappearing, and labels that match what the settings
    /// page shows.
    /// </summary>
    internal static class GameViewToolbarMenu
    {
        /// <summary>
        /// An entry that is greyed rather than absent while the feature is busy, so the settings can
        /// still be read off mid-job — the tick still says what the running one is using.
        /// </summary>
        public static void Entry(GenericMenu menu, string path, bool on, bool locked, GenericMenu.MenuFunction chosen)
        {
            GUIContent label = new GUIContent(path);

            if (locked) menu.AddDisabledItem(label, on);
            else menu.AddItem(label, on, chosen);
        }

        /// <summary>
        /// The same text the settings popups show, read off the enum's own InspectorName so a menu and
        /// the settings page cannot drift apart. Falls back to the plain name where there is no
        /// attribute, which is what an enum whose members already read well will do.
        /// </summary>
        public static string Label<T>(T value) where T : Enum
        {
            System.Reflection.MemberInfo[] member = typeof(T).GetMember(value.ToString());

            if (member.Length > 0 &&
                Attribute.GetCustomAttribute(member[0], typeof(InspectorNameAttribute)) is InspectorNameAttribute named)
            {
                return named.displayName;
            }

            return value.ToString();
        }
    }
}
