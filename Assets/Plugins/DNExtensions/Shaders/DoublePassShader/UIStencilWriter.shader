Shader "Custom/UI/StencilWriter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Emission Settings)][Space(10)]
        [Toggle] _EmissionEnabled ("Enable Emission", Float) = 1.0
        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0, 1)) = 1.0
        
        [Header(Stencil Settings)][Space(10)]
        [IntRange] _StencilRef ("Stencil Reference", Range(0, 255)) = 1
        
        // Standard UI properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [Toggle(UNITY_UI_CLIP_RECT)] _UseUIClipRect ("Use UI Clip Rect", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+200"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        // Pass 1: Normal rendering (when not occluded by anything)
        Pass
        {
            Name "Normal"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            
            // Write to stencil buffer
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _EmissionColor;
            half _EmissionEnabled;
            half _EmissionStrength;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                // For UI, we need to use the world position from the Canvas
                OUT.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.worldPosition.xy * 2 - _ClipRect.xy - _ClipRect.zw)) * 0.25);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // Add emission
                color.rgb += _EmissionColor.rgb * _EmissionEnabled * _EmissionStrength;

                return color;
            }
            ENDCG
        }
        
        // Pass 2: Force render on top of enemies (when occluded by enemies)
        Pass
        {
            Name "OnTopOfEnemies"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            ZWrite Off
            ZTest Greater  // Only when behind something
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            Stencil
            {
                Ref 1
                Comp Equal  // Only where enemies have written to stencil
                ReadMask 255
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _EmissionColor;
            half _EmissionEnabled;
            half _EmissionStrength;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                // For UI, we need to use the world position from the Canvas
                OUT.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.worldPosition.xy * 2 - _ClipRect.xy - _ClipRect.zw)) * 0.25);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // Add emission
                color.rgb += _EmissionColor.rgb * _EmissionEnabled * _EmissionStrength;

                return color;
            }
            ENDCG
        }
    }
}