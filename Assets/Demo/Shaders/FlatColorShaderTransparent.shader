Shader "Custom/FlatcolorShaderTransparent" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Transparent" }
		CGPROGRAM
		#pragma surface surf Standard alpha
		struct Input {
			float2 uv_MainTex;
		};
		fixed4 _Color;
		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Albedo = _Color.rgb;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
