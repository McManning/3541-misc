using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animation of a game object moving through control points in a
/// piecewise cubic Catmull-Rom spline. 
/// 
/// Starts at controlPoints[0], moves to [1], [2]... [size-1], then back to [0] (?)
/// 
/// Also performs re-parameterization by arc length to ensure the object moves at a
/// constant velocity along the curve, besides the edges which it'll ease in/out to
/// 
/// Also rotates the object to look in the direction it's moving, I guess. 
/// </summary>
public class CatmullRomMovement : MonoBehaviour
{
    public GameObject[] controlPoints;

    /// <summary>
    /// Tension factor between points
    /// </summary>
    public float tension = 0.5f;

    /// <summary>
    /// General speed value
    /// </summary>
    public float speed = 0.25f;

    /// <summary>
    /// Color to render the spline as in the editor
    /// </summary>
    public Color splineColor = Color.red;

    /// <summary>
    /// Whether the spline should be looped
    /// </summary>
    private bool loop = true;

    /// <summary>
    /// [0, 1] movement tick through the spline
    /// </summary>
    private float splineTick = 0;
    
    /// <summary>
    /// Samping rate to generate LUT table
    /// </summary>
    private float sampleStep = 0.01f;

    /// <summary>
    /// Lookup table mapping time to arc lengths
    /// </summary>
    private List<Vector3> LUT;

    /// <summary>
    /// Arc length of each subspline
    /// </summary>
    private float[] arcLengths;

    /// <summary>
    /// Arc length of the total system
    /// </summary>
    private float totalArcLength;
    
	void Start()
    {
        // Generate LUT mapping positions on the arc
        // to time steps and subspline indices
        GenerateLUT();

        // DebugDump();

        transform.position = GetPoint(0);
    }

    void Update()
    {
        /*
        splineTick += maxVelocity * Time.deltaTime;
        
        if (splineTick > 1.0f)
        {
            splineTick = 0;
        }
        
        transform.position = GetPoint(splineTick);
        */

        splineTick += Time.deltaTime * speed;
        if (splineTick > 1.0f)
        {
            splineTick = 0;
        }

        // Cubic easing function
        float s = -2 * Mathf.Pow(splineTick, 3) + 3 * Mathf.Pow(splineTick, 2);
        transform.position = GetPoint(s);

        Vector3 d = GetDirection(s);
        transform.rotation = Quaternion.LookRotation(d);
	}

    /// <summary>
    /// Retrieve the control point referenced by index.
    /// This will perform looping of indices if we're on the edge
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private Vector3 GetControlPoint(int index)
    {
        if (loop)
        {
            if (index < 0)
            {
                index = controlPoints.Length + index;
            }

            if (index > controlPoints.Length - 1)
            {
                index = controlPoints.Length - index;
            }
        }

        index = Mathf.Clamp(index, 0, controlPoints.Length - 1);
        return controlPoints[index].transform.position;
    }

    /// <summary>
    /// Get point on a sub-spline (4 controls), where the spline's primary
    /// control point is specified by index. 
    /// </summary>
    /// <param name="index">Primary control point of the sub-spline</param>
    /// <param name="u">Interpolated value [0, 1]</param>
    /// <returns></returns>
    private Vector3 GetSubsplinePoint(int index, float u)
    {
        Vector3 p, pN1, pN2, pP1;

        float u3 = u * u * u;
        float u2 = u * u;
        
        p = GetControlPoint(index);
        pN1 = GetControlPoint(index - 1);
        pN2 = GetControlPoint(index - 2);
        pP1 = GetControlPoint(index + 1);
        
        // TODO: Optimize math
        Vector3 c0 = pN1;
        Vector3 c1 = -1 * tension * pN2 + tension * p;
        Vector3 c2 = 2 * tension * pN2 + (tension - 3) * pN1 + (3 - 2 * tension) * p + -1 * tension * pP1;
        Vector3 c3 = -1 * tension * pN2 + (2 - tension) * pN1 + (tension - 2) * p + tension * pP1;

        return c3 * u3 + c2 * u2 + c1 * u + c0;
    }

