Shader "Custom/Sprite-Lit-Stylized"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 1)) = 1
        [MaterialToggle] _Stylize ("Stylize", int) = 0
        _StyleBands ("Style Bands", Range(1, 8)) = 4
        _RedTint ("Red Tint", Range(0, 1)) = 0
        [Toggle] _IGNORE_SUNLIGHT ("Ignore Sunlight", Float) = 0
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

            #pragma shader_feature _IGNORE_SUNLIGHT_ON
            
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
            float _RedTint;

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
                half4 shape_light0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, i.lightingUV);
                const half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float result_alpha = shape_light0.r;
                if (_Stylize)
                {
                    const float cel_size = 1 / _StyleBands;
                    result_alpha = round(result_alpha * _StyleBands + 1 / (2 * _StyleBands)) * cel_size;
                    
                    return half4(main.rgb * result_alpha, main.a);
                }

                half final_light;
                half3 final_main;
                half3 final_color;

                #if _IGNORE_SUNLIGHT_ON
                    final_light = result_alpha;
                    final_main = main.rgb * final_light;
                    final_color = final_main;
                #else
                    final_light = max(_Brightness, result_alpha);
                    final_main = main.rgb * final_light;
                    
                    const half3 red_tint_color = half3(1.0, 0.3, 0.0);
                    final_color = max(final_main, main.rgb * red_tint_color * _RedTint);
                #endif
                

                return half4(final_color, main.a);
            }
            ENDHLSL
        }
    }
}
