Shader "Custom/CaveBg"
{
    Properties
    {
        [HideInInspector] _MaskTex("Mask", 2D) = "white" {}
        [HideInInspector] _Color ("Color", Color) = (1,1,1,1)
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _TerrainTex("Terrain", 2D) = "white" {}
        _SunLightAngle ("Sun Light Angle", Range(0, 360)) = 0
        _Brightness ("Brightness", Range(0, 1)) = 1
        _BlurSize ("Blur Size", Range(0, 20)) = 8
        _BlurSkip ("Blur Skip", Range(1, 20)) = 2
//        _BlurOffset ("Blur Offset", Range(0, 10)) = 1
        [MaterialToggle] _SquareBlur ("Blur Squared", int) = 0
        [MaterialToggle] _Stylize ("Stylize", int) = 0
        _StyleBands ("Style Bands", Range(1, 8)) = 4
        _GrassColor ("Grass Color", Color) = (1,1,1,1)
        _GrassThickness ("Grass Thickness", Range(1, 10)) = 3
    }
    
    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        
        Pass {
            Name "BASE"
            Tags { "LightMode" = "Universal2D" }
        
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #pragma vertex vert
            // #pragma fragment frag
            
            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment
            
            // #include "UnityCG.cginc"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"
            
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile _ DEBUG_DISPLAY
            
            float3 rgb_to_hsv_no_clip(float3 RGB)
            {
                float3 HSV;
               
                float maxChannel = max(RGB.x, RGB.y);
                float minChannel = min(RGB.x, RGB.y);
                
                if (RGB.z > maxChannel) maxChannel = RGB.z;
                if (RGB.z < minChannel) minChannel = RGB.z;
                
                HSV.xy = 0;
                HSV.z = maxChannel;
                const float delta = maxChannel - minChannel;             //Delta RGB value
                if (delta != 0) {                    // If gray, leave H  S at zero
                   HSV.y = delta / HSV.z;
                   float3 delRGB = (HSV.zzz - RGB + 3 * delta) / (6.0 * delta);
                   if      ( RGB.x == HSV.z ) HSV.x = delRGB.z - delRGB.y;
                   else if ( RGB.y == HSV.z ) HSV.x = 1.0/3.0 + delRGB.x - delRGB.z;
                   else if ( RGB.z == HSV.z ) HSV.x = 2.0/3.0 + delRGB.y - delRGB.x;
                }
                return HSV;
            }
     
            float3 hsv_to_rgb(float3 HSV)
            {
                // float3 RGB = HSV.z;
                float3 RGB;
                const float var_h = HSV.x * 6;
                const float var_i = floor(var_h);   // Or ... var_i = floor( var_h )
                float var_1 = HSV.z * (1.0 - HSV.y);
                float var_2 = HSV.z * (1.0 - HSV.y * (var_h-var_i));
                float var_3 = HSV.z * (1.0 - HSV.y * (1-(var_h-var_i)));
                if      (var_i == 0) { RGB = float3(HSV.z, var_3, var_1); }
                else if (var_i == 1) { RGB = float3(var_2, HSV.z, var_1); }
                else if (var_i == 2) { RGB = float3(var_1, HSV.z, var_3); }
                else if (var_i == 3) { RGB = float3(var_1, var_2, HSV.z); }
                else if (var_i == 4) { RGB = float3(var_3, var_1, HSV.z); }
                else                 { RGB = float3(HSV.z, var_1, var_2); }
               
                return RGB;
            }

            float shortestRelativeAngle(float from, float to)
            {
                return ((to - from) % 360.0 + 540.0) % 360.0 - 180.0;
            }
            
            float angleLerp(float from, float to, float t)
            {
                return (360.0 + from + shortestRelativeAngle(from, to) * t) % 360;
            }
            
            float customHueShift(float x)
            {
                const float y = 0.99 - pow(x - .18, 2) * 0.92;
                return x < 0.5 ? 1 - y : y;
            }

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                half2   lightingUV  : TEXCOORD1;
                #if defined(DEBUG_DISPLAY)
                float3  positionWS  : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            TEXTURE2D(_MainTex);
            TEXTURE2D(_TerrainTex);
            SAMPLER(sampler_MainTex);
            SAMPLER(sampler_TerrainTex);
            float4 _TerrainTex_TexelSize;
            half4 _MainTex_ST;
            // TEXTURE2D(_MaskTex);
            // SAMPLER(sampler_MaskTex);
            float4 _Color;
            float _SunLightAngle;
            float _Brightness;
            float _BlurSize;
            float _BlurSkip;
            // float _BlurOffset;
            float _SquareBlur;
            float _Stylize;
            float _StyleBands;
            float _GrassThickness;
            float4 _GrassColor;

            // #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            // #endif

            // #if USE_SHAPE_LIGHT_TYPE_1
            // SHAPE_LIGHT(1)
            // #endif
            //
            // #if USE_SHAPE_LIGHT_TYPE_2
            // SHAPE_LIGHT(2)
            // #endif
            //
            // #if USE_SHAPE_LIGHT_TYPE_3
            // SHAPE_LIGHT(3)
            // #endif
            
            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(v.positionOS);
                #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);

                o.color = v.color * _Color;
                return o;
            }
            
            // v2f vert (appdata_base v) {
            //     v2f o;
            //     o.pos = UnityObjectToClipPos(v.vertex);
            //     o.uv = v.texcoord;
            //     return o;
            // }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            
            half4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {
                half4 shapeLight0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, i.lightingUV);
                const half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                const half4 terrain = SAMPLE_TEXTURE2D(_TerrainTex, sampler_TerrainTex, i.uv);

                // return main;
                
                if (terrain.a > 0) return half4(0, 0, 0, 1);
                
                if (main.a == 0) discard;

                const float sunRad = radians(_SunLightAngle);
                const float2 sunDir = float2(cos(sunRad), sin(sunRad));
                float alphaSum = 0;
                
                // for (int x = -_BlurSize; x < _BlurSize; x += _BlurSkip)
                // {
                //     for (int y = -_BlurSize; y < _BlurSize; y += _BlurSkip)
                //     {
                //         const float2 blurOffset = float2(x, y) * _MainTex_TexelSize.x * _BlurOffset;
                //
                //         for (int n = 1; n <= 20; n++)
                //         {
                //             const float dist = n * _MainTex_TexelSize.x * 5;
                //             const float2 offsetUv = i.uv + sunDir * dist + blurOffset;
                //
                //             // Use SAMPLE_TEXTURE2D_LOD to explicitly specify mipmap LOD, so that it doesn't
                //             // have to be determined in the loop (which causes a warning about using a
                //             // gradient instruction in a loop with variable size).
                //             float4 sample = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, offsetUv, 0);
                //
                //             if (sample.a > 0)
                //             {
                //                 alphaSum += sample.a;
                //                 break;
                //             }
                //         }
                //     }
                // }

                const float coneRad = radians(45);
                const float coneRayCount = 8;
                const float coneRadStep = coneRad / coneRayCount;
                const float coneRaySamples = _BlurSize;
                for (int r = 0; r < coneRayCount; r++)
                {
                    const float offR = r - coneRayCount / 2;
                    const float rayRad = sunRad + offR * coneRadStep;
                    const float2 rayDir = float2(cos(rayRad), sin(rayRad));

                    for (int n = 1; n <= coneRaySamples; n++)
                    {
                        const float sunRaySamples = 20;
                        const float2 coneRaySampleOffset = rayDir * n * _TerrainTex_TexelSize.x * _BlurSkip;

                        for (int m = 1; m <= sunRaySamples; m++)
                        {
                            const float2 sunRaySampleOffset = coneRaySampleOffset + sunDir * m * _TerrainTex_TexelSize.x * 5;
                            const float2 offsetUv = i.uv + sunRaySampleOffset;
                            
                            // Use SAMPLE_TEXTURE2D_LOD to explicitly specify mipmap LOD, so that it doesn't
                            // have to be determined in the loop (which causes a warning about using a
                            // gradient instruction in a loop with variable size).
                            float4 terrainSample = SAMPLE_TEXTURE2D_LOD(_TerrainTex, sampler_TerrainTex, offsetUv, 0);
                            // return sample;
                            
                            if (terrainSample.a > 0)
                            {
                                alphaSum += terrainSample.a;
                                break;
                            }
                        }
                    }
                }
                
                // alphaSum = alphaSum / pow(_BlurSize * 2 / _BlurSkip, 2);
                alphaSum = alphaSum / (coneRayCount * coneRaySamples);
                
                // TODO: Consider removing if statements.
                if (_SquareBlur)
                {
                    alphaSum = alphaSum * alphaSum;
                }

                alphaSum = 1 - alphaSum;
                alphaSum *= _Brightness;
                
                // raised to a power to limit the light reach in terrain
                alphaSum = clamp(alphaSum + shapeLight0.r, 0, 1);
                
                // alphaSum = smoothstep(0, 1, alphaSum);

                float resultAlpha = alphaSum;
                float3 resultColor = main.rgb;
                if (_Stylize)
                {
                    const float celSize = 1 / _StyleBands;
                    // Stepped shading. Addition is to center the bands so that the surface
                    // is not thinner than the other bands.
                    // NOTE: After changing from a square blur to a cone blur, the addition seems to just smooth the bands.
                    resultAlpha = round(alphaSum * _StyleBands + 1 / (2 * _StyleBands)) * celSize;
                    
                    float3 hsvColor = rgb_to_hsv_no_clip(resultColor);
                    const float ialpha = 1 - resultAlpha;
                    const float ia01 = ialpha/0.75;

                    // Different easing functions for hue.
                    // const float t = 1 - cos(ialpha/0.75 * PI / 2.0);
                    const float t = pow(ia01, 3);
                    // const float t = ia01 < 0.5 ? 4 * pow(ia01, 3) : 1 - pow(-2 * ia01 + 2, 3) / 2;
                    // const float t = -(cos(PI * ia01) - 1) / 2;

                    // hue 240 = blue
                    hsvColor.r = angleLerp(hsvColor.r * 360.0, 240.0, t) / 360.0;
                    hsvColor.g = lerp(hsvColor.g, hsvColor.g * 0.675, ia01);
                    hsvColor.b = lerp(hsvColor.b, hsvColor.b * 0.125, 1 - pow(1 - ia01, 2));
                    
                    resultColor = hsv_to_rgb(hsvColor);
                    
                    const float bitMask = resultAlpha > 0 ? 1 : 0;
                    return half4(resultColor * bitMask, main.a);
                }

                return half4(resultColor * resultAlpha, main.a);

                // Stuff for normal 2D lighting:
                
                // SurfaceData2D surfaceData;
                // InputData2D inputData;
                //
                // InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
                // InitializeInputData(i.uv, i.lightingUV, inputData);
                //
                // return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }
    }
}
