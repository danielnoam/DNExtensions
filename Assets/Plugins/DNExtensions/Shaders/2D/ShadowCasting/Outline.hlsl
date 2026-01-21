#ifndef OUTLINE_HLSL
#define OUTLINE_HLSL

// ...

#define MAX_OUTLINE_RADIUS 4

bool IsTransparent(float alpha)
{
    return alpha < 0.5;
}

void GetOutline_float(Texture2D tex, float2 uv, float2 texelSize, int radius, out float4 output)
{
    output = SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, uv);
        
    if (!IsTransparent(output.a))
    {
        output.a = 0.0;
        return;
    }

    // Loop full max range, but conditionally skip outside real radius.
    
    for (int x = -MAX_OUTLINE_RADIUS; x <= MAX_OUTLINE_RADIUS; x++)
    {
        for (int y = -MAX_OUTLINE_RADIUS; y <= MAX_OUTLINE_RADIUS; y++)
        {
            // Skip anything beyond actual ["requested"] radius.
            
            if (abs(x) > radius || abs(y) > radius)
            {
                continue;
            }

            float2 offset = float2(x, y) * texelSize;
            output = SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, uv + offset);
                        
            // Edge detected.
            
            if (!IsTransparent(output.a))
            {
                output.a = 1.0;                
                return; 
            }
        }
    }
}

#endif