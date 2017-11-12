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
    /// Whether the spline should be looped
    /// </summary>
    public bool loop = false;

    [Range(0, 20)]
    public int debugSamplePoints = 10;

    public float speed = 0.02f;

    private float splineTick = 0;
    
    private float sampleStep = 0.5f;

    /// <summary>
    /// Lookup table mapping the following:
    /// x. control point index number we're within at timestep y
    /// y. timestep normalized to [0, 1]
    /// z. arc distance traveled across index x
    /// w. total distance traveled at time y (*not* normalized)
    /// </summary>
    private List<Vector4> LUT;

    /// <summary>
    /// Arc length of each subspline
    /// </summary>
    private float[] arcLengths;

    /// <summary>
    /// Arc length of the total system
    /// </summary>
    private float totalArcLength;

    private Vector3 velocity;

	void Start()
    {
        // Generate LUT mapping positions on the arc
        // to time steps and subspline indices
        GenerateLUT();

        DebugDump();

        transform.position = GetPoint(0);
    }

    void Update()
    {
        splineTick += speed * Time.deltaTime;

        if (splineTick > 1.0f)
        {
            splineTick = 0;
        }
        
        transform.position = GetPoint(splineTick);
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
                int indexHigh = (int)LUT[i].x;
                int indexLow = (int)LUT[i - 1].x;
                int index = indexHigh;
                float timeHigh = LUT[i].y / GetArcLength();
                float timeLow = LUT[i - 1].y / GetArcLength();
                float stepHigh = LUT[i].z;
                float stepLow = LUT[i - 1].z;
                /*
                // If we're between indices, use a point in either index
                if (Mathf.Abs(indexHigh - indexLow) > 0.01f)
                {
                    if (u / GetSubsplineArcLength(indexLow) > 1.0f)
                    {
                        // In high index, set low values to 0
                        stepLow = 0;
                        timeLow = 0;
                        index = indexHigh;
                        Debug.Log("use high");
                    }
                    else
                    {
                        // In low index, set highs to 1
                        stepHigh = 1.0f;
                        timeHigh = 1.0f;
                        index = indexLow;
                        Debug.Log("use low");
                    }
                }

                Debug.Log("index " + index + " stepLow " + stepLow + " stepHigh " + stepHigh + " timeLow " + timeLow + " timeHigh " + timeHigh);

                // Interpolate the point between low & high
                float interpolated = Mathf.Lerp(
                    stepLow,
                    stepHigh,
                    (u - timeLow) / (timeHigh - timeLow)
                );
                */
                
                float interpolated = stepHigh;
                index = indexHigh;

                return GetSubsplinePoint(
                    index, 
                    interpolated // / GetSubsplineArcLength(index)
                );
            }
        }

        // Not on the table? Assume to be at the end of the arc then
        return GetSubsplinePoint(controlPoints.Length, 1.0f);
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

    private void GenerateLUTOld()
    {
        /// Lookup table mapping the following:
        /// x. control point index number we're within at timestep y
        /// y. timestep normalized to [0, 1]
        /// z. arc distance traveled across index x
        /// w. total distance traveled at time y (*not* normalized)

        LUT = new List<Vector4>
        {
            Vector4.zero
        };

        arcLengths = new float[controlPoints.Length];
        totalArcLength = 0;

        for (int index = 0; index < controlPoints.Length; index++)
        {
            Vector3 prev = GetSubsplinePoint(index, 0);
            Vector3 next;
            
            arcLengths[index] = 0;

            for (float f = sampleStep; f < 1.0f; f += sampleStep)
            {
                next = GetSubsplinePoint(index, f);
                float length = (next - prev).magnitude;
                arcLengths[index] += length;
                totalArcLength += length;
                prev = next;
                
                LUT.Add(new Vector4()
                {
                    x = index,
                    y = totalArcLength,
                    z = arcLengths[index],
                    w = 0
                });
            }
        }
    }

    void GenerateLUT()
    {
        LUT = new List<Vector4>
        {
            Vector4.zero
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
                    z = f / arcLengths[index],
                    w = 0
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
        Gizmos.color = Color.red;

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
