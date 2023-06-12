#if !SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#endif

#ifndef GHETTO_BLUR_INCLUDED
#define GHETTO_BLUR_INCLUDED

float GhettoBlur(UnityTexture2D inputTexture, UnitySamplerState samplerState, float2 uvPosition, float texelSize, int blurSize)
{
    // if (SAMPLE_TEXTURE2D(inputTexture, samplerState, uvPosition).a == 0) return 0;
    // if (tex2D(inputTexture, uvPosition).a == 0) discard;
    
    const int sampleCount = pow(blurSize, 2);

    float sum = 0;
    for (float x = -blurSize; x < blurSize; x += 2)
    {
        for (float y = -blurSize; y < blurSize; y += 2)
        {
            sum += SAMPLE_TEXTURE2D(inputTexture, samplerState, uvPosition + float2(x, y) * texelSize).a;
            // sum += inputTexture.Sample(samplerState, uvPosition + float2(x, y) * texelSize).a;
        }
    }

    return sum / sampleCount;
}

void TerrainShade_float(UnityTexture2D terrainTexture, UnitySamplerState samplerState, float2 uv, float sunlightAngle, float texelSize, float blurSize, out float4 result)
{
    const float4 terrainSample = SAMPLE_TEXTURE2D(terrainTexture, samplerState, uv);
    
    const float sunRad = DegToRad(sunlightAngle);
    float x = cos(sunRad);
    float y = sin(sunRad);
    const float2 sunVec = float2(x, y);

    const float2 initOffset = sunVec * texelSize * 5;
    float2 initUvOffset = uv + initOffset;
    float initOffsetAlpha = SAMPLE_TEXTURE2D(terrainTexture, samplerState, initUvOffset).a;
    float initInverse = 1 - initOffsetAlpha;
    const float edgeAlpha = initInverse * terrainSample.a;

    float product = edgeAlpha;
    
    for (int i = 2; i < 7; i++)
    {
        const float2 offset = sunVec * i * texelSize * 5;
        const float2 uvOffset = uv + offset;
        const float alpha = SAMPLE_TEXTURE2D(terrainTexture, samplerState, uvOffset).a;
        const float inverse = 1 - alpha;
        product *= inverse;
        // product += alpha * .16;
    }
    
    const float blur = GhettoBlur(terrainTexture, samplerState, uv, texelSize, blurSize);
    const float blurInverse = 1 - blur;
    product *= blurInverse;

    // result = product * terrainSample;
    result = product;

    // float inverseLerped = x;
    // result = float4(inverseLerped, inverseLerped, inverseLerped, 1) * terrainSample.a;
}

#endif