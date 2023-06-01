Shader "Custom/DarknessShader"
{
    Properties {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Colour", Color) = (1, 1, 1, 1)
        _BlurStrength("Blur Strength", Int) = 10
        [MaterialToggle] _RetroLighting("Retro Lighting", Int) = 0
    }

    SubShader {
        Tags { "Queue" = "Transparent" }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "BASE"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct v2f 
            {
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
            };

            v2f vert(v2f v) 
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 _Color;
            int _BlurStrength;
            int _RetroLighting;

            // this basically makes it so darkness values change by 0.135 (/ 0.15 chosen using trial and error)
            fixed applyRetroLight(fixed value, float samples)
            {
                return 0.135 * (floor(value / 0.15) + 1);
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                // not multiplying by 2 because of the + 2 step in the for loops
                const int samples = pow(_BlurStrength + 0.5, 2);
                const half2 texelSize = _MainTex_TexelSize.xy;

                half sum = 0;

                // + 2 step for optimization, dw abt it :)
                // if you do want to use ++ then you need to multiply samples by 4
                // which would be equal to (2 * (_BlurStrength + 0.5)) ^ 2
                for (float x = -_BlurStrength; x < _BlurStrength; x += 2)
                {
                    for (float y = -_BlurStrength; y < _BlurStrength; y += 2)
                    {
                        sum += tex2D(_MainTex, i.uv + float2(x, y) * texelSize).a;
                    }
                }

                // average this fucker 🗞️💥
                half result = sum / samples;
                if (!result) discard;
                if (_RetroLighting) result = applyRetroLight(result, samples);
                // multiply by alpha so darkness doesn't get drawn on non-terrain texels
                return fixed4(_Color.rgb, smoothstep(0, 1, result) * tex2D(_MainTex, i.uv).a);
            }
            ENDCG
        }
    }
}
