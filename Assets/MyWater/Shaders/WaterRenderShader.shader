// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/WaterRenderShader"
{
    Properties
    {
        _Color ("Color", color) = (1,1,1,1)
		_MainTex("MainTex", 2D) = "white" {}
		_SkyboxTex("SkyboxTex", Cube) = "_Skybox" {}
		_WaveScale("WaveScale", Range(0,10)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "WaveUtils.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 worldSpaceReflect : TEXCOORD1;
            };

            sampler2D _MainTex;
			sampler2D _WaveResult;
			float4 _Color;
			float _WaveScale;
			samplerCUBE _SkyboxTex;

            v2f vert (appdata v)
            {
                v2f o;

				float4 localPos = v.vertex;
				float4 waveTransmit = tex2Dlod(_WaveResult, float4(v.uv, 0, 0));
				float waveHeight = DecodeFloatRGBA(waveTransmit);

				localPos.y += waveHeight * _WaveScale;

				float3 worldPos = mul(unity_ObjectToWorld, localPos);
				float3 worldSpaceNormal = mul(unity_ObjectToWorld, v.normal);
				float3 worldSpaceViewDir = UnityWorldSpaceViewDir(worldPos);

                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
				o.uv = v.uv;
				o.worldSpaceReflect = reflect(-worldSpaceViewDir, worldSpaceNormal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float4 waveTransmit = tex2Dlod(_WaveResult, float4(i.uv, 0, 0));
				float waveHeight = DecodeFloatRGBA(waveTransmit) * _WaveScale;

				float3 reflect = normalize(i.worldSpaceReflect);
				fixed4 skyboxCol = fixed4(texCUBE(_SkyboxTex, reflect).rgb, 1) * _Color;
				skyboxCol = lerp(skyboxCol, _Color, waveHeight);
                return skyboxCol;
            }
            ENDCG
        }
    }
}
