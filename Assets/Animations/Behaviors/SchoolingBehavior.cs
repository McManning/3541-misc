using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SchoolingBehavior : StateMachineBehaviour {
    private FishController agent;
    private GameObject goal;
    private GameObject bounds;
    private FishController[] school;

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponentInParent<FishController>();
        school = FindObjectsOfType<FishController>();
        goal = GameObject.Find("Goal");
        bounds = GameObject.Find("Bounds");
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Update velocity based on the flocking ruleset
        Vector3 acceleration = CalculateAlignment().normalized * agent.alignmentWeight
            + CalculateCohesion().normalized * agent.cohesionWeight
            + CalculateSeparation().normalized * agent.separationWeight
            + CalculateGoal().normalized * agent.goalWeight
            + CalculateAvoidance().normalized * agent.avoidanceWeight;

        agent.velocity = Vector3.ClampMagnitude(acceleration, agent.maxAcceleration);
        agent.velocity.y = 0;

        agent.transform.Translate(agent.velocity * Time.deltaTime);

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
        return Vector3.Normalize(goal.transform.position - agent.transform.position);
    }

    /// <summary>
    /// Determine an alignment vector to match other agents in the flock
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateAlignment()
    {
        Vector3 velocity = Vector3.zero;
        int neighbors = 0;

        foreach (var other in GetNearbyAgents())
        {
            velocity += other.velocity;
            neighbors++;
        }

        if (neighbors > 0)
        {
            velocity = velocity / neighbors;
        }

        return velocity;
    }

    /// <summary>
    /// Determine a cohesion vector to steel into average position of neighbor agents
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateCohesion()
    {
        Vector3 velocity = Vector3.zero;
        int neighbors = 0;

        foreach (var other in GetNearbyAgents())
        {
            velocity += other.transform.position;
            neighbors++;
        }

        if (neighbors > 0)
        {
            velocity = velocity / neighbors - agent.transform.position;
        }

        return velocity;
    }

    /// <summary>
    /// Determine an opposite vector to avoid collision with nearest neighbors
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateSeparation()
    {
        Vector3 velocity = Vector3.zero;
        int neighbors = 0;

        foreach (var other in GetNearbyAgents())
        {
            if (Vector3.Distance(other.transform.position, agent.transform.position) < agent.avoidDistance)
            {
                velocity += other.transform.position - agent.transform.position;
                neighbors++;
            }
        }

        if (neighbors > 0)
        {
            velocity = -velocity / neighbors;
        }

        return velocity;
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
        Vector3 velocity = Vector3.zero;
        
        if (agent.transform.position.x < minX)
        {
            velocity.x = 100;
        }
        else if (agent.transform.position.x > maxX)
        {
            velocity.x = -100;
        }

        if (agent.transform.position.z < minZ)
        {
            velocity.z = 100;
        }
        else if (agent.transform.position.z > maxZ)
        {
            velocity.z = -100;
        }

        return velocity;
    }

    /// <summary>
    /// Get all other agents (fish) that are near my agent
    /// </summary>
    /// <returns></returns>
    private IEnumerable<FishController> GetNearbyAgents()
    {
        foreach (var fish in school)
        {
            if (fish != agent && Vector3.Distance(fish.transform.position, agent.transform.position) < agent.fov)
            {
                yield return fish;
            }
        }
    }
}
