// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NBCustom/Sprites/Brightful"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)

		_BrightnessAmount("Brightness Amount", Range(0.0, 10)) = 10
		_BrightnessRatio("Brightness Ratio", Range(0.0, 1)) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;

			bool _UseClipRect;
			float4 _ClipRect;

			bool _UseAlphaClip;
			uniform float _BrightnessAmount;
			uniform float _BrightnessRatio;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;

				#ifdef UNITY_HALF_TEXEL_OFFSET
					OUT.vertex.xy += (_ScreenParams.zw - 1.0)*float2(-1,1);
				#endif

				OUT.color = IN.color * _Color;
				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f IN) : SV_Target
			{
				half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

				if (_UseClipRect)
				color *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

				if (_UseAlphaClip)
				clip(color.a - 0.001);

				float minRatio = 1 / (_BrightnessAmount + 1);
				float3 brtColor = color.rgb * (_BrightnessAmount + 1) * (_BrightnessRatio + minRatio);
				color.rgb = brtColor;
				return color;
			}
			ENDCG
		}
	}
	FallBack "UI/Default"
}