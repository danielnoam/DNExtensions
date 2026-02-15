// ============================================================
// SDF Shape Functions
// All shapes take raw 0-1 UV, apply centering internally
// Rotation is handled separately via TransformUV
// ============================================================

// Shared UV transform: centers UV and applies rotation + offset
void TransformUV_float(float2 UV, float Rotation, float2 Offset, out float2 Out)
{
    // Center UV from 0-1 to -0.5 to 0.5, then apply offset
    float2 centered = UV - 0.5 + Offset;
    
    // Rotate around center (0,0) in centered space
    float rad = Rotation * 3.14159265359 / 180.0;
    float s, c;
    sincos(rad, s, c);
    Out = float2(
        centered.x * c - centered.y * s,
        centered.x * s + centered.y * c
    );
}

// ============================================================
// Shape SDFs
// All take centered UV (-0.5 to 0.5 range) and RectSize
// Return signed distance in pixel space (negative = inside)
// ============================================================

void CircleSDF_float(float2 UV, float Radius, float2 RectSize, out float Dist)
{
    float2 pixelUV = UV * RectSize;
    Dist = length(pixelUV) - Radius;
}

void RectangleSDF_float(float2 UV, float Width, float Height, float4 CornerRounding, float cornerRounding, float2 RectSize, out float Dist)
{
    float2 pixelUV = UV * RectSize;
    float2 q = abs(pixelUV);
    float2 size = float2(Width, Height);
    
    float top_mask = step(0.0, pixelUV.y);
    float right_mask = step(0.0, pixelUV.x);
    
    float left_side_rounding = lerp(CornerRounding.x, CornerRounding.y, top_mask);
    float right_side_rounding = lerp(CornerRounding.w, CornerRounding.z, top_mask);
    
    float r_individual = lerp(left_side_rounding, right_side_rounding, right_mask);
    float r = r_individual + cornerRounding;
    
    float2 d = q - size + r;
    Dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - r;
}

void PolygonSDF_float(float2 UV, float Size, float InnerRadius, int Points, float2 RectSize, out float Dist)
{
    float2 pixelUV = UV * RectSize;
    
    Points = clamp(Points, 3, 12);
    
    float m = lerp(2.0, float(Points), InnerRadius);
    
    float an = 3.14159265359 / float(Points);
    float en = 3.14159265359 / m;
    
    float2 acs = float2(cos(an), sin(an));
    float2 ecs = float2(cos(en), sin(en));
    
    float bn = fmod(atan2(pixelUV.x, -pixelUV.y) + 3.14159265359, 2.0 * an) - an;
    
    float r = length(pixelUV);
    float2 p = r * float2(cos(bn), abs(sin(bn)));
    
    p -= Size * acs;
    p += ecs * clamp(-dot(p, ecs), 0.0, Size * acs.y / ecs.y);
    
    Dist = length(p) * sign(p.x);
}

