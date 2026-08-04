float2 hash(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

void StarsFunc_float(float3 WorldPosition, float4 Color, float Size, float Softness, float HorizonFade, UnityTexture2D MaskTexture, UnitySamplerState MaskSampler, out float4 Out)
{
    float3 dir = normalize(WorldPosition);

    float2 sphericalUV = float2(atan2(dir.z, dir.x), asin(clamp(dir.y, -1.0, 1.0)));
    float2 uv = sphericalUV * Size / 3.14159;

    float2 cellId = floor(uv);
    float2 cellFrac = frac(uv);

    float star = 0;
    float brightness = 0;

    [unroll] for (int x = -1; x <= 1; x++)
        [unroll] for (int y = -1; y <= 1; y++)
        {
            float2 offset = float2(x, y);
            float2 h = hash(cellId + offset);
            float d = length(offset + h - cellFrac);
            float s = smoothstep(Softness, 0.0, d);
            if (s > star) { star = s; brightness = h.x; }
        }

    float horizonMask = saturate(dir.y / HorizonFade);
    float2 maskUV = sphericalUV / (2.0 * 3.14159) + 0.5;
    float maskValue = SAMPLE_TEXTURE2D(MaskTexture.tex, MaskSampler.samplerstate, maskUV).r;

    Out = Color * (star * brightness * horizonMask * maskValue);
}