Shader "Custom/StylizedTerrain"
{
    Properties
    {
        [HideInInspector] _MaskTex("Mask", 2D) = "white" {}
        [HideInInspector] _Color ("Color", Color) = (1,1,1,1)
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _SunLightAngle ("Sun Light Angle", Range(0, 360)) = 0
        _Brightness ("Brightness", Range(0, 1)) = 1
        _BlurSize ("Blur Size", Range(0, 8)) = 8
        _BlurSkip ("Blur Skip", Range(1, 10)) = 2
        _BlurOffset ("Blur Offset", Range(0, 10)) = 1
        [MaterialToggle] _SquareBlur ("Blur Squared", int) = 0
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
            // TEXTURE2D(_MaskTex);
            // SAMPLER(sampler_MaskTex);
            float4 _Color;
            float _SunLightAngle;
            float _Brightness;
            float _BlurSize;
            float _BlurSkip;
            float _BlurOffset;
            float _SquareBlur;
            float _Stylize;
            float _StyleBands;

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

                if (main.a == 0) discard;

                const float rad = radians(_SunLightAngle);
                const float2 sunDir = float2(cos(rad), sin(rad));
                float alphaSum = 0;
                
                for (int x = -_BlurSize; x < _BlurSize; x += _BlurSkip)
                {
                    for (int y = -_BlurSize; y < _BlurSize; y += _BlurSkip)
                    {
                        const float2 blurOffset = float2(x, y) * _MainTex_TexelSize.x * _BlurOffset;
                
                        for (int n = 1; n <= 20; n++)
                        {
                            const float dist = n * _MainTex_TexelSize.x * 5;
                            const float2 offsetUv = i.uv + sunDir * dist + blurOffset;
                
                            // Use SAMPLE_TEXTURE2D_LOD to explicitly specify mipmap LOD, so that it doesn't
                            // have to be determined in the loop (which causes a warning about using a
                            // gradient instruction in a loop with variable size).
                            float4 sample = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, offsetUv, 0);
                
                            if (sample.a > 0)
                            {
                                alphaSum += sample.a;
                                break;
                            }
                        }
                    }
                }
                
                alphaSum = alphaSum / pow(_BlurSize * 2 / _BlurSkip, 2);

                // TODO: Consider removing if statements.
                if (_SquareBlur)
                {
                    alphaSum = alphaSum * alphaSum;
                }

                alphaSum = 1 - alphaSum;
                alphaSum *= _Brightness;
                // power of 4 to limit the light reach in terrain
                alphaSum = clamp(alphaSum + pow(shapeLight0.r, 4), 0, 1);

                alphaSum = smoothstep(0, 1, alphaSum);
                
                float result = alphaSum;
                if (_Stylize)
                {
                    const float celSize = 1 / _StyleBands;
                    result = round(alphaSum * _StyleBands) * celSize;
                }
                
                return half4(main.rgb * result, main.a);

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
