using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SchoolingBehavior : StateMachineBehaviour {
    private FishController controller;
    private GameObject goal;
    private GameObject fov;
    private List<GameObject> school;

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        controller = animator.GetComponentInParent<FishController>();
        fov = animator.transform.Find("FOV").gameObject;

        // Track the other fish in our school
        foreach (var obj in FindObjectsOfType<FishController>())
        {
            school.Add(obj.gameObject);
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.transform.Translate(Vector3.forward * 0.25f);

        // TODO: If any predators intersect our FOV, run. FOV could be a trigger, but I might have
        // to have a separate thing set a flag. 
        // animator.SetTrigger("Flee");
	}
}
