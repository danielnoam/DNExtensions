Shader "PostProcess/UltimateOutline"
{
    Properties
    {
        [Header(Main Settings)]
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 5)) = 1
        [ToggleUI] _UseDynamicScale ("Dynamic Scale (Clean Distant Objects)", Float) = 0
        [ToggleUI] _UseAntiAliasing ("Use Anti-Aliasing", Float) = 1
        
        [Header(Threshold Settings)]
        _DepthThreshold ("Depth Threshold", Range(0, 10)) = 1
        _NormalThreshold ("Normal Threshold", Range(0, 2)) = 0.5
        _ColorSensitivity ("Color Sensitivity", Range(0, 5)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "OutlinePass"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _OutlineColor;
            float _OutlineThickness;
            
            float _UseDynamicScale; // Renamed
            float _DepthThreshold;
            
            float _NormalThreshold;
            float _ColorSensitivity; 
            
            float _UseAntiAliasing;

            float GetLinearDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            // Returns float3: x=Depth, y=Normal, z=Color
            float3 GetSobelValues(float2 uv, float2 texelSize, float centerDepth, float3 centerNormal, float3 centerColor, float depthSens, float normalSens, float colorSens)
            {
                float depthGX = 0; float depthGY = 0;
                float normalGX = 0; float normalGY = 0;
                float3 colorGX = 0; float3 colorGY = 0;

                // Optimization: Skip work if threshold is 0
                bool doDepth = depthSens > 0.001;
                bool doNormals = normalSens > 0.001;
                bool doColor = colorSens > 0.001;

                [unroll]
                for (int x = -1; x <= 1; x++)
                {
                    [unroll]
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;

                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = uv + offset;

                        float weightX = (x == 0) ? 0 : (x * (abs(y) == 0 ? 2 : 1));
                        float weightY = (y == 0) ? 0 : (y * (abs(x) == 0 ? 2 : 1));

                        // 1. DEPTH
                        if (doDepth)
                        {
                            float d = GetLinearDepth(sampleUV);
                            depthGX += d * weightX;
                            depthGY += d * weightY;
                        }

                        // 2. NORMALS
                        if (doNormals)
                        {
                            float3 n = SampleSceneNormals(sampleUV);
                            float dotVal = dot(centerNormal, n);
                            normalGX += dotVal * weightX;
                            normalGY += dotVal * weightY;
                        }

                        // 3. COLOR
                        if (doColor)
                        {
                            float3 c = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV).rgb;
                            colorGX += c * weightX;
                            colorGY += c * weightY;
                        }
                    }
                }

                float depthEdge = doDepth ? length(float2(depthGX, depthGY)) : 0;
                float normalEdge = doNormals ? length(float2(normalGX, normalGY)) : 0;
                float colorEdge = doColor ? (length(colorGX) + length(colorGY)) : 0;

                return float3(depthEdge, normalEdge, colorEdge);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float4 originalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                float rawDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    if(rawDepth <= 0.0001) return originalColor;
                #else
                    if(rawDepth >= 0.9999) return originalColor;
                #endif

                float2 texelSize = _BlitTexture_TexelSize.xy * _OutlineThickness;
                float centerDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                
                // Optimization: Skip fetching center normal if threshold is 0
                float3 centerNormal = float3(0,0,0);
                if (_NormalThreshold > 0.001) centerNormal = SampleSceneNormals(uv);

                // --- 1. DETECT EDGES ---
                float3 sobel = GetSobelValues(uv, texelSize, centerDepth, centerNormal, originalColor.rgb, _DepthThreshold, _NormalThreshold, _ColorSensitivity);
                
                float depthResult = sobel.x;
                float normalResult = sobel.y;
                float colorResult = sobel.z;

                // --- 2. CALCULATE SCALE FACTOR ---
                // If Dynamic Scale is ON, we increase the required threshold as objects get further away.
                // This makes the sensitivity "weaker" in the distance.
                float scaleFactor = 1.0;
                if (_UseDynamicScale > 0.5)
                {
                    // As depth increases, scale factor increases
                    scaleFactor = centerDepth;
                }

                // --- 3. PROCESS RESULTS (Apply Scale to ALL) ---
                
                // DEPTH
                // Note: We multiply threshold by scaleFactor. Higher threshold = harder to see lines.
                float finalDepthEdge = 0;
                if (_DepthThreshold > 0.001)
                {
                    // We use 0.1 multiplier here to keep the slider user-friendly (0-10 range)
                    finalDepthEdge = depthResult / max(_DepthThreshold * scaleFactor * 0.1, 0.001);
                }

                // NORMALS
                float finalNormalEdge = 0;
                if (_NormalThreshold > 0.001)
                {
                    // Scale applied here too!
                    finalNormalEdge = normalResult / max(_NormalThreshold * scaleFactor, 0.01);
                }

                // COLOR
                float finalColorEdge = 0;
                if (_ColorSensitivity > 0.001)
                {
                    // For Color, we usually divide by distance to reduce noise
                    // But since 'Sensitivity' is a multiplier, we DIVIDE the sensitivity by the scaleFactor
                    // (Less sensitive at distance)
                    finalColorEdge = colorResult * (_ColorSensitivity / max(scaleFactor, 1.0));
                }

                // --- 4. COMBINE ---
                float edge = max(finalDepthEdge, max(finalNormalEdge, finalColorEdge));

                // --- 5. AA / SHARPNESS ---
                if (_UseAntiAliasing > 0.5)
                {
                    float edgeWidth = clamp(fwidth(edge), 0.05, 0.3); 
                    edge = smoothstep(0.3 - edgeWidth, 0.3 + edgeWidth, edge);
                }
                else
                {
                    edge = smoothstep(0.1, 0.4, edge);
                    edge = saturate(edge * 20.0); 
                }

                return lerp(originalColor, _OutlineColor, edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}