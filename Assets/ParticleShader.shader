Shader "Unlit/ParticleShader"
{
	Properties 
	{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		// Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Cull off // Disable backface culling for particle triangles

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			// Data to be passed from vertex to fragment shader
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

				// Abuse TEXCOORD0 to pass our particle ID through to the fragment shader
				// TODO: https://msdn.microsoft.com/en-us/library/bb509647(VS.85).aspx says
				// it should be a float - am I going to run into issues having it be an int?
				int id : TEXCOORD1; 
			};

			// Properties of a distinct particle in the system
			// MUST match GpuParticleSystem.Particle
			struct Particle
			{
				float3 position;
				float3 velocity;
				float3 acceleration;

				float4 color;
				float life;
			};

			StructuredBuffer<Particle> ParticleBuffer;
			StructuredBuffer<float3> VertexBuffer;

			sampler2D _MainTex;

			v2f vert (uint id : SV_VertexID)
			{
				v2f o;
				// o.vertex = UnityObjectToClipPos(v.vertex);
				// o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				// o.vertex = UnityObjectToClipPos(VertexBuffer[id]);

				// Billboarding around local space
				//o.vertex = mul(UNITY_MATRIX_P,
				//	mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0))
				//	- float4(VertexBuffer[id].x, VertexBuffer[id].y, 0.0, 0.0)
				//);
				// Doesn't offset by the local space

				// model maps local space to world space
				// view maps from world space to camera space
				// projection from camera to view screen space

				// do billboard for (0, 0)
				// then offset by the original offset in local space?

				//o.vertex = mul(
				//	UNITY_MATRIX_P,
				//	mul(
				//		UNITY_MATRIX_MV,
				//		float4(0, 0, 0, 1)
				//	) + float4(VertexBuffer[id].x, VertexBuffer[id].y, 0.0, 0.0)
				//);

				o.id = floor(id / 3);

				float3 right = UNITY_MATRIX_V[0].xyz;
				float3 up = UNITY_MATRIX_V[1].xyz;

				o.vertex = float4(
					ParticleBuffer[o.id].position +
					right * VertexBuffer[id].x +
					up * VertexBuffer[id].y,
					1.0
				);

				o.vertex = mul(UNITY_MATRIX_VP, o.vertex);
				
				//float3 rot = VertexBuffer[id].x * left + VertexBuffer[id].y * up;
				//o.vertex = mul(UNITY_MATRIX_VP, rot);

				// o.vertex = UnityObjectToClipPos(VertexBuffer[id]);

				// Shift the vertex buffer points to [0, 1] and just copy for UV. 
				o.uv = VertexBuffer[id].xy + 0.5;

				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				//float a = clamp(1 - distance(i.vertex, ParticleBuffer[i.id].position) / 10, 0, 1);
				
				//return float4(a, a, a, 1.0);

				fixed4 col = tex2D(_MainTex, i.uv);
				
				col = col * 0.7;
				clip(col.a - 0.5);

				return col * ParticleBuffer[i.id].color;

				//return float4(
				//	ParticleBuffer[i.id].color.xyz,
				//	a
				//);
			}
			ENDCG
		}
	}
}
