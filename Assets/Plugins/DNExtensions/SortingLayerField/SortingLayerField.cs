using UnityEngine;


namespace DNExtensions
{
    /// <summary>
    /// A serializable field that provides Unity Inspector support for selecting sorting layers.
    /// Use this as a field type to get a dropdown selector with layer validation in the inspector.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyRenderer : MonoBehaviour
    /// {
    ///     [SerializeField] private SortingLayerField sortingLayer;
    ///     
    ///     void Start()
    ///     {
    ///         GetComponent&lt;Renderer&gt;().sortingLayerID = sortingLayer;
    ///     }
    /// }
    /// </code>
    /// </example>
    [System.Serializable]
    public class SortingLayerField
    {
        /// <summary>
        /// The name of the selected sorting layer.
        /// </summary>
        [SerializeField]
        private string layerName = "Default";

        /// <summary>
        /// The unique ID of the selected sorting layer.
        /// </summary>
        [SerializeField]
        private int layerID;

        /// <summary>
        /// Gets the name of the currently selected sorting layer.
        /// </summary>
        public string LayerName => layerName;

        /// <summary>
        /// Gets the ID of the currently selected sorting layer.
        /// </summary>
        public int LayerID => layerID;

        /// <summary>
        /// Validates that the stored layer still exists and updates the ID if the layer was reordered.
        /// </summary>
        /// <returns>True if the layer exists and is valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(layerName)) return false;
            
            var layers = SortingLayer.layers;
            foreach (var sortingLayer in layers)
            {
                if (sortingLayer.name == layerName)
                {
                    layerID = sortingLayer.id;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the sorting layer by name. Updates both name and ID.
        /// </summary>
        /// <param name="name">The name of the sorting layer to set</param>
        /// <returns>True if the layer was found and set, false otherwise</returns>
        public bool SetLayer(string name)
        {
            var layers = SortingLayer.layers;
            foreach (var sortingLayer in layers)
            {
                if (sortingLayer.name == name)
                {
                    layerName = name;
                    layerID = sortingLayer.id;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Implicit conversion operator that allows SortingLayerField to be used directly as an int (layer ID).
        /// Enables seamless integration with Unity's rendering APIs that expect sorting layer IDs.
        /// </summary>
        /// <param name="sortingField">The SortingLayerField to convert</param>
        /// <returns>The sorting layer ID as an integer</returns>
        public static implicit operator int(SortingLayerField sortingField)
        {
            return sortingField?.layerID ?? 0;
        }

        /// <summary>
        /// Implicit conversion operator that allows SortingLayerField to be used directly as a string (layer name).
        /// </summary>
        /// <param name="sortingField">The SortingLayerField to convert</param>
        /// <returns>The sorting layer name as a string</returns>
        public static implicit operator string(SortingLayerField sortingField)
        {
            return sortingField?.layerName ?? "";
        }
    }
}