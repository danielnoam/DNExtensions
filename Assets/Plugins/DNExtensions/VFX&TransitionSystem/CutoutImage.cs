using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;


namespace DNExtensions.VFXManager
{
    
    public class CutoutImage : Image
    {
        private static readonly int StencilComp = Shader.PropertyToID("_StencilComp");

        public override Material materialForRendering
        {
            get
            {
                // if (!maskable) return base.materialForRendering;
                
                Material forRendering = new Material(base.materialForRendering);
                forRendering.SetFloat(StencilComp, (float)CompareFunction.NotEqual);
                
                return forRendering;
            }
        }
    }

}