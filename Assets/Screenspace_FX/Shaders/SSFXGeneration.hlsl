#ifndef SSFX_UTILS_GENERATION
#define SSFX_UTILS_GENERATION

#include "ParticlesCommons.cginc"
#include "Assets/Commons/HypesioCommons.cginc"

#define DEBUG_RENDER_MESH 0
#if DEBUG_RENDER_MESH
    #define DEBUG_RENDER_MESH_COLOR half4(0, 1, 0, 0.0)
#else
    #define DEBUG_RENDER_MESH_COLOR 
#endif

uniform RWStructuredBuffer<ParticleDatas> _ParticlesDatasBuffer : register(u1);
uniform RWBuffer<int> _ParticlesDrawArgs : register(u2);
#define _ParticlesCounter _ParticlesDrawArgs[1]

// Uniform set for whole pass
uniform int _MaxParticlesCount;
uniform Texture2D<float4> _CameraColor;
SamplerState sampler_CameraColor;
uniform Texture2D<float4> _GBuffer2;
SamplerState sampler_GBuffer2;

// Uniforms set once from SSFX particle system
// indexConfig, durationMin, durationMax, SpawnRate
uniform float4 _ParticleEmissionData;
// minStartSize, maxStartSize, minStartSpeed, maxStartSpeed
uniform float4 _ParticleEmissionData2;
// startSpeedType, startSpeedX, startSpeedY, startSpeedZ
uniform float4 _ParticleEmissionData3;
// IsContinuousEmetter, IsAlphaWorldSpace, IsEmetterInvsibile, SplineIndex
uniform float4 _ParticleEmissionData4;

#define _IsContinousEmmeter _ParticleEmissionData4.x
#define _IsAlphaWorldSpace _ParticleEmissionData4.y
#define _IsEmetterinvisible _ParticleEmissionData4.z

// Data update each frame
// SpherePosition.x, SpherePosition.x, SpherePosition.x, SphereRadius
uniform float4 _ParticleSphereEffectData;
// SpherePosition.x, SpherePosition.x, SpherePosition.x, SphereRadius
uniform float4 _PreviousParticleSphereEffectData;

#define _SpherePosition _ParticleSphereEffectData.xyz
#define _PreviousSpherePosition _PreviousParticleSphereEffectData.xyz
#define _SphereRadius _ParticleSphereEffectData.w
#define _PreviousSphereRadius _PreviousParticleSphereEffectData.w
#define _SplineToFollowIndex _ParticleEmissionData4.w;

// Uniforms set from mat
uniform float _TimeProgressEffect;
uniform float _durationEffect;
uniform Texture2D<float> _AlphaMap;
float4 _AlphaMap_ST;
SamplerState sampler_AlphaMap;
uniform float _WorldSpaceAlpha;

inline float3 GetStartSpeedDirection(float3 normal, float3 localPosition)
{
    if (_ParticleEmissionData3.x == 0)
        return normal;
    if (_ParticleEmissionData3.x == 1)
        return localPosition;
    if (_ParticleEmissionData3.x == 2)
        return -localPosition;
    if (_ParticleEmissionData3.x == 3)
        return float3(_ParticleEmissionData3.y, _ParticleEmissionData3.z, _ParticleEmissionData3.w);
    return 0;
}

inline float GetParticleDuration(float randomValue)
{
    return lerp(_ParticleEmissionData.y, _ParticleEmissionData.z, randomValue);
}

inline float GetParticleInitialSpeed(float randomValue) 
{
    return lerp(_ParticleEmissionData2.z, _ParticleEmissionData2.w, randomValue);
}

inline float GetParticleInitialSize(float randomValue)
{
    return lerp(_ParticleEmissionData2.x, _ParticleEmissionData2.y, randomValue);
}

inline int IsPixelDiscardTimed(float timePassed, float2 uv, float3 worldPosition)
{
    if (timePassed > 0)
    {
        if (timePassed > _durationEffect)
            return 1;
        
        // Is alpha world space
        if (_IsAlphaWorldSpace >= 1)
            uv = normalize(worldPosition.xy) * 5; 

        float alpha = _AlphaMap.SampleLevel(sampler_AlphaMap, uv, 0).x;
        float actualAlphaCutout = saturate(timePassed / _durationEffect);

        if (alpha < actualAlphaCutout)
            return 1;
    }
    return 0;
}

inline int IsPixelDiscardSphere(float3 worldPosition, float3 spherePosition, float sphereRadius)
{
    float dist = length(worldPosition - spherePosition);
    if (dist < sphereRadius)
        return 1;
    return 0;
}

