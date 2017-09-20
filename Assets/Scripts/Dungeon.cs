using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dungeon : MonoBehaviour
{
    const int MAX_ROOM_ITERATIONS = 200;

    // Configurations exposed to Unity
    public IntVector2 size;

    public DungeonCell cellPrefab;

    public struct CellMetadata
    {
        public IntVector2 position;
        public bool isWall;
        public int region;

        public CellMetadata(int x, int z) : this()
        {
            position.x = x;
            position.z = z;
            isWall = true;
            region = 0;
        }
    }

    /// <summary>
    /// Amount of "randomness" for the tree growing algorithm.
    /// A value of 1 will create very windy hallways, while a 
    /// value of 0 will create as straight of halls as possible
    /// </summary>
    [Range(0.0f, 1.0f)]
    public float treeGrowingRandomness;

    /// <summary>
    /// Maximum rooms to be generated in the dungeon
    /// </summary>
    public int desiredRooms;

    /// <summary>
    /// Smallest room size to generate (in cells)
    /// </summary>
    public int minimumRoomSize;

    /// <summary>
    /// Largest room size to generate (in cells)
    /// MUST be smaller than size.z and size.x
    /// </summary>
    public int maximumRoomSize;
    
    private int currentRegion;

    private CellMetadata[,] cells;

    /// <summary>
    /// Minor optimization when searching for open cells
    /// </summary>
    private List<CellMetadata> openCells;


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
    /// Return a random odd number between min and max (inclusive)
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    private int RandomOdd(int min, int max)
    {
        return 1 + 2 * (int)(Random.Range(min - 1, max - 1) / 2);
    }

    /// <summary>
    /// Run through the full generator process
    /// </summary>
    public void Generate()
    {
        currentRegion = 0;

        cells = new CellMetadata[size.x, size.z];
        openCells = new List<CellMetadata>();

        // Initialize everything in the cell metadata array
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                cells[x, z] = new CellMetadata(x, z);
            }
        }
        
        // Populate the dungeon with open rooms
        BuildRooms();

        // In open spaces between rooms, generate perfect mazes
        BuildMazes();

        // Finally, generate prefabs for every cell
        BuildPrefabs();
    }

    /// <summary>
    /// Create a new room in the Dungeon
    /// </summary>
    /// <param name="rect"></param>
    private void CarveRoom(IntRect rect)
    {
        // Switch to a new region first
        currentRegion++;

        // Open up all cells in the rectangle and set their region
        for (int x = rect.x; x < rect.x + rect.width; x++)
        {
            for (int z = rect.z; z < rect.z + rect.depth; z++)
            {
                cells[x, z].isWall = false;
                cells[x, z].region = currentRegion;
                openCells.Add(cells[x, z]);
            }
        }
    }

    /// <summary>
    /// Randomly place up to `desiredRooms` rooms within the dungeon
    /// </summary>
    private void BuildRooms()
    {
        int iterations = 0;
        int rooms = 0;
        IntRect rect;

        while (iterations < MAX_ROOM_ITERATIONS && rooms < desiredRooms)
        {
            rect = new IntRect()
            {
                width = RandomOdd(minimumRoomSize, maximumRoomSize),
                depth = RandomOdd(minimumRoomSize, maximumRoomSize)
            };

            rect.x = RandomOdd(0, size.x - rect.width);
            rect.z = RandomOdd(0, size.z - rect.depth);
            
            if (!ContainsOpenCell(rect))
            {
                CarveRoom(rect);
                rooms++;
            }

            iterations++;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void BuildMazes()
    {
        // pass
    }

    /// <summary>
    /// Returns true if any open cells are in the given area
    /// </summary>
    /// <param name="area"></param>
    /// <returns></returns>
    private bool ContainsOpenCell(IntRect rect)
    {
        foreach (CellMetadata cell in openCells)
        {
            if (rect.Contains(cell.position))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Place a prefab from the cell metadata in the given position
    /// </summary>
    /// <param name="position"></param>
    private void CreatePrefab(IntVector2 position)
    {
        // For now just generate for the ones that are walls, for testing
        if (cells[position.x, position.z].isWall)
        {
            return;
        }
        
        DungeonCell cell = Instantiate(cellPrefab) as DungeonCell;
        cell.LoadMetadata(cells[position.x, position.z]);
        
        // Transform cell to a local space position relative to its (x, z)
        // Ensuring the parent Dungeon is centered in the generated cells
        cell.transform.parent = transform;
        cell.transform.localPosition = new Vector3(
            position.x - size.x * 0.5f + 0.5f, 
            0f, 
            position.z - size.z * 0.5f + 0.5f
        );
    }

    /// <summary>
    /// Populate the dungeon with prefab objects that represent
    /// walls, doors, floors, props, whatever
    /// </summary>
    private void BuildPrefabs()
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                CreatePrefab(new IntVector2(x, z));
            }
        }
    }
}
