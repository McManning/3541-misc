Shader "Unlit/SparkShader"
{
    Properties
    {
        _Softness("Alpha Softness", Range(0, 1.0)) = 1.0
        _Emissive("Emissive", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        // Additive blend point particles together so clusters are highlighted
        // Blend OneMinusDstColor One

        // Alpha blend the particle (when appropriate)

        Pass
        {
            // Disable backface culling for particle triangles
            Cull off

            // Alpha blend the particle (when appropriate)
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Raytracing.cginc"

            // Data to be passed from vertex to fragment shader
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;

                // Abuse TEXCOORD1 to pass our particle ID through to the fragment shader
                int id : TEXCOORD1; 
                
                float3 eyeDir : TEXCOORD2;
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

            float _Softness;
            float4 _Emissive;

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                o.id = floor(id / 3);

                // TODO: Set initial vertex positions based on the particle
                // position/velocity (i.e., don't use VertexBuffer).
                // We're going to have a triangle that's stretched out to 
                // some factor of the velocity of the particle

                // View matrix right/up vectors
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;

                // TODO: distort 

                // Distort vertices
                float3 v;
                if (id % 3 == 0) { // Top vertex
                    v = float3(0.0, 10.1, 0.0);
                    o.uv = float2(0, 1);
                    // Distort on speed
                    // v = v * -ParticleBuffer[o.id].velocity;
                }
                else if (id % 3 == 1) { // Bottom right vertex
                    v = float3(-10.1, -10.1, 0.0);
                    o.uv = float2(-1, -1);
                }
                else if (id % 3 == 2) { // Bottom left vertex
                    v = float3(10.1, -10.1, 0.0);
                    o.uv = float2(1, -1);
                }

                // Rescale the triangle based on mass, for debugging
                // v = v * clamp(ParticleBuffer[o.id].mass, 0.1, 1.0);

                // Vertex is oriented to always face the camera while using
                // local position of the particle in the system
                // (billboarding, basically)
                /*o.vertex = float4(
                    ParticleBuffer[o.id].position +
                    right * v.x +
                    up * v.y,
                    1.0
                );*/

                o.vertex = float4(v, 1.0);

                o.vertex = mul(UNITY_MATRIX_VP, o.vertex);
            
                // Grab some info about our eye for raytracing
                o.eyeDir = normalize(-WorldSpaceViewDir(o.vertex));

                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Drop unborn particles
                if (!ParticleBuffer[i.id].born) {
                    clip(-1);
                }
            
                // Width of the bottom of a particle triangle
                float width = 0.2 * clamp(ParticleBuffer[i.id].mass, 0.1, 1.0);

                // Raytraced capsule rendering
                if (intersectCapsule(
                    _WorldSpaceCameraPos,
                    i.eyeDir,
                    ParticleBuffer[i.id].position,
                    ParticleBuffer[i.id].position - float3(0, 0, 1),
                    0.6
                )) {
                    return fixed4(0, 1, 0, 1);
                }

                // Create a circle sprite
                fixed4 col = fixed4(1, 1, 1, 1);

                float2 origin = float2(0, -width * 0.5);
                float sqrDist = (i.uv.x - origin.x) * (i.uv.x - origin.x) +
                                (i.uv.y - origin.y) * (i.uv.y - origin.y);

                // Pixel rejection for anything outside the sphere
                // clip(1 - d);
                
                // Change alpha to create soft/hard particles
                col.rgb = 1 - sqrDist * 20; //  1 - d * 0.1;
                
                // Tint with whatever the particle's color is
                return col;
            }
            ENDCG
        }
    }
}
