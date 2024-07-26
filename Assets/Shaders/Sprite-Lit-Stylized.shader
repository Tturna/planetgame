Shader "Custom/Sprite-Lit-Stylized"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 1)) = 1
        [MaterialToggle] _Stylize ("Stylize", int) = 0
        _StyleBands ("Style Bands", Range(1, 8)) = 4
    }
    
    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Cull Off
        ZWrite Off
        
        // traditional transparency blending
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass {
            Name "BASE"
            Tags { "LightMode" = "Universal2D" }
        
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment
            
            // #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            // #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            // #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            // #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            // #pragma multi_compile _ DEBUG_DISPLAY
            
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
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            half4 _MainTex_ST;
            float4 _Color;
            float _Brightness;
            float _Stylize;
            float _StyleBands;

            SHAPE_LIGHT(0)
            
            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                
                // #if defined(DEBUG_DISPLAY)
                // o.positionWS = TransformObjectToWorld(v.positionOS);
                // #endif
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);

                o.color = v.color * _Color;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            
            half4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {
                half4 shapeLight0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, i.lightingUV);
                const half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float resultAlpha = shapeLight0.r;
                if (_Stylize)
                {
                    const float celSize = 1 / _StyleBands;
                    resultAlpha = round(resultAlpha * _StyleBands + 1 / (2 * _StyleBands)) * celSize;
                    
                    // float3 hsvColor = rgb_to_hsv_no_clip(resultColor);
                    // const float ialpha = 1 - resultAlpha;
                    // const float ia01 = ialpha/0.75;

                    // Different easing functions for hue.
                    // const float t = 1 - cos(ialpha/0.75 * PI / 2.0);
                    // const float t = pow(ia01, 3);
                    // const float t = ia01 < 0.5 ? 4 * pow(ia01, 3) : 1 - pow(-2 * ia01 + 2, 3) / 2;
                    // const float t = -(cos(PI * ia01) - 1) / 2;

                    // hue 240 = blue
                    // hsvColor.r = angleLerp(hsvColor.r * 360.0, 240.0, t) / 360.0;
                    // hsvColor.g = lerp(hsvColor.g, hsvColor.g * 0.675, ia01);
                    // hsvColor.b = lerp(hsvColor.b, hsvColor.b * 0.125, 1 - pow(1 - ia01, 2));
                    //
                    // resultColor = hsv_to_rgb(hsvColor);
                    
                    // const float bitMask = resultAlpha > 0 ? 1 : 0;
                    return half4(main.rgb * resultAlpha, main.a);
                }
                
                return half4(main.rgb * _Brightness, main.a);
            }
            ENDHLSL
        }
    }
}
