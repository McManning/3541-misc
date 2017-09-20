using UnityEngine;
using System.Collections;

public class HierarchicalTF : MonoBehaviour {
	
	int t;
	const int dt = 1;
	const int maxT = 100;
	
	// Use this for initialization
	void Start () {
		t = 0;
		
		// call the recursive method that traverses the tree and applies rotation to objects with no children
		PrintHierachyPositions(this.transform);
		Debug.Log ("\n");
	}
	
	// Update is called once per frame
	void Update () {
		t = (t + dt)%maxT;
		float normalizedTime = (float) t / maxT;

		// transform the root object (and thus all of its children)
		transform.Translate(0,0.1f*Mathf.Sin((normalizedTime-0.5f)*2.0f*Mathf.PI),0);
	}
	
	void PrintHierachyPositions(Transform t)
	{
		Debug.Log(t.position + t.name);

		for(int i = 0; i < t.childCount; i++)
			PrintHierachyPositions(t.GetChild(i));
	}
}
