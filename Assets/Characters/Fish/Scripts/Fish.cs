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

    public float thinkThreshold;

    [HideInInspector]
    public Vector3 velocity;

    [HideInInspector]
    public bool eaten;

    [HideInInspector]
    public BoxCollider[] colliders;

    private Fish[] school;
    private Shark[] predators;
    
    void Start()
    {
        school = FindObjectsOfType<Fish>();
        predators = FindObjectsOfType<Shark>();

        // Box colliders represent all other collidables in the scene (ground, cubes, etc)
        colliders = FindObjectsOfType<BoxCollider>();

        eaten = false;
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
            if (fish != this && IsInFOV(fish.transform.position, distance)) {
                yield return fish;
            }
        }
    }

    /// <summary>
    /// Get all predators visible to my agent
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public IEnumerable<Shark> GetNearbyPredators()
    {
        foreach (var predator in predators)
        {
            if (IsInFOV(predator.transform.position, predatorDistance))
            {
                yield return predator;
            }
        }
    }

    /// <summary>
    /// Return true if the position is within our FOV and is not obstructed by colliders
    /// </summary>
    /// <param name="position"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public bool IsInFOV(Vector3 position, float distance)
    {
        float angle = Vector3.Angle(position - transform.position, transform.forward);

        return Mathf.Abs(angle) < fov * 0.5f
            && Vector3.Distance(position, transform.position) < distance
            && !Physics.Linecast(position, transform.position);
    }
}
