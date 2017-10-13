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

    [Range(1, 1024)]
    public int particleCount;

    private ComputeBuffer particleBuffer;
    private int kernel;

    public struct Particle
    {
        public Vector3 position; // 3 floats - 12 bytes
        public Color color; // 4 floats - 16 bytes
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

        // ... etc

        computeShader.SetBuffer(kernel, "ParticleBuffer", particleBuffer);

        // Execute with one thread per "pixel" 
        computeShader.Dispatch(kernel, particleCount, 1, 1); // 256 / 8, 256 / 8, 1);

        // need to push that texture back into something - like another shader.

        // Quick test to pull the buffer back out for analysis
        Particle[] output = new Particle[particleCount];
        particleBuffer.GetData(output);

        Debug.Log(output[55].color);
	}
	
    /**
     * Recompute particles once per frame
     */
	void Update ()
    {
        // Redispatch test
        computeShader.Dispatch(kernel, particleCount, 1, 1);
    }

    /**
     * Called after all regular rendering IFF this script is attached to the camera
     */
    void OnPostRender()
    {
        material.SetBuffer("ParticleBuffer", particleBuffer);
        material.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Points, particleCount, 1);
    }

    /**
     * Cleanup lingering compute buffer from the GPU
     */
    void OnDestroy()
    {
        particleBuffer.Dispose();
    }
}
