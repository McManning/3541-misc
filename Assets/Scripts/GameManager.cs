using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Dungeon dungeonPrefab;
    public GameObject player;

    public float sceneRotationSpeed;

    public Rect minimapRect;

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
        Camera.main.clearFlags = CameraClearFlags.Skybox;
        Camera.main.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

        dungeonInstance = Instantiate(dungeonPrefab) as Dungeon;
        dungeonInstance.Generate();

        dungeonInstance.AddPlayer(player);

        Camera.main.clearFlags = CameraClearFlags.Depth;
        Camera.main.rect = minimapRect;
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
