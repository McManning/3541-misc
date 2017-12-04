using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Adaption of my original GPU particle emitter for emitting spark effects
/// </summary>
public class SparkParticleEmitter : MonoBehaviour
{
    const int CURVE_SAMPLE_RATE = 32;

    public enum EmitterType
    {
        Point = 0,
        Box = 1,
        Spline = 2
    }

    #region Configurations exposed to the Unity editor

    [Header("Resources")]

    /// <summary>
    /// Compute shader used to update particle positions
    /// </summary>
    public ComputeShader computeShader;

    /// <summary>
    /// Material with a SparkShader used for rendering
    /// </summary>
    public Material particleMaterial;

    [Header("Emitter Settings")]

    /// <summary>
    /// How particles are emitted
    /// </summary>
    public EmitterType emitterType;

    /// <summary>
    /// Maximum number of particles to be spawned
    /// </summary>
    public int particleCount;

    /// <summary>
    /// Delay between spawning new particles
    /// </summary>
    public float spawnDelay;

    /// <summary>
    /// Scaling factor for the initial velocity of particles
    /// </summary>
    public float initialVelocityScale;

    /// <summary>
    /// Randomness factor for the initial velocity
    /// </summary>
    public float initialVelocityNoise;

    /// <summary>
    /// Scaling factor for how much the emitter's velocity
    /// factors into initial velocity of new particles
    /// </summary>
    public float emitterVelocityScale;

    [Header("Particle Settings")]

    /// <summary>
    /// Distribution of randomly chosen mass values for new 
    /// particles. Mass will affect how gravity affects the
    /// particle, as well as how curl noise may affect it
    /// </summary>
    public AnimationCurve mass;

    /// <summary>
    /// Fixed life for particles
    /// </summary>
    public AnimationCurve TTL;

    /// <summary>
    /// Gravity affector, basically. If there's wind in
    /// the scene it could be factor into this as well
    /// </summary>
    public Vector3 constantAcceleration;

    /// <summary>
    /// Curl applied to particle movement. 
    /// Has more effect on lower acceleration particles
    /// </summary>
    public float curlFactor;
    
    /// <summary>
    /// Scaling factor applied to perlin noise generation
    /// TODO: Non-configurable? Kind of a debug setting
    /// </summary>
    public float noiseScale;

    #endregion

    /// <summary>
    /// GPU buffer of all our distinct particles
    /// </summary>
    private ComputeBuffer particleBuffer;
    
    /// <summary>
    /// Compute shader kernel to execute each update
    /// </summary>
    private int kernel;

    private Texture2D splineSamplesTexture;
    private Texture2D curveSamplesTexture;

    /// <summary>
    /// Typically just transform.position
    /// </summary>
    private Vector3 emitterPosition;

    /// <summary>
    /// Emitter's velocity direction. Used to affect
    /// initial velocity of new particles
    /// </summary>
    private Vector3 emitterVelocity;

    /// <summary>
    /// Bounding volume for new particles 
    /// </summary>
    private BoxCollider bounds;

    /// <summary>
    /// Distinct particle in the system. This data structure is pushed
    /// to the GPU and updated each frame without returning to the CPU.
    /// Utilized by both the compute shader and the vert/frag shaders.
    /// </summary>
    protected struct Particle
    {
        /// <summary>
        /// World position of the particle
        /// </summary>
        public Vector3 position; // 3 floats - 12 bytes

        /// <summary>
        /// Current velocity of the particle
        /// </summary>
        public Vector3 velocity; // 3 floats - 12 bytes

        /// <summary>
        /// Particle mass
        /// </summary>
        public float mass; // 4 bytes

        /// <summary>
        /// Length of time the particle has been alive
        /// </summary>
        public float life; // 4 bytes

        /// <summary>
        /// Whether this particle in the system has been born yet
        /// (used for staggered initial spawning)
        /// </summary>
        public int born; // 2 bytes
    }
    
    /// <summary>
    /// Update compute shader's global settings
    /// </summary>
    private void UpdateComputeShaderSettings()
    {
        // Physics simulation timing
        computeShader.SetFloat("DeltaTime", Time.deltaTime);
        computeShader.SetFloat("Time", Time.realtimeSinceStartup);

        // Emitter properties
        computeShader.SetInt("ParticleCount", particleCount);
        computeShader.SetInt("EmitterType", (int)emitterType);
        computeShader.SetVector("EmitterPosition", emitterPosition);
        computeShader.SetVector("EmitterVelocity", emitterVelocity);

        if (emitterType == EmitterType.Box)
        {
            computeShader.SetVector("EmitterBoxMin", bounds.bounds.min);
            computeShader.SetVector("EmitterBoxMax", bounds.bounds.max);
        }
        
        computeShader.SetFloat("SpawnDelay", spawnDelay);
        computeShader.SetFloat("InitialVelocityScale", initialVelocityScale);
        computeShader.SetFloat("InitialVelocityNoise", initialVelocityNoise);
        computeShader.SetFloat("EmitterVelocityScale", emitterVelocityScale);

        // Note: I'm running into an issue where the compute shader refuses to 
        // acknowledge indices > 7 in `float Mass[anything > 8]`. It's a problem
        // that's outside my domain. Write a unity forum question later...
        // computeShader.SetFloats("Mass", new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

        // Workaround for the above. I'm packing animation curves into textures
        UpdateSampleTextures();
        
        // Physics properties
        computeShader.SetVector("ConstantAcceleration", constantAcceleration);
        computeShader.SetFloat("CurlFactor", curlFactor);
        computeShader.SetFloat("NoiseScale", noiseScale);
    }

