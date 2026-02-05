using System.IO;

namespace DNExtensions.Shapes
{
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class SDFShapeBase : MaskableGraphic, ISerializationCallbackReceiver, ILayoutElement, ICanvasRaycastFilter
    {
        protected static readonly int BaseColorID = Shader.PropertyToID("_Base_Color");
        protected static readonly int RotationID = Shader.PropertyToID("_Rotation");
        protected static readonly int OffsetID = Shader.PropertyToID("_Offset");
        protected static readonly int FillAmountID = Shader.PropertyToID("_Fill_Amount");
        protected static readonly int FillTypeID = Shader.PropertyToID("_Fill_Type");
        protected static readonly int FillOriginID = Shader.PropertyToID("_Fill_Origin");
        protected static readonly int OutlineThicknessID = Shader.PropertyToID("_Outline_Thickness");
        protected static readonly int OutlineColorID = Shader.PropertyToID("_Outline_Color");
        protected static readonly int RectSize = Shader.PropertyToID("_Rect_Size");


        [SerializeField] protected Color m_BaseColor = Color.white;
        [SerializeField, Range(0, 360)] protected float m_Rotation;
        [SerializeField] protected FillType m_FillType = FillType.None;
        [SerializeField, Range(0f, 1f)] protected float m_FillAmount = 1.0f;
        [SerializeField, Range(0f, 360f)] protected float m_FillOrigin = 0f;
        [SerializeField] protected Vector2 m_Offset;
        [SerializeField, Range(0f, 0.5f)] protected float m_OutlineThickness = 0.02f;
        [SerializeField] protected Color m_OutlineColor = Color.red;

        protected Material m_InstanceMaterial;
        
        public enum FillType
        {
            None = 0,
            Radial = 1,
            Horizontal = 2,
            Vertical = 3
        }


        protected SDFShapeBase()
        {
            useLegacyMeshGeneration = false;
        }

        public override Material material
        {
            get
            {
                if (m_InstanceMaterial == null)
                {
                    CreateInstanceMaterial();
                }

                return m_InstanceMaterial;
            }
            set { base.material = value; }
        }

        protected virtual void CreateInstanceMaterial()
        {
            Shader shader = Shader.Find("UI/SDFShape");
            if (!shader)
            {
                Debug.LogError("SDFShape shader not found! Make sure it's named 'UI/SDFShape'");
                return;
            }

            m_InstanceMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UpdateMaterialProperties();
        }

        protected abstract void SetShapeProperties();
        protected abstract string GetShapeKeyword();

        protected virtual void UpdateMaterialProperties()
        {
            if (m_InstanceMaterial == null) return;

            DisableAllShapeKeywords();
            m_InstanceMaterial.EnableKeyword(GetShapeKeyword());

            Vector2 rectSize = rectTransform.rect.size;
            float minDim = Mathf.Min(rectSize.x, rectSize.y);

            m_InstanceMaterial.SetColor(BaseColorID, m_BaseColor);
            m_InstanceMaterial.SetFloat(RotationID, m_Rotation);
            m_InstanceMaterial.SetVector(OffsetID, m_Offset);
            m_InstanceMaterial.SetFloat(OutlineThicknessID, m_OutlineThickness * minDim);
            m_InstanceMaterial.SetColor(OutlineColorID, m_OutlineColor);
            m_InstanceMaterial.SetFloat(FillAmountID, m_FillAmount);
            m_InstanceMaterial.SetInt(FillTypeID, (int)m_FillType);
            m_InstanceMaterial.SetFloat(FillOriginID, m_FillOrigin);

            SetShapeProperties();
        }

        private void DisableAllShapeKeywords()
        {
            m_InstanceMaterial.DisableKeyword("_SHAPE_CIRCLE");
            m_InstanceMaterial.DisableKeyword("_SHAPE_RECTANGLE");
            m_InstanceMaterial.DisableKeyword("_SHAPE_POLYGON");
            m_InstanceMaterial.DisableKeyword("_SHAPE_HEART");
            m_InstanceMaterial.DisableKeyword("_SHAPE_RING");
            m_InstanceMaterial.DisableKeyword("_SHAPE_CROSS");
            m_InstanceMaterial.DisableKeyword("_SHAPE_LINE");
        }

        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();
            UpdateMaterialProperties();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_InstanceMaterial)
            {
                UpdateMaterialProperties();
            }
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_InstanceMaterial == null)
            {
                CreateInstanceMaterial();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_InstanceMaterial != null)
            {
                DestroyImmediate(m_InstanceMaterial);
                m_InstanceMaterial = null;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Rect r = GetPixelAdjustedRect();
            var color32 = color;

            vh.Clear();
            vh.AddVert(new Vector3(r.x, r.y), color32, new Vector2(0, 0));
            vh.AddVert(new Vector3(r.x, r.y + r.height), color32, new Vector2(0, 1));
            vh.AddVert(new Vector3(r.x + r.width, r.y + r.height), color32, new Vector2(1, 1));
            vh.AddVert(new Vector3(r.x + r.width, r.y), color32, new Vector2(1, 0));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
    
            Vector2 actualSize = rectTransform.rect.size;
            material.SetVector(RectSize, new Vector4(actualSize.x, actualSize.y, 0, 0));
        }
        
        public Color baseColor
        {
            get { return m_BaseColor; }
            set
            {
                if (m_BaseColor != value)
                {
                    m_BaseColor = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public float rotation
        {
            get { return m_Rotation; }
            set
            {
                if (!Mathf.Approximately(m_Rotation, value))
                {
                    m_Rotation = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public Vector2 offset
        {
            get { return m_Offset; }
            set
            {
                if (m_Offset != value)
                {
                    m_Offset = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public float outlineThickness
        {
            get { return m_OutlineThickness; }
            set
            {
                value = Mathf.Clamp(value, 0f, 0.5f);
                if (m_OutlineThickness != value)
                {
                    m_OutlineThickness = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public Color outlineColor
        {
            get { return m_OutlineColor; }
            set
            {
                if (m_OutlineColor != value)
                {
                    m_OutlineColor = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }
        
        public float fillAmount
        {
            get { return m_FillAmount; }
            set
            {
                value = Mathf.Clamp01(value);
                if (m_FillAmount != value)
                {
                    m_FillAmount = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public FillType fillType
        {
            get { return m_FillType; }
            set
            {
                if (m_FillType != value)
                {
                    m_FillType = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }

        public float fillOrigin
        {
            get { return m_FillOrigin; }
            set
            {
                if (m_FillOrigin != value)
                {
                    m_FillOrigin = value;
                    UpdateMaterialProperties();
                    SetMaterialDirty();
                }
            }
        }


        public void ExportToPNG(int width, int height, string path)
        {
            int tempLayer = 31; 
            
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture.active = renderTexture;

            GameObject tempCameraObj = new GameObject("TempCamera");
            Camera tempCamera = tempCameraObj.AddComponent<Camera>();
            tempCamera.targetTexture = renderTexture;
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.backgroundColor = Color.clear;
            tempCamera.orthographic = true;
            tempCamera.enabled = false;
            tempCamera.cullingMask = 1 << tempLayer;

            GameObject tempCanvasObj = new GameObject("TempCanvas");
            tempCanvasObj.layer = tempLayer;
            Canvas tempCanvas = tempCanvasObj.AddComponent<Canvas>();
            tempCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            tempCanvas.worldCamera = tempCamera;

            CanvasScaler scaler = tempCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            GameObject tempShapeObj = new GameObject("TempShape");
            tempShapeObj.layer = tempLayer;
            tempShapeObj.transform.SetParent(tempCanvas.transform, false);

            SDFShapeBase tempShape = (SDFShapeBase)tempShapeObj.AddComponent(GetType());
            RectTransform tempRect = tempShape.GetComponent<RectTransform>();

            CopyPropertiesTo(tempShape);

            tempRect.anchorMin = Vector2.zero;
            tempRect.anchorMax = Vector2.one;
            tempRect.sizeDelta = Vector2.zero;

            tempCamera.orthographicSize = height / 2f;
            tempCameraObj.transform.position = new Vector3(width / 2f, height / 2f, -10f);

            tempShape.SetAllDirty();
            Canvas.ForceUpdateCanvases();

            tempCamera.Render();

            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            DestroyImmediate(tempCameraObj);
            DestroyImmediate(tempCanvasObj);
            DestroyImmediate(texture);

            Debug.Log($"Shape exported to: {path}");
        }

        protected virtual void CopyPropertiesTo(SDFShapeBase target)
        {
            target.m_BaseColor = this.m_BaseColor;
            target.m_Rotation = this.m_Rotation;
            target.m_Offset = this.m_Offset;
            target.m_OutlineThickness = this.m_OutlineThickness;
            target.m_OutlineColor = this.m_OutlineColor;
            target.color = this.color;
            target.m_FillAmount = this.m_FillAmount;
            target.m_FillType = this.m_FillType;
            target.m_FillOrigin = this.m_FillOrigin;
        }


        public virtual void OnBeforeSerialize()
        {
        }

        public virtual void OnAfterDeserialize()
        {
        }


        public virtual void CalculateLayoutInputHorizontal()
        {
        }

        public virtual void CalculateLayoutInputVertical()
        {
        }

        public virtual float minWidth
        {
            get { return 0; }
        }

        public virtual float preferredWidth
        {
            get { return 0; }
        }

        public virtual float flexibleWidth
        {
            get { return -1; }
        }

        public virtual float minHeight
        {
            get { return 0; }
        }

        public virtual float preferredHeight
        {
            get { return 0; }
        }

        public virtual float flexibleHeight
        {
            get { return -1; }
        }

        public virtual int layoutPriority
        {
            get { return 0; }
        }

     
        public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return raycastTarget;
        }
    }
}