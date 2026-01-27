namespace DNExtensions.Utilities.Shapes
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("UI/SDF Shapes/Line", 17)]
    public class SDFLine : SDFShapeBase
    {
        private static readonly int StartPosID = Shader.PropertyToID("_Start_Position");
        private static readonly int EndPosID = Shader.PropertyToID("_End_Position");
        private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");

        [SerializeField] private Vector2 m_StartPos = new Vector2(-0.3f, 0);
        [SerializeField] private Vector2 m_EndPos = new Vector2(0.3f, 0);
        [SerializeField, Range(0.001f, 0.1f)] private float m_Thickness = 0.05f;

        protected override string GetShapeKeyword()
        {
            return "_SHAPE_LINE";
        }

        protected override void SetShapeProperties()
        {
            m_InstanceMaterial.SetVector(StartPosID, m_StartPos);
            m_InstanceMaterial.SetVector(EndPosID, m_EndPos);
            m_InstanceMaterial.SetFloat(ThicknessID, m_Thickness);
        }

        public Vector2 startPos
        {
            get { return m_StartPos; }
            set
            {
                if (m_StartPos != value)
                {
                    m_StartPos = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public Vector2 endPos
        {
            get { return m_EndPos; }
            set
            {
                if (m_EndPos != value)
                {
                    m_EndPos = value;
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
                value = Mathf.Clamp(value, 0.001f, 0.1f);
                if (!Mathf.Approximately(m_Thickness, value))
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
            if (target is SDFLine line)
            {
                line.m_StartPos = this.m_StartPos;
                line.m_EndPos = this.m_EndPos;
                line.m_Thickness = this.m_Thickness;
            }
        }
    }
}