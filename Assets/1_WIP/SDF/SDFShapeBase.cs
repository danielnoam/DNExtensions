using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public abstract class SDFShapeBase : MaskableGraphic, ISerializationCallbackReceiver, ILayoutElement, ICanvasRaycastFilter
{
    // Common properties all shapes share
    [SerializeField] protected Color m_BaseColor = Color.white;
    [SerializeField, Range(0, 360)] protected float m_Rotation = 0;
    
    [SerializeField, Range(0f, 0.5f)] protected float m_OutlineThickness = 0.02f;
    [SerializeField] protected Color m_OutlineColor = Color.red;
    
    [SerializeField, Range(0f, 0.5f)] protected float m_InlineThickness = 0f;
    [SerializeField] protected Color m_InlineColor = Color.blue;

    protected Material m_InstanceMaterial;
    
    // Shader property IDs (shared by all shapes)
    protected static readonly int BaseColorID = Shader.PropertyToID("_Base_Color");
    protected static readonly int RotationID = Shader.PropertyToID("_Rotation");
    protected static readonly int OutlineThicknessID = Shader.PropertyToID("_Outline_Thickness");
    protected static readonly int OutlineColorID = Shader.PropertyToID("_Outline_Color");
    protected static readonly int InlineThicknessID = Shader.PropertyToID("_Inline_Thickness");
    protected static readonly int InlineColorID = Shader.PropertyToID("_Inline_Color");

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
        set
        {
            base.material = value;
        }
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

        // Disable all shape keywords
        DisableAllShapeKeywords();
        
        // Enable this shape's keyword
        m_InstanceMaterial.EnableKeyword(GetShapeKeyword());
        
        // Set common properties
        m_InstanceMaterial.SetColor(BaseColorID, m_BaseColor);
        m_InstanceMaterial.SetFloat(RotationID, m_Rotation);
        m_InstanceMaterial.SetFloat(OutlineThicknessID, m_OutlineThickness);
        m_InstanceMaterial.SetColor(OutlineColorID, m_OutlineColor);
        m_InstanceMaterial.SetFloat(InlineThicknessID, m_InlineThickness);
        m_InstanceMaterial.SetColor(InlineColorID, m_InlineColor);
        
        // Let derived class set its specific properties
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
    }

    // Common property accessors
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

    public float inlineThickness
    {
        get { return m_InlineThickness; }
        set
        {
            value = Mathf.Clamp(value, 0f, 0.5f);
            if (m_InlineThickness != value)
            {
                m_InlineThickness = value;
                UpdateMaterialProperties();
                SetMaterialDirty();
            }
        }
    }

    public Color inlineColor
    {
        get { return m_InlineColor; }
        set
        {
            if (m_InlineColor != value)
            {
                m_InlineColor = value;
                UpdateMaterialProperties();
                SetMaterialDirty();
            }
        }
    }

    // Export functionality
    public void ExportToPNG(int width, int height, string path)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture.active = renderTexture;

        GameObject tempCameraObj = new GameObject("TempCamera");
        Camera tempCamera = tempCameraObj.AddComponent<Camera>();
        tempCamera.targetTexture = renderTexture;
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.backgroundColor = Color.clear;
        tempCamera.orthographic = true;
        tempCamera.enabled = false;

        GameObject tempCanvasObj = new GameObject("TempCanvas");
        Canvas tempCanvas = tempCanvasObj.AddComponent<Canvas>();
        tempCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        tempCanvas.worldCamera = tempCamera;
        
        CanvasScaler scaler = tempCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        GameObject tempShapeObj = new GameObject("TempShape");
        tempShapeObj.transform.SetParent(tempCanvas.transform, false);
        
        // Create instance of the same type as this shape
        SDFShapeBase tempShape = (SDFShapeBase)tempShapeObj.AddComponent(GetType());
        RectTransform tempRect = tempShape.GetComponent<RectTransform>();
        
        // Copy properties using reflection or implement CopyTo method
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
        System.IO.File.WriteAllBytes(path, bytes);

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
        target.m_OutlineThickness = this.m_OutlineThickness;
        target.m_OutlineColor = this.m_OutlineColor;
        target.m_InlineThickness = this.m_InlineThickness;
        target.m_InlineColor = this.m_InlineColor;
        target.color = this.color;
    }

    // ISerializationCallbackReceiver
    public virtual void OnBeforeSerialize() {}
    public virtual void OnAfterDeserialize() {}

    // ILayoutElement
    public virtual void CalculateLayoutInputHorizontal() {}
    public virtual void CalculateLayoutInputVertical() {}
    
    public virtual float minWidth { get { return 0; } }
    public virtual float preferredWidth { get { return 0; } }
    public virtual float flexibleWidth { get { return -1; } }
    
    public virtual float minHeight { get { return 0; } }
    public virtual float preferredHeight { get { return 0; } }
    public virtual float flexibleHeight { get { return -1; } }
    
    public virtual int layoutPriority { get { return 0; } }

    // ICanvasRaycastFilter
    public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        return raycastTarget;
    }
}