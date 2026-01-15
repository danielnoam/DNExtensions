
void CircleSDF_float(float2 UV, float Radius, out float Dist)
{
    Dist = length(UV) - Radius;
}

void RectangleSDF_float(float2 UV, float Width, float Height, float4 CornerRounding, float cornerRounding, out float Dist)
{
    float2 centered = UV;
    float2 q = abs(centered);
    float2 size = float2(Width, Height);
    
    float top_mask = step(0.0, centered.y);
    float right_mask = step(0.0, centered.x);
    
    float left_side_rounding = lerp(CornerRounding.x, CornerRounding.y, top_mask);
    float right_side_rounding = lerp(CornerRounding.w, CornerRounding.z, top_mask);
    
    float r_individual = lerp(left_side_rounding, right_side_rounding, right_mask);
    
    float r = r_individual + cornerRounding;
    
    float2 d = q - size + r;
    Dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - r;
}


void PolygonSDF_float(float2 UV, float Size, float InnerRadius, int Points, out float Dist)
{
    Points = clamp(Points, 3, 12);
    
    // Map InnerRadius (0-1) to m parameter (2 to Points)
    float m = lerp(2.0, float(Points), InnerRadius);
    
    float an = 3.14159265359 / float(Points);
    float en = 3.14159265359 / m;
    
    float2 acs = float2(cos(an), sin(an));
    float2 ecs = float2(cos(en), sin(en));
    
    // Get angle and fold into one sector
    float bn = fmod(atan2(UV.y, UV.x) + 3.14159265359, 2.0 * an) - an;
    
    // Convert back to cartesian in folded space
    float r = length(UV);
    float2 p = r * float2(cos(bn), abs(sin(bn)));
    
    // Distance calculation
    p -= Size * acs;
    p += ecs * clamp(-dot(p, ecs), 0.0, Size * acs.y / ecs.y);
    
    Dist = length(p) * sign(p.x);
}

void HeartSDF_float(float2 UV, float Size, out float Dist)
{
    float2 p = UV / (Size * 2.0);
    
    p.y += 0.5;
    
    p.x = abs(p.x);
    
    if (p.y + p.x > 1.0)
    {
        Dist = sqrt(dot(p - float2(0.25, 0.75), p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;
    }
    else
    {
        float d1 = dot(p - float2(0.0, 1.0), p - float2(0.0, 1.0));
        float d2 = dot(p - 0.5 * max(p.x + p.y, 0.0), p - 0.5 * max(p.x + p.y, 0.0));
        Dist = sqrt(min(d1, d2)) * sign(p.x - p.y);
    }
    
    Dist *= Size;
}


void RingSDF_float(float2 UV, float OuterRadius, float InnerRadius, out float Dist)
{
    float d = length(UV) - OuterRadius;
    Dist = abs(d) - InnerRadius;
}


void CrossSDF_float(float2 UV, float Width, float Height, float Thickness, out float Dist)
{
    float2 p = abs(UV);
    
    // Horizontal bar
    float2 d1 = p - float2(Width, Thickness);
    float dist1 = length(max(d1, 0.0)) + min(max(d1.x, d1.y), 0.0);
    
    // Vertical bar
    float2 d2 = p - float2(Thickness, Height);
    float dist2 = length(max(d2, 0.0)) + min(max(d2.x, d2.y), 0.0);
    
    Dist = min(dist1, dist2);
}





void OutlineSDF_float(float Distance, float Thickness, out float Dist)
{
    Dist = abs(Distance) - Thickness;
}
void InlineSDF_float(float Distance, float Thickness, out float Dist)
{
    Dist = max(Distance, -Distance - Thickness);
}