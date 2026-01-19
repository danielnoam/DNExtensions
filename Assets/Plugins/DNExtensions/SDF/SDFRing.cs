namespace DNExtensions.Shapes
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("UI/SDF Shapes/Ring", 16)]
    public class SDFRing : SDFShapeBase
    {
        private static readonly int OuterRadiusID = Shader.PropertyToID("_Outer_Radius");
        private static readonly int InnerRadiusID = Shader.PropertyToID("_Inner_Radius");

        [SerializeField, Range(0f, 0.5f)] private float m_OuterRadius = 0.2f;
        [SerializeField, Range(0f, 0.5f)] private float m_InnerRadius = 0.06f;

        protected override string GetShapeKeyword()
        {
            return "_SHAPE_RING";
        }

        protected override void SetShapeProperties()
        {
            m_InstanceMaterial.SetFloat(OuterRadiusID, m_OuterRadius);
            m_InstanceMaterial.SetFloat(InnerRadiusID, m_InnerRadius);
        }

        public float outerRadius
        {
            get { return m_OuterRadius; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (!Mathf.Approximately(m_OuterRadius, value))
                {
                    m_OuterRadius = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public float innerRadius
        {
            get { return m_InnerRadius; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (!Mathf.Approximately(m_InnerRadius, value))
                {
                    m_InnerRadius = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        protected override void CopyPropertiesTo(SDFShapeBase target)
        {
            base.CopyPropertiesTo(target);
            if (target is SDFRing ring)
            {
                ring.m_OuterRadius = this.m_OuterRadius;
                ring.m_InnerRadius = this.m_InnerRadius;
            }
        }
    }
}