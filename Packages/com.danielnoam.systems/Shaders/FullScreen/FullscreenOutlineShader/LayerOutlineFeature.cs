using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ProjectWallE.Shaders.OutlineShader
{
    [System.Serializable]
    public sealed class OutlineSettings
    {
        [Header("Rendering")]
        public LayerMask layerMask = -1;
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingOpaques;

        [Header("Edge Detection")]
        public bool useDepth = true;
        [Range(0f, 10f)] public float depthThreshold = 0.1f;
        public bool useNormals = true;
        [Range(0f, 5f)] public float normalThreshold = 0.1f;
        public bool useColor;
        [Range(0f, 5f)] public float colorThreshold = 0.1f;
        
        [Header("Appearance")]
        [ColorUsage(true,true)]public Color outlineColor = Color.black;
        [Range(0.5f, 5f)] public float outlineWidth = 1f;

        [Header("Distance Fade")]
        public bool useDistanceFade;
        public bool invertDistanceFade = true;
        [Min(0f)] public float fadeStartDistance = 50f;
        [Min(0f)] public float fadeEndDistance = 55f;
    }

    public sealed class LayerOutlineFeature : ScriptableRendererFeature
    {
        [SerializeField] private OutlineSettings settings = new();

        private StencilWritePass _stencilPass;
        private OutlineBlitPass _outlinePass;
        private Material _outlineMaterial;

        private static readonly string ShaderName = "Hidden/ProjectWallE/LayerOutline";

        public override void Create()
        {
            _stencilPass = new StencilWritePass();
            _outlinePass = new OutlineBlitPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;

            EnsureMaterial();
            if (_outlineMaterial == null)
                return;

            UpdateMaterialProperties();

            _stencilPass.renderPassEvent = settings.injectionPoint;
            _stencilPass.SetLayerMask(settings.layerMask);
            renderer.EnqueuePass(_stencilPass);

            _outlinePass.renderPassEvent = settings.injectionPoint + 1;
            _outlinePass.SetMaterial(_outlineMaterial);
            renderer.EnqueuePass(_outlinePass);
        }

        private void EnsureMaterial()
        {
            if (_outlineMaterial != null)
                return;

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[LayerOutlineFeature] Shader '{ShaderName}' not found.");
                return;
            }

            _outlineMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        private void UpdateMaterialProperties()
        {
            _outlineMaterial.SetFloat(ShaderIds.UseDepth, settings.useDepth ? 1f : 0f);
            _outlineMaterial.SetFloat(ShaderIds.UseNormals, settings.useNormals ? 1f : 0f);
            _outlineMaterial.SetFloat(ShaderIds.UseColor, settings.useColor ? 1f : 0f);
            _outlineMaterial.SetFloat(ShaderIds.DepthThreshold, settings.depthThreshold);
            _outlineMaterial.SetFloat(ShaderIds.NormalThreshold, settings.normalThreshold);
            _outlineMaterial.SetFloat(ShaderIds.ColorThreshold, settings.colorThreshold);
            _outlineMaterial.SetColor(ShaderIds.OutlineColor, settings.outlineColor);
            _outlineMaterial.SetFloat(ShaderIds.OutlineWidth, settings.outlineWidth);
            _outlineMaterial.SetFloat(ShaderIds.UseDistanceFade, settings.useDistanceFade ? 1f : 0f);
            _outlineMaterial.SetFloat(ShaderIds.InvertDistanceFade, settings.invertDistanceFade ? 1f : 0f);
            _outlineMaterial.SetFloat(ShaderIds.DistanceFadeStart, settings.fadeStartDistance);
            _outlineMaterial.SetFloat(ShaderIds.DistanceFadeEnd, settings.fadeEndDistance);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_outlineMaterial);
            _outlineMaterial = null;
        }
    }

    internal static class ShaderIds
    {
        internal static readonly int UseDepth = Shader.PropertyToID("_UseDepth");
        internal static readonly int UseNormals = Shader.PropertyToID("_UseNormals");
        internal static readonly int UseColor = Shader.PropertyToID("_UseColor");
        internal static readonly int DepthThreshold = Shader.PropertyToID("_DepthThreshold");
        internal static readonly int NormalThreshold = Shader.PropertyToID("_NormalThreshold");
        internal static readonly int ColorThreshold = Shader.PropertyToID("_ColorThreshold");
        internal static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        internal static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        internal static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");
        internal static readonly int UseDistanceFade = Shader.PropertyToID("_UseDistanceFade");
        internal static readonly int InvertDistanceFade = Shader.PropertyToID("_InvertDistanceFade");
        internal static readonly int DistanceFadeStart = Shader.PropertyToID("_DistanceFadeStart");
        internal static readonly int DistanceFadeEnd = Shader.PropertyToID("_DistanceFadeEnd");
    }

    internal sealed class StencilWritePass : ScriptableRenderPass
    {
        private LayerMask _layerMask;

        private static readonly List<ShaderTagId> ShaderTags = new()
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly")
        };

        private sealed class PassData
        {
            internal RendererListHandle RendererListHandle;
        }

        public void SetLayerMask(LayerMask mask)
        {
            _layerMask = mask;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            var stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(CompareFunction.Always);
            stencilState.SetPassOperation(StencilOp.Replace);
            stencilState.SetFailOperation(StencilOp.Keep);
            stencilState.SetZFailOperation(StencilOp.Keep);
            stencilState.readMask = 255;
            stencilState.writeMask = 255;

            var blendState = BlendState.defaultValue;
            blendState.blendState0 = new RenderTargetBlendState { writeMask = 0 };

            var renderStateBlock = new RenderStateBlock(RenderStateMask.Stencil | RenderStateMask.Blend)
            {
                stencilReference = 1,
                stencilState = stencilState,
                blendState = blendState
            };

            var filterSettings = new FilteringSettings(RenderQueueRange.opaque, _layerMask);
            var sortingCriteria = cameraData.defaultOpaqueSortFlags;
            var drawingSettings = RenderingUtils.CreateDrawingSettings(
                ShaderTags, renderingData, cameraData, lightData, sortingCriteria);

            var rendererListHandle = CreateRendererListWithStateBlock(
                renderGraph, ref renderingData.cullResults, drawingSettings, filterSettings, renderStateBlock);

            using var builder = renderGraph.AddRasterRenderPass<PassData>("LayerOutline_StencilWrite", out var passData);

            passData.RendererListHandle = rendererListHandle;

            builder.UseRendererList(rendererListHandle);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.RendererListHandle);
            });
        }

        private static RendererListHandle CreateRendererListWithStateBlock(
            RenderGraph renderGraph, ref CullingResults cullResults,
            DrawingSettings drawingSettings, FilteringSettings filteringSettings,
            RenderStateBlock renderStateBlock)
        {
            var shaderTagValues = new NativeArray<ShaderTagId>(1, Allocator.Temp);
            var stateBlocks = new NativeArray<RenderStateBlock>(1, Allocator.Temp);

            shaderTagValues[0] = ShaderTagId.none;
            stateBlocks[0] = renderStateBlock;

            var param = new RendererListParams(cullResults, drawingSettings, filteringSettings)
            {
                tagValues = shaderTagValues,
                stateBlocks = stateBlocks,
                isPassTagName = false
            };

            var handle = renderGraph.CreateRendererList(param);

            shaderTagValues.Dispose();
            stateBlocks.Dispose();

            return handle;
        }
    }

    internal sealed class OutlineBlitPass : ScriptableRenderPass
    {
        private Material _material;

        private sealed class CopyPassData
        {
            internal TextureHandle Source;
        }

        private sealed class OutlinePassData
        {
            internal TextureHandle Source;
            internal Material Material;
        }

        public void SetMaterial(Material material)
        {
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var cameraColor = resourceData.activeColorTexture;
            if (!cameraColor.IsValid())
                return;

            var desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            var copiedColor = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_OutlineSourceTex", false);

            using (var copyBuilder = renderGraph.AddRasterRenderPass<CopyPassData>("LayerOutline_CopyColor", out var copyData))
            {
                copyData.Source = cameraColor;

                copyBuilder.UseTexture(cameraColor);
                copyBuilder.SetRenderAttachment(copiedColor, 0);
                copyBuilder.AllowPassCulling(false);

                copyBuilder.SetRenderFunc(static (CopyPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }

            using var outlineBuilder = renderGraph.AddRasterRenderPass<OutlinePassData>("LayerOutline_Draw", out var outlineData);

            outlineData.Source = copiedColor;
            outlineData.Material = _material;

            outlineBuilder.UseTexture(copiedColor);
            outlineBuilder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            outlineBuilder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
            outlineBuilder.AllowPassCulling(false);

            outlineBuilder.SetRenderFunc(static (OutlinePassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), data.Material, 0);
            });
        }
    }
}