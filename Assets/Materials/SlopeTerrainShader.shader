Shader "Custom/SlopeTerrainShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Color2("Color", Color) = (1, 1, 1, 1)
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_pow("Power", Range(1, 10)) = 0.0
	}
		SubShader
	{
			Tags { "RenderType" = "Opaque"}
			LOD 200
			CGPROGRAM
			#pragma surface surf Standard fullforwardshadows
			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			fixed4 _Color;
			fixed4 _Color2;
			float _pow;
			half _Glossiness;
			half _Metallic;
	
			struct Input
			{
				float2 uv_MainTex;
				float4 color : COLOR;
				float3 worldNormal;
			};
		   void surf(Input IN, inout SurfaceOutputStandard o) 
		   {
			   float4 c = _Color;
			   float4 c2 = _Color2;
			   float _finalBlend = pow(dot(IN.worldNormal, float3(0, 1, 0)), _pow);
			   o.Albedo = lerp(c.rgb, c2.rgb, _finalBlend); // vertex RGB
			   o.Alpha = 1;
			   o.Metallic = _Metallic;
			   o.Smoothness = _Glossiness;
		   }
		   ENDCG
	}
		FallBack "Diffuse"
}
