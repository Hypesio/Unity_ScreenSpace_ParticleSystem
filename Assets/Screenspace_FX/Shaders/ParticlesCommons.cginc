#ifndef PARTICLES_COMMONS
#define PARTICLES_COMMONS


#define INDIRECT_DRAW_ARGS_SIZE 8

// x = time since startup in second, 
// y = deltatime 
uniform float4 _Time_SSFX; 

// If change are made here, it MUST be replicated to SSFXRenderPassUtils.cs
struct ParticleDatas
{
    float4 worldPosition;
    float3 color;
    float3 startColor;
    float timeApparition;
    float3 normal;
    float duration;
    float3 speed;
    float indexConfig;
    float startSize;
    float size;
    float startSpeed;
};

#define SSFX_MAX_GRAD_KEYS 10
#define FLAG_GRAVITY_MODIFIER (1 << 1)
#define FLAG_COLOR_OVER_LIFETIME (1 << 2)
#define FLAG_ALPHA_OVER_LIFETIME (1 << 3)
#define FLAG_SIZE_OVER_LIFETIME (1 << 4)
#define FLAG_TARGET (1 << 5)
#define FLAG_SPEED_OVER_LIFETIME (1 << 6)
#define FLAG_KILL_ALL (1 << 7)
#define FLAG_TARGET_DIE_ON_REACH (1 << 8)

struct ParticlesConfig {
    // used to tell in shader what to use or not
    int flagsFeature;
    float gravity;
    // grouped by 4 - xyz = color,w = timestamp
    float grad_colorOverLifetime[SSFX_MAX_GRAD_KEYS * 4];
    // grouped by 2 - x = size, y = timestamp
    float grad_alphaOverLifetime[SSFX_MAX_GRAD_KEYS * 2];
    // grouped by 2 - x = size, y = timestamp
    float grad_sizeOverLifetime[SSFX_MAX_GRAD_KEYS * 2];
    // grouped by 2 - x = size, y = timestamp
    float grad_speedOverLifetime[SSFX_MAX_GRAD_KEYS * 2];

    float4 targetDatas;
};

#endif //PARTICLE_COMMONS