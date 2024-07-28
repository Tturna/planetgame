Shader "Custom/StylizedTerrainCone"
{
    Properties
    {
        [HideInInspector] _MaskTex("Mask", 2D) = "white" {}
        [HideInInspector] _Color ("Color", Color) = (1,1,1,1)
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _CaveBgTerrainTex ("Cave Background Terrain Texture", 2D) = "white" {}
        _SunLightAngle ("Sun Light Angle", Range(0, 360)) = 0
        _Brightness ("Brightness", Range(0, 1)) = 1
        _BlurConeArc ("Blur Cone Arc", Range(1, 90)) = 45
        _BlurConeRayLength ("Blur Cone Ray Length", Range(0, 20)) = 8
        _BlurConeResolution ("Blur Cone Ray Resolution", Range(1, 20)) = 8
        _BlurConeSkip ("Blur Cone Skip", Range(1, 20)) = 2
        _SunRayLength ("Sun Ray Length", Range(0, 80)) = 20
        _SunRaySkip ("Sun Ray Skip", Range(1, 20)) = 5
//        _BlurOffset ("Blur Offset", Range(0, 10)) = 1
        [MaterialToggle] _SquareBlur ("Blur Squared", int) = 0
        [MaterialToggle] _Stylize ("Stylize", int) = 0
        _StyleBands ("Style Bands", Range(1, 8)) = 4
        _GrassColor ("Grass Color", Color) = (1,1,1,1)
        _GrassThickness ("Grass Thickness", Range(1, 10)) = 3
        _ShadeDistortionStrength ("Shade Distortion Strength", Range(0, 1)) = 0
        _ShadeDistortionFidelity ("Shade Distortion Fidelity", Range(1, 100)) = 15
        _RedTint ("Red Tint", Range(0, 1)) = 0
        [Toggle] _IS_CAVE_BG ("Is Cave Background", Float) = 0
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
            
            #pragma vertex combined_shape_light_vertex
            #pragma fragment combined_shape_light_fragment
            
            // #include "UnityCG.cginc"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"
            
            // #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            // #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            // #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            // #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            // #pragma multi_compile _ DEBUG_DISPLAY

            #pragma shader_feature _IS_CAVE_BG_ON

            inline half unity_noise_random_value (const half2 uv)
            {
                return frac(sin(dot(uv, half2(12.9898, 78.233))) * 43758.5453);
            }
            
            inline half unity_noise_interpolate (const half a, const half b, const half t)
            {
                return (1.0 - t) * a + t * b;
            }
            
            inline half unity_value_noise (const half2 uv)
            {
                const half2 i = floor(uv);
                half2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
            
                const half2 c0 = i + half2(0.0, 0.0);
                const half2 c1 = i + half2(1.0, 0.0);
                const half2 c2 = i + half2(0.0, 1.0);
                const half2 c3 = i + half2(1.0, 1.0);
                const half r0 = unity_noise_random_value(c0);
                const half r1 = unity_noise_random_value(c1);
                const half r2 = unity_noise_random_value(c2);
                const half r3 = unity_noise_random_value(c3);

                const half bottom_of_grid = unity_noise_interpolate(r0, r1, f.x);
                const half top_of_grid = unity_noise_interpolate(r2, r3, f.x);
                const half t = unity_noise_interpolate(bottom_of_grid, top_of_grid, f.y);
                return t;
            }
            
            half unity_simple_noise_float(half2 uv, const half scale)
            {
                half t = 0.0;
            
                half freq = pow(2.0, half(0));
                half amp = pow(0.5, half(3 - 0));
                t += unity_value_noise(half2(uv.x * scale / freq, uv.y * scale / freq)) * amp;
            
                freq = pow(2.0, half(1));
                amp = pow(0.5, half(3 - 1));
                t += unity_value_noise(half2(uv.x * scale / freq, uv.y * scale / freq)) * amp;
            
                freq = pow(2.0, half(2));
                amp = pow(0.5, half(3 - 2));
                t += unity_value_noise(float2(uv.x * scale / freq, uv.y * scale / freq)) * amp;
            
                return t;
            }
            
            half3 rgb_to_hsv_no_clip(half3 rgb)
            {
                half3 hsv;
               
                half max_channel = max(rgb.x, rgb.y);
                half min_channel = min(rgb.x, rgb.y);
                
                if (rgb.z > max_channel) max_channel = rgb.z;
                if (rgb.z < min_channel) min_channel = rgb.z;
                
                hsv.xy = 0;
                hsv.z = max_channel;
                const half delta = max_channel - min_channel;             //Delta RGB value
                
                if (delta != 0) {                    // If gray, leave H  S at zero
                   hsv.y = delta / hsv.z;
                   half3 del_rgb = (hsv.zzz - rgb + 3 * delta) / (6.0 * delta);
                   if      ( rgb.x == hsv.z ) hsv.x = del_rgb.z - del_rgb.y;
                   else if ( rgb.y == hsv.z ) hsv.x = 1.0 / 3.0 + del_rgb.x - del_rgb.z;
                   else if ( rgb.z == hsv.z ) hsv.x = 2.0 / 3.0 + del_rgb.y - del_rgb.x;
                }
                return hsv;
            }
     
            half3 hsv_to_rgb(half3 hsv)
            {
                // half3 RGB = HSV.z;
                half3 rgb;
                const half var_h = hsv.x * 6;
                const half var_i = floor(var_h);   // Or ... var_i = floor( var_h )
                half var_1 = hsv.z * (1.0 - hsv.y);
                half var_2 = hsv.z * (1.0 - hsv.y * (var_h - var_i));
                half var_3 = hsv.z * (1.0 - hsv.y * (1 - (var_h - var_i)));
                if      (var_i == 0) { rgb = float3(hsv.z, var_3, var_1); }
                else if (var_i == 1) { rgb = float3(var_2, hsv.z, var_1); }
                else if (var_i == 2) { rgb = float3(var_1, hsv.z, var_3); }
                else if (var_i == 3) { rgb = float3(var_1, var_2, hsv.z); }
                else if (var_i == 4) { rgb = float3(var_3, var_1, hsv.z); }
                else                 { rgb = float3(hsv.z, var_1, var_2); }
               
                return rgb;
            }

            half shortest_relative_angle(const half from, const half to)
            {
                return ((to - from) % 360.0 + 540.0) % 360.0 - 180.0;
            }
            
            half angle_lerp(const half from, const half to, const half t)
            {
                return (360.0 + from + shortest_relative_angle(from, to) * t) % 360;
            }
            
            float custom_hue_shift(const half x)
            {
                const half y = 0.99 - pow(x - .18, 2) * 0.92;
                return x < 0.5 ? 1 - y : y;
            }

            struct attributes
            {
                half3 position_os   : POSITION;
                half4 color        : COLOR;
                half2  uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct varyings
            {
                half4  position_cs  : SV_POSITION;
                half4   color        : COLOR;
                half2  uv           : TEXCOORD0;
                half2   lighting_uv  : TEXCOORD1;
                // #if defined(DEBUG_DISPLAY)
                half3  position_ws  : TEXCOORD2;
                // #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            TEXTURE2D(_MainTex);
            TEXTURE2D(_CaveBgTerrainTex);
            SAMPLER(sampler_MainTex);
            SAMPLER(sampler_CaveBgTerrainTex);
            half4 _MainTex_TexelSize;
            half4 _MainTex_ST;
            half4 _CaveBgTerrainTex_TexelSize;
            // TEXTURE2D(_MaskTex);
            // SAMPLER(sampler_MaskTex);
            half4 _Color;
            half _SunLightAngle;
            half _Brightness;
            half _BlurConeArc;
            half _BlurConeRayLength;
            half _BlurConeResolution;
            half _BlurConeSkip;
            half _SunRayLength;
            half _SunRaySkip;
            // half _BlurOffset;
            half _SquareBlur;
            half _Stylize;
            half _StyleBands;
            half _GrassThickness;
            half4 _GrassColor;
            half _ShadeDistortionStrength;
            half _ShadeDistortionFidelity;
            half _RedTint;

            SHAPE_LIGHT(0)
            
            varyings combined_shape_light_vertex(attributes v)
            {
                varyings o = (varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.position_cs = TransformObjectToHClip(v.position_os);
                // #if defined(DEBUG_DISPLAY)
                o.position_ws = TransformObjectToWorld(v.position_os);
                // #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.lighting_uv = half2(ComputeScreenPos(o.position_cs / o.position_cs.w).xy);

                o.color = v.color * _Color;
                return o;
            }
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            
            half4 combined_shape_light_fragment(const varyings i) : SV_Target
            {
                const half noise = unity_simple_noise_float(i.position_ws.xy, _ShadeDistortionFidelity);
                
                half4 shape_light0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, i.lighting_uv);
                const half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);

                #if _IS_CAVE_BG_ON
                    const half4 terrain = SAMPLE_TEXTURE2D(_CaveBgTerrainTex, sampler_CaveBgTerrainTex, i.uv);
                    if (terrain.a > 0) return half4(0, 0, 0, 1);
                #endif

                if (main.a == 0) discard;
                
                const half sun_rad = radians(_SunLightAngle);
                const half2 sun_dir = float2(cos(sun_rad), sin(sun_rad));
                half alpha_sum = 0;
                
                const half cone_rad = radians(_BlurConeArc);
                const half cone_ray_count = _BlurConeResolution;
                const half cone_rad_step = cone_rad / cone_ray_count;
                const half cone_ray_samples = _BlurConeRayLength;
                
                for (int r = 0; r < cone_ray_count; r++)
                {
                    const half off_r = r - cone_ray_count / 2;
                    const half ray_rad = sun_rad + off_r * cone_rad_step;
                    const half2 ray_dir = float2(cos(ray_rad), sin(ray_rad));

                    for (int n = 1; n <= cone_ray_samples; n++)
                    {
                        const half sun_ray_samples = _SunRayLength;
                        const half2 cone_ray_sample_offset = ray_dir * n * _MainTex_TexelSize.x * _BlurConeSkip;

                        for (int m = 1; m <= sun_ray_samples; m++)
                        {
                            // start with a smaller sun ray skip to avoid skipping nearby terrain
                            const half t = m / sun_ray_samples;
                            const half sun_ray_skip = lerp(1, _SunRaySkip, sqrt(t));
                        
                            const half2 sun_ray_sample_offset = cone_ray_sample_offset + sun_dir * m * _MainTex_TexelSize.x * sun_ray_skip;
                            const half2 offset_uv = i.uv + sun_ray_sample_offset;

                            if (offset_uv.x < 0 || offset_uv.x > 1 || offset_uv.y < 0 || offset_uv.y > 1) break;
                            
                            // Use SAMPLE_TEXTURE2D_LOD to explicitly specify mipmap LOD, so that it doesn't
                            // have to be determined in the loop (which causes a warning about using a
                            // gradient instruction in a loop with variable size).
                            half4 sample;

                            #if _IS_CAVE_BG_ON
                                sample = SAMPLE_TEXTURE2D_LOD(_CaveBgTerrainTex, sampler_CaveBgTerrainTex, offset_uv, 0);
                            #else
                                sample = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, offset_uv, 0);
                            #endif
                            
                            if (sample.a > 0)
                            {
                                alpha_sum += sample.a;
                                break;
                            }
                        }
                    }
                }
                
                // alphaSum = alphaSum / pow(_BlurSize * 2 / _BlurSkip, 2);
                alpha_sum = alpha_sum / (cone_ray_count * cone_ray_samples);
                
                // TODO: Consider removing if statements.
                if (_SquareBlur)
                {
                    alpha_sum = alpha_sum * alpha_sum;
                }

                alpha_sum = 1 - alpha_sum;
                alpha_sum *= _Brightness;
                
                // raised to a power to limit the light reach in terrain
                // alphaSum = clamp(alphaSum + pow(shapeLight0.r, 2), 0, 1);
                
                alpha_sum = saturate(max(alpha_sum, shape_light0.r));

                // alphaSum = smoothstep(0, 1, alphaSum);

                half result_alpha = alpha_sum;
                half3 result_color = main.rgb;
                
                if (!_Stylize)
                {
                    return half4(result_color * result_alpha, main.a);
                }

                const half distortion_noise = noise * _ShadeDistortionStrength;
                // darker = noisier. multiply by alpha_sum to limit noise to lit areas.
                // sqrt to amplify noise in darker areas.
                alpha_sum = lerp(alpha_sum, distortion_noise * sqrt(alpha_sum), 1 - alpha_sum);
                
                // Stepped shading. Addition is to center the bands so that the surface
                // is not thinner than the other bands.
                // NOTE: After changing from a square blur to a cone blur, the addition seems to just smooth the bands.
                const half cel_size = 1 / _StyleBands;
                result_alpha = round(alpha_sum * _StyleBands + 1 / (2 * _StyleBands)) * cel_size;
                // resultAlpha = round(saturate(alphaSum * noise * _TempNoiseOffset) * _StyleBands + 1 / (2 * _StyleBands)) * celSize;

                #if !_IS_CAVE_BG_ON
                    const half4 grass_check_sample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(0, _MainTex_TexelSize.y * _GrassThickness));
                    
                    if (grass_check_sample.a == 0)
                    {
                        return half4(_GrassColor.rgb * result_alpha, main.a);
                    }
                #endif
                
                float3 hsv_color = rgb_to_hsv_no_clip(result_color);
                const half ialpha = 1 - result_alpha;
                const half ia01 = ialpha/0.75;

                // Different easing functions for hue.
                // const float t = 1 - cos(ialpha/0.75 * PI / 2.0);
                const half t = pow(ia01, 3);
                // const float t = ia01 < 0.5 ? 4 * pow(ia01, 3) : 1 - pow(-2 * ia01 + 2, 3) / 2;
                // const float t = -(cos(PI * ia01) - 1) / 2;

                // hue 240 = blue
                hsv_color.r = angle_lerp(hsv_color.r * 360.0, 240.0, t) / 360.0;
                hsv_color.g = lerp(hsv_color.g, hsv_color.g * 0.675, ia01);
                hsv_color.b = lerp(hsv_color.b, hsv_color.b * 0.125, 1 - pow(1 - ia01, 2));
                
                result_color = hsv_to_rgb(hsv_color);
                const half bit_mask = result_alpha > 0 ? 1 : 0;
                
                return half4(result_color * bit_mask, main.a);
            }
            ENDHLSL
        }
    }
}
