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
    /// Starting color for a particle
    /// </summary>
    public Color startColor;

    /// <summary>
    /// Ending color for a particle
    /// TODO: Different ramp options? Probably just a LERP for now
    /// </summary>
    public Color endColor;

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
    /// Limit to how fast a particle can be accelerated
    /// </summary>
    public float terminalVelocity;

    /// <summary>
    /// Material with a ParticleShader used for rendering resulting particles.
    /// TODO: Do I even need a material or can I just instantiate the shader directly?
    /// Guess it depends on whether I want to attach textures to them or not
    /// </summary>
    public Material particleMaterial;
    
    /// <summary>
    /// GPU buffer of all our distinct particles
    /// </summary>
    private ComputeBuffer particleBuffer;

    /// <summary>
    /// GPU buffer of particle system metadata (uploaded from CPU once per frame)
    /// </summary>
    private ComputeBuffer metadataBuffer;

    private ComputeBuffer vertexBuffer;

    /// <summary>
    /// Spheres in the scene that act as colliders
    /// </summary>
    private List<GameObject> collisionSpheres;
    
    /// <summary>
    /// Ground plane for planar collisions
    /// </summary>
    private GameObject groundPlane;

    private GameObject collisionSphere;

    #endregion
    
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

        // 56 bytes
        // ~ 53 MB at 1mil particles
    }
    
    /// <summary>
    /// Additional non-constant metadata sent to the compute shader per draw call
    /// </summary>
    struct FrameMetadata
    {
        public float time; // 4 bytes
        public Vector3 groundPlanePosition; // 12 bytes
        public Vector3 groundPlaneNormal; // 12 bytes

        public Vector3 spherePosition;
        public float sphereRadius;
    }
    
    GameObject FindGroundPlane()
    {
        return GameObject.Find("Ground");
    }

    void Start ()
    {
        kernel = computeShader.FindKernel("CSMain");
        
        groundPlane = FindGroundPlane();
        collisionSphere = GameObject.Find("Sphere");

        // TODO: Look into SystemInfo.supportsComputeShaders check
        
        // Set constant properties of the particle system
        computeShader.SetInt("ParticleCount", particleCount);
        computeShader.SetVector("StartColor", startColor);
        computeShader.SetVector("EndColor", startColor);
        computeShader.SetVector("EmitterPosition", transform.position);
        computeShader.SetFloat("EmitterRadius", emitterRadius);
        computeShader.SetFloat("MinLife", minLife);
        computeShader.SetFloat("MaxLife", maxLife);
        computeShader.SetVector("InitialAcceleration", initialAcceleration);
        computeShader.SetVector("ConstantAcceleration", constantAcceleration);
        computeShader.SetFloat("TerminalVelocity", terminalVelocity);
        
        // Instantiate particles and push onto the buffer
        Particle[] particles = new Particle[particleCount];
        particleBuffer = new ComputeBuffer(particles.Length, Marshal.SizeOf(typeof(Particle)));
        particleBuffer.SetData(particles);
        computeShader.SetBuffer(kernel, "ParticleBuffer", particleBuffer);

        // TODO: is it more efficient to use this - or to use a SetFloat/SetVector/etc each draw call?
        metadataBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(FrameMetadata)));
        computeShader.SetBuffer(kernel, "MetadataBuffer", metadataBuffer);
        
        Vector3[] vertices = new Vector3[particleCount * 3];
        vertexBuffer = new ComputeBuffer(vertices.Length, Marshal.SizeOf(typeof(Vector3)));
        vertexBuffer.SetData(vertices);
        computeShader.SetBuffer(kernel, "VertexBuffer", vertexBuffer);
	}

    /// <summary>
    /// Recompute particles once per frame
    /// </summary>
    void Update()
    {
        FrameMetadata[] meta = new FrameMetadata[1];
        meta[0].time = Time.deltaTime;
        meta[0].spherePosition = collisionSphere.transform.position;
        meta[0].sphereRadius = collisionSphere.transform.lossyScale.x * 0.5f; // Assume all scales are equivalent...

        metadataBuffer.SetData(meta);
        
        // Redispatch test
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
        particleMaterial.SetBuffer("ParticleBuffer", particleBuffer);
        particleMaterial.SetBuffer("VertexBuffer", vertexBuffer);
        particleMaterial.SetPass(0);

        // Run ParticleShader over the scene
        Graphics.DrawProcedural(MeshTopology.Triangles, vertexBuffer.count, 1);
    }

    /// <summary>
    /// Cleanup lingering compute buffer from the GPU
    /// </summary>
    void OnDisable()
    {
        particleBuffer.Dispose();
        metadataBuffer.Dispose();
        vertexBuffer.Dispose();
    }
}
