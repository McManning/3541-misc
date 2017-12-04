using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalEffect : MonoBehaviour
{
    public float scaleSpeed;
    public float scaleMax;
    
	void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            new Vector3(scaleMax, scaleMax, scaleMax), 
            scaleSpeed * Time.deltaTime
        );
    }
}
