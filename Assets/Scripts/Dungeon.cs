using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dungeon : MonoBehaviour
{ 
    public IntVector2 size;
    public DungeonCell cellPrefab;
   
    private DungeonCell[,] cells;

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    /// <summary>
    /// Returns whether the given cell position is in acceptable bounds
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool Contains(IntVector2 position)
    {
        return position.x >= 0 && position.x < size.x && 
            position.z >= 0 && position.z < size.z;
    }

    /// <summary>
    /// Run through the full generator process
    /// </summary>
    public void Generate()
    {
        cells = new DungeonCell[size.x, size.z];
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                CreateCell(new IntVector2(x, z));
            }
        }
    }

    private void CreateCell(IntVector2 position)
    {
        DungeonCell cell = Instantiate(cellPrefab) as DungeonCell;
        cells[position.x, position.z] = cell;
        
        // Transform cell to a local space position relative to its (x, z)
        // Ensuring the parent Dungeon is centered in the generated cells
        cell.position = position;
        cell.transform.parent = transform;
        cell.transform.localPosition = new Vector3(
            position.x - size.x * 0.5f + 0.5f, 
            0f, 
            position.z - size.z * 0.5f + 0.5f
        );
    }
}
