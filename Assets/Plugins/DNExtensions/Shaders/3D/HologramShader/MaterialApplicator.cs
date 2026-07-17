using DNExtensions.Utilities.Button;
using UnityEngine;

namespace ProjectWallE
{
    public sealed class MaterialApplicator : MonoBehaviour
    {
        [SerializeField] private Material material;

        [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
        public void Apply()
        {
            if (!material)
            {
                Debug.LogError("No material assigned.", this);
                return;
            }

            var renderers = GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogError("No renderers found.", this);
                return;
            }

            foreach (var r in renderers)
                r.material = material;
        }
    }
}