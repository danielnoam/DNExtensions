using UnityEngine;
using System;

namespace DNExtensions.Button
{
    public enum ButtonPlayMode
    {
        Both,
        OnlyWhenPlaying,
        OnlyWhenNotPlaying
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute 
    {
        public readonly string Name = "";
        public readonly int Height = 30;
        public readonly int Space = 3;
        public readonly ButtonPlayMode PlayMode = ButtonPlayMode.Both;
        public readonly string Group = "";
        public Color Color = Color.white;

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
        public ButtonAttribute(int height, string name = "")
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
        public ButtonAttribute(int height, int space, string name = "")
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
        public ButtonAttribute(int height, int space, Color color, string name = "")
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
        public ButtonAttribute(int height, int space, Color color, ButtonPlayMode playMode, string name = "")
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
        public ButtonAttribute(ButtonPlayMode playMode, string name = "")
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
        public ButtonAttribute(string group, string name = "")
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
        public ButtonAttribute(string group, ButtonPlayMode playMode, string name = "")
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
        public ButtonAttribute(string group, int height, string name = "")
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
        public ButtonAttribute(string group, int height, int space, string name = "")
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
        public ButtonAttribute(string group, int height, int space, Color color, string name = "")
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
        public ButtonAttribute(string group, int height, int space, Color color, ButtonPlayMode playMode, string name = "")
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