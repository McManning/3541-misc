using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishController : MonoBehaviour {

    public Vector3 velocity;
    public float fov;
    public float avoidDistance;
    public float maxAcceleration;

    // Flocking behavior weights

    public float maxVelocity;

    //[Range(0, 10.0f)]
    public float alignmentForce;
    public float alignmentDistance;

    //[Range(0, 10.0f)]
    public float cohesionForce;
    public float cohesionDistance;

    //[Range(0, 10.0f)]
    public float separationForce;
    public float separationDistance;

    //[Range(0, 10.0f)]
    public float goalForce;

    //[Range(0, 10.0f)]
    public float avoidanceForce;

    // private Animator animator;

    void Start()
    {
        // animator = GetComponent<Animator>();
	}

    void Update()
    {
		
	}
}
