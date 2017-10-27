using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleeBehavior : StateMachineBehaviour
{
    private Fish agent;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponentInParent<Fish>();

        // Reset velocity from previous state
        // agent.velocity = Vector3.zero;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 acceleration = CalculateEscape() * agent.escapeForce;

        agent.velocity += acceleration;
        agent.velocity = Vector3.ClampMagnitude(agent.velocity, agent.escapeForce);

        agent.transform.position += agent.velocity * Time.deltaTime;
        
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
}
