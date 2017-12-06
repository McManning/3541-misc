Shader "Custom/Smoke"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ActiveTexture ("Active Texture ID", Int) = 0
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			int _ActiveTexture;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				
				// Discrete value (red channel)
				if (_ActiveTexture > 0) {
					float d = saturate(col.r * 3.0);
					return fixed4(d, d, d, 1);
				}

				// Velocity. Note negatives won't render well,
				// but that's fine...
				return col;
			}
			ENDCG
		}
	}
}
