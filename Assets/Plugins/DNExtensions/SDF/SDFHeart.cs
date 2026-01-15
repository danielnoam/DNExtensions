namespace DNExtensions.Shapes
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("UI/SDF Shapes/Heart", 15)]
    public class SDFHeart : SDFShapeBase
    {
        private static readonly int SizeID = Shader.PropertyToID("_Size");

        [SerializeField, Range(0f, 0.5f)] private float m_Size = 0.3f;

        protected override string GetShapeKeyword()
        {
            return "_SHAPE_HEART";
        }

        protected override void SetShapeProperties()
        {
            m_InstanceMaterial.SetFloat(SizeID, m_Size);
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

        protected override void CopyPropertiesTo(SDFShapeBase target)
        {
            base.CopyPropertiesTo(target);
            if (target is SDFHeart heart)
            {
                heart.m_Size = this.m_Size;
            }
        }
    }
}