
# Maze generator written in python so I can actually iterate over it faster
# (man, I hate compiled languages)
# requires Python 3ish

import random

def random_odd(low, high):
    return 1 + 2 * int(random.randint(low - 1, high - 1) / 2)

def vmult(v, scalar):
    """lazy vector multiplication"""
    return [x * scalar for x in v]

def vadd(v1, v2):
    """lazy vector addition"""
    return [v1[x] + v2[x] for x in range(len(v1))]

CARDINALS = [
    [0, -1], # N
    [1, 0], # E
    [0, 1], # S
    [-1, 0] # W
]

current_region = 0

class Cell:
    wall = True
    door = False
    region = 0

    # note that this should only ever be 2 region IDs
    adjacent_regions = []

    # debugging
    removed_connector = False

    def __init__(self, x, y):
        self.x = x
        self.y = y

    def ascii(self):
        if self.is_connector():
            return '?' #str(len(self.adjacent_regions))

        if self.removed_connector:
            return '.'

        if self.door:
            return 'X'

        if self.wall:
            return ' '

        return format(self.region, 'x') # print region
        return ' '

    def same_region(self, cell):
        """whether this cell is the same region as the other"""
        # Cells with no region are "same region" as any others
        # (unvisited walls)
        return (
            self.region == cell.region or
            cell.region == None or
            self.region == None
        )

    def is_connector(self):
        """true if this is a wall between cells of more than one region"""
        return self.wall and len(self.adjacent_regions) > 1

    def only_connects_within_regions(self, regions):
        for x in self.adjacent_regions:
            if x not in regions:
                return False

        return True

    def connects_external_to_regions(self, regions):
        """true if at least one item from regions is in the list,
            but it doesn't only connect within the list"""
        found_internal = False
        found_external = False

        # This is probably stupid, but I'm tired. Fix when porting to C#
        for x in self.adjacent_regions:
            if x in regions:
                found_internal = True
            else:
                found_external = True

        return found_internal and found_external


def algo_1(w, h, cells):
    touched = []

    # Select start cell and add to the list
    touched.append([5, 5])

    # Loop until list is empty
    while len(touched):
        # Select cell to use at random
        index = random.randint(0, len(touched) - 1)
        x, y = touched[index]

        # Iterate over random directions and find unvisited neighbor
        # If no unvisited neighbor, delete from the list and move on
        random.shuffle(CARDINALS)

        for vec in CARDINALS:
            if index != None:
                nx, ny = vadd([x, y], vmult(vec, 2))
                print(nx, ny)
                if nx >= 0 and ny >= 0 and nx < w and ny < h and cells[ny][nx].wall:
                    cells[y][x].wall = False
                    cells[ny][nx].wall = False
                    touched.append([nx, ny])
                    index = None

        if index != None:
            del touched[index]


def algo_2(w, h, cells):
    """Tree growing variation of the maze algo"""
    global current_region

    frontier = []

    def is_isolated(x, y):
        """check if (x, y) has walls in all 8 cardinal directions"""
        return (
            cells[y][x-1].wall and cells[y][x+1].wall and
            cells[y-1][x].wall and cells[y+1][x].wall and
            cells[y-1][x-1].wall and cells[y+1][x+1].wall and
            cells[y-1][x+1].wall and cells[y+1][x-1].wall
        )

    def find_start():
        for y in range(1, h-1):
            for x in range(1, w-1):
                if is_isolated(x, y):
                    return [x, y]

        return None

    def carve(position):
        x, y = position
        cells[y][x].wall = False # Floor
        cells[y][x].region = current_region

    def can_carve(position, direction):
        """test if (x, y) can be opened"""
        # Ensure it stays in bounds
        x, y = vadd(position, vmult(direction, 3))
        if x < 0 or y < 0 or x >= w or y >= h:
            return False

        x, y = vadd(position, vmult(direction, 2))
        return cells[y][x].wall # wall

    # start = [random_odd(1, 10), random_odd(1, 10)]
    start = find_start()

    print('Start', start)

    # Can't carve anywhere else
    if start == None:
        return

    # Mark this flood fill as a new region
    current_region += 1

    carve(start)
    frontier.append(start)

    while len(frontier):
        valid_directions = []

        pos = frontier[len(frontier)-1]

        # Get all directions we can go from this cell
        for direction in CARDINALS:
            if can_carve(pos, direction):
                valid_directions.append(direction)

        if len(valid_directions):
            # Pick one at random and carve in that direction
            d = random.choice(valid_directions)
            carve(vadd(pos, d))

            new_pos = vadd(pos, vmult(d, 2))
            carve(new_pos)

            frontier.append(new_pos)
        else:
            # No adjacent wall cells we can open, remove from list
            del frontier[len(frontier) - 1]

    # Recursively run ourselves until we run out of regions to flood fill
    algo_2(w, h, cells)


def build_rooms(w, h, cells, limit):
    global current_region

    def has_open(x, y, w, h):
        for i in range(y, y + h):
            for j in range(x, x + w):
                if not cells[i][j].wall:
                    return True

        return False

    def carve_room(x, y, w, h):
        print('Room', x, y, w, h)
        for i in range(y, y + h):
            for j in range(x, x + w):
                cells[i][j].wall = False
                cells[i][j].region = current_region

    iterations = 0
    rooms = 0
    while iterations < 200 and rooms < limit:
        room_w = random_odd(3, 9)
        room_h = random_odd(3, 9)
        x = random_odd(0, w - room_w)
        y = random_odd(0, h - room_h)

        if not has_open(x, y, room_w, room_h):
            # Give the room a new region ID
            current_region += 1

            # Cut out the room
            carve_room(x, y, room_w, room_h)
            rooms += 1

        iterations += 1


