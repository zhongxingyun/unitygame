Shader "Custom/AlphaNoColor" {
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque"}
		Lighting Off
		ColorMask A
		
		LOD 200

		CGPROGRAM
		//#pragma surface surf Lambert
		#pragma surface surf Lambert alpha:blend
  
		sampler2D _MainTex;  
  
        struct Input {  
            float2 uv_MainTex;  
        };
  
		void surf (Input IN, inout SurfaceOutput o) {
			//half4 c = tex2D (_MainTex, IN.uv_MainTex);
            //o.Albedo = c.rgb;
			//o.Alpha = c.a;
			o.Alpha = 0.0f;
		}
		ENDCG
	}
	FallBack "Diffuse"
}