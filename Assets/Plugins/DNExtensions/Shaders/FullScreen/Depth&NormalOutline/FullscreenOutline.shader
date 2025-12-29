Shader "Outline/FullScreenOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness("Outline Thickness", Range(0, 10)) = 1
        _DepthSensitivity("Depth Sensitivity", Range(0, 50)) = 10
        _NormalSensitivity("Normal Sensitivity", Range(0, 10)) = 1
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

            // Core URP Libraries
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            // REQUIRED: Include the Normals Texture library
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _EdgeThreshold;

            // ---------------------------------------------------------
            // EDGE DETECTION
            // ---------------------------------------------------------

            // Sobel Filter for Depth (Detects distance jumps)
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
                        float depth = LinearEyeDepth(SampleSceneDepth(uv + offset), _ZBufferParams);
                        
                        sobelX += depth * sobelXWeights[index];
                        sobelY += depth * sobelYWeights[index];
                        index++;
                    }
                }
                // Return magnitude
                return sqrt(sobelX * sobelX + sobelY * sobelY);
            }

            // Normals Edge (Detects corners/creases)
            // UPDATED: Now samples the actual texture instead of guessing
            float DetectNormalEdge(float2 uv, float2 texelSize)
            {
                // Sample 4 neighbors directly from the G-Buffer
                float3 normalC = SampleSceneNormals(uv);
                float3 normalL = SampleSceneNormals(uv + float2(-texelSize.x, 0));
                float3 normalR = SampleSceneNormals(uv + float2(texelSize.x, 0));
                float3 normalU = SampleSceneNormals(uv + float2(0, texelSize.y));
                float3 normalD = SampleSceneNormals(uv + float2(0, -texelSize.y));

                // Check how different the neighbors are
                float edgeX = length(normalR - normalL);
                float edgeY = length(normalU - normalD);

                return (edgeX + edgeY) * 0.5;
            }

            // ---------------------------------------------------------
            // FRAGMENT SHADER
            // ---------------------------------------------------------

            float4 Frag(Varyings input) : SV_Target
            {
                float4 originalColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float2 texelSize = _OutlineThickness * float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                
                // 1. Skybox Check
                float centerDepth = SampleSceneDepth(input.texcoord);
                // In standard URP (Reverse Z), the sky is at 0.0. 
                // We use a small epsilon to catch it safely.
                #if UNITY_REVERSED_Z
                    if(centerDepth <= 0.0001) return originalColor;
                #else
                    if(centerDepth >= 0.9999) return originalColor;
                #endif

                // 2. Calculate Edges
                float depthEdge = DetectDepthEdge(input.texcoord, texelSize) * _DepthSensitivity;
                float normalEdge = DetectNormalEdge(input.texcoord, texelSize) * _NormalSensitivity;

                // 3. Combine
                // We use max() to keep the strongest line
                float combinedEdge = max(depthEdge, normalEdge);

                // 4. Threshold & Cutoff
                // If the edge is weak (like a gentle slope), ignore it
                combinedEdge = smoothstep(_EdgeThreshold, _EdgeThreshold + 0.05, combinedEdge);
                combinedEdge = saturate(combinedEdge * 2.0);

                float3 finalColor = lerp(originalColor.rgb, _OutlineColor.rgb, combinedEdge);
                return float4(finalColor, originalColor.a);
            }
            ENDHLSL
        }
    }
}