// Call from material shader
inline int IsPixelDiscard(float2 uv, float3 worldPosition)
{
    float timePassed = _TimeProgressEffect;
    float isDiscard = IsPixelDiscardTimed(timePassed, uv, worldPosition);
    // use previous position and radius for sphere to be sure generated particles will still have colors
    return _IsEmetterinvisible || isDiscard || IsPixelDiscardSphere(worldPosition, _PreviousSpherePosition, _PreviousSphereRadius);
}

struct VertexInputSSFXGeneration
{
    float4 vertexPosition  : POSITION;     
    float3 vertexNormal     : NORMAL;
    float2 uv           : TEXCOORD0;     
};

struct FragmentInputSSFXGeneration
{
    float4 fragmentPosition  : SV_POSITION;
    float3 worldPosition : TEXCOORD0;
    float2 uv : TEXCOORD1;
    float3 localPosition : TEXCOORD2;
    float3 fragNormal : NORMAL;
};            

FragmentInputSSFXGeneration vertSSFXGeneration(VertexInputSSFXGeneration i)
{
    FragmentInputSSFXGeneration o;

    o.worldPosition = TransformObjectToWorld(i.vertexPosition.xyz);
    o.fragmentPosition = TransformWorldToHClip(o.worldPosition);
    o.uv = TRANSFORM_TEX(i.uv, _AlphaMap);
    o.fragNormal = i.vertexNormal;
    o.localPosition = i.vertexPosition.xyz;
    return o;
}

/** 
Emit particles if new part of the mesh are transparent 
Enable DEBUG_RENDER_MESH to check if a mesh is render in this pass
**/
#if DEBUG_RENDER_MESH
float4 fragSSFXGeneration(FragmentInputSSFXGeneration i) : SV_Target
#else 
void fragSSFXGeneration(FragmentInputSSFXGeneration i)
#endif
{
    float randomFloat = RandomFloat(i.fragmentPosition.xy + float2(_Time_SSFX.x, _Time_SSFX.x));

    if (_ParticlesCounter >= _MaxParticlesCount || _ParticleEmissionData.w < randomFloat)
        return DEBUG_RENDER_MESH_COLOR;

    float timePassed = _TimeProgressEffect;

    if (timePassed <= 0)
        return DEBUG_RENDER_MESH_COLOR;

    // Check if the fragment will disappear in next frame
    // If visible now but not on next frame => Generate particles
    float nextTime = (timePassed + _Time_SSFX.y);
    if (_IsContinousEmmeter 
        || (!IsPixelDiscardTimed(timePassed, i.uv, i.worldPosition) && IsPixelDiscardTimed(nextTime, i.uv, i.worldPosition))
        || (IsPixelDiscardSphere(i.worldPosition, _SpherePosition, _SphereRadius) && !IsPixelDiscardSphere(i.worldPosition, _PreviousSpherePosition, _PreviousSphereRadius)))
    {
        // Get the position in the append buffer 
        // and increase counter for other fragments
        int index = 0;
        InterlockedAdd(_ParticlesCounter, 1, index);
        if (index >= _MaxParticlesCount)
            return DEBUG_RENDER_MESH_COLOR;

        // Add values from GBuffer to the particles datas buffer
        float2 fragCoords = i.fragmentPosition.xy / _ScreenParams.xy;

        ParticleDatas particle;
        particle.worldPosition = i.worldPosition.xyzz;
        particle.color = _CameraColor.SampleLevel(sampler_CameraColor, fragCoords, 0).xyz;
        particle.startColor = particle.color;
        #if OVERRIDE_MAT 
            // Taking normal from GBuffer allow to use the real normal displayed on screen
            particle.normal = _GBuffer2.SampleLevel(sampler_GBuffer2, fragCoords, 0).xyz;
        #else
            particle.normal = i.fragNormal;
        #endif

        particle.timeApparition = _Time_SSFX.x;
        particle.duration = GetParticleDuration(RandomFloat(i.fragmentPosition.xy));
        float2 randomValue = RandomFloat2(fragCoords);
        
        particle.indexConfig = _ParticleEmissionData.x;
        particle.startSpeed = _ParticleEmissionData2.z;//GetParticleInitialSpeed(RandomFloat(i.fragmentPosition.xy * 2.0));
        particle.speed = normalize(GetStartSpeedDirection(particle.normal, i.localPosition)) * particle.startSpeed;
        particle.startSize = GetParticleInitialSize(RandomFloat(i.fragmentPosition.xy * 5.0)); 
        particle.size = particle.startSize;
        particle.splineFollowIndex = (int)_SplineToFollowIndex;
        particle.splineCurrentStep = 0;
        particle.splineOffset = randomFloat;
        _ParticlesDatasBuffer[index] = particle;
    }
    
    return DEBUG_RENDER_MESH_COLOR;
}




#endif //SSFX_UTILS_GENERATION