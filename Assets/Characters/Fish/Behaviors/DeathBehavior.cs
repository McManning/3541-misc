using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBehavior : StateMachineBehaviour
{
    private Fish agent;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponentInParent<Fish>();

        // Go poof
        agent.GetComponentInParent<Renderer>().enabled = false;

        // TODO: Spawn some blood particles
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Nothing. We're dead.
    }
}
