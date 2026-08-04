using System;
using UnityEngine;

namespace DNExtensions.Utilities.Inline
{
    /// <summary>
    /// Displays any Unity Object's properties inline in the inspector with a foldout.
    /// Works on ScriptableObjects, Prefabs, GameObjects, Components, and any Object reference.
    /// </summary>
    /// <example>
    /// <code>
    /// [Inline]
    /// [SerializeField] private EnemyConfig enemyConfig;
    /// 
    /// [Inline]
    /// [SerializeField] private GameObject enemyPrefab;
    /// 
    /// [Inline]
    /// [SerializeField] private Rigidbody targetRigidbody;
    /// 
    /// [InlineSO(DrawBox = false)]
    /// [SerializeField] private WeaponData weaponData;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class InlineAttribute : PropertyAttribute
    {
        /// <summary>
        /// Draw a box around the expanded properties. Default is true.
        /// </summary>
        public bool DrawBox { get; set; } = true;
    }
}