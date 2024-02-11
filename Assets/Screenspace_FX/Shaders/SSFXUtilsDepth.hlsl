#include "SSFXGeneration.hlsl"

// **
// Depth Prepass to only generate particles from visible triangles
// **
struct VertexInputDepthSSFX
{
    float4 vertexPosition  : POSITION;     
    float3 vertexNormal     : NORMAL;
    float2 uv           : TEXCOORD0;     
};

struct FragmentInputDepthSSFX
{
    float4 fragmentPosition  : SV_POSITION;
    float2 uv : TEXCOORD0;
};            

FragmentInputDepthSSFX vertDepthSSFX(VertexInputDepthSSFX i)
{
    FragmentInputDepthSSFX o;

    o.fragmentPosition = TransformObjectToHClip(i.vertexPosition.xyz);
    o.uv = TRANSFORM_TEX(i.uv, _AlphaMap);
    return o;
}

void fragDepthSSFX(FragmentInputDepthSSFX i)
{
    if (IsPixelDiscard(i.uv, float3(0, 0, 0)))
        discard;
}