    private Vector3 GetPoint(float u)
    {
        for (int i = 1; i < LUT.Count; i++)
        {
            if (u < LUT[i].y / GetArcLength())
            {
                return GetSubsplinePoint(
                    (int)LUT[i].x,
                    LUT[i].z
                );
            }
        }
        
        return Vector3.zero;
    }
    
    private Vector3 GetDirection(float u)
    {
        Vector3 start = GetPoint(u);
        Vector3 end;

        if (u + sampleStep > 1.0f)
        {
            end = GetPoint(0);
        } 
        else
        {
            end = GetPoint(u + sampleStep);
        }

        return (end - start).normalized;
    }

    /// <summary>
    /// Approximate an arc length of the subspline at index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private float GetSubsplineArcLength(int index)
    {
        return arcLengths[index];
    }

    /// <summary>
    /// Approximate arc length for the entire spline
    /// </summary>
    /// <returns></returns>
    private float GetArcLength()
    {
        return totalArcLength;
    }
    
    void GenerateLUT()
    {
        LUT = new List<Vector3>
        {
            Vector3.zero
        };

        arcLengths = new float[controlPoints.Length];
        totalArcLength = 0;
        
        float f = 0;
        for (int index = 0; index < controlPoints.Length; index++)
        {
            Vector3 prev = GetSubsplinePoint(index, 0);
            Vector3 next;

            arcLengths[index] = CalcSubsplineLength(index);
            for (; f < arcLengths[index]; f += sampleStep)
            {
                next = GetSubsplinePoint(index, f / arcLengths[index]);
                float length = (next - prev).magnitude;
                totalArcLength += length;
                prev = next;

                LUT.Add(new Vector4()
                {
                    x = index,
                    y = totalArcLength,
                    z = f / arcLengths[index]
                });
            }

            f -= arcLengths[index]; // Offset for next control point
        }
    }

    void DebugDump()
    {
        foreach (var entry in LUT)
        {
            Debug.Log(entry);
        }

        foreach (var length in arcLengths)
        {
            Debug.Log(length);
        }
    }
    
    float CalcSubsplineLength(int index)
    {
        float arcLength = 0;
        Vector3 prev = GetSubsplinePoint(index, 0);
        Vector3 next;
        
        for (float f = sampleStep; f < 1.0f; f += sampleStep)
        {
            next = GetSubsplinePoint(index, f);
            float length = (next - prev).magnitude;
            arcLength += length;
            prev = next;
        }

        return arcLength;
    }

    void OnDrawGizmos()
    {
        /*Gizmos.color = Color.white;
        Vector3 prev = controlPoints[0].transform.position;

        // Draw direct edge between points
        foreach (var point in controlPoints)
        {
            Gizmos.DrawLine(prev, point.transform.position);
            prev = point.transform.position;
        }
        */

        // Draw spline estimations
        Gizmos.color = splineColor;

        for (float i = 0.02f; i < 1.0f; i += 0.02f)
        {
            for (int index = 0; index < controlPoints.Length; index++)
            {
                Gizmos.DrawLine(
                    GetSubsplinePoint(index, i - 0.02f),
                    GetSubsplinePoint(index, i)
                );
            }
        }

        // Debug how LUT samping is done, to ensure it's evenly divided at all times
        float f = 0;
        for (int index = 0; index < controlPoints.Length; index++)
        {
            Vector3 prev = GetSubsplinePoint(index, 0);
            Vector3 next;

            float step = 1.0f;

            float len = CalcSubsplineLength(index);
            for (; f < len; f += step)
            {
                next = GetSubsplinePoint(index, f / len);
                prev = next;

                Gizmos.DrawSphere(prev, 0.1f);
            }

            f -= len; // Offset for next control point
        }
    }
}
