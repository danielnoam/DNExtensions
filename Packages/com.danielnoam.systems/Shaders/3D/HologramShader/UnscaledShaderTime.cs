using UnityEngine;

namespace ProjectWallE
{
    public class UnscaledShaderTime : MonoBehaviour
    {
        private static readonly int UnscaledTime = Shader.PropertyToID("_UnscaledTime");

        private void Update()
        {
            Shader.SetGlobalFloat(UnscaledTime, Time.unscaledTime);
        }
    }
}
