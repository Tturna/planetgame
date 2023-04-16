Shader "Custom/GrassShader"
{
    Properties {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Colour", Color) = (1, 1, 1, 1)
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            Name "BASE"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct v2f {
                float4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
            };

            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 _Color;
            float4 _MainTex_TexelSize;

            fixed4 frag (v2f i) : SV_Target {
                const fixed2 x_pos = fixed2(_MainTex_TexelSize.x, 0),
                y_pos = fixed2(0, _MainTex_TexelSize.y);

                const float edge_count = tex2D(_MainTex, i.uv + y_pos).a +
                    tex2D(_MainTex, i.uv - y_pos).a +
                    tex2D(_MainTex, i.uv + x_pos).a +
                    tex2D(_MainTex, i.uv - x_pos).a;
                if (!edge_count) discard;

                if (!tex2D(_MainTex, i.uv).a
                  || edge_count < 4) return _Color;
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}