using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishController : MonoBehaviour {

    public Vector3 velocity;
    public float fov;
    public float avoidDistance;
    public float maxAcceleration;

    // Flocking behavior weights

    public float speed;

    //[Range(0, 10.0f)]
    public float alignmentWeight;

    //[Range(0, 10.0f)]
    public float cohesionWeight;

    //[Range(0, 10.0f)]
    public float separationWeight;

    //[Range(0, 10.0f)]
    public float goalWeight;

    //[Range(0, 10.0f)]
    public float avoidanceWeight;

    // private Animator animator;

    void Start()
    {
        // animator = GetComponent<Animator>();
	}

    void Update()
    {
		
	}
}
