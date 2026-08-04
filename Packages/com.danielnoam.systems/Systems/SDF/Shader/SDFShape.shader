Shader "UI/SDFShape"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Base_Color ("Base Color", Color) = (1,1,1,1)
        _Rotation ("Rotation", Float) = 0
        _Offset ("Offset", Vector) = (0,0,0,0)
        _Rect_Size ("Rect Size", Vector) = (100,100,0,0)

        _Fill_Type ("Fill Type", Int) = 0
        _Fill_Amount ("Fill Amount", Float) = 1
        _Fill_Origin ("Fill Origin", Float) = 0

        _Outline_Thickness ("Outline Thickness", Float) = 0
        _Outline_Color ("Outline Color", Color) = (1,0,0,1)

        // Shape properties (shared names, only the relevant ones are read per keyword)
        _Radius ("Radius", Float) = 40
        _Width ("Width", Float) = 40
        _Height ("Height", Float) = 40
        _Corners ("Corner Rounding", Vector) = (0,0,0,0)
        _Rounding ("Rounding", Float) = 0
        _Size ("Size", Float) = 40
        _Inner_Radius ("Inner Radius", Float) = 0
        _Sides ("Sides", Int) = 6
        _Outer_Radius ("Outer Radius", Float) = 40
        _Thickness ("Thickness", Float) = 5
        _Start_Position ("Start Position", Vector) = (-20,0,0,0)
        _End_Position ("End Position", Vector) = (20,0,0,0)

        // Stencil properties for Unity UI Mask support
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_local _ _SHAPE_CIRCLE _SHAPE_RECTANGLE _SHAPE_POLYGON _SHAPE_HEART _SHAPE_RING _SHAPE_CROSS _SHAPE_LINE

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _Base_Color;
            float _Rotation;
            float4 _Offset;
            float4 _Rect_Size;
            int _Fill_Type;
            float _Fill_Amount;
            float _Fill_Origin;
            float _Outline_Thickness;
            float4 _Outline_Color;
            float4 _ClipRect;

            float _Radius;
            float _Width;
            float _Height;
            float4 _Corners;
            float _Rounding;
            float _Size;
            float _Inner_Radius;
            int _Sides;
            float _Outer_Radius;
            float _Thickness;
            float4 _Start_Position;
            float4 _End_Position;

            static const float PI = 3.14159265359;
            static const float TWO_PI = 6.28318530718;

            float2 TransformUV(float2 uv, float rotation, float2 offset)
            {
                float2 centered = uv - 0.5 + offset;
                float rad = rotation * PI / 180.0;
                float s, c;
                sincos(rad, s, c);
                return float2(
                    centered.x * c - centered.y * s,
                    centered.x * s + centered.y * c
                );
            }

            float CircleSDF(float2 p, float radius)
            {
                return length(p) - radius;
            }

            float RectangleSDF(float2 p, float w, float h, float4 corners, float rounding)
            {
                float2 q = abs(p);
                float2 size = float2(w, h);

                float topMask = step(0.0, p.y);
                float rightMask = step(0.0, p.x);

                float leftR = lerp(corners.x, corners.y, topMask);
                float rightR = lerp(corners.w, corners.z, topMask);
                float r = lerp(leftR, rightR, rightMask) + rounding;

                float2 d = q - size + r;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - r;
            }

            float PolygonSDF(float2 p, float size, float innerRadius, int points)
            {
                points = clamp(points, 3, 12);
                float m = lerp(2.0, float(points), innerRadius);
                float an = PI / float(points);
                float en = PI / m;

                float2 acs = float2(cos(an), sin(an));
                float2 ecs = float2(cos(en), sin(en));

                float bn = fmod(atan2(p.y, p.x) + PI, 2.0 * an) - an;
                float r = length(p);
                float2 q = r * float2(cos(bn), abs(sin(bn)));

                q -= size * acs;
                q += ecs * clamp(-dot(q, ecs), 0.0, size * acs.y / ecs.y);

                return length(q) * sign(q.x);
            }

            float HeartSDF(float2 p, float size)
            {
                p /= (size * 2.0);
                p.y += 0.5;
                p.x = abs(p.x);

                float s = max(p.x + p.y, 0.0);

                float d1 = sqrt(dot(p - float2(0.25, 0.75), p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;

                float d2a = dot(p - float2(0.0, 1.0), p - float2(0.0, 1.0));
                float d2b = dot(p - 0.5 * s * float2(1.0, 1.0), p - 0.5 * s * float2(1.0, 1.0));
                float d2 = sqrt(min(d2a, d2b)) * sign(p.x - p.y);

                float t = saturate((p.y + p.x - 1.0) * 1000.0);
                return lerp(d2, d1, t) * size;
            }

            float RingSDF(float2 p, float outerRadius, float innerRadius)
            {
                return abs(length(p) - outerRadius) - innerRadius;
            }

            float CrossSDF(float2 p, float w, float h, float thickness)
            {
                float2 q = abs(p);

                float2 d1 = q - float2(w, thickness);
                float dist1 = length(max(d1, 0.0)) + min(max(d1.x, d1.y), 0.0);

                float2 d2 = q - float2(thickness, h);
                float dist2 = length(max(d2, 0.0)) + min(max(d2.x, d2.y), 0.0);

                return min(dist1, dist2);
            }

            float LineSDF(float2 p, float2 startPos, float2 endPos, float thickness)
            {
                float2 ba = endPos - startPos;
                float2 pa = p - startPos;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h) - thickness;
            }

            float RadialFill(float2 uv, float fillAmount, float fillOrigin)
            {
                float originRad = fillOrigin * PI / 180.0;
                float s, c;
                sincos(-originRad, s, c);
                float2 p = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);

                float angle = atan2(p.y, p.x);
                angle = angle < 0.0 ? angle + TWO_PI : angle;
                float fillAngle = fillAmount * TWO_PI;

                float fullFill = step(0.9999, fillAmount);
                float emptyFill = step(fillAmount, 0.0001);

                float inside = step(angle, fillAngle);
                float normalDist = lerp(angle - fillAngle, -min(angle, fillAngle - angle), inside);

                return lerp(lerp(normalDist, -1.0, fullFill), 1.0, emptyFill);
            }

            float HorizontalFill(float2 uv, float fillAmount)
            {
                return (uv.x + 0.5) - fillAmount;
            }

            float VerticalFill(float2 uv, float fillAmount)
            {
                return (uv.y + 0.5) - fillAmount;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 rectSize = _Rect_Size.xy;
                float2 centeredUV = TransformUV(i.uv, _Rotation, _Offset.xy);
                float2 pixelUV = centeredUV * rectSize;

                float dist = 0;

                #if defined(_SHAPE_CIRCLE)
                    dist = CircleSDF(pixelUV, _Radius);
                #elif defined(_SHAPE_RECTANGLE)
                    dist = RectangleSDF(pixelUV, _Width, _Height, _Corners, _Rounding);
                #elif defined(_SHAPE_POLYGON)
                    dist = PolygonSDF(pixelUV, _Size, _Inner_Radius, _Sides);
                #elif defined(_SHAPE_HEART)
                    dist = HeartSDF(pixelUV, _Size);
                #elif defined(_SHAPE_RING)
                    dist = RingSDF(pixelUV, _Outer_Radius, _Inner_Radius);
                #elif defined(_SHAPE_CROSS)
                    dist = CrossSDF(pixelUV, _Width, _Height, _Thickness);
                #elif defined(_SHAPE_LINE)
                    dist = LineSDF(pixelUV, _Start_Position.xy, _End_Position.xy, _Thickness);
                #else
                    dist = CircleSDF(pixelUV, _Radius);
                #endif

                float dd = abs(ddx(dist)) + abs(ddy(dist));
                float aa = max(dd, 0.001);
                float shapeMask = smoothstep(aa, -aa, dist);

                float outlineMask = 0.0;
                if (_Outline_Thickness > 0.0)
                {
                    float outlineDist = abs(dist) - _Outline_Thickness;
                    float ddOutline = abs(ddx(outlineDist)) + abs(ddy(outlineDist));
                    float aaOutline = max(ddOutline, 0.001);
                    float outerMask = smoothstep(aaOutline, -aaOutline, outlineDist);
                    outlineMask = saturate(outerMask - shapeMask);
                }

                float fillDist = -1.0;
                if (_Fill_Type == 1)
                    fillDist = RadialFill(centeredUV, _Fill_Amount, _Fill_Origin);
                else if (_Fill_Type == 2)
                    fillDist = HorizontalFill(centeredUV, _Fill_Amount);
                else if (_Fill_Type == 3)
                    fillDist = VerticalFill(centeredUV, _Fill_Amount);

                float filledDist = max(dist, fillDist);
                float ddFilled = abs(ddx(filledDist)) + abs(ddy(filledDist));
                float aaFilled = max(ddFilled, 0.001);
                float filledMask = smoothstep(aaFilled, -aaFilled, filledDist);

                float3 fillColor = _Base_Color.rgb * filledMask;
                float fillAlpha = filledMask * _Base_Color.a;

                float3 outColor = _Outline_Color.rgb * outlineMask;
                float outAlpha = outlineMask * _Outline_Color.a;

                float3 finalColor = fillColor + outColor;
                float finalAlpha = saturate(fillAlpha + outAlpha);

                finalColor *= i.color.rgb;
                finalAlpha *= i.color.a;

                // Premultiply for Blend One OneMinusSrcAlpha
                finalColor *= finalAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                    float clipMask = UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                    finalColor *= clipMask;
                    finalAlpha *= clipMask;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(finalAlpha - 0.001);
                #endif

                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
