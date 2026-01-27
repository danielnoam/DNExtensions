using UnityEngine;
using System;

namespace DNExtensions.Utilities.Button
{
    public enum ButtonPlayMode
    {
        UseDefault = -1,
        Both = 0,
        OnlyWhenPlaying = 1,
        OnlyWhenNotPlaying = 2
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonAttribute : Attribute 
    {

        public string Name { get; private set; } = null;
        public int Height { get; private set; } = -1; 
        public int Space { get; private set; } = -1;
        public ButtonPlayMode PlayMode { get; private set; } = ButtonPlayMode.UseDefault;
        public string Group { get; private set; } = null;
        public Color Color { get; set; } = Color.clear; 

        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute() {}
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string name)
        {
            Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, string name = null)
        {
            Height = height;
            Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, int space, string name = null)
        {
            Height = height;
            Space = space;
            Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="color">Background color of the button</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, int space, Color color, string name = null)
        {
            Height = height;
            Space = space;
            Color = color;
            Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="color">Background color of the button</param>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, int space, Color color, ButtonPlayMode playMode, string name = null)
        {
            Height = height;
            Space = space;
            Color = color;
            PlayMode = playMode;
            Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector with specific play mode restriction
        /// </summary>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(ButtonPlayMode playMode, string name = null)
        {
            PlayMode = playMode;
            Name = name;
        }

        // GROUP CONSTRUCTORS - Group first, name last (optional)
        
        /// <summary>
        /// Adds a button for the method in the inspector with group support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, string name = null)
        {
            Group = group;
            Name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group and play mode support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, ButtonPlayMode playMode, string name = null)
        {
            Group = group;
            PlayMode = playMode;
            Name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group and height support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, string name = null)
        {
            Group = group;
            Height = height;
            Name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group, height and space support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, int space, string name = null)
        {
            Group = group;
            Height = height;
            Space = space;
            Name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group, height, space and color support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="color">Background color of the button</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, int space, Color color, string name = null)
        {
            Group = group;
            Height = height;
            Space = space;
            Color = color;
            Name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with full customization and group support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="color">Background color of the button</param>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, int space, Color color, ButtonPlayMode playMode, string name = null)
        {
            Group = group;
            Height = height;
            Space = space;
            Color = color;
            PlayMode = playMode;
            Name = name;
        }
    }
}