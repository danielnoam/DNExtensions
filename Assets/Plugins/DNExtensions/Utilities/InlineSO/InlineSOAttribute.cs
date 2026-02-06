using System;
using UnityEngine;

namespace DNExtensions.Utilities.InlineSO
{
    /// <summary>
    /// Displays a ScriptableObject's properties inline in the inspector with a foldout.
    /// Only works on ScriptableObject fields.
    /// </summary>
    /// <example>
    /// <code>
    /// [InlineSO]
    /// [SerializeField] private EnemyConfig enemyConfig;
    /// 
    /// [InlineSO(DrawBox = false)]
    /// [SerializeField] private WeaponData weaponData;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class InlineSOAttribute : PropertyAttribute
    {
        /// <summary>
        /// Draw a box around the expanded properties.
        /// </summary>
        public bool DrawBox { get; set; } = true;
    }
}