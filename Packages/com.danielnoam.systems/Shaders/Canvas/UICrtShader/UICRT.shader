Shader "ProjectWallE/UICRT"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "black" {}

        [Header(Curvature)]
        [Toggle(USE_CURVATURE)] _UseCurvature ("Enabled", Float) = 1
        _CurvatureAmount ("Amount", Range(-1, 1)) = 0.15

        [Space][Header(Scanlines)]
        [Toggle(USE_SCANLINES)] _UseScanlines ("Enabled", Float) = 1
        _ScanlineSize ("Size", Range(0, 1)) = 0.3
        _ScanlineCount ("Count", Range(100, 800)) = 300
        _ScanlineSpeed ("Speed", Range(0, 10)) = 1.0
        [HDR] _ScanlineColor ("Color", Color) = (0, 0, 0, 1)

        [Space][Header(Vignette)]
        [Toggle(USE_VIGNETTE)] _UseVignette ("Enabled", Float) = 1
        _VignetteIntensity ("Intensity", Range(0, 1)) = 0.4
        _VignetteSmoothness ("Smoothness", Range(0.01, 1)) = 0.3

        [Space][Header(Chromatic Aberration)]
        [Toggle(USE_CHROMATIC)] _UseChromatic ("Enabled", Float) = 1
        _ChromaticAmount ("Amount", Range(0, 0.05)) = 0.005

        [Space][Header(Flicker)]
        [Toggle(USE_FLICKER)] _UseFlicker ("Enabled", Float) = 1
        _FlickerIntensity ("Intensity", Range(0, 0.5)) = 0.05
        _FlickerSpeed ("Speed", Range(0, 30)) = 10.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UICRT"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local USE_CURVATURE
            #pragma shader_feature_local USE_SCANLINES
            #pragma shader_feature_local USE_VIGNETTE
            #pragma shader_feature_local USE_CHROMATIC
            #pragma shader_feature_local USE_FLICKER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _CurvatureAmount;
                float _ScanlineSize;
                float _ScanlineCount;
                float _ScanlineSpeed;
                half4 _ScanlineColor;
                float _VignetteIntensity;
                float _VignetteSmoothness;
                float _ChromaticAmount;
                float _FlickerIntensity;
                float _FlickerSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float2 ApplyCurvature(float2 uv, float amount)
            {
                float2 centered = uv * 2.0 - 1.0;
                float2 offset = centered.yx * centered.yx * centered * amount;
                return (centered + offset) * 0.5 + 0.5;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                #if USE_CURVATURE
                    uv = ApplyCurvature(uv, _CurvatureAmount);
                    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                        return half4(0, 0, 0, 0);
                #endif

                half4 color;

                #if USE_CHROMATIC
                    float2 caOffset = float2(_ChromaticAmount, 0.0);
                    half r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + caOffset).r;
                    half g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                    half b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - caOffset).b;
                    color = half4(r, g, b, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a);
                #else
                    color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                #endif

                #if USE_SCANLINES
                    float scroll = uv.y + _Time.y * _ScanlineSpeed * 0.1;
                    float band = frac(scroll * _ScanlineCount);
                    float inLine = step(_ScanlineSize, band);
                    color.rgb = lerp(_ScanlineColor.rgb, color.rgb, inLine);
                #endif

                #if USE_VIGNETTE
                    float2 vigUV = uv * 2.0 - 1.0;
                    float vignette = length(vigUV);
                    vignette = smoothstep(1.0 - _VignetteSmoothness, 1.0, vignette * _VignetteIntensity);
                    color.rgb *= 1.0 - vignette;
                #endif

                #if USE_FLICKER
                    float flicker = 1.0 - _FlickerIntensity * (sin(_Time.y * _FlickerSpeed) * 0.5 + 0.5);
                    color.rgb *= flicker;
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}
