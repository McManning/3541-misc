using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Procedural generator for a Rez-style limb
 */
public class ProceduralLimb : MonoBehaviour
{
    #region Configurations exposed to the Unity editor

    public int quads;

    public float startThickness;
    public float middleThickness;
    public float endThickness;
    public float tilt;
    
    #endregion

    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Vector3> normals;
    private List<Vector2> uv;

    void AddQuad(Vector3 center, float size, bool flip)
    {
        // Ensure triangle indices track only new triangles
        int index = vertices.Count;

        // Tilt will be applied along the X axis
        Vector3 tiltVec = new Vector3(0, 0, tilt);

        // Center the vertices around
        Vector3 offset = new Vector3(
            center.x - size * 0.5f, 
            center.y - size * 0.5f, 
            center.z
        );

        // Add vertices in the order:
        // 0 - bottom left, 1 - bottom right, 2 - top left, 3 - top right

        // If we want to flip the quad, we change the order of the 
        // vertices (and apply tilt to different ones)
        // Tilt is applied as a positive factor to the the top vertices
        // and a negative factor to the bottom vertices
        if (!flip)
        {
            vertices.Add(new Vector3(0, 0, 0) + offset + tiltVec);
            vertices.Add(new Vector3(size, 0, 0) + offset + tiltVec);
            vertices.Add(new Vector3(0, size, 0) + offset);
            vertices.Add(new Vector3(size, size, 0) + offset);
        }
        else
        {
            vertices.Add(new Vector3(0, 0, 0) + offset + tiltVec);
            vertices.Add(new Vector3(0, size, 0) + offset);
            vertices.Add(new Vector3(size, 0, 0) + offset + tiltVec);
            vertices.Add(new Vector3(size, size, 0) + offset);
        }
        
        // Add 2 tris clockwise, with (offsetted indices):
        // 1 -> 0 -> 2, 2 -> 3 -> 1
        triangles.Add(index + 1);
        triangles.Add(index);
        triangles.Add(index + 2);

        triangles.Add(index + 2);
        triangles.Add(index + 3);
        triangles.Add(index + 1);

        // Add per-vertex normals (all pointing negative Z)
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);

        // Add per-vertex UVs (spans the whole [1, 0] UV map cuz laziness)
        uv.Add(new Vector2(0, 0));
        uv.Add(new Vector2(1, 0));
        uv.Add(new Vector2(0, 1));
        uv.Add(new Vector2(1, 1));
    }

    void Start()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        normals = new List<Vector3>();
        uv = new List<Vector2>();
      
        float spacing = 1.0f / quads;
        float midpoint = quads * 0.5f;
        float scale;

        // Spawn quads down the limb
        for (int i = 0; i < quads; i++)
        {
            // Lerp between start/mid/end to create sort of an hourglass
            if (i < midpoint)
            {
                scale = Mathf.Lerp(startThickness, middleThickness, i / midpoint);
            }
            else
            {
                scale = Mathf.Lerp(endThickness, middleThickness, midpoint / i);
            }
            
            AddQuad(
                Vector3.forward * spacing * i, 
                scale,
                false
            );

            // Add another quad that is just a flip of the first
            // because apparently I can't do backface culling for
            // a specific mesh without writing a custom shader? :\
            AddQuad(
                Vector3.forward * spacing * i,
                scale,
                true
            );
        }

        BuildMesh();
    }
	
	void Update()
    {
		
	}

    /// <summary>
    /// Build the final mesh and apply it
    /// </summary>
    void BuildMesh()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.uv = uv.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
    }
}
