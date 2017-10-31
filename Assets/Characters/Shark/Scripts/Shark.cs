using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shark : MonoBehaviour
{
    /// <summary>
    /// Forward FOV in degrees
    /// </summary>
    public float fovDegree;
    public float fovRadius;

    /// <summary>
    /// How long the shark will stay in the idle state 
    /// until the next hunt
    /// </summary>
    public float idleDuration;

    public float biteRadius;

    public float swimmingSpeed;
    public float chasingSpeed;

    [HideInInspector]
    public Vector3 velocity;

    /// <summary>
    /// Objective to swim toward. Can be a waypoint or a fish.
    /// </summary>
    [HideInInspector]
    public GameObject target;

    private Fish[] school;

    void Start()
    {
        school = FindObjectsOfType<Fish>();
    }
    
    public void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }

    /// <summary>
    /// Find all prey within our FOV
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Fish> GetNearbyPrey()
    {
        foreach (var prey in school)
        {
            if (!prey.eaten && IsInFOV(prey.transform.position))
            {
                yield return prey;
            }
        }
    }

    /// <summary>
    /// Determine if a position is in our FOV cone and is not obstructed by colliders
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool IsInFOV(Vector3 position)
    {
        float angle = Vector3.Angle(position - transform.position, transform.forward);

        return Mathf.Abs(angle) < fovDegree * 0.5f
            && Vector3.Distance(position, transform.position) < fovRadius
            && !Physics.Linecast(position, transform.position);
    }

}
