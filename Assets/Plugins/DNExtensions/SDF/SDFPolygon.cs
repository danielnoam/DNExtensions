namespace DNExtensions.Utilities.Shapes
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("UI/SDF Shapes/Polygon", 14)]
    public class SDFPolygon : SDFShapeBase
    {
        private static readonly int SizeID = Shader.PropertyToID("_Size");
        private static readonly int SidesID = Shader.PropertyToID("_Sides");
        private static readonly int InnerRadiusID = Shader.PropertyToID("_Inner_Radius");

        [SerializeField, Range(0f, 0.5f)] private float m_Size = 0.3f;
        [SerializeField, Range(3, 12)] private int m_Sides = 5;
        [SerializeField, Range(0f, 1f)] private float m_InnerRadius;

        protected override string GetShapeKeyword()
        {
            return "_SHAPE_POLYGON";
        }

        protected override void SetShapeProperties()
        {
            m_InstanceMaterial.SetFloat(SizeID, m_Size);
            m_InstanceMaterial.SetInt(SidesID, m_Sides);
            m_InstanceMaterial.SetFloat(InnerRadiusID, m_InnerRadius);
        }

        public float size
        {
            get { return m_Size; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (m_Size != value)
                {
                    m_Size = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public int sides
        {
            get { return m_Sides; }
            set
            {
                value = Mathf.Clamp(value, 3, 12);
                if (m_Sides != value)
                {
                    m_Sides = value;
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
                value = Mathf.Clamp(value, 0f, 1f);
                if (m_InnerRadius != value)
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
            if (target is SDFPolygon polygon)
            {
                polygon.m_Size = this.m_Size;
                polygon.m_Sides = this.m_Sides;
                polygon.m_InnerRadius = this.m_InnerRadius;
            }
        }
    }
}