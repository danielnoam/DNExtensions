using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class MaterialExtensions
    {
        #region ZWrite Control

        /// <summary>
        /// Enables ZWrite (allows material to write to the Z buffer)
        /// </summary>
        public static void EnableZWrite(this Material material)
        {
            if (!material) return;
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>
        /// Disables ZWrite (prevents material from writing to the Z buffer)
        /// </summary>
        public static void DisableZWrite(this Material material)
        {
            if (!material) return;
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
        }

        #endregion

        #region Color

        /// <summary>
        /// Sets the alpha of the material color
        /// </summary>
        public static void SetAlpha(this Material material, float alpha)
        {
            if (!material || !material.HasProperty("_Color")) return;

            Color color = material.color;
            color.a = alpha;
            material.color = color;
        }

        /// <summary>
        /// Fades the material to a target alpha over time
        /// </summary>
        public static void FadeTo(this Material material, float targetAlpha, float speed)
        {
            if (!material || !material.HasProperty("_Color")) return;

            Color color = material.color;
            color.a = Mathf.Lerp(color.a, targetAlpha, speed * Time.deltaTime);
            material.color = color;
        }

        #endregion

        #region Property Checks

        /// <summary>
        /// Checks if the material has a property and sets it if it exists
        /// </summary>
        public static bool TrySetFloat(this Material material, string propertyName, float value)
        {
            if (!material) return false;
            if (!material.HasProperty(propertyName)) return false;

            material.SetFloat(propertyName, value);
            return true;
        }

        /// <summary>
        /// Checks if the material has a property and sets it if it exists
        /// </summary>
        public static bool TrySetColor(this Material material, string propertyName, Color value)
        {
            if (!material) return false;
            if (!material.HasProperty(propertyName)) return false;

            material.SetColor(propertyName, value);
            return true;
        }

        /// <summary>
        /// Checks if the material has a property and sets it if it exists
        /// </summary>
        public static bool TrySetTexture(this Material material, string propertyName, Texture value)
        {
            if (!material) return false;
            if (!material.HasProperty(propertyName)) return false;

            material.SetTexture(propertyName, value);
            return true;
        }

        /// <summary>
        /// Checks if the material has a property and sets it if it exists
        /// </summary>
        public static bool TrySetVector(this Material material, string propertyName, Vector4 value)
        {
            if (!material) return false;
            if (!material.HasProperty(propertyName)) return false;

            material.SetVector(propertyName, value);
            return true;
        }

        #endregion

        #region Shader Keywords

        /// <summary>
        /// Toggles a shader keyword on or off
        /// </summary>
        public static void ToggleKeyword(this Material material, string keyword, bool enabled)
        {
            if (!material) return;

            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        /// <summary>
        /// Checks if a shader keyword is enabled
        /// </summary>
        public static bool IsKeywordEnabled(this Material material, string keyword)
        {
            if (!material) return false;
            return material.IsKeywordEnabled(keyword);
        }

        #endregion

        #region Render Queue

        /// <summary>
        /// Sets the render queue to a predefined value
        /// </summary>
        public static void SetRenderQueue(this Material material, UnityEngine.Rendering.RenderQueue queue)
        {
            if (!material) return;
            material.renderQueue = (int)queue;
        }

        /// <summary>
        /// Resets the render queue to the shader's default
        /// </summary>
        public static void ResetRenderQueue(this Material material)
        {
            if (!material) return;
            material.renderQueue = -1;
        }

        #endregion

        #region Tiling and Offset

        /// <summary>
        /// Sets the texture tiling
        /// </summary>
        public static void SetTiling(this Material material, string propertyName, Vector2 tiling)
        {
            if (!material || !material.HasProperty(propertyName)) return;
            material.SetTextureScale(propertyName, tiling);
        }

        /// <summary>
        /// Sets the texture offset
        /// </summary>
        public static void SetOffset(this Material material, string propertyName, Vector2 offset)
        {
            if (!material || !material.HasProperty(propertyName)) return;
            material.SetTextureOffset(propertyName, offset);
        }

        /// <summary>
        /// Sets both tiling and offset
        /// </summary>
        public static void SetTilingAndOffset(this Material material, string propertyName, Vector2 tiling, Vector2 offset)
        {
            if (!material || !material.HasProperty(propertyName)) return;
            material.SetTextureScale(propertyName, tiling);
            material.SetTextureOffset(propertyName, offset);
        }

        #endregion

        #region Instance Check

        /// <summary>
        /// Checks if this material is an instance (has "(Instance)" in its name)
        /// </summary>
        public static bool IsInstance(this Material material)
        {
            if (!material) return false;
            return material.name.EndsWith("(Instance)");
        }

        #endregion
    }
}