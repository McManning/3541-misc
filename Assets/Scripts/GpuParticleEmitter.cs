using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class GpuParticleEmitter : MonoBehaviour
{
    #region Configurations exposed to the Unity editor

    /// <summary>
    /// Instance of PhysicsParticle compute shader used to 
    /// update particle locations per frame via different
    /// affectors (vector fields, collision responses, etc)
    /// </summary>
    public ComputeShader computeShader;

    /// <summary>
    /// Maximum number of particles to be spawned
    /// </summary>
    public int particleCount;
    
    /// <summary>
    /// Emitter radius for spawning new particles
    /// </summary>
    public float emitterRadius;
    
    /// <summary>
    /// Minimum lifetime for a particle
    /// </summary>
    public float minLife;

    /// <summary>
    /// Maximum lifetime for a particle
    /// </summary>
    public float maxLife;

    /// <summary>
    /// Initial acceleration of newly spawned particles
    /// </summary>
    public Vector3 initialAcceleration;

    /// <summary>
    /// Constant acceleration affector. Typically
    /// used to apply gravitational force, but
    /// flexible for whatever.
    /// </summary>
    public Vector3 constantAcceleration;
    
    /// <summary>
    /// Damping ratio applied to elastic collisions
    /// </summary>
    public float dampingRatio;

    /// <summary>
    /// Material with a ParticleShader used for rendering resulting particles
    /// </summary>
    public Material particleMaterial;

    /// <summary>
    /// Whether the shader should use point particle rendering. This changes
    /// how data is pipelined into the shader, and cannot be changed at runtime
    /// </summary>
    public bool usePointParticles;

    /// <summary>
    /// Sphere for demoing collision responses
    /// </summary>
    public GameObject collisionSphere;

    /// <summary>
    /// Object used as an attractor target
    /// </summary>
    public GameObject attractor;

    /// <summary>
    /// Factor applied to acceleration toward attractor
    /// </summary>
    public float attractionAcceleration;

    /// <summary>
    /// Acceleration applied towards curling
    /// </summary>
    public float curlAcceleration;

    /// <summary>
    /// Scaling factor applied to perlin noise generation
    /// </summary>
    public float noiseScale;

    #endregion

    /// <summary>
    /// GPU buffer of all our distinct particles
    /// </summary>
    private ComputeBuffer particleBuffer;
    
    /// <summary>
    /// GPU buffer of vertex data (constant)
    /// </summary>
    private ComputeBuffer vertexBuffer;
    
    /// <summary>
    /// Compute shader kernel to execute each update
    /// </summary>
    private int kernel;

    /// <summary>
    /// Distinct particle in the system. This data structure is pushed
    /// to the GPU and updated each frame without returning to the CPU
    /// </summary>
    protected struct Particle
    {
        public Vector3 position; // 3 floats - 12 bytes
        public Vector3 velocity; // 3 floats - 12 bytes
        public Vector3 acceleration; // 3 floats - 12 bytes

        public Color color; // 4 floats - 16 bytes
        public float life; // 4 bytes
        public int born; // 2 bytes

        // 56 bytes
        // ~ 53 MB at 1mil particles
    }
    
    /// <summary>
    /// Update compute shader's global settings based on current 
    /// configurations and state of relevant game objects
    /// </summary>
    private void UpdateComputeShaderSettings()
    {
        // Emitter properties
        computeShader.SetInt("ParticleCount", particleCount);
        computeShader.SetVector("EmitterPosition", transform.position);
        computeShader.SetFloat("EmitterRadius", emitterRadius);

        // Particle properties
        computeShader.SetFloat("MinLife", minLife);
        computeShader.SetFloat("MaxLife", maxLife);

        // Physics simulation
        computeShader.SetFloat("DeltaTime", Time.deltaTime);
        computeShader.SetFloat("Time", Time.realtimeSinceStartup);

        // Relevant to the elastic particle system
        computeShader.SetVector("InitialAcceleration", initialAcceleration);
        computeShader.SetVector("ConstantAcceleration", constantAcceleration);
        computeShader.SetFloat("DampingRatio", dampingRatio);

        // Relevant to the curl noise particle system
        computeShader.SetFloat("CurlAcceleration", curlAcceleration);
        computeShader.SetFloat("NoiseScale", noiseScale);

        // Spherical collider
        if (collisionSphere)
        {
            computeShader.SetVector("SphereColliderPosition", collisionSphere.transform.position);
            computeShader.SetFloat("SphereColliderRadius", collisionSphere.transform.lossyScale.x * 0.5f);
        }

        // Affector
        if (attractor)
        {
            computeShader.SetVector("AttractorPosition", attractor.transform.position);
            computeShader.SetFloat("AttractionAcceleration", attractionAcceleration);
        }
    }

    void Start ()
    {
        // TODO: Look into SystemInfo.supportsComputeShaders check

        kernel = computeShader.FindKernel("CSMain");
        
        UpdateComputeShaderSettings();

        // Instantiate particles and push onto the buffer
        Particle[] particles = new Particle[particleCount];
        particleBuffer = new ComputeBuffer(particles.Length, Marshal.SizeOf(typeof(Particle)));
        particleBuffer.SetData(particles);
        computeShader.SetBuffer(kernel, "ParticleBuffer", particleBuffer);

        // Generate static buffer of triangles - one per particle
        // TODO: Just let the vertex shader calculate these? It might be 
        // faster than the setup time if we have N-million vertices to setup
        if (!usePointParticles)
        {
            Vector3[] vertices = new Vector3[particleCount * 3];
            for (int i = 0; i < vertices.Length; i += 3)
            {
                vertices[i] = new Vector3(0f, 0.5f, 0f);
                vertices[i + 1] = new Vector3(-0.5f, -0.5f, 0f);
                vertices[i + 2] = new Vector3(0.5f, -0.5f, 0f);
            }

            vertexBuffer = new ComputeBuffer(vertices.Length, Marshal.SizeOf(typeof(Vector3)));
            vertexBuffer.SetData(vertices);
            computeShader.SetBuffer(kernel, "VertexBuffer", vertexBuffer);
        }
	}

    /// <summary>
    /// Recompute particles once per frame
    /// </summary>
    void Update()
    {
        UpdateComputeShaderSettings();
        
        // Execute kernel to parallel update all particles
        computeShader.Dispatch(kernel, particleCount, 1, 1);
        
        // Particle[] output = new Particle[particleCount];
        // particleBuffer.GetData(output);
        // Debug.Log("Sample " + output[10].position);
    }

    /// <summary>
    /// Called *after* the camera has rendered the scene. Unlike OnPostRender,
    /// this script can be attached to any object and this'll be called. 
    /// </summary>
    void OnRenderObject()
    {
        // TODO: Required to SetBuffer every draw?
        particleMaterial.SetPass(0);
        particleMaterial.SetBuffer("ParticleBuffer", particleBuffer);

        // Run either a billboard shader using our vertex buffer 
        // of triangles or a point shader - for a point per particle
        if (!usePointParticles)
        {
            particleMaterial.SetBuffer("VertexBuffer", vertexBuffer);
            Graphics.DrawProcedural(MeshTopology.Triangles, vertexBuffer.count, 1);
        }
        else
        {
            Graphics.DrawProcedural(MeshTopology.Points, particleBuffer.count, 1);
        }
    }

    /// <summary>
    /// Cleanup lingering compute buffer from the GPU
    /// </summary>
    void OnDestroy()
    {
        particleBuffer.Dispose();

        if (vertexBuffer != null)
        {
            vertexBuffer.Dispose();
        }
    }
}
