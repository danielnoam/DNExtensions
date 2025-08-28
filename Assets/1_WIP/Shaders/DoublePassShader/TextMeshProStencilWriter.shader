Shader "TextMeshPro/Distance Field Stencil" {

Properties {
	_MainTex			("Font Atlas", 2D) = "white" {}
	_FaceColor		    ("Face Color", Color) = (1,1,1,1)
	_FaceDilate			("Face Dilate", Range(-1,1)) = 0
	_OutlineColor	    ("Outline Color", Color) = (0,0,0,1)
	_OutlineWidth		("Outline Thickness", Range(0, 1)) = 0
	_OutlineSoftness	("Outline Softness", Range(0,1)) = 0
	
	_WeightNormal		("Weight Normal", float) = 0
	_WeightBold			("Weight Bold", float) = 0.5
	_ScaleRatioA		("Scale RatioA", float) = 1
	_TextureWidth		("Texture Width", float) = 512
	_TextureHeight		("Texture Height", float) = 512
	_GradientScale		("Gradient Scale", float) = 5.0
	_ScaleX				("Scale X", float) = 1.0
	_ScaleY				("Scale Y", float) = 1.0
	_Sharpness			("Sharpness", Range(-1,1)) = 0

	// Custom stencil properties for reticle system
	[Header(Stencil Settings)][Space(10)]
	[IntRange] _StencilRef ("Stencil Reference", Range(0, 255)) = 2
	
	_ClipRect			("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
	_StencilComp		("Stencil Comparison", Float) = 8
	_Stencil			("Stencil ID", Float) = 0
	_StencilOp			("Stencil Operation", Float) = 0
	_StencilWriteMask	("Stencil Write Mask", Float) = 255
	_StencilReadMask	("Stencil Read Mask", Float) = 255
	_CullMode			("Cull Mode", Float) = 0
	_ColorMask			("Color Mask", Float) = 15
}

SubShader {

	Tags
	{
		"Queue"="Transparent+300"
		"IgnoreProjector"="True"
		"RenderType"="Transparent"
	}

	// Pass 1: Normal rendering with stencil writing
	Pass {
		Name "Normal"
		Tags { "LightMode" = "UniversalForward" }
		
		Stencil
		{
			Ref [_StencilRef]
			Comp Always
			Pass Replace
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull [_CullMode]
		ZWrite Off
		Lighting Off
		Fog { Mode Off }
		ZTest LEqual
		Blend One OneMinusSrcAlpha
		ColorMask [_ColorMask]

		CGPROGRAM
		#pragma target 3.0
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP

		#include "UnityCG.cginc"

		struct appdata_t
		{
			float4 vertex   : POSITION;
			fixed4 color    : COLOR;
			float2 texcoord0 : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};

		struct v2f
		{
			float4 vertex   : SV_POSITION;
			fixed4 color    : COLOR;
			float2 texcoord : TEXCOORD0;
			float4 mask     : TEXCOORD2;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;
		fixed4 _FaceColor;
		fixed4 _OutlineColor;
		float _FaceDilate;
		float _OutlineWidth;
		float _OutlineSoftness;
		float _WeightNormal;
		float _WeightBold;
		float _ScaleRatioA;
		float _TextureWidth;
		float _TextureHeight;
		float _GradientScale;
		float _ScaleX;
		float _ScaleY;
		float _Sharpness;
		float4 _ClipRect;

		v2f vert(appdata_t v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.color = v.color;
			o.texcoord = v.texcoord0;
			
			// Simplified masking for UI clipping
			float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			o.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25, 0.25);
			
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			// Sample the distance field
			float d = tex2D(_MainTex, i.texcoord).a;
			
			// Simple distance field rendering
			float scale = _GradientScale;
			float bias = 0.5;
			float outline = _OutlineWidth * _ScaleRatioA * scale;
			
			// Calculate face and outline
			float faceDist = (bias - d + _FaceDilate * _ScaleRatioA) * scale;
			float outlineDist = faceDist - outline;
			
			// Blend face and outline colors
			fixed4 faceColor = _FaceColor * i.color;
			fixed4 outlineColor = _OutlineColor;
			
			float faceAlpha = saturate(0.5 - faceDist);
			float outlineAlpha = saturate(0.5 - outlineDist) - faceAlpha;
			
			fixed4 color = faceColor * faceAlpha + outlineColor * outlineAlpha;
			
			#ifdef UNITY_UI_CLIP_RECT
			float2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
			color.a *= m.x * m.y;
			#endif

			return color;
		}
		ENDCG
	}

	// Pass 2: Force render on top of enemies (when occluded by enemies)
	Pass {
		Name "OnTopOfEnemies"
		Tags { "LightMode" = "SRPDefaultUnlit" }
		
		Stencil
		{
			Ref 1
			Comp Equal  // Only where enemies have written to stencil
			ReadMask 255
		}

		Cull [_CullMode]
		ZWrite Off
		Lighting Off
		Fog { Mode Off }
		ZTest Greater  // Only when behind something
		Blend One OneMinusSrcAlpha
		ColorMask [_ColorMask]

		CGPROGRAM
		#pragma target 3.0
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP

		#include "UnityCG.cginc"

		struct appdata_t
		{
			float4 vertex   : POSITION;
			fixed4 color    : COLOR;
			float2 texcoord0 : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};

		struct v2f
		{
			float4 vertex   : SV_POSITION;
			fixed4 color    : COLOR;
			float2 texcoord : TEXCOORD0;
			float4 mask     : TEXCOORD2;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;
		fixed4 _FaceColor;
		fixed4 _OutlineColor;
		float _FaceDilate;
		float _OutlineWidth;
		float _OutlineSoftness;
		float _WeightNormal;
		float _WeightBold;
		float _ScaleRatioA;
		float _TextureWidth;
		float _TextureHeight;
		float _GradientScale;
		float _ScaleX;
		float _ScaleY;
		float _Sharpness;
		float4 _ClipRect;

		v2f vert(appdata_t v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.color = v.color;
			o.texcoord = v.texcoord0;
			
			// Simplified masking for UI clipping
			float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			o.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25, 0.25);
			
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			// Sample the distance field
			float d = tex2D(_MainTex, i.texcoord).a;
			
			// Simple distance field rendering
			float scale = _GradientScale;
			float bias = 0.5;
			float outline = _OutlineWidth * _ScaleRatioA * scale;
			
			// Calculate face and outline
			float faceDist = (bias - d + _FaceDilate * _ScaleRatioA) * scale;
			float outlineDist = faceDist - outline;
			
			// Blend face and outline colors
			fixed4 faceColor = _FaceColor * i.color;
			fixed4 outlineColor = _OutlineColor;
			
			float faceAlpha = saturate(0.5 - faceDist);
			float outlineAlpha = saturate(0.5 - outlineDist) - faceAlpha;
			
			fixed4 color = faceColor * faceAlpha + outlineColor * outlineAlpha;
			
			#ifdef UNITY_UI_CLIP_RECT
			float2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
			color.a *= m.x * m.y;
			#endif

			return color;
		}
		ENDCG
	}
}

Fallback "TextMeshPro/Mobile/Distance Field"
CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}