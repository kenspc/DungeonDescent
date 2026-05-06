namespace DungeonDescent;

class Map
{
    public const int Width  = 60;
    public const int Height = 20;

    private readonly Tile[,] _tiles = new Tile[Width, Height];
    private readonly Random  _rng;

    public List<Room> Rooms { get; } = [];

    public Tile this[int x, int y] => _tiles[x, y];
    public Tile this[Point p]      => _tiles[p.X, p.Y];

    public bool InBounds(Point p) =>
        p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

    public Map(int seed)
    {
        _rng = new Random(seed);
        Fill(TileType.Wall);
        GenerateRooms();
        ScatterFloorVariants();
        PlaceStairs();
    }

    private void Fill(TileType type)
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width;  x++)
            _tiles[x, y] = new Tile(type);
    }

    private void GenerateRooms()
    {
        int attempts = 60;
        while (attempts-- > 0)
        {
            int w = _rng.Next(5, 12);
            int h = _rng.Next(4, 8);
            int x = _rng.Next(1, Width  - w - 1);
            int y = _rng.Next(1, Height - h - 1);
            var room = new Room(x, y, w, h);

            if (Rooms.Any(r => r.Overlaps(room))) continue;

            CarveRoom(room);
            if (Rooms.Count > 0)
                CarveCorridors(Rooms[^1].Center, room.Center);
            Rooms.Add(room);
        }
    }

    private void CarveRoom(Room room)
    {
        for (int y = room.Y; y < room.Y + room.Height; y++)
        for (int x = room.X; x < room.X + room.Width;  x++)
            _tiles[x, y] = new Tile(TileType.Floor);
    }

    private void CarveCorridors(Point a, Point b)
    {
        // Horizontal then vertical
        int x = a.X, y = a.Y;
        int dx = Math.Sign(b.X - x);
        while (x != b.X) { if (_tiles[x, y].Type == TileType.Wall) _tiles[x, y] = new Tile(TileType.Floor); x += dx; }
        int dy = Math.Sign(b.Y - y);
        while (y != b.Y) { if (_tiles[x, y].Type == TileType.Wall) _tiles[x, y] = new Tile(TileType.Floor); y += dy; }
        _tiles[x, y] = new Tile(TileType.Floor);
    }

    // Sprinkle decorative floor variants ',' (mossy) and '\'' (cracked) over
    // already-carved Floor tiles. Rooms get a higher density (~5% combined,
    // 2.5% mossy + 2.5% cracked) than corridors (~1% combined). Density was
    // halved from the original 10% room rate after Task 6 manual review
    // showed the variants were reading as "stuff" rather than as texture
    // (F2 risk). Walls and stairs are skipped because the loop continues on
    // non-Floor cells. The shared _rng makes the layout reproducible from
    // the seed passed to Map(int).
    //
    // Thresholds are cumulative on a single roll: roll < Mossy gives mossy,
    // Mossy <= roll < Cracked gives cracked, otherwise plain floor. Tune
    // these constants only — do not duplicate magic numbers inline.
    private const double RoomMossyChance     = 0.025;
    private const double RoomCrackedChance   = 0.05;
    private const double CorridorMossyChance   = 0.005;
    private const double CorridorCrackedChance = 0.010;

    private void ScatterFloorVariants()
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width;  x++)
        {
            if (_tiles[x, y].Type != TileType.Floor) continue;
            var p = new Point(x, y);
            bool inRoom = Rooms.Any(r => r.Contains(p));
            double roll = _rng.NextDouble();
            if (inRoom)
            {
                if (roll < RoomMossyChance)        _tiles[x, y].Type = TileType.FloorMossy;
                else if (roll < RoomCrackedChance) _tiles[x, y].Type = TileType.FloorCracked;
            }
            else
            {
                if (roll < CorridorMossyChance)        _tiles[x, y].Type = TileType.FloorMossy;
                else if (roll < CorridorCrackedChance) _tiles[x, y].Type = TileType.FloorCracked;
            }
        }
    }

    private void PlaceStairs()
    {
        if (Rooms.Count < 2) return;
        var last = Rooms[^1].Center;
        _tiles[last.X, last.Y].Type = TileType.StairsDown;
    }

    public void PlaceUpStairs(Point p)
    {
        _tiles[p.X, p.Y].Type = TileType.StairsUp;
    }

    // Simple BFS pathfinding — returns next step toward target, or null
    public Point? BfsNextStep(Point from, Point to)
    {
        if (from == to) return null;
        var queue   = new Queue<Point>();
        var visited = new Dictionary<Point, Point>(); // child → parent
        queue.Enqueue(from);
        visited[from] = from;

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == to)
            {
                // Trace back to find first step
                var step = cur;
                while (visited[step] != from) step = visited[step];
                return step;
            }
            foreach (var dir in Direction.Cardinals)
            {
                var next = cur.Add(dir);
                if (!InBounds(next) || !_tiles[next.X, next.Y].IsWalkable || visited.ContainsKey(next)) continue;
                visited[next] = cur;
                queue.Enqueue(next);
            }
        }
        return null;
    }

    // Mark tiles visible within radius (simple circle)
    public void UpdateFov(Point origin, int radius = 8)
    {
        // Reset visibility
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width;  x++)
            _tiles[x, y].IsVisible = false;

        for (int y = origin.Y - radius; y <= origin.Y + radius; y++)
        for (int x = origin.X - radius; x <= origin.X + radius; x++)
        {
            var p = new Point(x, y);
            if (!InBounds(p)) continue;
            if (p.ManhattanDistance(origin) > radius) continue;
            _tiles[x, y].IsVisible  = true;
            _tiles[x, y].IsExplored = true;
        }
    }
}