    void Awake()
    {
        bounds = GetComponent<BoxCollider>();

        // Note that this is going to throw an assertion failure in Unity 2017.1.1f1. It's currently
        // a bug in the engine. See: https://issuetracker.unity3d.com/issues/assertion-transfer-dot-isremappptrtransfer-and-and-transfer-dot-isreadingpptr-is-thrown-when-instantiating-a-compute-shader
        // computeShader = Instantiate(Resources.Load("Shaders/Sparks")) as ComputeShader;
        
        // However, it still loads the shader and we need to do this in order to have
        // multiple instances of the same compute shader active in a scene at once.
        // Otherwise we run into a problem of shared GPU memory between instances. 
        // The bug is fixed in a future build of Unity.

        kernel = computeShader.FindKernel("CSMain");

        // Compressed representation of our spline curve (if applicable)
        // and the distribution curves. Both sampled in the compute shader.
        // Note that this needs to be an RGBAFloat in order to support negatives
        // (because I'm too lazy to scale to [0, 1]). Not sure what the hardware
        // support for that is though...
        curveSamplesTexture = new Texture2D(32, 32, TextureFormat.RGBAFloat, false, true);
        computeShader.SetTexture(kernel, "CurvesSampler", curveSamplesTexture);

        splineSamplesTexture = new Texture2D(32, 32, TextureFormat.RGBAFloat, false, true);
        computeShader.SetTexture(kernel, "SplineSampler", splineSamplesTexture);

        // Instantiate particles and push onto the buffer
        Particle[] particles = new Particle[particleCount];
        particleBuffer = new ComputeBuffer(particles.Length, Marshal.SizeOf(typeof(Particle)));
        particleBuffer.SetData(particles);
        computeShader.SetBuffer(kernel, "ParticleBuffer", particleBuffer);
    }

    /// <summary>
    /// Cleanup lingering compute buffer from the GPU
    /// </summary>
    void OnDestroy()
    {
        if (particleBuffer != null)
        {
            particleBuffer.Dispose();
        }
    }

    void Update()
    {
        // Update constants in the compute shader so we 
        // can modify particle behaviors at runtime
        UpdateComputeShaderSettings();

        // Execute kernel to parallel update all particles
        computeShader.Dispatch(kernel, particleCount, 1, 1);

        emitterVelocity = transform.position - emitterPosition;
        emitterPosition = transform.position;

        // Debug 
        /*Particle[] particles = new Particle[particleCount];
        particleBuffer.GetData(particles);
        Debug.Log(particleBuffer.count);
        foreach (var p in particles)
        {
            Debug.Log(p.velocity.x + ", " + p.velocity.y + " = " + p.mass);
        }*/
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

        // Push to particle shader - rendering a line per particle (debugging)
        Graphics.DrawProcedural(MeshTopology.Lines, particleBuffer.count * 2, 1);

        // Graphics.DrawProcedural(MeshTopology.Triangles, particleBuffer.count * 3, 1);
    }

    /// <summary>
    /// Generates an array CURVE_SAMPLE_RATE samples from an AnimationCurve
    /// </summary>
    /// <returns></returns>
    private float[] GetAnimationCurveSamples(AnimationCurve curve)
    {
        var samples = new float[CURVE_SAMPLE_RATE];
        for (int i = 0; i < CURVE_SAMPLE_RATE; i++)
        {
            samples[i] = curve.Evaluate(
                0.5f
            );

            Debug.Log(samples[i]);
        }
        
        return samples;
    }

    /// <summary>
    /// Convert an animation curve to a texture for use on the GPU
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    private Texture2D GetAnimationCurveTexture(AnimationCurve curve)
    {
        var texture = new Texture2D(32, 32);

        var pixels = new Color[32];
        for (int i = 0; i < 32; i++)
        {
            pixels[i] = new Color(curve.Evaluate(
                i / CURVE_SAMPLE_RATE
            ), 0, 0);
        }

        texture.SetPixels(pixels);
        return texture;
    }
    
    private void UpdateSampleTextures()
    {
        BezierSpline spline = GetComponent<BezierSpline>();
        var splinePixels = new Color[1024];
        var curvePixels = new Color[1024];

        for (int i = 0; i < 1024; i++)
        {
            // R channel - mass distribution
            float r = mass.Evaluate(i / 1024.0f);

            // G channel - TTL distribution
            float g = TTL.Evaluate(i / 1024.0f);

            // If we have a spline emitter, set RGB channels 
            // to a point sampled on the spline
            if (emitterType == EmitterType.Spline)
            {
                var p = spline.GetPoint(i / 1024.0f);
                splinePixels[i] = new Color(p.x, p.y, p.z);
            }

            curvePixels[i] = new Color(r, g, 0, 1);
        }

        // Set pixels and reupload to the GPU
        splineSamplesTexture.SetPixels(splinePixels);
        splineSamplesTexture.Apply();

        curveSamplesTexture.SetPixels(curvePixels);
        curveSamplesTexture.Apply();
    }
}
