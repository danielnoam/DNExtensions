Shader "PostProcess/BasicOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness("Outline Thickness", Range(0, 10)) = 1
        
        // SENSITIVITY CONTROLS
        _DepthSensitivity("Depth Sensitivity", Range(0, 50)) = 10
        _NormalSensitivity("Normal Sensitivity", Range(0, 10)) = 1
        _ColorSensitivity("Color Sensitivity", Range(0, 10)) = 1 
        
        _EdgeThreshold("Edge Threshold", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "OutlinePass"
            ZTest Always
            ZWrite Off
            Cull Off

HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _ColorSensitivity; 
            float _EdgeThreshold;

            // ---------------------------------------------------------
            // 1. DEPTH (Sobel Filter - High Quality)
            // ---------------------------------------------------------
            float DetectDepthEdge(float2 uv, float2 texelSize)
            {
                float sobelX = 0.0; float sobelY = 0.0;
                float sobelXWeights[9] = { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
                float sobelYWeights[9] = { -1, -2, -1, 0, 0, 0, 1, 2, 1 };

                int index = 0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        float depth = Linear01Depth(SampleSceneDepth(uv + offset), _ZBufferParams);
                        
                        sobelX += depth * sobelXWeights[index];
                        sobelY += depth * sobelYWeights[index];
                        index++;
                    }
                }
                return sqrt(sobelX * sobelX + sobelY * sobelY);
            }

            // ---------------------------------------------------------
            // 2. NORMALS (Cross Sample - Efficient)
            // ---------------------------------------------------------
            float DetectNormalEdge(float2 uv, float2 texelSize)
            {
                float3 normalC = SampleSceneNormals(uv);
                float3 normalL = SampleSceneNormals(uv + float2(-texelSize.x, 0));
                float3 normalR = SampleSceneNormals(uv + float2(texelSize.x, 0));
                float3 normalU = SampleSceneNormals(uv + float2(0, texelSize.y));
                float3 normalD = SampleSceneNormals(uv + float2(0, -texelSize.y));

                float edgeX = length(normalR - normalL);
                float edgeY = length(normalU - normalD);
                return (edgeX + edgeY) * 0.5;
            }

            // ---------------------------------------------------------
            // 3. COLOR (NEW: Detects hue/brightness changes)
            // ---------------------------------------------------------
            float DetectColorEdge(float2 uv, float2 texelSize)
            {
                // We use SAMPLE_TEXTURE2D_X to read the main screen color
                float3 colorC = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float3 colorL = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-texelSize.x, 0)).rgb;
                float3 colorR = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize.x, 0)).rgb;
                float3 colorU = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, texelSize.y)).rgb;
                float3 colorD = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, -texelSize.y)).rgb;

                // Calculate difference in brightness/hue
                float edgeX = length(colorR - colorL);
                float edgeY = length(colorU - colorD);
                
                return (edgeX + edgeY) * 0.5;
            }

            // ---------------------------------------------------------
            // FRAGMENT SHADER
            // ---------------------------------------------------------
            float4 Frag(Varyings input) : SV_Target
            {
                float4 originalColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float2 texelSize = _OutlineThickness * float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                
                // --- SKYBOX CHECK ---
                float centerDepth = SampleSceneDepth(input.texcoord);
                #if UNITY_REVERSED_Z
                    if(centerDepth <= 0.0001) return originalColor;
                #else
                    if(centerDepth >= 0.9999) return originalColor;
                #endif

                // --- CALCULATE ALL 3 EDGES ---
                float depthEdge = DetectDepthEdge(input.texcoord, texelSize) * _DepthSensitivity;
                float normalEdge = DetectNormalEdge(input.texcoord, texelSize) * _NormalSensitivity;
                float colorEdge = DetectColorEdge(input.texcoord, texelSize) * _ColorSensitivity; // <--- NEW CALC

                // --- COMBINE THEM ---
                // We use max() to ensure if ANY detection sees an edge, it draws it.
                float combinedEdge = max(depthEdge, max(normalEdge, colorEdge));

                // --- THRESHOLD & DRAW ---
                combinedEdge = smoothstep(_EdgeThreshold, _EdgeThreshold + 0.05, combinedEdge);
                combinedEdge = saturate(combinedEdge * 2.0);

                float3 finalColor = lerp(originalColor.rgb, _OutlineColor.rgb, combinedEdge);
                return float4(finalColor, originalColor.a);
            }
            ENDHLSL
        }
    }
}