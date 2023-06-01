Shader "Custom/DarknessShader"
{
    Properties {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Colour", Color) = (1, 1, 1, 1)
        _BlurSize("Blur Size", Int) = 10
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

            v2f vert(const v2f v) 
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 _Color;
            int _BlurSize;
            int _RetroLighting;

            // this basically makes it so darkness values change by 0.135 (/ 0.15 chosen using trial and error)
            fixed applyRetroLight(fixed value, float samples)
            {
                return 0.135 * (floor(value / 0.15) + 1);
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                if (tex2D(_MainTex, i.uv).a == 0) discard;
                
                const int sampleCount = pow(_BlurSize, 2);
                const half2 texelSize = _MainTex_TexelSize.xy;

                half sum = 0;
                for (float x = -_BlurSize; x < _BlurSize; x += 2)
                {
                    for (float y = -_BlurSize; y < _BlurSize; y += 2)
                    {
                        sum += tex2D(_MainTex, i.uv + float2(x, y) * texelSize).a;
                    }
                }

                half result = sum / sampleCount;

                // TODO: Make it so blur starts after a certain distance from the surface.
                
                if (_RetroLighting) result = applyRetroLight(result, sampleCount);

                // yoinked from grass shader to check for edges
                // const fixed2 x_pos = fixed2(_MainTex_TexelSize.x, 0),
                // y_pos = fixed2(0, _MainTex_TexelSize.y);
                //
                // const float edge_count = tex2D(_MainTex, i.uv + y_pos).a +
                //     tex2D(_MainTex, i.uv - y_pos).a +
                //     tex2D(_MainTex, i.uv + x_pos).a +
                //     tex2D(_MainTex, i.uv - x_pos).a;
                //
                // if (!edge_count) discard;
                // if (edge_count < 4) discard;
                
                return fixed4(_Color.rgb, result * result);
            }
            ENDCG
        }
    }
}
