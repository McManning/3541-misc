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
				
				// Density
				if (_ActiveTexture == 1) {
					float d = saturate(col.r * 3.0);
					return fixed4(d, d, d, 1);
				}
				else if (_ActiveTexture == 2) {
					// Pressure
					return fixed4(0, col.r, 0, 1);
				}
				else if (_ActiveTexture == 3) {
					// Temperature
					col.g = 0;
					col.b = 0;

					// Super hot
					if (col.r > 1.0) {
						col.r = saturate(col.r);
					}
					else if (col.r < 0.5) { // Cold
						col.b = col.r;
						col.r = 0;
					}
					else { // medium (orange)
						col.r = saturate(col.r);
						col.g = 0.5;
					}

					return col;
				}
				else if (_ActiveTexture == 4) {
					// Solids
					float r = col.r;
					return fixed4(r, r, r, 1);
				}

				// Velocity. Offset upward to render negatives .. kinda
				float4 v = saturate(col + 0.5);
				return fixed4(v.x, v.y, 0, 1);
			}
			ENDCG
		}
	}
}
