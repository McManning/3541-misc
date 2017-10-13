Shader "Unlit/ParticleShader"
{
	SubShader
	{
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

				// Abuse TEXCOORD0 to pass our particle ID through to the fragment shader
				// TODO: https://msdn.microsoft.com/en-us/library/bb509647(VS.85).aspx says
				// it should be a float - am I going to run into issues having it be an int?
				int id : TEXCOORD0; 
			};

			// Replication of PhysicsParticle.compute:Particle
			struct Particle
			{
				float3 position;
				float4 color;
			};

			StructuredBuffer<Particle> ParticleBuffer;
			
			v2f vert (uint id : SV_VertexID)
			{
				v2f o;
				// o.vertex = UnityObjectToClipPos(v.vertex);
				// o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.vertex = UnityObjectToClipPos(ParticleBuffer[id].position);
				o.id = id;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return ParticleBuffer[i.id].color;
			}
			ENDCG
		}
	}
}
