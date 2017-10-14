Shader "Unlit/ParticleShader"
{
	Properties 
	{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha

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
				o.id = floor(id / 3);

				// View matrix right/up vectors
				float3 right = UNITY_MATRIX_V[0].xyz;
				float3 up = UNITY_MATRIX_V[1].xyz;

				// Vertex is oriented to always face the camera while using
				// local position of the particle in the system
				o.vertex = float4(
					ParticleBuffer[o.id].position +
					right * VertexBuffer[id].x +
					up * VertexBuffer[id].y,
					1.0
				);

				o.vertex = mul(UNITY_MATRIX_VP, o.vertex);
				
				// Shift the vertex buffer points to [0, 1] and just copy for UV. 
				o.uv = VertexBuffer[id].xy + 0.5;

				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				
				// Create a circular sprite region 
				float d = 20 * ((i.uv.x - 0.5) * (i.uv.x - 0.5) + (i.uv.y - 0.5) * (i.uv.y - 0.5));

				clip(1 - d);
				
				// col.a = 1 - d; //  1 - d / 0.05;

				col.a = 1;

				return col * ParticleBuffer[i.id].color;
			}
			ENDCG
		}
	}
}