def calculate_adjacent_regions(w, h, cells):
    # Calculate and cache all connector cells
    for i in range(1, h - 2):
        for j in range(1, w - 2):
            # Aggregate all distinct regions around the cell
            # note that C# would probably use a HashSet for this
            # I know, this shit is ugly. I'm being lazy, shut up
            cells[i][j].adjacent_regions = list(set([
                cells[i][j-1].region,
                cells[i][j+1].region,
                cells[i-1][j].region,
                cells[i+1][j].region
            ]))

            # Axe "no region" from the list
            try:
                cells[i][j].adjacent_regions.remove(0)
            except:
                pass


def get_connectors(w, h, cells, region):
    """return list of (x,y) pairs for connectors to the region"""
    connectors = []

    for i in range(h):
        for j in range(w):
            if cells[i][j].is_connector() and region in cells[i][j].adjacent_regions:
                connectors.append([j, i])

    return connectors

def merge_regions(w, h, cells, regions):
    """Combine regions into region 1"""

    for i in range(h):
        for j in range(w):
            if cells[i][j].is_connector() and region in cells[i][j].adjacent_regions:
                connectors.append([j, i])


def connect_regions(w, h, cells):
    """create bridges between connected mazes and rooms"""

    """
        Algorithm is:
        pick random room to be the main region
        pick a random connector that touches the main region and open it
            unify the two regions across that connector
            remove all other connectors that are between the connected regions
            if there's connectors left, repeat at "pick random connector"

        Detailed implementation (spanning tree of regions, basically):
        Master region is 1 (the first room created)

        0. calculate all connectors for all cells
        1. C <- all connectors connected to region 1
        2. if C is empty, we're done.
        3. pick a random connector from C and turn into a door
        4. get the other region (non-1) that connector was connected to as N
        5. remove all connectors from C that connect [1, N]
            (for a non-perfect spanning tree, can add a chance
            of turning into a door)
        6. for all cells:
            if region is N, set region to 1
            if connector & connected regions contains N, replace with 1
                add connector to C
        7. loop from step 2

    """
    calculate_adjacent_regions(w, h, cells)
    connectors = get_connectors(w, h, cells, 1)

    # Regions we've joined, so we don't need to keep updating connectors
    joined_regions = []

    # while len(connectors) > 0:
    while len(connectors) > 0:
        # Pick a connector at random
        random.shuffle(connectors)
        x, y = connectors.pop(0)

        # Turn chosen connector into a door and
        # get adjacent region ID to merge.
        # Also remove from list
        cells[y][x].wall = False
        cells[y][x].door = True

        # print('Set Door', x, y, cells[y][x].region)

        # Track the other region merged
        joined_regions += cells[y][x].adjacent_regions
        # print('regions', joined_regions)

        # Remove all other connectors to the same region
        # new_connectors = []
        # for pos in connectors:
        #     x, y = pos
        #     if region_to_merge not in cells[y][x].adjacent_regions:
        #         new_connectors.append(pos)

        # connectors = new_connectors
        # print('new connectors', connectors)

        # Change cells in the other region to 1, and grab an
        # updated list of connectors from the joined region
        # for i in range(h):
        #     for j in range(w):
        #         if cells[i][j].region == region_to_merge:
        #             cells[i][j].region = 1

        #         if region_to_merge in cells[i][j].adjacent_regions and cells[i][j].is_connector():
        #             cells[i][j].adjacent_regions.remove(region_to_merge)
        #             cells[i][j].adjacent_regions.append(1)
        #             connectors.append([j, i])

        # go through all cells and see if they're in the region list,
        # update to region 1 if so. If they're a connector, see if they
        # connect to anything outside the region list from something inside
        # the region list. If so, add to connectors list. If they only connect
        # from within the current region, unflag as connector.
        connectors = []
        for i in range(h):
            for j in range(w):
                if cells[i][j].region in joined_regions:
                    cells[i][j].region = 1

                if cells[i][j].is_connector():
                    # If the connector is in the middle of the joined regions,
                    # make it no longer a connector
                    if cells[i][j].only_connects_within_regions(joined_regions):
                        # print('Remove connector', j, i, cells[i][j].region)
                        cells[i][j].adjacent_regions = []
                        cells[i][j].wall = True
                    elif cells[i][j].connects_external_to_regions(joined_regions):
                        connectors.append([j, i])

        print(len(connectors))

def join_regions(w, h, cells):
    pass


def uncarve(w, h, cells):
    """fill in dead ends. easy peasy"""
    pass


def generate():
    w = 41
    h = 21
    cells = [[Cell(x, y) for x in range(w)] for y in range(h)]

    # Build giant hole
    # for y in range(11, h-1):
    #     for x in range(11, w-1):
    #         cells[y][x] = 1

    # Fixed seeding, for testing algos overlayed
    # This makes a good partition (with 10 rooms)
    # that prevents a single maze algo from flooding the209 entire
    # rest of the region (so I can test recursion on that)
    random.seed(12)

    # Build some random rooms
    build_rooms(w, h, cells, 10)

    # Run maze flood fill in all open spaces
    algo_2(w, h, cells)

    # Connect regions
    connect_regions(w, h, cells)

    # Uncarve dead ends
    uncarve(w, h, cells)

    # Printer
    for y in range(len(cells)):
        for x in range(len(cells[y])):
            print(cells[y][x].ascii(), end='')

        print('')

generate()

