Shader "Unlit/SparkDebugShader"
{
	Properties
	{
		_Emissive("Emissive", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

		// Additive blend point particles together so clusters are highlighted
		Blend One One

		Pass
		{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Raytracing.cginc"

            // Data to be passed from vertex to fragment shader
            struct v2f
            {
                float4 vertex : SV_POSITION;
				float u : TEXCOORD0;

                // Abuse TEXCOORD1 to pass our particle ID through to the fragment shader
				// Note that this value does interpolate, but we abuse type conversions
				// to still properly index the appropriate particle
                int id : TEXCOORD1;
            };

            // Properties of a distinct particle in the system
            // MUST match SparkParticleSystem.Particle
            struct Particle
            {
                float3 position;
                float3 velocity;
                float mass;
                float life;
                int born;
            };

            StructuredBuffer<Particle> ParticleBuffer;

            float4 _Emissive;

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                o.id = floor(id / 2);

				float3 p = ParticleBuffer[o.id].position;
				o.u = 1;

				if (id % 2 == 1) { // Spark tail
					float3 v = ParticleBuffer[o.id].velocity;
					float speed = length(v);

					// Always ensure we render a little bit extra,
					// to visualize non-moving particles
					if (speed < 0.0001) {
						p.y += 0.01;
					}
					else {
						p -= normalize(v) * speed * 0.1;
					}

					o.u = 0; // 1 / (speed + 0.5) * 2;
				}

				o.vertex = UnityObjectToClipPos(p);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
				// Drop unborn particles
				if (!ParticleBuffer[i.id].born) {
					clip(-1);
				}

				// Debug stopped particles
				if (length(ParticleBuffer[i.id].velocity) < 0.0001) {
					// return fixed4(1, 0, 0, 1);
				}

				// Debug low mass particles
				if (ParticleBuffer[i.id].mass < 0.1) {
					// return fixed4(0, 1, 0, 1);
					// return _Emissive * 2;
				}

				float3 rgb = _Emissive.rgb * saturate(i.u) * 2;
				rgb *= saturate(ParticleBuffer[i.id].life * 4);

				return fixed4(rgb, 1);
            }
            ENDCG
		}
	}
}
