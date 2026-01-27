namespace DNExtensions.Utilities.Shapes
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("UI/SDF Shapes/Cross", 17)]
    public class SDFCross : SDFShapeBase
    {
        private static readonly int WidthID = Shader.PropertyToID("_Width");
        private static readonly int HeightID = Shader.PropertyToID("_Height");
        private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");

        [SerializeField, Range(0f, 0.5f)] private float m_Width = 0.3f;
        [SerializeField, Range(0f, 0.5f)] private float m_Height = 0.3f;
        [SerializeField, Range(0f, 0.5f)] private float m_Thickness = 0.1f;

        protected override string GetShapeKeyword()
        {
            return "_SHAPE_CROSS";
        }

        protected override void SetShapeProperties()
        {
            m_InstanceMaterial.SetFloat(WidthID, m_Width);
            m_InstanceMaterial.SetFloat(HeightID, m_Height);
            m_InstanceMaterial.SetFloat(ThicknessID, m_Thickness);
        }

        public float width
        {
            get { return m_Width; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (m_Width != value)
                {
                    m_Width = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public float height
        {
            get { return m_Height; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (m_Height != value)
                {
                    m_Height = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public float thickness
        {
            get { return m_Thickness; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (m_Thickness != value)
                {
                    m_Thickness = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        protected override void CopyPropertiesTo(SDFShapeBase target)
        {
            base.CopyPropertiesTo(target);
            if (target is SDFCross cross)
            {
                cross.m_Width = this.m_Width;
                cross.m_Height = this.m_Height;
                cross.m_Thickness = this.m_Thickness;
            }
        }
    }
}