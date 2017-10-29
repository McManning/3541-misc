using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Follow a Bezier spline over some period of time
/// </summary>
public class BezierMovement : MonoBehaviour
{
    public BezierSpline spline;

    public float duration;

    private float step;
    
    void Start()
    {
        step = 0;
    }

    void Update()
    {
        step += Time.deltaTime / duration;

        // If we hit the end of the path, loop 
        if (step > 1.0f)
        {
            step -= 1f;
        }

        transform.position = spline.GetPoint(step);
    }
}
