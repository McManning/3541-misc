using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonCell : MonoBehaviour {
    public bool IsWall { get; internal set; }
    public int Region { get; internal set; }

    DungeonCell()
    {
        IsWall = true;
        Region = 0;
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void LoadMetadata(Dungeon.CellMetadata metadata)
    {
        // TODO: something
    }
}
