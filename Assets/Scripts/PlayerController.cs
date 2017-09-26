using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float forwardSpeed;
    public float rotationSpeed;
    public Rigidbody rigidBody;
    public bool tankControls;

    private Quaternion targetRotation;

    // Use this for initialization
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        targetRotation = Quaternion.identity;
	}

    void ResetPosition()
    {
        transform.position = new Vector3(0.0f, 0.25f, 0.0f);
    }

    /// <summary>
    /// Apply the given forces to the player and move as tank controls
    /// Was my original implementation, but decided against it. I added
    /// as a toggle though for shits and gigs
    /// </summary>
    /// <param name="forward"></param>
    /// <param name="rotation"></param>
    void TankControls(float forward, float rotation)
    {
        Vector3 rotationVec = new Vector3(0.0f, rotation, 0.0f);
        Vector3 forwardVec = new Vector3(0.0f, 0.0f, forward);

        // Calculate a normalized rotation and position vector
        rotationVec = rotationVec.normalized;
        forwardVec = forwardVec.normalized * forwardSpeed * Time.deltaTime;

        // Apply rotation
        if (!rotationVec.Equals(Vector3.zero))
        {
            targetRotation = Quaternion.Euler(
                transform.localRotation.eulerAngles + rotationVec * 30.0f
            );
        }
        
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, 
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
        
        // Apply forward force in the new direction we're facing
        if (!forwardVec.Equals(Vector3.zero))
        {
            rigidBody.AddRelativeForce(forwardVec);
        }
    }

    /// <summary>
    /// Alternative non-tank controls version
    /// </summary>
    /// <param name="vertical"></param>
    /// <param name="horizontal"></param>
    void Move(float vertical, float horizontal)
    {
        Vector3 vec = new Vector3(horizontal, 0.0f, vertical);

        // Only good for a fixed camera
        rigidBody.AddForce(vec.normalized * forwardSpeed * Time.deltaTime);

        // Rotate the player to face the direction of movement
        // transform.LookAt(transform.position + vec);

        // Update facing direction
        if (!vec.Equals(Vector3.zero))
        {
            targetRotation = Quaternion.LookRotation(vec.normalized);
        }

        // Lerp toward the facing direction
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
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
            if (tankControls)
            {

                TankControls(vertical, horizontal);
            }
            else
            {
                Move(vertical, horizontal);
            }
        }
    }
}
