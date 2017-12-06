using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeEmitter : MonoBehaviour
{
    static readonly Vector2[] frustumUVs = new Vector2[] {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1)
    };

    public enum DebugTexture
    {
        Velocity = 0,
        Density = 1,
        Pressure = 2,
        Temperature = 3,
        Solids = 4
    }

    public DebugTexture activeTexture = DebugTexture.Density;

    // Static since this is always the same, regardless of instance
    static float[] frustumRays = new float[16];

    public Camera cam;

    public ComputeShader compute;

    public int jacobianIterations = 1;

    public float diffusion = 0.1f;
    public float viscosity = 0.001f;
    public float buoyancy = 5.0f;
    
    public int scale = 32;

    public float timeScale;

    public bool simulate;

    // private Texture3D texture;

    private RenderTexture velocityTex;
    private RenderTexture velocityOutTex;
    private RenderTexture densityTex;
    private RenderTexture densityOutTex;
    private RenderTexture pressureTex;
    private RenderTexture pressureOutTex;
    private RenderTexture temperatureTex;
    private RenderTexture temperatureOutTex;
    private RenderTexture solidsTex;
    private RenderTexture solidsOutTex;

    private int threadGroupsX;
    private int threadGroupsY;
    private int threadGroupsZ;

    private DebugTexture lastActiveTexture;

    void Start()
    {
        threadGroupsX = scale / 8;
        threadGroupsY = scale / 8;
        threadGroupsZ = 1;

        CreateTextures();

        UpdateComputeShaderSettings();
        ComputeTestWrite();

        UpdateActiveTexture();
    }

    void OnDestroy()
    {
        ReleaseTextures();
    }

    private void CreateTextures()
    {
        velocityTex = CreateTexture(RenderTextureFormat.ARGBFloat);
        velocityOutTex = CreateTexture(RenderTextureFormat.ARGBFloat);
        densityTex = CreateTexture(RenderTextureFormat.RFloat);
        densityOutTex = CreateTexture(RenderTextureFormat.RFloat);
        pressureTex = CreateTexture(RenderTextureFormat.RFloat);
        pressureOutTex = CreateTexture(RenderTextureFormat.RFloat);
        temperatureTex = CreateTexture(RenderTextureFormat.RFloat);
        temperatureOutTex = CreateTexture(RenderTextureFormat.RFloat);
        solidsTex = CreateTexture(RenderTextureFormat.RFloat);
        solidsOutTex = CreateTexture(RenderTextureFormat.RFloat);
    }
    
    private void ReleaseTextures()
    {
        velocityTex.Release();
        velocityOutTex.Release();
        densityTex.Release();
        densityOutTex.Release();
        pressureTex.Release();
        pressureOutTex.Release();
        temperatureTex.Release();
        temperatureOutTex.Release();
        solidsTex.Release();
        solidsOutTex.Release();
    }

    private void UpdateActiveTexture()
    {

        Material mat = GetComponent<MeshRenderer>().material;

        RenderTexture texture;
        switch (activeTexture)
        {
            case DebugTexture.Velocity:
                texture = velocityTex;
                break;
            case DebugTexture.Pressure:
                texture = pressureTex;
                break;
            case DebugTexture.Temperature:
                texture = temperatureTex;
                break;
            case DebugTexture.Solids:
                texture = solidsTex;
                break;
            default: // Density
                texture = densityTex;
                break;
        }
        
        mat.SetTexture("_MainTex", texture);
        mat.SetInt("_ActiveTexture", (int)activeTexture);
    }

    private RenderTexture CreateTexture(RenderTextureFormat format)
    {
        var texture = new RenderTexture(scale, scale, 0)
        {
            enableRandomWrite = true,
            format = format,
            filterMode = FilterMode.Point
            // wrapMode = TextureWrapMode.Clamp
        };

        texture.Create();

        // TODO: FilterMode? Should they be billinear, point.. does it matter?
        // I'm not even sure how the compute shader does sampling, so it might
        // not even do interpolation without some funky workarounds

        return texture;
    }

    void Update()
    {
        UpdateComputeShaderSettings();
        
        if (activeTexture != lastActiveTexture)
        {
            lastActiveTexture = activeTexture;
            UpdateActiveTexture();
        }

        if (simulate)
        {
            ComputeVelocityAdvection();

            Swap(ref velocityTex, ref velocityOutTex);

            Project();

            Viscosity();

            ComputeDensityAdvection();
            Swap(ref densityTex, ref densityOutTex);

            Diffusion();

            ComputeTemperatureAdvection();
            Swap(ref temperatureTex, ref temperatureOutTex);
        }
        
        CheckForMouse();
    }

    private void CheckForMouse()
    {
        var mousePos = Input.mousePosition;
        mousePos.z = 1.0f;
        var worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        if (Input.GetMouseButton(0))
        {
            AddPoint(worldPos);
        }
    }
    
    private void AddPoint(Vector3 point)
    {
        int kernel = compute.FindKernel("HitTest");

        Debug.Log(point);

        compute.SetTexture(kernel, "_Velocity", velocityTex);
        compute.SetTexture(kernel, "_Density", densityTex);
        compute.SetTexture(kernel, "_Temperature", temperatureTex);
        compute.SetVector("_HitPoint", point);
        
        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void UpdateComputeShaderSettings()
    {
        compute.SetFloat("_DeltaTime", Time.deltaTime * timeScale);
        compute.SetFloat("_Scale", scale);
        compute.SetFloat("_Viscosity", viscosity);
        compute.SetFloat("_DensityDiffusion", diffusion);
    }
    
    private void Diffusion()
    {
        for (int i = 0; i < jacobianIterations; i++)
        {
            ComputeDiffusion();
            Swap(ref densityTex, ref densityOutTex);
        }
    }

    private void Viscosity()
    {
        for (int i = 0; i < jacobianIterations; i++)
        {
            ComputeViscosity();
            Swap(ref velocityTex, ref velocityOutTex);
        }
    }

    private void Project()
    {
        ClearTexture(pressureTex);

        for (int i = 0; i < jacobianIterations; i++)
        {
            ComputePressure();
            Swap(ref pressureTex, ref pressureOutTex);
        }

        ComputeGradient();
        Swap(ref velocityTex, ref velocityOutTex);
    }

    private void ComputeTestWrite()
    {
        int kernel = compute.FindKernel("TestWrite");

        compute.SetTexture(kernel, "_Solids", solidsTex);
        compute.SetTexture(kernel, "_Velocity", velocityTex);
        compute.SetTexture(kernel, "_VelocityOut", velocityOutTex);

        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void ComputeVelocityAdvection()
    {
        int kernel = compute.FindKernel("VelocityAdvection");

        compute.SetTexture(kernel, "_Solids", solidsTex);
        compute.SetTexture(kernel, "_Velocity", velocityTex);
        compute.SetTexture(kernel, "_VelocityOut", velocityOutTex);

        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void ComputeDensityAdvection()
    {
        int kernel = compute.FindKernel("DensityAdvection");

        compute.SetTexture(kernel, "_Solids", solidsTex);
        compute.SetTexture(kernel, "_Velocity", velocityTex);
        compute.SetTexture(kernel, "_Density", densityTex);
        compute.SetTexture(kernel, "_DensityOut", densityOutTex);

        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void ComputeViscosity()
    {
        int kernel = compute.FindKernel("Viscosity");

        compute.SetTexture(kernel, "_Solids", solidsTex);
        compute.SetTexture(kernel, "_Velocity", velocityTex);
        compute.SetTexture(kernel, "_VelocityOut", velocityOutTex);

        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void ComputeDiffusion()
    {
        int kernel = compute.FindKernel("Diffusion");

        compute.SetTexture(kernel, "_Solids", solidsTex);
        compute.SetTexture(kernel, "_Density", densityTex);
        compute.SetTexture(kernel, "_DensityOut", densityOutTex);

        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void ComputePressure()
    {
        int kernel = compute.FindKernel("Pressure");

        compute.SetTexture(kernel, "_Solids", solidsTex);
        compute.SetTexture(kernel, "_Velocity", velocityTex);
        compute.SetTexture(kernel, "_Pressure", pressureTex);
        compute.SetTexture(kernel, "_PressureOut", pressureOutTex);
        
        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void ComputeGradient()
    {
        int kernel = compute.FindKernel("Gradient");

        compute.SetTexture(kernel, "_Solids", solidsTex);
        compute.SetTexture(kernel, "_Pressure", pressureTex);
        compute.SetTexture(kernel, "_Velocity", velocityTex);
        compute.SetTexture(kernel, "_VelocityOut", velocityOutTex);
        
        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void ComputeTemperatureAdvection()
    {
        int kernel = compute.FindKernel("TemperatureAdvection");

        compute.SetTexture(kernel, "_Solids", solidsTex);
        compute.SetTexture(kernel, "_Velocity", velocityTex);
        compute.SetTexture(kernel, "_Temperature", temperatureTex);
        compute.SetTexture(kernel, "_TemperatureOut", temperatureOutTex);

        compute.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    private void Swap(ref RenderTexture a, ref RenderTexture b)
    {
        var c = a;
        a = b;
        b = c;
    }

    private void ClearTexture(RenderTexture texture)
    {
        Graphics.SetRenderTarget(texture);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
        Graphics.SetRenderTarget(null);
    }

    /// <summary>
    /// Push current camera frustum rays into the compute shader
    /// </summary>
    private void SetCameraFrustum()
    {
        float far = cam.farClipPlane;
        Vector3 pos = cam.transform.position;
        Vector2[] uvs = frustumUVs;

        for (int i = 0; i < 4; i++)
        {
            Vector3 ray = cam.ViewportToWorldPoint(new Vector3(uvs[i].x, uvs[i].y, far)) - pos;
            frustumRays[i * 4 + 0] = ray.x;
            frustumRays[i * 4 + 1] = ray.y;
            frustumRays[i * 4 + 2] = ray.z;
            frustumRays[i * 4 + 3] = 0;
        }

        compute.SetVector("CameraPosition", pos);
        compute.SetFloats("FrustumRays", frustumRays);
    }
    
    /// <summary>
    /// Push new Texture3D into the compute shader
    /// </summary>
    /*private void CreateTexture()
    {
        Color[] colors = new Color[scale * scale * scale];
        texture = new Texture3D(scale, scale, scale, TextureFormat.RGBA32, true);

        float r = 1.0f / (scale - 1.0f);
        for (int x = 0; x < scale; x++)
        {
            for (int y = 0; y < scale; y++)
            {
                for (int z = 0; z < scale; z++)
                {
                    Color c = new Color(x * r, y * r, z * r, 1.0f);
                    colors[x + (y * scale) + (z * scale * scale)] = c;
                }
            }
        }
        texture.SetPixels(colors);
        texture.Apply();

        compute.SetTexture(kernel, "Tex", texture);
    }
    
    void OnRenderObject()
    {
        mat.SetPass(0);
        mat.SetTexture("_MainTex", texture);
        mat.SetInt("_Scale", scale);
        
        Graphics.DrawProcedural(MeshTopology.Points, scale * scale * scale, 1);
    }*/
}
