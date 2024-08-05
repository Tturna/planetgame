Shader "Custom/TMP_Atlas-Custom-Dualcolor" {

Properties {
	_MainTex		("Font Atlas", 2D) = "white" {}
	_FaceTex		("Font Texture", 2D) = "white" {}
	/*[HDR]*/_FaceColor	("Text Color", Color) = (1,1,1,1)
	/*[HDR]*/_AccentColor	("Accent Color", Color) = (0,0,0,0)
	// Color Weight is the color of anti-aliased pixels between FaceColor and AccentColor
	_AAColorWeight	("AA Color Weight", Range(0, 1)) = 0.5

	_VertexOffsetX	("Vertex OffsetX", float) = 0
	_VertexOffsetY	("Vertex OffsetY", float) = 0
	_MaskSoftnessX	("Mask SoftnessX", float) = 0
	_MaskSoftnessY	("Mask SoftnessY", float) = 0

	_ClipRect("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
	_Padding		("Padding", float) = 0

	_StencilComp("Stencil Comparison", Float) = 8
	_Stencil("Stencil ID", Float) = 0
	_StencilOp("Stencil Operation", Float) = 0
	_StencilWriteMask("Stencil Write Mask", Float) = 255
	_StencilReadMask("Stencil Read Mask", Float) = 255

	_CullMode("Cull Mode", Float) = 0
	_ColorMask("Color Mask", Float) = 15
}

SubShader{

	Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

	Stencil
	{
		Ref[_Stencil]
		Comp[_StencilComp]
		Pass[_StencilOp]
		ReadMask[_StencilReadMask]
		WriteMask[_StencilWriteMask]
	}


	Lighting Off
	Cull [_CullMode]
	ZTest [unity_GUIZTestMode]
	ZWrite Off
	Fog { Mode Off }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask[_ColorMask]

	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag

		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP


		#include "UnityCG.cginc"

		struct appdata_t {
			float4 vertex		: POSITION;
			fixed4 color		: COLOR;
			float2 texcoord0	: TEXCOORD0;
			float2 texcoord1	: TEXCOORD1;
		};

		struct v2f {
			float4	vertex		: SV_POSITION;
			fixed4	color		: COLOR;
			float2	texcoord0	: TEXCOORD0;
			float2	texcoord1	: TEXCOORD1;
			float4	mask		: TEXCOORD2;
		};

		CBUFFER_START(UnityPerMaterial)
		uniform	sampler2D 	_MainTex;
		uniform	sampler2D 	_FaceTex;
		uniform float4		_FaceTex_ST;
		uniform	fixed4		_FaceColor;
		uniform	fixed4		_AccentColor;
		uniform float 		_AAColorWeight;

		uniform float		_VertexOffsetX;
		uniform float		_VertexOffsetY;
		uniform float4		_ClipRect;
		uniform float		_MaskSoftnessX;
		uniform float		_MaskSoftnessY;
		CBUFFER_END

		fixed3 rgb_lerp(const fixed3 a, const fixed3 b, const float v)
		{
			return a + (b - a) * v;
		}

		float2 UnpackUV(float uv)
		{
			float2 output;
			output.x = floor(uv / 4096);
			output.y = uv - 4096 * output.x;

			return output * 0.001953125;
		}

		v2f vert (appdata_t v)
		{
			float4 vert = v.vertex;
			vert.x += _VertexOffsetX;
			vert.y += _VertexOffsetY;

			vert.xy += (vert.w * 0.5) / _ScreenParams.xy;

			float4 vPosition = UnityPixelSnap(UnityObjectToClipPos(vert));

			v2f OUT;
			OUT.vertex = vPosition;
			OUT.texcoord0 = v.texcoord0;
			OUT.texcoord1 = TRANSFORM_TEX(UnpackUV(v.texcoord1), _FaceTex);
			float2 pixelSize = vPosition.w;
			pixelSize /= abs(float2(_ScreenParams.x * UNITY_MATRIX_P[0][0], _ScreenParams.y * UNITY_MATRIX_P[1][1]));

			// Clamp _ClipRect to 16bit.
			float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			OUT.mask = float4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));

			fixed4 faceColor = v.color;
			// faceColor *= _FaceColor;
			
			OUT.color = faceColor;

			return OUT;
		}

		fixed4 frag (v2f IN) : SV_Target
		{
			// fixed4 color = tex2D(_MainTex, IN.texcoord0) * tex2D(_FaceTex, IN.texcoord1) * IN.color;
			// fixed4 color = fixed4(IN.color.rgb, tex2D(_MainTex, IN.texcoord0).a);
			// fixed4 color = IN.color;
			fixed4 baseFont = tex2D(_MainTex, IN.texcoord0);
			fixed colorWeight = 1 - baseFont.r;
			
			if (baseFont.a != 1)
			{
				colorWeight = _AAColorWeight;
			}
			
			const fixed4 color = fixed4(rgb_lerp(_FaceColor.rgb, _AccentColor.rgb, colorWeight).rgb, baseFont.a);

			// Alternative implementation to UnityGet2DClipping with support for softness.
			// #if UNITY_UI_CLIP_RECT
			// 	half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
			// 	color *= m.x * m.y;
			// #endif
			//
			// #if UNITY_UI_ALPHACLIP
			// 	clip(color.a - 0.001);
			// #endif

			return color;
		}
		ENDCG
	}
}

//	CustomEditor "TMPro.EditorUtilities.TMP_BitmapShaderGUI"
}
