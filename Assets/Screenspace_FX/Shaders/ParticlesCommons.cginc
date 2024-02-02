#ifndef PARTICLES_COMMONS
#define PARTICLES_COMMONS

struct ParticleDatas
{
    float4 worldPosition;
    float3 color;
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