#ifndef SSFX_UTILS
#define SSFX_UTILS

#include "ParticlesCommons.cginc"

// Used for data handle with animation curve (one float for value and one for step)
// Used for alpha, size and speed
float GetCurveValue(float2 values[SSFX_MAX_GRAD_KEYS], float lerpValue)
{
    // Find lower and upper key
    float2 step_progress = 0;
    int step_index = 0;
    step_progress = values[step_index];
    while (step_progress.y < lerpValue)
    {
        step_index++;
        step_progress = values[step_index];
    }

    float2 upper_datas = step_progress;
    float2 lower_datas = step_index == 0 ? float2(0, 0) : values[step_index - 1];

    // Lerp between lower and upper bound
    float lerp_between = (lerpValue - lower_datas.y) / (upper_datas.y - lower_datas.y);
    return lerp(lower_datas.x, upper_datas.x, lerp_between);
}

float3 GetCurveColor(float4 values[SSFX_MAX_GRAD_KEYS], float lerpValue)
{
    // Find lower and upper key
    float4 step_progress = 0;
    int step_index = 0;
    step_progress = values[step_index];
    while (step_progress.w < lerpValue)
    {
        step_index++;
        step_progress = values[step_index];
    }

    float4 upper_datas = step_progress;
    float4 lower_datas = step_index == 0 ? float4(0, 0, 0, 0) : values[step_index - 1];

    // Lerp between lower and upper bound
    float lerp_between = (lerpValue - lower_datas.w) / (upper_datas.w - lower_datas.w);
    return lerp(lower_datas.xyz, upper_datas.xyz, lerp_between);
}
#endif // SSFX_UTILS