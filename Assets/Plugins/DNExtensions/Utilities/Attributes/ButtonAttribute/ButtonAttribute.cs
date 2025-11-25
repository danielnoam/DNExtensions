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
            this.Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, string name = "")
        {
            this.Height = height;
            this.Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="space">Space above the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(int height, int space, string name = "")
        {
            this.Height = height;
            this.Space = space;
            this.Name = name;
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
            this.Height = height;
            this.Space = space;
            this.Color = color;
            this.Name = name;
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
            this.Height = height;
            this.Space = space;
            this.Color = color;
            this.PlayMode = playMode;
            this.Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector with specific play mode restriction
        /// </summary>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(ButtonPlayMode playMode, string name = "")
        {
            this.PlayMode = playMode;
            this.Name = name;
        }

        // GROUP CONSTRUCTORS - Group first, name last (optional)
        
        /// <summary>
        /// Adds a button for the method in the inspector with group support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, string name = "")
        {
            this.Group = group;
            this.Name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group and play mode support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="playMode">When the button should be enabled (play mode, edit mode, or both)</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, ButtonPlayMode playMode, string name = "")
        {
            this.Group = group;
            this.PlayMode = playMode;
            this.Name = name;
        }

        /// <summary>
        /// Adds a button for the method in the inspector with group and height support
        /// </summary>
        /// <param name="group">Group name to organize buttons together</param>
        /// <param name="height">Height of the button in pixels</param>
        /// <param name="name">Display name for the button (uses method name if not specified)</param>
        public ButtonAttribute(string group, int height, string name = "")
        {
            this.Group = group;
            this.Height = height;
            this.Name = name;
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
            this.Group = group;
            this.Height = height;
            this.Space = space;
            this.Name = name;
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
            this.Group = group;
            this.Height = height;
            this.Space = space;
            this.Color = color;
            this.Name = name;
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
            this.Group = group;
            this.Height = height;
            this.Space = space;
            this.Color = color;
            this.PlayMode = playMode;
            this.Name = name;
        }
    }
}