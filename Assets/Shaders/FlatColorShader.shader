Shader "Custom/FlatcolorShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		struct Input {
			float2 uv_MainTex;
		};
		fixed4 _Color;
		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Albedo = _Color.rgb;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
