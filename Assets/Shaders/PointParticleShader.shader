Shader "Unlit/PointParticleShader"
{
	SubShader
	{
		// Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			// Data to be passed from vertex to fragment shader
			struct v2f
			{
				float4 vertex : SV_POSITION;
				int id : TEXCOORD0;
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
				int born;
			};

			StructuredBuffer<Particle> ParticleBuffer;

			v2f vert(uint id : SV_VertexID)
			{
				v2f o;
				o.id = id;
				o.vertex = UnityObjectToClipPos(ParticleBuffer[id].position);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = fixed4(1, 1, 1, 1);

				// Drop unborn particles
				if (!ParticleBuffer[i.id].born) {
					clip(-1);
				}

				// Tint with whatever the particle's color is
				return col * ParticleBuffer[i.id].color;
			}
			ENDCG
		}
	}
}
