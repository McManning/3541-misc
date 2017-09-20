using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Dungeon dungeonPrefab;
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
    }

    private void Restart()
    {
        Destroy(dungeonInstance.gameObject);
        Setup();
    }
}
