using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Dungeon dungeonPrefab;
    public GameObject player;

    private Dungeon dungeonInstance;
    
	// Use this for initialization
	void Start ()
    {
        Setup();
	}
	
	// Update is called once per frame
	void Update ()
    {
		if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
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
