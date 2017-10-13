using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GpuParticleEmitter : MonoBehaviour
{
    public ComputeShader computeShader;

    [Range(0.0f, 1.0f)]
    public float randomness;
    public Vector4 color;
    public GameObject debugTarget;
    public Material material;
    
    public int particleCount;

    /// <summary>
    /// GPU buffer of all our distinct particles
    /// </summary>
    private ComputeBuffer particleBuffer;

    /// <summary>
    /// GPU buffer of particle system metadata (uploaded from CPU once per frame)
    /// </summary>
    private ComputeBuffer metadataBuffer;

    private int kernel;

    public struct Particle
    {
        public Vector3 position; // 3 floats - 12 bytes
        public Color color; // 4 floats - 16 bytes
    }

    /// <summary>
    /// Additional metadata sent to the compute shader per draw call
    /// </summary>
    struct ParticleSystemMetadata
    {
        public float time;
    }

    void Start ()
    {
        kernel = computeShader.FindKernel("CSMain");

        // Look into SystemInfo.supportsComputeShaders check

        // Setup a writable texture buffer for the shader to work with
        // RenderTexture tex = new RenderTexture(256, 256, 24);
        // tex.enableRandomWrite = true;
        // tex.Create();

        // Set shader properties 
        // computeShader.SetTexture(kernel, "Result", tex);

        computeShader.SetFloat("Randomness", randomness);
        computeShader.SetVector("Color", color);
        computeShader.SetInt("ParticleCount", particleCount);

        //Particle[,] particles = new Particle[256, 256];

        Particle[] particles = new Particle[particleCount];
        particleBuffer = new ComputeBuffer(particles.Length, 28);
        computeShader.SetBuffer(kernel, "ParticleBuffer", particleBuffer);

        // TODO: is it more efficient to use this - or to use a SetFloat/SetVector/etc each draw call?
        metadataBuffer = new ComputeBuffer(1, 4);
        computeShader.SetBuffer(kernel, "MetadataBuffer", metadataBuffer);

        // Execute with one thread per "pixel" 
        // computeShader.Dispatch(kernel, particleCount, 1, 1); // 256 / 8, 256 / 8, 1);

        // need to push that texture back into something - like another shader.

        // Quick test to pull the buffer back out for analysis
        //Particle[] output = new Particle[particleCount];
        //particleBuffer.GetData(output);

        //Debug.Log(output[55].color);
	}

    /// <summary>
    /// Recompute particles once per frame
    /// </summary>
    void Update()
    {
        ParticleSystemMetadata[] meta = new ParticleSystemMetadata[1];
        meta[0].time = Time.realtimeSinceStartup;

        metadataBuffer.SetData(meta);
        
        // Redispatch test
        computeShader.Dispatch(kernel, particleCount, 1, 1);
    }

    /// <summary>
    /// Called *after* the camera has rendered the scene. Unlike OnPostRender,
    /// this script can be attached to any object and this'll be called. 
    /// </summary>
    void OnRenderObject()
    {
        // TODO: Required to SetBuffer every draw?
        material.SetBuffer("ParticleBuffer", particleBuffer);
        material.SetPass(0);

        // Debug.Log("Draw call " + particleCount);

        // Run ParticleShader over the scene
        Graphics.DrawProcedural(MeshTopology.Points, particleCount, 1);
    }

    /// <summary>
    /// Cleanup lingering compute buffer from the GPU
    /// </summary>
    void OnDisable()
    {
        particleBuffer.Dispose();
        metadataBuffer.Dispose();
    }
}
