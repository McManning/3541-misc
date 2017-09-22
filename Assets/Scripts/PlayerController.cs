using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float forwardSpeed;
    public float rotationSpeed;
    public Rigidbody rigidBody;

    // Use this for initialization
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
	}

    void ResetPosition()
    {
        transform.position = new Vector3(0.0f, 0.25f, 0.0f);
    }

    /// <summary>
    /// Apply the given forces to the player and move as tank controls
    /// </summary>
    /// <param name="forward"></param>
    /// <param name="rotation"></param>
    void TankControls(float forward, float rotation)
    {
        Vector3 rotationVec = new Vector3(0.0f, rotation, 0.0f);
        Vector3 forwardVec = new Vector3(0.0f, 0.0f, forward);

        rotationVec = rotationVec.normalized * rotationSpeed * Time.deltaTime;
        forwardVec = forwardVec.normalized * forwardSpeed * Time.deltaTime;

        // Apply rotation
        // Note that I normalize it as rotation may be a continuous value in [0, 1]
        if (!rotationVec.Equals(Vector3.zero))
        {
            Debug.Log("Rotation " + rotationVec);
        }
        transform.Rotate(rotationVec);

        // Apply forward force in the new direction we're facing
        if (!forwardVec.Equals(Vector3.zero))
        {
            Debug.Log("Forward " + forwardVec);
        }
        rigidBody.AddRelativeForce(forwardVec);
    }

    /// <summary>
    /// Alternative non-tank controls version, because I misread
    /// the instructions :^)
    /// </summary>
    /// <param name="vertical"></param>
    /// <param name="horizontal"></param>
    void Move(float vertical, float horizontal)
    {
        Vector3 vec = new Vector3(horizontal, 0.0f, vertical);

        // Only good for a fixed camera
        rigidBody.AddForce(vec.normalized * forwardSpeed * Time.deltaTime);

        // rigidBody.AddRelativeForce(vec.normalized * forwardSpeed * Time.deltaTime);

        // Rotate the player to face the direction of movement
        transform.LookAt(transform.position + vec);

        // TODO: Lerping our rotation
        // TODO: Figure out relative positions better so that
        // the over the shoulder camera can be used instead. 
        // (although not necessarily a requirement)
    }
	
	void FixedUpdate()
    {
        // These will assume a default Unity project input mapping
        float horizontal = Input.GetAxisRaw("Horizontal"); // W/S/Up/Down
        float vertical = Input.GetAxisRaw("Vertical"); // A/D/Left/Right
        
        if (Input.GetKey(KeyCode.R))
        {
            ResetPosition();
        }
        else
        {
            Move(vertical, horizontal);
        }
    }
}
