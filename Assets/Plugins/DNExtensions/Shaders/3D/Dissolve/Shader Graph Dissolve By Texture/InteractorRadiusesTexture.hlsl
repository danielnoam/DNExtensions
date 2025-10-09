float4 _ShaderInteractorsPositions[100];
float4 _ShaderInteractorsBoxBounds[100];
float4 _ShaderInteractorRotation[100];
float _ShaderInteractorsIndices[100];

float _ShapeCutoff;
float _ShapeSmoothness;


float3 Unity_RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation)
{
    Rotation = radians(Rotation);
    float s = sin(Rotation);
    float c = cos(Rotation);
    float one_minus_c = 1.0 - c;

    Axis = normalize(Axis);
    float3x3 rot_mat = 
    {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
    };
    return float3(mul(rot_mat,  In));
}

void InteractorRadiusesTexture_float(in float3 WorldPos, in UnityTexture2DArray shape, in SamplerState ss, out float Shapes) // multiple textures
//void InteractorRadiusesTexture_float(in float3 WorldPos, in UnityTexture2D shape, in SamplerState ss, out float Shapes)  // single texture
{
	#ifdef SHADERGRAPH_PREVIEW
        Shapes = 0;
	#else
		
        float boxes = 0;

        for  (int i = 0; i < 100; i++)
        {
            // boxes
            // rotate
            float3 rotation = _ShaderInteractorRotation[i].xyz;
            float3 rotatedPos = Unity_RotateAboutAxis_Degrees_float(_ShaderInteractorsPositions[i].xyz- WorldPos, float3(-1,0,0), rotation.x); 
            rotatedPos = Unity_RotateAboutAxis_Degrees_float(rotatedPos, float3(0,-1,0), rotation.y); 
            rotatedPos = Unity_RotateAboutAxis_Degrees_float(rotatedPos, float3(0,0,-1), rotation.z);
    
             // scale
            float3 scale = _ShaderInteractorsBoxBounds[i].xyz;
            
           // create the UV
            float2 uv = rotatedPos.xz / (scale.xz);

			uv += 0.5;
            
            float3 BoxPos = saturate(scale - abs(rotatedPos));

            // single texture
           // float tex = SAMPLE_TEXTURE2D(shape,ss, uv) * BoxPos.y;
             // texture array
            float tex = SAMPLE_TEXTURE2D_ARRAY(shape,ss, uv, _ShaderInteractorsIndices[i]) * BoxPos.y;

            // cutoff using the texture instead of a full box
            float3 boxCutoff = (smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness,tex));
            boxes +=  saturate(boxCutoff.r * boxCutoff.g * boxCutoff.b);
           
       }
       // all combined      
        Shapes = saturate(boxes);
	#endif
}