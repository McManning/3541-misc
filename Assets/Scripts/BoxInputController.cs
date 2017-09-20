using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxInputController : MonoBehaviour
{
    public GameObject relativeCamera;
    public float speed;

    void Start()
    {

    }

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;
        float depth = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            vertical = 1.0f * speed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            vertical = -1.0f * speed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontal = 1.0f * speed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            horizontal = -1.0f * speed;
        }
        if (Input.GetKey(KeyCode.E))
        {
            depth = 1.0f * speed;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            depth = -1.0f * speed;
        }

        if (Input.GetKey(KeyCode.R))
        {
            transform.position = new Vector3();
        }
        else
        {
            transform.Translate(horizontal, vertical, depth);
        }

        // So the below makes more sense from an architectural standpoint,
        // but doesn't work in the way we need to turn things in (as a .unitypackage)
        // since it relies on deployable project input settings. 
        // So the above is the ghetto way. :\

        /*float horizontal = Input.GetAxis("Horizontal") * speed;
        float vertical = Input.GetAxis("Vertical") * speed;
        float depth = Input.GetAxis("Depth") * speed;

        // On reset, set the box to origin
        if (Input.GetButtonDown("Reset"))
        {
            transform.position = new Vector3();
        }
        else // Otherwise, transform based on keys down
        {
            transform.Translate(horizontal, vertical, depth);
        }
        */
    }
}
