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
        public bool IsConnector { get; internal set; }

        public CellMetadata(IntVector2 position)
        {
            Position = position;
            IsWall = true;
            IsConnector = false;
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
    /// Prefab GameObject used to render doors between regions
    /// </summary>
    public DungeonCell doorPrefab;

    /// <summary>
    /// Seed for the random number generator
    /// </summary>
    public int randomSeed;

    /// <summary>
    /// Amount of "randomness" for the tree growing algorithm.
    /// A value of 1 will create very windy hallways, while a 
    /// value of 0 will create as straight of halls as possible
    /// </summary>
    [Range(0.0f, 1.0f)]
    public float treeGrowingRandomness;

    /// <summary>
    /// Chance that extra doors will be added to connect regions
    /// and make less of a true spanning tree
    /// </summary>
    [Range(0.0f, 1.0f)]
    public float doorLoopChance;

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
    /// Run through the full generator process
    /// </summary>
    public void Generate()
    {
        currentRegion = 0;

        if (randomSeed != 0)
        {
            Random.InitState(randomSeed);
        }
        
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

        // Bridge rooms and mazes with doors
        BuildDoors();

        // Finally, generate prefabs for every cell
        BuildPrefabs();
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

    /// <summary>
    /// Get the CellMetadata at the specific (x, z)
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private CellMetadata GetCell(IntVector2 position)
    {
        return cells[position.x, position.z];
    }

    /// <summary>
    /// Get all cells adjacent in the cardinal (and intercardinal) directions of the given cell
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="includeIntercardinals"></param>
    /// <returns></returns>
    private List<CellMetadata> GetAdjacentCells(CellMetadata cell, bool includeIntercardinals)
    {
        List<CellMetadata> adjacent = new List<CellMetadata>();
        IntVector2 position;

        // Grab cells in all cardinal directions
        foreach (IntVector2 direction in IntVector2.Cardinals)
        {
            position = cell.Position + direction;
            if (Contains(position))
            {
                adjacent.Add(GetCell(position));
            }
        }

        // If they ask for intercardinals, grab those too
        if (includeIntercardinals)
        {
            foreach (IntVector2 direction in IntVector2.Intercardinals)
            {
                position = cell.Position + direction;
                if (Contains(position))
                {
                    adjacent.Add(GetCell(position));
                }
            }
        }

        return adjacent;
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
    /// to fill all unoccupied (walled) space s.t. all halls are walled in
    /// </summary>
    /// <param name="start"></param>
    private void TreeGrowingFloodFill(CellMetadata start)
    {
        List<CellMetadata> frontier = new List<CellMetadata>();
        List<IntVector2> validDirections;
        IntVector2? lastDirection = null;
        CellMetadata cell;
        
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
        foreach (CellMetadata adjacent in GetAdjacentCells(cell, true))
        {
            if (!adjacent.IsWall)
            {
                return false;
            }
        }

        return true;
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
    /// Get all unique region IDs of adjacent cells, excluding any 
    /// that don't have a region ID
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    private HashSet<int> GetAdjacentRegions(CellMetadata cell)
    {
        HashSet<int> regions = new HashSet<int>();

        foreach (CellMetadata adjacent in GetAdjacentCells(cell, false))
        {
            regions.Add(adjacent.Region);
        }

        // Remove any "no region" results
        regions.Remove(0);
        return regions;
    }

    /// <summary>
    /// Get all cells that may bridge a gap between the given region 
    /// and another adjacent region
    /// </summary>
    /// <param name="region"></param>
    private List<CellMetadata> GetRegionConnectorCells(int region)
    {
        List<CellMetadata> connectors = new List<CellMetadata>();
        HashSet<int> regions;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                regions = GetAdjacentRegions(cells[x, z]);
                if (regions.Count > 1 && regions.Contains(region))
                {
                    connectors.Add(cells[x, z]);
                }
            }
        }

        return connectors;
    }

    /// <summary>
    /// Return *all* connectors between different regions
    /// </summary>
    /// <returns></returns>
    private List<CellMetadata> GetAllConnectorCells()
    {
        List<CellMetadata> connectors = new List<CellMetadata>();
        HashSet<int> regions;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                regions = GetAdjacentRegions(cells[x, z]);
                if (regions.Count > 1)
                {
                    connectors.Add(cells[x, z]);
                }
            }
        }

        return connectors;
    }

    private void CarveDoor(CellMetadata cell)
    {
        cell.IsConnector = true;
        cell.IsWall = false;
    }
    
    /// <summary>
    /// Change all cells of the given regions to the new region
    /// </summary>
    /// <param name="regions"></param>
    /// <param name="newRegion"></param>
    private void UpdateRegions(HashSet<int> regions, int newRegion)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                if (regions.Contains(cells[x, z].Region)) {
                    cells[x, z].Region = newRegion;
                }
            }
        }
    }

    /// <summary>
    /// Go through all regions generated (distinct rooms and maze halls) 
    /// and connect them together via a simple spanning tree, creating 
    /// a door cell between each.
    /// 
    /// Note that this also has some randomness in it to also allow additional
    /// loops (since a true spanning tree is boring...)
    /// </summary>
    private void BuildDoors()
    {
        List<CellMetadata> allConnectors = GetAllConnectorCells();
        List<CellMetadata> connectors = new List<CellMetadata>();
        HashSet<int> regions;
        HashSet<int> mergedRegions = new HashSet<int>() { 1 };
        CellMetadata door;

        int looper = 0;
        while (allConnectors.Count > 0 && looper++ < 100)
        {
            // Update our list with connectors that connect the merged
            // regions to unmerged regions
            connectors.Clear();
            foreach (CellMetadata cell in allConnectors)
            {
                regions = GetAdjacentRegions(cell);
                regions.IntersectWith(mergedRegions);
                if (regions.Count > 0)
                {
                    connectors.Add(cell);
                }
            }

            // Pick a connector at random to turn into a door
            door = connectors[Random.Range(0, connectors.Count)];

            CarveDoor(door);
            connectors.Remove(door);
            allConnectors.Remove(door);

            // Merge all adjacent regions to the door
            mergedRegions.UnionWith(GetAdjacentRegions(door));

            // For all other connectors that *only* connect to merged regions,
            // either remove them or give them a (small) chance of becoming
            // an extra door (to be less of a spanning tree)
            foreach (CellMetadata cell in connectors)
            {
                regions = GetAdjacentRegions(cell);
                if (regions.IsSubsetOf(mergedRegions)) {
                    allConnectors.Remove(cell);
                }
            }
        }
    }

    /// <summary>
    /// Place a prefab from the cell metadata in the given position
    /// </summary>
    /// <param name="position"></param>
    private void CreatePrefab(IntVector2 position)
    {
        DungeonCell cell;
        
        if (cells[position.x, position.z].IsConnector)
        {
            cell = Instantiate(doorPrefab) as DungeonCell;
        }
        else if (cells[position.x, position.z].IsWall)
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
