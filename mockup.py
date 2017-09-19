
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
                if nx >= 0 and ny >= 0 and nx < w and ny < h and cells[ny][nx] == 0:
                    cells[y][x] = 1
                    cells[ny][nx] = 1
                    touched.append([nx, ny])
                    index = None

        if index != None:
            del touched[index]


def algo_2(w, h, cells):
    """Tree growing variation of the maze algo"""

    frontier = []

    def is_isolated(x, y):
        """check if (x, y) has walls in all 8 cardinal directions"""
        return (
            cells[y][x-1] == 0 and cells[y][x+1] == 0 and
            cells[y-1][x] == 0 and cells[y+1][x] == 0 and
            cells[y-1][x-1] == 0 and cells[y+1][x+1] == 0 and
            cells[y-1][x+1] == 0 and cells[y+1][x-1] == 0
        )

    def find_start():
        for y in range(1, h-1):
            for x in range(1, w-1):
                if is_isolated(x, y):
                    return [x, y]

        return None

    def carve(position):
        x, y = position
        cells[y][x] = 1 # Floor

    def can_carve(position, direction):
        """test if (x, y) can be opened"""
        # Ensure it stays in bounds
        x, y = vadd(position, vmult(direction, 3))
        if x < 0 or y < 0 or x >= w or y >= h:
            return False

        x, y = vadd(position, vmult(direction, 2))
        return cells[y][x] == 0 # wall

    # start = [random_odd(1, 10), random_odd(1, 10)]
    start = find_start()

    print('Start', start)

    # Can't carve anywhere else
    if start == None:
        return

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


def build_rooms(w, h, cells):

    def has_open(x, y, w, h):
        for i in range(y, y + h):
            for j in range(x, x + w):
                if cells[i][j] == 1:
                    return True

        return False

    def carve_room(x, y, w, h):
        print('Room', x, y, w, h)
        for i in range(y, y + h):
            for j in range(x, x + w):
                cells[i][j] = 1

    iterations = 0
    rooms = 0
    while iterations < 200 and rooms < 10:
        room_w = random_odd(3, 9)
        room_h = random_odd(3, 9)
        x = random_odd(0, w - room_w)
        y = random_odd(0, h - room_h)

        if not has_open(x, y, room_w, room_h):
            carve_room(x, y, room_w, room_h)
            rooms += 1

        iterations += 1

def generate():
    w = 41
    h = 21
    cells = [[0 for x in range(w)] for y in range(h)]

    # Build giant hole
    # for y in range(11, h-1):
    #     for x in range(11, w-1):
    #         cells[y][x] = 1

    # Fixed seeding, for testing algos overlayed
    # This makes a good partition (with < 10 rooms)
    # that prevents a single maze algo from flooding the entire
    # rest of the region (so I can test recursion on that)
    random.seed(12)

    # Build some random rooms
    build_rooms(w, h, cells)

    # Run maze flood fill in all open spaces
    algo_2(w, h, cells)



    # Printer
    for y in range(len(cells)):
        for x in range(len(cells[y])):
            if cells[y][x] == 0:
                print('#', end='')
            else: # Open
                print(' ', end='')
        print('')

generate()

