Shader "Hidden/ProjectWallE/LayerOutline"
{
    Properties
    {
        _UseDepth ("Use Depth", Float) = 1
        _UseNormals ("Use Normals", Float) = 1
        _UseColor ("Use Color", Float) = 0
        _DepthThreshold ("Depth Threshold", Float) = 1.5
        _NormalThreshold ("Normal Threshold", Float) = 0.4
        _ColorThreshold ("Color Threshold", Float) = 0.3
        [HDR]_OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Float) = 1

        _UseDistanceFade ("Use Distance Fade", Float) = 0
        _InvertDistanceFade ("Invert Distance Fade", Float) = 0
        _DistanceFadeStart ("Distance Fade Start", Float) = 5
        _DistanceFadeEnd ("Distance Fade End", Float) = 15
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "LayerOutline"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _UseDepth;
            float _UseNormals;
            float _UseColor;
            float _DepthThreshold;
            float _NormalThreshold;
            float _ColorThreshold;
            float4 _OutlineColor;
            float _OutlineWidth;

            float _UseDistanceFade;
            float _InvertDistanceFade;
            float _DistanceFadeStart;
            float _DistanceFadeEnd;

            float SampleDepthLinear(float2 uv)
            {
                float raw = SampleSceneDepth(uv);
                return LinearEyeDepth(raw, _ZBufferParams);
            }

            float DepthEdge(float2 uv, float2 offset)
            {
                float center = SampleDepthLinear(uv);
                float left   = SampleDepthLinear(uv - float2(offset.x, 0));
                float right  = SampleDepthLinear(uv + float2(offset.x, 0));
                float up     = SampleDepthLinear(uv + float2(0, offset.y));
                float down   = SampleDepthLinear(uv - float2(0, offset.y));

                float diff = abs(center - right) + abs(center - up);
                diff += abs(left - center) + abs(down - center);

                return smoothstep(_DepthThreshold * 0.8, _DepthThreshold * 1.2, diff);
            }

            float NormalEdge(float2 uv, float2 offset)
            {
                float3 center = SampleSceneNormals(uv);
                float3 left   = SampleSceneNormals(uv - float2(offset.x, 0));
                float3 right  = SampleSceneNormals(uv + float2(offset.x, 0));
                float3 up     = SampleSceneNormals(uv + float2(0, offset.y));
                float3 down   = SampleSceneNormals(uv - float2(0, offset.y));

                float diff = 0;
                diff += 1.0 - dot(center, left);
                diff += 1.0 - dot(center, right);
                diff += 1.0 - dot(center, up);
                diff += 1.0 - dot(center, down);

                return smoothstep(_NormalThreshold * 0.8, _NormalThreshold * 1.2, diff * 0.25);
            }

            float ColorEdge(float2 uv, float2 offset)
            {
                float3 center = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float3 left   = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(offset.x, 0)).rgb;
                float3 right  = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, 0)).rgb;
                float3 up     = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(0, offset.y)).rgb;
                float3 down   = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(0, offset.y)).rgb;

                float diff = 0;
                diff += distance(center, left);
                diff += distance(center, right);
                diff += distance(center, up);
                diff += distance(center, down);

                return smoothstep(_ColorThreshold * 0.8, _ColorThreshold * 1.2, diff * 0.25);
            }

            float3 ReconstructWorldPos(float2 uv)
            {
                float depth = SampleSceneDepth(uv);
                float3 posNDC = float3(uv * 2.0 - 1.0, depth);
                #if UNITY_UV_STARTS_AT_TOP
                posNDC.y = -posNDC.y;
                #endif
                float4 posWS = mul(UNITY_MATRIX_I_VP, float4(posNDC, 1.0));
                return posWS.xyz / posWS.w;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 offset = _CameraDepthTexture_TexelSize.xy * _OutlineWidth;

                float edge = 0;

                if (_UseDepth > 0.5)
                    edge = max(edge, DepthEdge(uv, offset));

                if (_UseNormals > 0.5)
                    edge = max(edge, NormalEdge(uv, offset));

                if (_UseColor > 0.5)
                    edge = max(edge, ColorEdge(uv, offset));

                float distanceFade = 1.0;
                if (_UseDistanceFade > 0.5)
                {
                    float3 worldPos = ReconstructWorldPos(uv);
                    float dist = distance(worldPos, _WorldSpaceCameraPos.xyz);
                    distanceFade = saturate((dist - _DistanceFadeStart) / max(_DistanceFadeEnd - _DistanceFadeStart, 0.001));
                    if (_InvertDistanceFade > 0.5)
                        distanceFade = 1.0 - distanceFade;
                }

                return float4(_OutlineColor.rgb, _OutlineColor.a * edge * distanceFade);
            }

            ENDHLSL
        }
    }
}
