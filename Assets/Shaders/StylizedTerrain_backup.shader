Shader "Custom/StylizedTerrain"
{
    Properties
    {
        [HideInInspector] _Color ("Color", Color) = (1,1,1,1)
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _SunLightAngle ("Sun Light Angle", Range(0, 360)) = 0
        _BlurSize ("Blur Size", Range(0, 8)) = 8
        [MaterialToggle] _SquareBlur ("Blur Squared", int) = 0
        _BlurSkip ("Blur Skip", Range(1, 10)) = 2
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
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"

            struct v2f {
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            UNITY_DECLARE_TEX2D(_MainTex);
            float4 _MainTex_TexelSize;
            float _SunLightAngle;
            float _BlurSize;
            float _SquareBlur;
            float _BlurSkip;
            
            v2f vert (appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                const float4 color = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);

                if (color.a == 0) discard;
                
                const float rad = radians(_SunLightAngle);
                const float2 sunDir = float2(cos(rad), sin(rad));

                float alphaSum = 0;
                
                // TODO: Optimize blurring. Maybe split into 2 passes?
                for (int x = -_BlurSize; x < _BlurSize; x += _BlurSkip)
                {
                    for (int y = -_BlurSize; y < _BlurSize; y += _BlurSkip)
                    {
                        const float2 blurOffset = float2(x, y) * _MainTex_TexelSize.x;

                        for (int n = 1; n <= 20; n++)
                        {
                            const float dist = n * _MainTex_TexelSize.x * 5;
                            const float2 offsetUv = i.uv + sunDir * dist + blurOffset;

                            // Use UNITY_SAMPLE_TEX2D_LOD to explicitly specify mipmap LOD, so that it doesn't
                            // have to be determined in the loop (which causes a warning about using a
                            // gradient instruction in a loop with variable size).
                            float4 sample = UNITY_SAMPLE_TEX2D_LOD(_MainTex, offsetUv, 0);
                            // float4 sample = UNITY_SAMPLE_TEX2D(_MainTex, offsetUv);
                            
                            if (sample.a > 0)
                            {
                                alphaSum += sample.a;
                                break;
                            }
                        }
                    }
                }

                alphaSum = alphaSum / pow(_BlurSize * 2 / _BlurSkip, 2);
                
                if (_SquareBlur)
                {
                    alphaSum = pow(alphaSum, 2);
                }
                
                alphaSum = 1 - alphaSum;

                return fixed4(color.rgb * alphaSum, color.a);
            }
            
            ENDHLSL
        }
    }
}
