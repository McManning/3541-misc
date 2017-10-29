using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    /// <summary>
    /// Forward FOV in degrees
    /// </summary>
    public float fov;

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

    /// <summary>
    /// Visual distance to see predators if in FOV
    /// </summary>
    public float predatorDistance;

    /// <summary>
    /// Factor applied to acceleration for running from predators
    /// </summary>
    public float escapeForce;

    [HideInInspector]
    public Vector3 velocity;

    [HideInInspector]
    public bool eaten;

    private Fish[] school;
    private Shark[] predators;
    
    void Start()
    {
        school = FindObjectsOfType<Fish>();
        predators = FindObjectsOfType<Shark>();
        eaten = false;
    }

    void Update()
    {
		
	}
    
    /// <summary>
    /// Get all other agents (fish) that are near my agent
    /// </summary>
    /// <param name="distance">Visual distance to check (may vary based on need)</param>
    /// <returns></returns>
    public IEnumerable<Fish> GetNearbyAgents(float distance)
    {
        foreach (var fish in school)
        {
            if (Vector3.Distance(fish.transform.position, transform.position) < distance
                && fish != this
                && IsInFOV(fish.transform.position)
            ) {
                yield return fish;
            }
        }
    }

    /// <summary>
    /// Get all predators near my agent
    /// Note this ignores FOV, fish are just inherently capable of sensing danger,
    /// because gills.
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public IEnumerable<Shark> GetNearbyPredators()
    {
        foreach (var predator in predators)
        {
            if (Vector3.Distance(predator.transform.position, transform.position) < predatorDistance)
            {
                yield return predator;
            }
        }
    }

    /// <summary>
    /// Return true if the position is within our FOV
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool IsInFOV(Vector3 position)
    {
        float angle = Vector3.Angle(position - transform.position, transform.forward);
        return Mathf.Abs(angle) < fov * 0.5f;
    }
}
