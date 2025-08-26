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
        public readonly int Size = 30;
        public readonly int Space = 3;
        public readonly ButtonPlayMode PlayMode = ButtonPlayMode.Both;
        public Color Color = Color.white;

        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute() {}
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute(string name)
        {
            this.Name = name;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute(string name, int size)
        {
            this.Name = name;
            this.Size = size;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute(string name, int size, int space)
        {
            this.Name = name;
            this.Size = size;
            this.Space = space;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute(string name, int size, int space, Color color)
        {
            this.Name = name;
            this.Size = size;
            this.Space = space;
            this.Color = color;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute(string name, int size, int space, Color color, ButtonPlayMode playMode)
        {
            this.Name = name;
            this.Size = size;
            this.Space = space;
            this.Color = color;
            this.PlayMode = playMode;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector with specific play mode restriction
        /// </summary>
        public ButtonAttribute(ButtonPlayMode playMode)
        {
            this.PlayMode = playMode;
        }
        
        /// <summary>
        /// Adds a button for the method in the inspector
        /// </summary>
        public ButtonAttribute(string name, ButtonPlayMode playMode)
        {
            this.Name = name;
            this.PlayMode = playMode;
        }
    }

}