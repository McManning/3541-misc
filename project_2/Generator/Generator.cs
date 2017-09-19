using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    class Generator
    {
        protected struct Tile
        {
            /// <summary>
            /// Whether the tile is open or a wall
            /// </summary>
            public bool open;

            /// <summary>
            /// Calculated region of a tile
            /// </summary>
            public int region;

            /// <summary>
            /// Whether this tile is a connector between regions
            /// </summary>
            public bool connector;

            /// <summary>
            /// Whether this tile has been indicated to be a door merging
            /// two different regions
            /// </summary>
            public bool door;

            /// <summary>
            /// Return ASCII representation of the tile, for debugging
            /// </summary>
            /// <returns>ASCII char</returns>
            public char Ascii()
            {
                if (open)
                {
                    if (door)
                    {
                        return 'D';
                    }

                    return ' ';
                }

                return '#';
            }
        }

        protected struct Room
        {
            /// <summary>
            /// Calculated region for the room. Shared 
            /// by all tiles that make up the room
            /// </summary>
            public int region;

            public int x;
            public int y;
            public int width;
            public int height;

            public bool placed;
        }

        protected struct Maze
        {
            /// <summary>
            /// Calculated region for the maze. Shared
            /// by all tiles that make up the maze
            /// </summary>
            public int region;
        }

        protected Tile[,] tiles;
        protected List<Room> rooms;
        protected List<Maze> mazes;

        // Lazy hardcoding of adjustables

        // Global settings
        protected const int DUNGEON_WIDTH = 5;
        protected const int DUNGEON_HEIGHT = 5;

        // Room generator settings
        protected const int DESIRED_ROOMS = 20;
        protected const int ROOM_SIZE_MIN = 5;
        protected const int ROOM_SIZE_MAX = 10;
        protected const int ROOM_GEN_ITERATION_THRESHOLD = 100;

        protected Random random;

        protected int nextRegion;

        public Generator()
        {
            random = new Random();
            nextRegion = 1;
        }

        /// <summary>
        /// Determine if a specific rectangle of tiles are open
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        protected bool IsOpen(int x, int y, int w, int h)
        {
            for (int j = y; j < y + h; j++)
            {
                for (int i = x; i < x + w; i++)
                {
                    if (tiles[i, j].open)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Generate a random odd number
        /// </summary>
        /// <param name="min">Minimum (inclusive)</param>
        /// <param name="max">Maximum (exclusive)</param>
        /// <returns></returns>
        protected int RandomOdd(int min, int max)
        {
            return 1 + 2 * random.Next(min / 2, max / 2);
        }

        /// <summary>
        /// Mark a rectangle of tiles as an open room, along with a new region ID
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected void PlaceRoom(int x, int y, int width, int height)
        {
            // Mark all tiles in the rect as being part of the room
            // and give them a new region index
            for (int j = y; j < y + height; j++)
            {
                for (int i = x; i < x + width; i++)
                {
                    tiles[i, j].open = true;
                    tiles[i, j].region = nextRegion;
                }
            }

            // Ensure that the next placement is a different region
            nextRegion++;
        }

        /// <summary>
        /// Place large rooms randomly within our world, ensuring none overlap.
        /// </summary>
        protected void GenerateRooms()
        {
            /* Algorithm is as follows:
             * 1. pick a random room size within acceptable bounds, and is an odd number
             * 2. ++ step counter. If > some threshold, stop placing rooms (potential infinite loop)
             * 3. pick a random top coordinate (x,y) for the room, where x and y are odd numbers
             * 4. check if room can be placed with the given size and coordinates
             *      if not - go back to step 2
             *      if so - place room, ++ the number of placed rooms
             * 5. repeat from 1 until desired room count
             */
            int i = 0;
            int iterations = 0;
            int x;
            int y;
            int width;
            int height;

            while (i < DESIRED_ROOMS)
            {
                // Generate a room somewhere randomly
                width = RandomOdd(ROOM_SIZE_MIN, ROOM_SIZE_MAX);
                height = RandomOdd(ROOM_SIZE_MIN, ROOM_SIZE_MAX);
                x = RandomOdd(0, DUNGEON_WIDTH - width);
                y = RandomOdd(0, DUNGEON_HEIGHT - height);

                // Place if nothing else has been placed here
                // Otherwise, we'll need to try again.
                if (!IsOpen(x, y, width, height))
                {
                    PlaceRoom(x, y, width, height);
                    i++;
                }

                // Too many iterations? Give up
                iterations++;
                if (iterations > ROOM_GEN_ITERATION_THRESHOLD)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Check if any neighbors (8 axis) are open
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected bool HasOpenNeighbor(int x, int y)
        {
            for (int j = -1; j < 2; j++)
            {
                for (int i = -1; i < 2; i++)
                {
                    if (i != 0 && j != 0) // not same tile
                    {
                        // TODO: Edges
                        if (tiles[i, j].open)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public struct Point
        {
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; set; }
            public int Y { get; set; }
        }

        protected void GrowingTree(int x, int y)
        {
            List<Point> points = new List<Point>();
            List<Point> adjacent;

            tiles[x, y].open = true;
            tiles[x, y].region = nextRegion;

            points.Add(new Point(x, y));

            Point p;
            while (points.Count > 0)
            {
                p = points[points.Count - 1];

                // Find adjacent walls to open up
                adjacent = GetAdjacentWalls(p.X, p.Y);

                if (adjacent.Count > 0)
                {
                    // grab random adjacent wall to open


                }
                else // Nothing adjacent we can remove, dead end
                {
                    points.Remove(p);
                }
            }
        }

        protected List<Point> GetAdjacentWalls(int x, int y)
        {
            // for each cardinal direction
            // if the tile is a wall, and further from the tile isn't the end of the map
            //  add
            List<Point> walls = new List<Point>();

            for (int j = y - 1; j < y + 2; j++)
            {
                for (int i = x - 1; i < x + 2; i++)
                {
                    // ensure it's not the same tile or off the map
                    if (i != 0 && j != 0 && i > 1 && j > 1 &&
                        i < DUNGEON_WIDTH && j < DUNGEON_HEIGHT
                    )
                    {
                        if (!tiles[i, j].open)
                        {
                            walls.Add(new Point(i, j));
                        }
                    }
                }
            }

            return walls;
        }

        /// <summary>
        /// Flood fill a perfect maze starting at the given (x, y) coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected void FloodFillMaze(int x, int y)
        {
            // Going to go with Randomized Prim's algorithm because it's easy. May switch. Idk.

            /*
             * Randomized Prims is basically:
             *  1. start with a grid full of walls (check)
             *  2. pick (x, y), mark as part of the maze (set region). 
             *      And add walls to the wall list (and set regions)
             *  3. While there are walls on the wall list:
             *      1. pick random wall from the list. If only one of the two cells that
             *          wall divides is visited (has a region) then:
             *          1. Open the wall tile and mark the unvisited tile as part of the region
             *          2. Add the neighboring walls of the tile to the wall list and mark to region
             *      2. remove the wall from the list
             */
            List<Point> walls = new List<Point>();
            Point wall;
            int i;
            int j;

            tiles[x, y].open = true;
            tiles[x, y].region = nextRegion;

            // Add surrounding walls to the wall list and mark region
            for (j = y - 1; j < y + 2; j++)
            {
                for (i = x - 1; i < x + 2; i++)
                {
                    // ensure it's not the same tile or off the map
                    if (i != 0 && j != 0 && i >= 0 && j >= 0 &&
                        i < DUNGEON_WIDTH && j < DUNGEON_HEIGHT
                    )
                    {
                        if (!tiles[i, j].open)
                        {
                            tiles[i, j].region = nextRegion;
                            walls.Add(new Point(i, j));
                            Console.WriteLine("Add Wall " + i + ", " + j);
                        }
                    }
                }
            }

            // While there's walls, pick one at random
            while (walls.Count > 0)
            {
                wall = walls[random.Next(walls.Count)];
                Console.WriteLine("Pick Wall " + wall.X + ", " + wall.Y);

                // Try north/south
                if (wall.Y > 0 && wall.Y < DUNGEON_HEIGHT - 2 &&
                    (tiles[wall.X, wall.Y - 1].region == 0 ||
                    tiles[wall.X, wall.Y + 1].region == 0)
                )
                {
                    tiles[wall.X, wall.Y].open = true;

                    // mark the unvisited one as part of the region
                    if (tiles[wall.X, wall.Y - 1].region == 0)
                    {
                        wall.Y--;
                    }
                    else
                    {
                        wall.Y++;
                    }

                    tiles[wall.X, wall.Y].region = nextRegion;

                    // Add neighbors to the list
                    for (j = wall.Y - 1; j < wall.Y + 2; j++)
                    {
                        for (i = wall.X - 1; i < wall.X + 2; i++)
                        {
                            // ensure it's not the same tile or off the map
                            if (i != 0 && j != 0 && i >= 0 && j >= 0 &&
                                i < DUNGEON_WIDTH && j < DUNGEON_HEIGHT
                            )
                            {
                                if (!tiles[i, j].open)
                                {
                                    tiles[i, j].region = nextRegion;
                                    walls.Add(new Point(i, j));
                                }
                            }
                        }
                    }
                }

                walls.Remove(wall);
            }
        }

        /// <summary>
        /// Place perfect mazes in all remaining unopen regions in the map.
        /// This gets called recursively after each successful maze fill to rescan
        /// dungeon for the next open maze to write. 
        /// </summary>
        /// <returns>False if it cannot find any new regions to generate mazes within</returns>
        protected bool GenerateMazes()
        {
            bool found = false;

            for (int y = 0; y < DUNGEON_HEIGHT && !found; y++)
            {
                for (int x = 0; x < DUNGEON_WIDTH && !found; x++)
                {
                    if (!tiles[x, y].open && !HasOpenNeighbor(x, y))
                    {
                        FloodFillMaze(x, y);
                        found = true;
                    }
                }
            }

            // If we can't find anywhere else to make a maze,
            // we're done. Break recursion.
            if (!found)
            {
                return false;
            }

            // Try searching for somewhere else to make a maze
            return GenerateMazes();
        }

        public void Generate()
        {
            CreateEmptyWorld();
            // GenerateRooms();
            FloodFillMaze(1, 1);
        }

        private void CreateEmptyWorld()
        {
            tiles = new Tile[DUNGEON_WIDTH, DUNGEON_HEIGHT];
        }

        public void Print()
        {
            for (int y = 0; y < DUNGEON_HEIGHT; y++)
            {
                for (int x = 0; x < DUNGEON_WIDTH; x++)
                {
                    Console.Write(tiles[x, y].Ascii());
                }
                Console.Write("\n");
            }
        }
    }
}
