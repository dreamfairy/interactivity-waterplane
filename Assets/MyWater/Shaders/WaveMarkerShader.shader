Shader "Unlit/WaveMarkerShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			float4 _WaveMarkParams;
			sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float dx = i.uv.x - _WaveMarkParams.x;
				float dy = i.uv.y - _WaveMarkParams.y;

				float disSqr = dx * dx + dy * dy;

				int hasCol = step(0, _WaveMarkParams.z - disSqr);

				float waveValue = DecodeHeight(tex2D(_MainTex, i.uv));

				if (hasCol == 1) {
					waveValue = _WaveMarkParams.w;
				}
				
                return EncodeHeight(waveValue);
            }
            ENDCG
        }
	}
}
