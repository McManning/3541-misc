using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Dungeon dungeonPrefab;
    public GameObject player;

    public float sceneRotationSpeed;

    private Dungeon dungeonInstance;

    private bool isSceneRotating;

	// Use this for initialization
	void Start()
    {
        isSceneRotating = false;

        Setup();
	}
	
	// Update is called once per frame
	void Update()
    {
		if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0) ||
            Input.GetKeyDown(KeyCode.Keypad0)
        ) {
            ToggleSceneRotation();
        }

        if (isSceneRotating)
        {
            dungeonInstance.transform.Rotate(Vector3.up, sceneRotationSpeed * Time.deltaTime);
        }
	}

    private void ToggleSceneRotation()
    {
        isSceneRotating = !isSceneRotating;
    }

    private void Setup()
    {
        dungeonInstance = Instantiate(dungeonPrefab) as Dungeon;
        dungeonInstance.Generate();

        dungeonInstance.AddPlayer(player);
    }

    private void Restart()
    {
        // Detach player from existing dungeon
        player.transform.parent = null;

        // Delete dungeon and generate a new one
        Destroy(dungeonInstance.gameObject);
        Setup();
    }
}
