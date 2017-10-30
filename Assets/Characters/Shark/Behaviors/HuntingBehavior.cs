using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntingBehavior : StateMachineBehaviour
{
    private Shark agent;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponentInParent<Shark>();
        agent.target = GameObject.Find("SharkHuntingObjective");
    }
    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Swim into the hunting grounds spline
        MoveTowardObjective();
        
        // If there's any fish within view, switch to attack
        foreach (var prey in agent.GetNearbyPrey())
        {
            agent.target = prey.gameObject;
            animator.SetTrigger("Attack");
            break;
        }
    }

    /// <summary>
    /// Swim toward the current objective
    /// </summary>
    private void MoveTowardObjective()
    {
        agent.velocity += (agent.target.transform.position - agent.transform.position).normalized;

        agent.velocity = Vector3.ClampMagnitude(agent.velocity, agent.swimmingSpeed);

        agent.transform.LookAt(agent.transform.position + agent.velocity);

        agent.transform.position += agent.velocity * Time.deltaTime;
    }
}