void HeartSDF_float(float2 UV, float Size, float2 RectSize, out float Dist)
{
    // Guard against zero size
    if (Size <= 0.001)
    {
        Dist = 1000.0;
        return;
    }
    
    float2 pixelUV = UV * RectSize;
    
    // Normalize to unit heart space
    float2 p = pixelUV / (Size * 1.65);
    
    // Flip and center vertically
    p.y = p.y + 0.6;
    p.x = abs(p.x);
    
    // Branchless heart SDF (IQ formula)
    float s = max(p.x + p.y, 0.0);
    
    float d1 = sqrt(dot(p - float2(0.25, 0.75), p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;
    
    float d2a = dot(p - float2(0.0, 1.0), p - float2(0.0, 1.0));
    float d2b = dot(p - 0.5 * s * float2(1.0, 1.0), p - 0.5 * s * float2(1.0, 1.0));
    float d2 = sqrt(min(d2a, d2b)) * sign(p.x - p.y);
    
    float t = saturate((p.y + p.x - 1.0) * 1000.0);
    
    Dist = lerp(d2, d1, t) * Size;
}
void RingSDF_float(float2 UV, float OuterRadius, float InnerRadius, float2 RectSize, out float Dist)
{
    float2 pixelUV = UV * RectSize;
    float d = length(pixelUV) - OuterRadius;
    Dist = abs(d) - InnerRadius;
}

void CrossSDF_float(float2 UV, float Width, float Height, float Thickness, float2 RectSize, out float Dist)
{
    float2 pixelUV = UV * RectSize;
    float2 p = abs(pixelUV);
    
    float2 d1 = p - float2(Width, Thickness);
    float dist1 = length(max(d1, 0.0)) + min(max(d1.x, d1.y), 0.0);
    
    float2 d2 = p - float2(Thickness, Height);
    float dist2 = length(max(d2, 0.0)) + min(max(d2.x, d2.y), 0.0);
    
    Dist = min(dist1, dist2);
}

void LineSDF_float(float2 UV, float2 StartPos, float2 EndPos, float Thickness, float2 RectSize, out float Dist)
{
    float2 pixelUV = UV * RectSize;
    
    float2 ba = EndPos - StartPos;
    float2 pa = pixelUV - StartPos;
    
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    
    Dist = length(pa - ba * h) - Thickness;
}


// ============================================================
// Fill Functions
// All take centered UV (-0.5 to 0.5 range, pre-rotation)
// Return signed distance: negative = inside fill region
// ============================================================

void RadialFillSDF_float(float2 UV, float FillAmount, float FillOrigin, out float Dist)
{
    float originRad = FillOrigin * 3.14159265359 / 180.0;
    float s, c;
    sincos(-originRad, s, c);
    float2 p = float2(UV.x * c - UV.y * s, UV.x * s + UV.y * c);

    float angle = atan2(p.y, p.x);
    angle = angle < 0.0 ? angle + 6.28318530718 : angle;
    float fillAngle = FillAmount * 6.28318530718;
    
    // Branchless edge cases
    float fullFill = step(0.9999, FillAmount);
    float emptyFill = step(FillAmount, 0.0001);
    
    // Normal case: distance to fill boundary
    float inside = step(angle, fillAngle);
    float distToStart = angle;
    float distToEnd = fillAngle - angle;
    float normalDist = lerp(angle - fillAngle, -min(distToStart, distToEnd), inside);
    
    // Select based on edge cases
    Dist = lerp(lerp(normalDist, -1.0, fullFill), 1.0, emptyFill);
}

void HorizontalFillSDF_float(float2 UV, float FillAmount, out float Dist)
{
    // UV is centered: -0.5 to 0.5
    // Map to 0-1 range for fill comparison
    float normalizedX = UV.x + 0.5;
    Dist = normalizedX - FillAmount;
}

void VerticalFillSDF_float(float2 UV, float FillAmount, out float Dist)
{
    float normalizedY = UV.y + 0.5;
    Dist = normalizedY - FillAmount;
}


// ============================================================
// Outline SDF
// ============================================================

void OutlineSDF_float(float Distance, float Thickness, out float Dist)
{
    Dist = abs(Distance) - Thickness;
}


// ============================================================
// Main Render Function
// Takes the raw SDF distance and all style parameters,
// outputs final BaseColor and Alpha.
//
// This replaces all the smoothstep/subtract/saturate nodes
// in the shader graph.
// ============================================================

void RenderSDF_float(float ShapeDist,float2 CenteredUV,int FillType,float FillAmount,float FillOrigin,float4 BaseColor,float OutlineThickness,float4 OutlineColor,out float3 OutColor,out float OutAlpha)
{
    // --- Anti-aliasing width from screen-space derivatives ---
    float dd = abs(ddx(ShapeDist)) + abs(ddy(ShapeDist));
    float aa = max(dd, 0.001);
    
    // --- Shape mask (before fill) - used for outline ---
    float shapeMask = smoothstep(aa, -aa, ShapeDist);
    
    // --- Outline mask: band around original shape edge ---
    float outlineMask = 0.0;
    if (OutlineThickness > 0.0)
    {
        float outlineDist = abs(ShapeDist) - OutlineThickness;
        float ddOutline = abs(ddx(outlineDist)) + abs(ddy(outlineDist));
        float aaOutline = max(ddOutline, 0.001);
        float outerMask = smoothstep(aaOutline, -aaOutline, outlineDist);
        // Outline is only the part outside the shape
        outlineMask = saturate(outerMask - shapeMask);
    }
    
    // --- Apply fill to shape ---
    float fillDist = -1.0; // Default: fully filled
    
    if (FillType == 1) // Radial
    {
        RadialFillSDF_float(CenteredUV, FillAmount, FillOrigin, fillDist);
    }
    else if (FillType == 2) // Horizontal
    {
        HorizontalFillSDF_float(CenteredUV, FillAmount, fillDist);
    }
    else if (FillType == 3) // Vertical
    {
        VerticalFillSDF_float(CenteredUV, FillAmount, fillDist);
    }
    
    // Filled shape = intersection of shape and fill region
    float filledDist = max(ShapeDist, fillDist);
    float ddFilled = abs(ddx(filledDist)) + abs(ddy(filledDist));
    float aaFilled = max(ddFilled, 0.001);
    float filledMask = smoothstep(aaFilled, -aaFilled, filledDist);
    
    // --- Composite layers ---
    // Fill region gets base color
    float3 fillColor = BaseColor.rgb * filledMask;
    float fillAlpha = filledMask * BaseColor.a;
    
    // Outline gets outline color (never affected by fill)
    float3 outColor = OutlineColor.rgb * outlineMask;
    float outAlpha = outlineMask * OutlineColor.a;
    
    // Combine: outline behind fill (outline only visible outside shape)
    OutColor = fillColor + outColor;
    OutAlpha = saturate(fillAlpha + outAlpha);
    
    // Premultiply for Blend One OneMinusSrcAlpha
    OutColor *= OutAlpha > 0.001 ? 1.0 : 0.0;
}
