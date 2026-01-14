using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class ShadowCastingSprite : MonoBehaviour
{
    [Header("Shadow Settings")]
    [SerializeField] private bool receiveShadows = true;
    [SerializeField] private ShadowCastingMode castShadows = ShadowCastingMode.On;

    private SpriteRenderer _renderer;
    private MaterialPropertyBlock _propBlock;
    
    private Color _lastColor;
    private bool _lastFlipX;
    private bool _lastFlipY;
    private SpriteDrawMode _lastDrawMode;
    private SpriteMaskInteraction _lastMaskInteraction;
    private SpriteSortPoint _lastSortPoint;
    
    private bool _lastReceiveShadows;
    private ShadowCastingMode _lastCastShadows;

    private bool _isDirty = true;

    private static readonly int ColorID = Shader.PropertyToID("_SpriteColor");
    private static readonly int FlipXid = Shader.PropertyToID("_FlipX");
    private static readonly int FlipYid = Shader.PropertyToID("_FlipY");
    private static readonly int DrawModeID = Shader.PropertyToID("_DrawMode");
    private static readonly int MaskInteractionID = Shader.PropertyToID("_MaskInteraction");
    private static readonly int SortPointID = Shader.PropertyToID("_SpriteSortPoint");

    private void OnEnable()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _propBlock = new MaterialPropertyBlock();
        _renderer.RegisterSpriteChangeCallback(OnSpriteChanged);
        
        ApplyShadowSettings(); 
        _isDirty = true;
    }

    private void OnDisable()
    {
        if (_renderer) _renderer.UnregisterSpriteChangeCallback(OnSpriteChanged);
    }

    private void OnSpriteChanged(SpriteRenderer r)
    {
        _isDirty = true;
    }

    private void OnValidate()
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        ApplyShadowSettings();
        _isDirty = true;
    }

    private void LateUpdate()
    {
        if (!_renderer) return;

        bool rendererChanged = _renderer.color != _lastColor ||
                               _renderer.flipX != _lastFlipX ||
                               _renderer.flipY != _lastFlipY ||
                               _renderer.drawMode != _lastDrawMode ||
                               _renderer.maskInteraction != _lastMaskInteraction ||
                               _renderer.spriteSortPoint != _lastSortPoint;

        bool shadowsChanged = _renderer.shadowCastingMode != castShadows ||
                              _renderer.receiveShadows != receiveShadows;

        if (_isDirty || rendererChanged || shadowsChanged)
        {
            if (shadowsChanged) ApplyShadowSettings();
            
            UpdateShader();
            UpdateCache();
        }
    }

    private void ApplyShadowSettings()
    {
        if (_renderer == null) return;
        _renderer.shadowCastingMode = castShadows;
        _renderer.receiveShadows = receiveShadows;
    }

    private void UpdateShader()
    {
        _renderer.GetPropertyBlock(_propBlock);

        _propBlock.SetColor(ColorID, _renderer.color);
        _propBlock.SetFloat(FlipXid, _renderer.flipX ? -1.0f : 1.0f);
        _propBlock.SetFloat(FlipYid, _renderer.flipY ? -1.0f : 1.0f);
        _propBlock.SetFloat(DrawModeID, (float)_renderer.drawMode);
        _propBlock.SetFloat(MaskInteractionID, (float)_renderer.maskInteraction);
        _propBlock.SetFloat(SortPointID, (float)_renderer.spriteSortPoint);

        _renderer.SetPropertyBlock(_propBlock);
    }

    private void UpdateCache()
    {
        _lastColor = _renderer.color;
        _lastFlipX = _renderer.flipX;
        _lastFlipY = _renderer.flipY;
        _lastDrawMode = _renderer.drawMode;
        _lastMaskInteraction = _renderer.maskInteraction;
        _lastSortPoint = _renderer.spriteSortPoint;
        
        _lastReceiveShadows = receiveShadows;
        _lastCastShadows = castShadows;
        
        _isDirty = false;
    }
}