﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SchoolingBehavior : StateMachineBehaviour {
    private FishController agent;
    private GameObject goal;
    private GameObject bounds;
    private FishController[] school;

    private Transform separationDebug;
    private Transform alignmentDebug;
    private Transform cohesionDebug;
    private Transform goalDebug;
    private Transform avoidanceDebug;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponentInParent<FishController>();
        school = FindObjectsOfType<FishController>();
        goal = GameObject.Find("Fishbait");
        bounds = GameObject.Find("Bounds");

        separationDebug = agent.transform.Find("Separation");
        alignmentDebug = agent.transform.Find("Alignment");
        cohesionDebug = agent.transform.Find("Cohesion");
        goalDebug = agent.transform.Find("Goal");
        avoidanceDebug = agent.transform.Find("Avoidance");

        // Give it a random velocity
        //Vector2 rand = Random.insideUnitCircle.normalized; // * 50.0f;
        //agent.velocity.x = rand.x;
        //agent.velocity.z = rand.y;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 alignment = CalculateAlignment() * agent.alignmentForce;
        Vector3 cohesion = CalculateCohesion() * agent.cohesionForce;
        Vector3 separation = CalculateSeparation() * agent.separationForce;
        Vector3 goal = CalculateGoal() * agent.goalForce;
        Vector3 avoidance = CalculateAvoidance() * agent.avoidanceForce;
        
        // Debug lines

        alignmentDebug.rotation = alignment.magnitude > 0 ? Quaternion.LookRotation(alignment) : alignmentDebug.rotation;
        cohesionDebug.rotation = cohesion.magnitude > 0 ? Quaternion.LookRotation(cohesion) : cohesionDebug.rotation;
        separationDebug.rotation = separation.magnitude > 0 ? Quaternion.LookRotation(separation) : separationDebug.rotation;
        goalDebug.rotation = goal.magnitude > 0 ? Quaternion.LookRotation(goal) : goalDebug.rotation;
        avoidanceDebug.rotation = avoidance.magnitude > 0 ? Quaternion.LookRotation(avoidance) : avoidanceDebug.rotation;

        /*
        alignmentDebug.localScale = new Vector3(0, 0, 1.0f + alignment.magnitude);
        cohesionDebug.localScale = new Vector3(0, 0, 1.0f + cohesion.magnitude);
        separationDebug.localScale = new Vector3(0, 0, 1.0f + separation.magnitude);
        goalDebug.localScale = new Vector3(0, 0, 1.0f + goal.magnitude);
        */

        // Update velocity based on the flocking ruleset
        Vector3 acceleration = alignment + cohesion + separation + goal + avoidance;

        agent.velocity += acceleration;
        agent.velocity = Vector3.ClampMagnitude(agent.velocity, agent.maxAcceleration);
        agent.velocity.y = 0;

        agent.transform.position += agent.velocity * Time.deltaTime;

        if (agent.velocity.magnitude > 0)
        {
            // agent.transform.rotation = Quaternion.LookRotation(agent.velocity);
        }
        
        // TODO: If any predators intersect our FOV, run. FOV could be a trigger, but I might have
        // to have a separate thing set a flag. 
        // animator.SetTrigger("Flee");
	}

    /// <summary>
    /// Determine a velocity vector toward the goal location
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateGoal()
    {
        Vector3 acceleration = Vector3.zero;

        acceleration = goal.transform.position - agent.transform.position;
        acceleration.Normalize();
        acceleration = Vector3.ClampMagnitude(acceleration, agent.maxAcceleration);

        return acceleration;
    }

    /// <summary>
    /// Determine an alignment vector to match other agents in the flock
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateAlignment()
    {
        Vector3 acceleration = Vector3.zero;
        int neighbors = 0;

        foreach (var other in GetNearbyAgents(agent.alignmentDistance))
        {
            acceleration += other.velocity;
            neighbors++;
        }

        if (neighbors > 0)
        {
            acceleration /= neighbors;
            acceleration.Normalize();
            acceleration -= agent.velocity;
            acceleration = Vector3.ClampMagnitude(acceleration, agent.maxAcceleration);
        }

        return acceleration;
    }

    /// <summary>
    /// Determine a cohesion vector to steel into average position of neighbor agents
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateCohesion()
    {
        Vector3 acceleration = Vector3.zero;
        int neighbors = 0;

        foreach (var other in GetNearbyAgents(agent.cohesionDistance))
        {
            acceleration += other.transform.position;
            neighbors++;
        }

        if (neighbors > 0)
        {
            acceleration /= neighbors;
            acceleration -= agent.transform.position;
            acceleration.Normalize();
            acceleration -= agent.velocity;
            acceleration = Vector3.ClampMagnitude(acceleration, agent.maxAcceleration);
        }

        return acceleration;
    }

    /// <summary>
    /// Determine an opposite vector to avoid collision with nearest neighbors
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateSeparation()
    {
        Vector3 acceleration = Vector3.zero;
        Vector3 force;
        int neighbors = 0;

        foreach (var other in GetNearbyAgents(agent.separationDistance))
        {
            force = other.transform.position - agent.transform.position;
            force.Normalize();
            force /= Vector3.Distance(other.transform.position, agent.transform.position);

            acceleration += force;
            neighbors++;
        }

        if (neighbors > 0)
        {
            acceleration /= neighbors;
            acceleration.Normalize();
            acceleration -= agent.velocity;
            acceleration = Vector3.ClampMagnitude(acceleration, agent.maxAcceleration);
        }

        // Repulsive force, negative final result
        return acceleration * -1;
    }

    /// <summary>
    /// Determine a vector away from colliders, including a bounding box
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateAvoidance()
    {
        float minX = bounds.transform.localScale.x * -0.5f;
        float maxX = bounds.transform.localScale.x * 0.5f;
        float minZ = bounds.transform.localScale.z * -0.5f;
        float maxZ = bounds.transform.localScale.z * 0.5f;
        Vector3 acceleration = Vector3.zero;
        
        if (agent.transform.position.x < minX)
        {
            acceleration.x = 1;
        }
        else if (agent.transform.position.x > maxX)
        {
            acceleration.x = -1;
        }

        if (agent.transform.position.z < minZ)
        {
            acceleration.z = 1;
        }
        else if (agent.transform.position.z > maxZ)
        {
            acceleration.z = -1;
        }
        
        if (acceleration.magnitude > 0)
        {
            // acceleration -= agent.velocity;
            acceleration = Vector3.ClampMagnitude(acceleration, agent.maxAcceleration);
        }

        return acceleration;
    }

    /// <summary>
    /// Get all other agents (fish) that are near my agent
    /// </summary>
    /// <returns></returns>
    private IEnumerable<FishController> GetNearbyAgents(float distance)
    {
        float angle;

        foreach (var fish in school)
        {
            angle = Vector3.Angle(fish.transform.position - agent.transform.position, agent.transform.forward);

            if (Vector3.Distance(fish.transform.position, agent.transform.position) < distance
                && fish != agent
                && Mathf.Abs(angle) < agent.fov * 0.5f
            ) {
                yield return fish;
            }
        }
    }
}
