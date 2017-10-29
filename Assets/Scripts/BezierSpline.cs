using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierSpline : MonoBehaviour
{
    [SerializeField]
    [HideInInspector]
    private Vector3[] points;

    public Color color;

    /// <summary>
    /// Number of curves within the spline (readonly)
    /// </summary>
    public int CurveCount
    {
        get
        {
            return (points.Length - 1) / 3;
        }
    }

    /// <summary>
    /// Number of control points in the spline
    /// </summary>
    public int ControlPointCount
    {
        get
        {
            return points.Length;
        }
    }

    /// <summary>
    /// On initialization or reset, set some defaults
    /// </summary>
    public void Reset()
    {
        // Should always start with 4 points (2 controls and 2 tangents)
        points = new Vector3[] {
            new Vector3(1.0f, 0, 0),
            new Vector3(2.0f, 0, 0),
            new Vector3(3.0f, 0, 0),
            new Vector3(4.0f, 0, 0)
        };

        color = Color.white;
    }

    public Vector3 GetControlPoint(int index)
    {
        return points[index];
    }

    public void SetControlPoint(int index, Vector3 point)
    {
        if (index % 3 == 0)
        {
            Vector3 delta = point - points[index];
            if (index == 0)
            {
                points[1] += delta;
                points[points.Length - 2] += delta;
                points[points.Length - 1] = point;
            }
            else if (index == points.Length - 1)
            {
                points[0] = point;
                points[1] += delta;
                points[index - 1] += delta;
            }
            else
            {
                points[index - 1] += delta;
                points[index + 1] += delta;
            }
        }

        points[index] = point;
        EnforceMirror(index);
    }

    private void EnforceMirror(int index)
    {
        int modeIndex = (index + 1) / 3;
        
        int middleIndex = modeIndex * 3;
        int fixedIndex, enforcedIndex;
        if (index <= middleIndex)
        {
            fixedIndex = middleIndex - 1;
            if (fixedIndex < 0)
            {
                fixedIndex = points.Length - 2;
            }
            enforcedIndex = middleIndex + 1;
            if (enforcedIndex >= points.Length)
            {
                enforcedIndex = 1;
            }
        }
        else
        {
            fixedIndex = middleIndex + 1;
            if (fixedIndex >= points.Length)
            {
                fixedIndex = 1;
            }
            enforcedIndex = middleIndex - 1;
            if (enforcedIndex < 0)
            {
                enforcedIndex = points.Length - 2;
            }
        }

        Vector3 middle = points[middleIndex];
        Vector3 enforcedTangent = middle - points[fixedIndex];
        points[enforcedIndex] = middle + enforcedTangent;
    }

    /// <summary>
    /// Get a point on the curve in world space
    /// </summary>
    /// <param name="t">Location on the curve [0,1]</param>
    /// <returns></returns>
    public Vector3 GetPoint(float t)
    {
        /*Vector3 point = Vector3.Lerp(
            Vector3.Lerp(points[0], points[1], t), 
            Vector3.Lerp(points[1], points[2], t), 
            t
        );
        */
        int i;
        if (t >= 1.0f)
        {
            t = 1.0f;
            i = points.Length - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }

        float oneMinusT = 1.0f - t;
        Vector3 point = oneMinusT * oneMinusT * oneMinusT * points[i]
                    + 3.0f * oneMinusT * oneMinusT * t * points[i + 1]
                    + 3.0f * oneMinusT * t * t * points[i + 2]
                    + t * t * t * points[i + 3];

        return transform.TransformPoint(point);
    }

    /// <summary>
    /// Add a new curve to the spline
    /// </summary>
    public void AddCurve()
    {
        Vector3 point = points[points.Length - 1];
        Array.Resize(ref points, points.Length + 3);

        // Add 3 additional points, and use the last point
        // of the previous curve as our 4th
        point.x += 1.0f;
        points[points.Length - 3] = point;
        point.x += 1.0f;
        points[points.Length - 2] = point;
        point.x += 1.0f;
        points[points.Length - 1] = point;

        // Loop the last point to the starting point
        points[points.Length - 1] = points[0];

        EnforceMirror(0);
    }

    /// <summary>
    /// Draw the spline in the editor window 
    /// </summary>
    public void OnDrawGizmos()
    {
        Gizmos.color = color;

        // Gizmos doesn't have a DrawBezier shorcut, so we
        // estimate this by hand
        Vector3 lineStart = GetPoint(0.0f);

        int step = 20 * CurveCount;
        for (int i = 1; i <= step; i++)
        {
            Vector3 lineEnd = GetPoint(i / (float)step);
            Gizmos.DrawLine(lineStart, lineEnd);
            lineStart = lineEnd;
        }
    }
}
