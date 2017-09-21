using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dungeon : MonoBehaviour
{
    private const int MAX_ROOM_ITERATIONS = 200;

    public class CellMetadata
    {
        public IntVector2 Position { get; internal set; }
        public bool IsWall { get; internal set; }
        public int Region { get; internal set; }

        public CellMetadata(IntVector2 position)
        {
            Position = position;
            IsWall = true;
            Region = 0;
        }
    }

    #region Configurations exposed to the Unity editor
    
    /// <summary>
    /// Dimensions (in cells) of the dungeon.
    /// MUST be an odd number for an optimal generation
    /// </summary>
    public IntVector2 size;

    /// <summary>
    /// Prefab GameObject used to render floors
    /// </summary>
    public DungeonCell floorPrefab;

    /// <summary>
    /// Prefab GameObject used to render walls
    /// </summary>
    public DungeonCell wallPrefab;

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

    #endregion

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
        return 1 + 2 * (Random.Range(min - 1, max - 1) / 2);
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
                cells[x, z] = new CellMetadata(new IntVector2(x, z));
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
                cells[x, z].IsWall = false;
                cells[x, z].Region = currentRegion;
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
    /// Mark a cell as a floor for the current region
    /// </summary>
    /// <param name="cell"></param>
    private void Carve(CellMetadata cell)
    {
        cell.IsWall = false;
        cell.Region = currentRegion;
    }

    private CellMetadata GetCell(IntVector2 position)
    {
        return cells[position.x, position.z];
    }

    /// <summary>
    /// Get all directions around the given cell that we can carve a path.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    private List<IntVector2> GetCarveableDirections(CellMetadata cell)
    {
        List<IntVector2> directions = new List<IntVector2>();

        foreach (IntVector2 dir in IntVector2.Cardinals)
        {
            if (Contains(cell.Position + dir * 3))
            {
                if (GetCell(cell.Position + dir * 2).IsWall)
                {
                    directions.Add(dir);
                }
            }
        }

        return directions;
    }

    /// <summary>
    /// Perform a Tree Growing maze algorithm originating from the given (x, z)
    /// </summary>
    /// <param name="start"></param>
    private void TreeGrowingFloodFill(CellMetadata start)
    {
        List<CellMetadata> frontier = new List<CellMetadata>();
        List<IntVector2> validDirections;
        IntVector2? lastDirection = null;
        CellMetadata cell;

        // quick iteration tracker so my stupidity doesn't cause 
        // Unity to hang again :^)
        int iterations = 0;

        Debug.Log("Tree Growing at " + start.Position.x + ", " + start.Position.z);
        
        // Use a new region for this flood fill maze
        currentRegion += 1;

        // Carve out starting point
        Carve(start);
        frontier.Add(start);

        // Loop until we run out of cells we can carve into
        while (frontier.Count > 0)
        {
            // Pick one at "random" (this implementation uses the last
            // in the stack to continue digging until it's at a dead end)
            cell = frontier[frontier.Count - 1];
            validDirections = GetCarveableDirections(cell);
            
            if (validDirections.Count > 0)
            {
                // Either walk in our last direction (if it is a valid direction)
                // or walk in a random direction if the RNG god says so
                if (!lastDirection.HasValue ||
                    !validDirections.Contains(lastDirection.Value) ||
                    Random.Range(0.0f, 1.0f) < treeGrowingRandomness
                ) {
                    lastDirection = validDirections[
                        Random.Range(0, validDirections.Count)
                    ];
                }

                // Carve into the cell at the given direction
                Carve(GetCell(cell.Position + lastDirection.Value));

                // Carve into the cell after the cell at the given direction
                // and add that cell as another possible branching path
                cell = GetCell(cell.Position + lastDirection.Value * 2);
                Carve(cell);

                frontier.Add(cell);
            }
            else // Can't go anywhere from this cell, we're done using it
            {
                frontier.Remove(cell);
            }
        }
    }

    /// <summary>
    /// Performs a Tree Growing Maze flood fill for every location
    /// we identify as fillable entry points (any isolated cell)
    /// </summary>
    private void BuildMazes()
    {
        CellMetadata cell = FindIsolatedCell();
        
        // lazy safety net until I can figure out unity coroutines
        int iterations = 0;

        while (cell != null && iterations < 100)
        {
            TreeGrowingFloodFill(cell);
            cell = FindIsolatedCell();

            iterations++;
        }
    }

    /// <summary>
    /// Determine if a cell is considered isolated (walls on all sides)
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private bool IsIsolatedCell(CellMetadata cell)
    {
        int x = cell.Position.x;
        int z = cell.Position.z;

        return 
            cell.IsWall &&
            cells[x - 1, z].IsWall && cells[x + 1, z].IsWall &&
            cells[x, z - 1].IsWall && cells[x, z + 1].IsWall &&
            cells[x - 1, z - 1].IsWall && cells[x + 1, z + 1].IsWall &&
            cells[x - 1, z + 1].IsWall && cells[x + 1, z - 1].IsWall;
    }

    /// <summary>
    /// Return a cell that has walls on all sides
    /// </summary>
    /// <returns></returns>
    private CellMetadata FindIsolatedCell()
    {
        // Note we don't touch the edge cells during this search,
        // as those can never be considered isolated
        for (int x = 1; x < size.x-1; x++)
        {
            for (int z = 1; z < size.z-1; z++)
            {
                if (IsIsolatedCell(cells[x, z]))
                {
                    Debug.Log(x + ", " + z);
                    return cells[x, z];
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns true if any open cells intersect the given rectangle
    /// </summary>
    /// <param name="area"></param>
    /// <returns></returns>
    private bool ContainsOpenCell(IntRect rect)
    {
        foreach (CellMetadata cell in openCells)
        {
            if (rect.Contains(cell.Position))
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
        DungeonCell cell;
        
        if (cells[position.x, position.z].IsWall)
        {
            cell = Instantiate(wallPrefab) as DungeonCell;
        }
        else
        {
            cell = Instantiate(floorPrefab) as DungeonCell;
        }
        
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
