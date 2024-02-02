#ifndef HYPESIO_COMMONS
#define HYPESIO_COMMONS

// Call this macro to interpolate between a triangle patch, passing the field name
#define BARYCENTRIC_INTERPOLATE(fieldName) \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z

// Determinist random
float2 RandomFloat2(float2 p)
{
    return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3))))*42758.5453);
}

// from here : https://stackoverflow.com/questions/5149544/can-i-generate-a-random-number-inside-a-pixel-shader
float RandomFloat(float2 p )
{
    float2 K1 = float2(
        23.14069263277926, // e^pi (Gelfond's constant)
         2.665144142690225 // 2^sqrt(2) (Gelfondâ€“Schneider constant)
    );
    return frac( cos( dot(p,K1) ) * 12345.6789 );
}

float3 RGB2HSV(float3 rgb)
{
    float4 K = float4(0.0, -1.0/3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HSV2RGB(float3 hsv)
{
    hsv = float3(hsv.x, clamp(hsv.yz, 0.0, 1.0));
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    return hsv.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), hsv.y);
}

float3 GradientHSVfromRGB(float3 color1, float3 color2, float alpha)
{
    float3 hsv1 = RGB2HSV(color1);
    float3 hsv2 = RGB2HSV(color2);
    
    // mix hue in toward closest direction
    float hue = ((((hsv2.x - hsv1.x) % 1.) % 1.5, 1.) - 0.5) * alpha + hsv1.x;
    float3 hsv = float3(hue, lerp(hsv1.yz, hsv2.yz, alpha));
    
    float3 color = HSV2RGB(hsv);
        
    return color.rgb;
}

float3 BezierQuadratique(float step, float3 bezier_base, float3 bezier_middle, float3 bezier_end) {
    return bezier_base   * (1 - step) * (1 - step)
         + bezier_middle * (1 - step) * step       * 2.0f
         + bezier_end    * step       * step;
}

float BezierQuadratiqueLength(float3 bezier_base, float3 bezier_middle, float3 bezier_end, int nb_steps)
{
    float actualStep = 0; 
    float stepLength = 1.0 / nb_steps;
    float totalLen = 0;
    float3 previousPos = bezier_base;
    for (int i = 0; i < nb_steps; i++)
    {
        actualStep += stepLength;
        float3 nextPos = BezierQuadratique(actualStep, bezier_base, bezier_middle, bezier_end);
        totalLen += distance(previousPos, nextPos);
        previousPos = nextPos;
    }
    return totalLen;
}

float3 BezierQuadratiquePlaceInLut(Buffer<float> lut, int lutSize, float distance, float3 bezier_base, float3 bezier_middle, float3 bezier_end)
{
    if (distance > lut[lutSize - 1])
        return BezierQuadratique(1.0, bezier_base, bezier_middle, bezier_end);
    if (distance < 0)
        return BezierQuadratique(0, bezier_base, bezier_middle, bezier_end);

    float len = lut[lutSize - 1];
    for (int i = 0; i < lutSize - 1; i++)
    {
        if (lut[i] < distance && lut[i + 1] > distance)
        {
            float range = lut[i + 1] - lut[i];
            float stepLen = len / lutSize;
            float step = lerp(i * stepLen, (i+1) * stepLen, (distance - lut[i]) / range);
            return BezierQuadratique(step, bezier_base, bezier_middle, bezier_end);
        }
    }

    return BezierQuadratique(0, bezier_base, bezier_middle, bezier_end);
}



float3 AdditiveBlend(float3 a, float3 b, float alpha)
{
    return lerp(a, b, alpha);
}

#endif //HYPESIO_COMMONS