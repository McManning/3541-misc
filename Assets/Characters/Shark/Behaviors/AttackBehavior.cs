using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackBehavior : StateMachineBehaviour
{
    private Shark agent;
    private Fish target;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("Enter attack state");
        agent = animator.GetComponentInParent<Shark>();
        target = agent.target.GetComponent<Fish>();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("Exit attack state");
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Swim toward the current target
        agent.velocity += (target.transform.position - agent.transform.position).normalized;

        agent.velocity = Vector3.ClampMagnitude(agent.velocity, agent.chasingSpeed);

        agent.transform.position += agent.velocity * Time.deltaTime;

        agent.transform.LookAt(agent.transform.position + agent.velocity);

        // If we get close enough to eat the target, signal eat and swap back to idle
        if (Vector3.Distance(agent.transform.position, target.transform.position) < agent.biteRadius)
        {
            Debug.Log("Ate target");
            target.eaten = true;
            animator.SetTrigger("Idle");
        }
    }
}
