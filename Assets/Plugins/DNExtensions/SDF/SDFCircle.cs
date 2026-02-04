namespace DNExtensions.Shapes
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("UI/SDF Shapes/Circle", 12)]
    public class SDFCircle : SDFShapeBase
    {
        private static readonly int RadiusID = Shader.PropertyToID("_Radius");

        [SerializeField, Range(0f, 0.5f)] private float m_Radius = 0.5f;

        protected override string GetShapeKeyword()
        {
            return "_SHAPE_CIRCLE";
        }

        protected override void SetShapeProperties()
        {
            m_InstanceMaterial.SetFloat(RadiusID, m_Radius);
        }

        public float radius
        {
            get { return m_Radius; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (m_Radius != value)
                {
                    m_Radius = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        protected override void CopyPropertiesTo(SDFShapeBase target)
        {
            base.CopyPropertiesTo(target);
            if (target is SDFCircle circle)
            {
                circle.m_Radius = this.m_Radius;
            }
        }
    }
}