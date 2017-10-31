using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleeBehavior : StateMachineBehaviour
{
    private Fish agent;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponentInParent<Fish>();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // If we got eaten, transition to death state
        if (agent.eaten)
        {
            animator.SetTrigger("Death");
        }

        // Try to run from all visible predators
        Vector3 acceleration = CalculateEscape() * agent.escapeForce + CalculateAvoidance() * agent.avoidanceForce;

        agent.velocity += acceleration;
        agent.velocity = Vector3.ClampMagnitude(agent.velocity, agent.escapeForce);

        agent.transform.position += agent.velocity * Time.deltaTime;

        agent.transform.LookAt(agent.transform.position + agent.velocity);

        // No visible predators? Go back to school
        if (acceleration == Vector3.zero)
        {
            animator.SetTrigger("Schooling");
        }
    }

    /// <summary>
    /// Get an escape vector from all visible predators, or a zero
    /// vector if there are none visible
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateEscape()
    {
        Vector3 acceleration = Vector3.zero;

        foreach (var predator in agent.GetNearbyPredators())
        {
            // TODO: Further they are away, the less we run
            acceleration += predator.transform.position - agent.transform.position;
        }

        acceleration.Normalize();

        return acceleration * -1;
    }

    /// <summary>
    /// Determine a vector away from colliders
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateAvoidance()
    {
        RaycastHit hit;
        Vector3 acceleration = Vector3.zero;

        if (Physics.Raycast(agent.transform.position, agent.velocity, out hit))
        {
            // This is a pretty lazy stopping force. Basically - don't pass through solids.
            // If the objective or the flock add forces through the solid, we'll pretty much
            // just sit there and wait until those forces change.
            if (hit.distance < 2.0f)
            {
                acceleration = hit.normal;
            }
        }

        if (acceleration.magnitude > 0)
        {
            acceleration = Vector3.ClampMagnitude(acceleration, agent.maxAcceleration);
        }

        return acceleration;
    }
}
