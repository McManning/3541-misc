using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleBehavior : StateMachineBehaviour
{
    private Shark agent;
    private float duration;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponentInParent<Shark>();
        agent.target = GameObject.Find("SharkIdleObjective");
        duration = 0;
    }
    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        MoveTowardObjective();

        duration += Time.deltaTime;
        
        // Getting hungry? Go hunting
        if (duration > agent.idleDuration)
        {
            animator.SetTrigger("Hunting");
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
