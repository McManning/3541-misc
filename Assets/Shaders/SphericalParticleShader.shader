Shader "Unlit/SphericalParticleShader"
{
	Properties 
	{
		_Softness("Alpha Softness", Range (0, 1.0)) = 1.0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

		Pass
		{ 
			// Disable backface culling for particle triangles
			Cull off

			// Alpha blend the particle (when appropriate)
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			// Data to be passed from vertex to fragment shader
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

				// Abuse TEXCOORD1 to pass our particle ID through to the fragment shader
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

			float _Softness;

			v2f vert (uint id : SV_VertexID)
			{
				v2f o;
				o.id = floor(id / 3);

				// View matrix right/up vectors
				float3 right = UNITY_MATRIX_V[0].xyz;
				float3 up = UNITY_MATRIX_V[1].xyz;

				// Vertex is oriented to always face the camera while using
				// local position of the particle in the system
				// (billboarding, basically)
				o.vertex = float4(
					ParticleBuffer[o.id].position +
					right * VertexBuffer[id].x +
					up * VertexBuffer[id].y,
					1.0
				);

				o.vertex = mul(UNITY_MATRIX_VP, o.vertex);
				
				// Shift the vertex buffer points to a [0, 1] range and just copy for UV. 
				o.uv = VertexBuffer[id].xy + 0.5;

				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = fixed4(1, 1, 1, 1);
				
				// Create a circular sprite region 
				float d = 20 * ((i.uv.x - 0.5) * (i.uv.x - 0.5) + (i.uv.y - 0.5) * (i.uv.y - 0.5));

				// Pixel rejection for anything outside the sphere
				clip(1 - d);
				
				// Change alpha to create soft/hard particles
				col.a = 1 - d * _Softness;
				
				// Tint with whatever the particle's color is
				return col * ParticleBuffer[i.id].color;
			}
			ENDCG
		}
	}
}
