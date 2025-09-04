Shader "Custom/UnifiedDissolve" {
    Properties {
        [MainTexture] _MainTex ("Primary (RGB)", 2D) = "white" {}
        [MainColor] _Color ("Primary Color", Color) = (1,1,1,1)
        _SecondTex ("Secondary (RGB)", 2D) = "white" {}
        _Color2 ("Secondary Color", Color) = (1,1,1,1)
        
        _NoiseTex ("Dissolve Noise", 2D) = "white" {} 
        _NScale ("Noise Scale", Range(0, 10)) = 1 
        _DisAmount ("Noise Cutoff", Range(0.01, 1)) = 0.01
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.5
        
        _DisLineWidth ("Line Width", Range(0, 2)) = 0.05
        [HDR]_DisLineColor ("Line Color", Color) = (1,1,1,1)
        
        _Radius ("Effect Radius", Range(0, 30)) = 5
        _ShapeType ("Shape Type (0=Sphere, 1=Box)", Float) = 0
        _BoxSize ("Box Size", Vector) = (1,1,1,0)
        _BoxRotation ("Box Rotation", Vector) = (0,0,0,0)
        _ShapeCutoff ("Shape Cutoff", Range(0, 1)) = 0.5
        _ShapeSmoothness ("Shape Smoothness", Range(0, 1)) = 0.1
        
        [KeywordEnum(SwapTextures, Appear, Disappear)] _STYLE ("Effect Style", Float) = 0
        [Toggle(_USE_MULTIPLE_INTERACTORS)] _UseMultipleInteractors ("Use Multiple Interactors", Float) = 0
        
        // URP-specific properties
        [HideInInspector] _BaseMap("BaseMap", 2D) = "white" {}
        [HideInInspector] _BaseColor("BaseColor", Color) = (1,1,1,1)
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _SpecColor("SpecColor", Color) = (0.2, 0.2, 0.2)
    }
 
    SubShader {
        Tags {
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300
        
        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _STYLE_SWAPTEXTURES _STYLE_APPEAR _STYLE_DISAPPEAR
            #pragma shader_feature_local _USE_MULTIPLE_INTERACTORS
            
            // Unity defined keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // Function for rotating boxes - defined globally for all passes
            float3 RotateAroundAxis(float3 position, float3 axis, float angle)
            {
                angle = radians(angle);
                float s = sin(angle);
                float c = cos(angle);
                float one_minus_c = 1.0 - c;
                
                axis = normalize(axis);
                float3x3 rot_mat;
                rot_mat[0] = float3(
                    one_minus_c * axis.x * axis.x + c,
                    one_minus_c * axis.x * axis.y - axis.z * s,
                    one_minus_c * axis.z * axis.x + axis.y * s
                );
                rot_mat[1] = float3(
                    one_minus_c * axis.x * axis.y + axis.z * s,
                    one_minus_c * axis.y * axis.y + c,
                    one_minus_c * axis.y * axis.z - axis.x * s
                );
                rot_mat[2] = float3(
                    one_minus_c * axis.z * axis.x - axis.y * s,
                    one_minus_c * axis.y * axis.z + axis.x * s,
                    one_minus_c * axis.z * axis.z + c
                );
                
                return mul(rot_mat, position);
            }
            
            // Interactor properties
            uniform float3 _Position;
            uniform float _ShapeType;
            uniform float3 _BoxSize;
            uniform float3 _BoxRotation;
            
            // Texture samplers
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SecondTex);
            SAMPLER(sampler_SecondTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            
            // Property variables
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _SecondTex_ST;
                half4 _Color;
                half4 _Color2;
                half4 _DisLineColor;
                float _DisAmount;
                float _NScale;
                float _DisLineWidth;
                float _NoiseStrength;
                float _Radius;
                float _ShapeCutoff;
                float _ShapeSmoothness;
                float _BaseColor;
            CBUFFER_END
            
            // Multiple interactor arrays
            float4 _ShaderInteractorsPositions[20];
            float _ShaderInteractorsRadiuses[20];
            float4 _ShaderInteractorsBoxBounds[20];
            float4 _ShaderInteractorRotation[20];
            int _InteractorCount;
            
            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float4 tangentOS : TANGENT;
                float4 color : COLOR;
            };
            
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 color : COLOR;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Transform position from object to world space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                
                // Transform normal from object to world space
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Pass through texture coordinates and vertex color
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                
                return output;
            }
            
            // Function to calculate box effect
            float CalculateBoxEffect(float3 position, float3 boxPosition, float3 boxSize, float3 boxRotation)
            {
                // Calculate local position
                float3 localPos = position - boxPosition;
                
                // Apply rotation
                float3 rotatedPos = localPos;
                rotatedPos = RotateAroundAxis(rotatedPos, float3(1,0,0), boxRotation.x);
                rotatedPos = RotateAroundAxis(rotatedPos, float3(0,1,0), boxRotation.y);
                rotatedPos = RotateAroundAxis(rotatedPos, float3(0,0,1), boxRotation.z);
                
                // Calculate box bounds
                float3 boxDistance = boxSize - abs(rotatedPos);
                
                // If all components are positive, point is inside box
                float boxMask = min(min(boxDistance.x, boxDistance.y), boxDistance.z);
                boxMask = saturate(boxMask); // Clamp to 0-1 range
                
                // Apply smoothing
                boxMask = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, boxMask);
                
                return boxMask;
            }
            
    half4 frag(Varyings input) : SV_Target
    {
        // Calculate interactor effect
        float interactorEffect = 0;
        
        #ifdef _USE_MULTIPLE_INTERACTORS
            float sphereEffect = 0;
            float boxEffect = 0;
            
            // Process all active interactors
            for (int i = 0; i < min(_InteractorCount, 20); i++) {
                float3 interactorPos = _ShaderInteractorsPositions[i].xyz;
                float shapeType = _ShaderInteractorsBoxBounds[i].w;
                
                if (shapeType < 0.5) { // Sphere shape
                    float dist = distance(interactorPos, input.positionWS);
                    float sphereRadius = 1.0 - saturate(dist / _ShaderInteractorsRadiuses[i]);
                    sphereRadius = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, sphereRadius);
                    sphereEffect += sphereRadius;
                }
                else { // Box shape
                    float3 boxSize = _ShaderInteractorsBoxBounds[i].xyz;
                    float3 boxRotation = _ShaderInteractorRotation[i].xyz;
                    float boxMask = CalculateBoxEffect(input.positionWS, interactorPos, boxSize, boxRotation);
                    boxEffect += boxMask;
                }
            }
            
            interactorEffect = saturate(sphereEffect + boxEffect); // Clamp to 0-1 range
        #else
            // Single interactor - calculate based on shape type
            if (_ShapeType < 0.5) { // Sphere shape
                float dist = distance(_Position, input.positionWS);
                interactorEffect = 1.0 - saturate(dist / _Radius);
                interactorEffect = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, interactorEffect);
            }
            else { // Box shape
                interactorEffect = CalculateBoxEffect(input.positionWS, _Position, _BoxSize, _BoxRotation);
            }
        #endif
        
        // Apply triplanar noise mapping for better-looking effects
        float3 blendNormal = saturate(pow(abs(input.normalWS) * 1.4, 4));
        blendNormal /= dot(blendNormal, 1.0);
        
        // Sample noise texture from three directions
        float4 noiseXY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.xy + _Time.y) * _NScale);
        float4 noiseXZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.xz + _Time.y) * _NScale);
        float4 noiseYZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.yz + _Time.y) * _NScale);
        
        // Blend noise samples based on normal direction
        float3 noiseValue = 
            noiseXY.rgb * blendNormal.z +
            noiseXZ.rgb * blendNormal.y +
            noiseYZ.rgb * blendNormal.x;
        
        // Combine noise with interactor effect
        float effectNoise = lerp(noiseValue.r * interactorEffect, interactorEffect, _NoiseStrength);
        
        // Create dissolve effect with cutoff
        float cutoff = step(_DisAmount, effectNoise);
        
        // Sample textures
        half4 c1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
        half4 c2 = SAMPLE_TEXTURE2D(_SecondTex, sampler_SecondTex, input.uv) * _Color2;
        
        // Create effect line
        float lineEffect = step(effectNoise - _DisLineWidth, _DisAmount) * cutoff;
        half3 dissolveLine = lineEffect * _DisLineColor.rgb;
        
        // Combine textures based on effect style
        half3 resultColor;
        
        #if defined(_STYLE_SWAPTEXTURES)
            resultColor = lerp(c1.rgb, c2.rgb, cutoff);
        #elif defined(_STYLE_APPEAR)
            // CHANGED: For appear style, always show primary texture (c1) and apply clipping
            resultColor = c1.rgb;
        #elif defined(_STYLE_DISAPPEAR)
            // For disappear style, can still swap textures if needed, or just use primary
            resultColor = c1.rgb;
        #else
            resultColor = lerp(c1.rgb, c2.rgb, cutoff);
        #endif
        
        // Add glow line
        resultColor += dissolveLine;
        
        // Apply different clipping based on style
        #if defined(_STYLE_APPEAR)
            clip(cutoff - 0.01);
        #elif defined(_STYLE_DISAPPEAR)
            clip(1.0 - (cutoff - lineEffect) - 0.01);
        #endif
        
        // Calculate lighting - simplified URP lighting
        // Get main light
        Light mainLight = GetMainLight();
        float3 normalWS = normalize(input.normalWS);
        float NdotL = saturate(dot(normalWS, mainLight.direction));
        float3 lighting = mainLight.color * NdotL;
        
        // Add ambient lighting
        float3 ambient = SampleSH(normalWS);
        lighting += ambient;
        
        // Combine lighting with albedo
        float3 finalColor = resultColor * lighting;
        finalColor += dissolveLine * _DisLineColor.a; // Add emission
        
        return half4(finalColor, c1.a);
    }
            ENDHLSL
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _STYLE_SWAPTEXTURES _STYLE_APPEAR _STYLE_DISAPPEAR
            
            // UNITY INCLUDES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            // Function for rotating boxes (duplicated for this pass)
            float3 RotateAroundAxis(float3 position, float3 axis, float angle)
            {
                angle = radians(angle);
                float s = sin(angle);
                float c = cos(angle);
                float one_minus_c = 1.0 - c;
                
                axis = normalize(axis);
                float3x3 rot_mat;
                rot_mat[0] = float3(
                    one_minus_c * axis.x * axis.x + c,
                    one_minus_c * axis.x * axis.y - axis.z * s,
                    one_minus_c * axis.z * axis.x + axis.y * s
                );
                rot_mat[1] = float3(
                    one_minus_c * axis.x * axis.y + axis.z * s,
                    one_minus_c * axis.y * axis.y + c,
                    one_minus_c * axis.y * axis.z - axis.x * s
                );
                rot_mat[2] = float3(
                    one_minus_c * axis.z * axis.x - axis.y * s,
                    one_minus_c * axis.y * axis.z + axis.x * s,
                    one_minus_c * axis.z * axis.z + c
                );
                
                return mul(rot_mat, position);
            }
            
            // Interactor properties
            uniform float3 _Position;
            uniform float _ShapeType;
            uniform float3 _BoxSize;
            uniform float3 _BoxRotation;
            
            // Texture samplers
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            
            // Property variables
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _DisAmount;
                float _NScale;
                float _DisLineWidth;
                float _NoiseStrength;
                float _Radius;
                float _ShapeCutoff;
                float _ShapeSmoothness;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
            };
            
            // Function to calculate box effect (same as above)
            float CalculateBoxEffect(float3 position, float3 boxPosition, float3 boxSize, float3 boxRotation)
            {
                float3 localPos = position - boxPosition;
                
                // Apply rotation
                float3 rotatedPos = localPos;
                rotatedPos = RotateAroundAxis(rotatedPos, float3(1,0,0), boxRotation.x);
                rotatedPos = RotateAroundAxis(rotatedPos, float3(0,1,0), boxRotation.y);
                rotatedPos = RotateAroundAxis(rotatedPos, float3(0,0,1), boxRotation.z);
                
                float3 boxDistance = boxSize - abs(rotatedPos);
                float boxMask = min(min(boxDistance.x, boxDistance.y), boxDistance.z);
                boxMask = saturate(boxMask);
                boxMask = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, boxMask);
                
                return boxMask;
            }
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                Light mainLight = GetMainLight();
                
                output.positionWS = positionWS;
                output.normalWS = normalWS;

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, mainLight.direction));
                output.positionCS = positionCS;

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                // Calculate effect based on shape type
                float effect = 0;
                
                if (_ShapeType < 0.5) { // Sphere shape
                    float dist = distance(_Position, input.positionWS);
                    effect = 1.0 - saturate(dist / _Radius);
                    effect = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, effect);
                }
                else { // Box shape
                    effect = CalculateBoxEffect(input.positionWS, _Position, _BoxSize, _BoxRotation);
                }
                
                // Apply triplanar noise mapping
                float3 blendNormal = saturate(pow(abs(input.normalWS) * 1.4, 4));
                blendNormal /= dot(blendNormal, 1.0);
                
                // Sample noise texture from three directions
                float4 noiseXY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.xy + _Time.y) * _NScale);
                float4 noiseXZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.xz + _Time.y) * _NScale);
                float4 noiseYZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.yz + _Time.y) * _NScale);
                
                // Blend noise samples based on normal direction
                float3 noiseValue = 
                    noiseXY.rgb * blendNormal.z +
                    noiseXZ.rgb * blendNormal.y +
                    noiseYZ.rgb * blendNormal.x;
                
                // Combine noise with effect
                float effectNoise = lerp(noiseValue.r * effect, effect, _NoiseStrength);
                
                // Apply different clipping based on style
                float cutoff = step(_DisAmount, effectNoise);
                float lineEffect = step(effectNoise - _DisLineWidth, _DisAmount) * cutoff;
                
                #if defined(_STYLE_APPEAR)
                    clip(cutoff - 0.01); // Keep clipping the same
                #elif defined(_STYLE_DISAPPEAR)
                    clip(1.0 - (cutoff - lineEffect) - 0.01); // Keep clipping the same
                #endif
                
                return 0;
            }
            ENDHLSL
        }
        
        // DepthOnly pass
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local _STYLE_SWAPTEXTURES _STYLE_APPEAR _STYLE_DISAPPEAR
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // Function for rotating boxes (duplicated for this pass)
            float3 RotateAroundAxis(float3 position, float3 axis, float angle)
            {
                angle = radians(angle);
                float s = sin(angle);
                float c = cos(angle);
                float one_minus_c = 1.0 - c;
                
                axis = normalize(axis);
                float3x3 rot_mat;
                rot_mat[0] = float3(
                    one_minus_c * axis.x * axis.x + c,
                    one_minus_c * axis.x * axis.y - axis.z * s,
                    one_minus_c * axis.z * axis.x + axis.y * s
                );
                rot_mat[1] = float3(
                    one_minus_c * axis.x * axis.y + axis.z * s,
                    one_minus_c * axis.y * axis.y + c,
                    one_minus_c * axis.y * axis.z - axis.x * s
                );
                rot_mat[2] = float3(
                    one_minus_c * axis.z * axis.x - axis.y * s,
                    one_minus_c * axis.y * axis.z + axis.x * s,
                    one_minus_c * axis.z * axis.z + c
                );
                
                return mul(rot_mat, position);
            }
            
            // Interactor properties
            uniform float3 _Position;
            uniform float _ShapeType;
            uniform float3 _BoxSize;
            uniform float3 _BoxRotation;
            
            // Texture samplers
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            
            // Property variables
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _DisAmount;
                float _NScale;
                float _DisLineWidth;
                float _NoiseStrength;
                float _Radius;
                float _ShapeCutoff;
                float _ShapeSmoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 position     : POSITION;
                float2 texcoord     : TEXCOORD0;
                float3 normal       : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Function to calculate box effect (same as above)
            float CalculateBoxEffect(float3 position, float3 boxPosition, float3 boxSize, float3 boxRotation)
            {
                float3 localPos = position - boxPosition;
                
                // Apply rotation
                float3 rotatedPos = localPos;
                rotatedPos = RotateAroundAxis(rotatedPos, float3(1,0,0), boxRotation.x);
                rotatedPos = RotateAroundAxis(rotatedPos, float3(0,1,0), boxRotation.y);
                rotatedPos = RotateAroundAxis(rotatedPos, float3(0,0,1), boxRotation.z);
                
                float3 boxDistance = boxSize - abs(rotatedPos);
                float boxMask = min(min(boxDistance.x, boxDistance.y), boxDistance.z);
                boxMask = saturate(boxMask);
                boxMask = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, boxMask);
                
                return boxMask;
            }

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.positionWS = TransformObjectToWorld(input.position.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normal);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Calculate effect based on shape type
                float effect = 0;
                
                if (_ShapeType < 0.5) { // Sphere shape
                    float dist = distance(_Position, input.positionWS);
                    effect = 1.0 - saturate(dist / _Radius);
                    effect = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, effect);
                }
                else { // Box shape
                    effect = CalculateBoxEffect(input.positionWS, _Position, _BoxSize, _BoxRotation);
                }
                
                // Apply triplanar noise mapping
                float3 blendNormal = saturate(pow(abs(input.normalWS) * 1.4, 4));
                blendNormal /= dot(blendNormal, 1.0);
                
                // Sample noise texture from three directions
                float4 noiseXY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.xy + _Time.y) * _NScale);
                float4 noiseXZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.xz + _Time.y) * _NScale);
                float4 noiseYZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.yz + _Time.y) * _NScale);
                
                // Blend noise samples based on normal direction
                float3 noiseValue = 
                    noiseXY.rgb * blendNormal.z +
                    noiseXZ.rgb * blendNormal.y +
                    noiseYZ.rgb * blendNormal.x;
                
                // Combine noise with effect
                float effectNoise = lerp(noiseValue.r * effect, effect, _NoiseStrength);
                
                // Apply different clipping based on style
                float cutoff = step(_DisAmount, effectNoise);
                float lineEffect = step(effectNoise - _DisLineWidth, _DisAmount) * cutoff;
                
                #if defined(_STYLE_APPEAR)
                    clip(cutoff - 0.01); // Keep clipping the same
                #elif defined(_STYLE_DISAPPEAR)
                    clip(1.0 - (cutoff - lineEffect) - 0.01); // Keep clipping the same
                #endif

                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}