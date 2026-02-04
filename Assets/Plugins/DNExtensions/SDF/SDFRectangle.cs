namespace DNExtensions.Shapes
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("UI/SDF Shapes/Rectangle", 13)]
    public class SDFRectangle : SDFShapeBase
    {
        private static readonly int WidthID = Shader.PropertyToID("_Width");
        private static readonly int HeightID = Shader.PropertyToID("_Height");
        private static readonly int CornersID = Shader.PropertyToID("_Corners");
        private static readonly int RoundingID = Shader.PropertyToID("_Rounding");

        [SerializeField, Range(0f, 0.5f)] private float m_Width = 0.25f;
        [SerializeField, Range(0f, 0.5f)] private float m_Height = 0.25f;
        [SerializeField, Range(0f, 0.5f)] private float m_Rounding = 0f;
        [SerializeField] private Vector4 m_Corners = new Vector4(0, 0, 0, 0);

        protected override string GetShapeKeyword()
        {
            return "_SHAPE_RECTANGLE";
        }

        protected override void SetShapeProperties()
        {
            m_InstanceMaterial.SetFloat(WidthID, m_Width);
            m_InstanceMaterial.SetFloat(HeightID, m_Height);
            m_InstanceMaterial.SetVector(CornersID, m_Corners);
            m_InstanceMaterial.SetFloat(RoundingID, m_Rounding);
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

        public Vector4 corners
        {
            get { return m_Corners; }
            set
            {
                if (m_Corners != value)
                {
                    m_Corners = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public float rounding
        {
            get { return m_Rounding; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (m_Rounding != value)
                {
                    m_Rounding = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        protected override void CopyPropertiesTo(SDFShapeBase target)
        {
            base.CopyPropertiesTo(target);
            if (target is SDFRectangle rect)
            {
                rect.m_Width = this.m_Width;
                rect.m_Height = this.m_Height;
                rect.m_Corners = this.m_Corners;
                rect.m_Rounding = this.m_Rounding;
            }
        }

    }
}