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

void GhettoBlur_float(UnityTexture2D inputTexture, UnitySamplerState samplerState, float2 uvPosition, float texelSize, int blurSize, out float blurredAlpha)
{
    blurredAlpha = GhettoBlur(inputTexture, samplerState, uvPosition, texelSize, blurSize);
}

#endif