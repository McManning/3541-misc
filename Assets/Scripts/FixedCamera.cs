using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedCamera : MonoBehaviour
{
    public Vector3 offset;

    private Quaternion rotation;

	void Start () {
        // cache initial rotation
        rotation = transform.rotation;
	}
    
	void Update () {
        // force rotation to always be at initial
        transform.rotation = rotation;

        transform.position = transform.parent.position + offset; // - Vector3.back * 5.0f;
	}
}
