# Dungeon Descent Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a turn-based ASCII roguelike dungeon crawler in .NET 8 Console with 5 floors, monsters, items, and a final boss.

**Architecture:** Single Console app, no third-party libraries. Each concern lives in its own file. Game loop in `Program.cs` → `Game.cs` orchestrates turns → `Renderer.cs` draws every frame after each turn.

**Tech Stack:** .NET 8, System.Console (ANSI colors via escape codes), Console.ReadKey for input.

---

### Task 1: Project Skeleton

**Files:**
- Modify: `DungeonDescent.csproj`
- Modify: `Program.cs`
- Create: `src/Core/Point.cs`
- Create: `src/Core/Direction.cs`
- Create: `src/Core/GameColors.cs`

**Step 1: Update csproj to allow unsafe code and set root namespace**

Replace `DungeonDescent.csproj` content:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>DungeonDescent</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Step 2: Create `src/Core/Point.cs`**
```csharp
namespace DungeonDescent;

record struct Point(int X, int Y)
{
    public Point Add(Point other) => new(X + other.X, Y + other.Y);
    public int ManhattanDistance(Point other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    public bool IsAdjacentTo(Point other) => ManhattanDistance(other) == 1;
}
```

**Step 3: Create `src/Core/Direction.cs`**
```csharp
namespace DungeonDescent;

static class Direction
{
    public static readonly Point Up    = new(0, -1);
    public static readonly Point Down  = new(0,  1);
    public static readonly Point Left  = new(-1, 0);
    public static readonly Point Right = new(1,  0);

    public static readonly Point[] Cardinals = [Up, Down, Left, Right];
}
```

**Step 4: Create `src/Core/GameColors.cs`**
```csharp
namespace DungeonDescent;

static class GameColors
{
    public const string Reset   = "\x1b[0m";
    public const string Bold    = "\x1b[1m";

    // Foreground
    public const string White   = "\x1b[97m";
    public const string Yellow  = "\x1b[93m";
    public const string Green   = "\x1b[92m";
    public const string Red     = "\x1b[91m";
    public const string Cyan    = "\x1b[96m";
    public const string Magenta = "\x1b[95m";
    public const string Blue    = "\x1b[94m";
    public const string Gray    = "\x1b[90m";
    public const string DarkRed = "\x1b[31m";

    // Background
    public const string BgBlack = "\x1b[40m";
}
```

**Step 5: Stub out `Program.cs` to verify it compiles**
```csharp
using DungeonDescent;
Console.WriteLine("Dungeon Descent — skeleton OK");
```

**Step 6: Build and verify**
```bash
cd /home/kenspc/projects/DungeonDescent
dotnet build
```
Expected: Build succeeded, 0 errors.

---

### Task 2: Tile & Map Data

**Files:**
- Create: `src/Map/TileType.cs`
- Create: `src/Map/Tile.cs`
- Create: `src/Map/Room.cs`
- Create: `src/Map/Map.cs`

**Step 1: Create `src/Map/TileType.cs`**
```csharp
namespace DungeonDescent;

enum TileType { Wall, Floor, StairsDown, StairsUp }
```

**Step 2: Create `src/Map/Tile.cs`**
```csharp
namespace DungeonDescent;

class Tile
{
    public TileType Type { get; set; }
    public bool IsExplored { get; set; }
    public bool IsVisible   { get; set; }

    public bool IsWalkable => Type != TileType.Wall;

    public Tile(TileType type) => Type = type;
}
```

**Step 3: Create `src/Map/Room.cs`**
```csharp
namespace DungeonDescent;

record Room(int X, int Y, int Width, int Height)
{
    public Point Center => new(X + Width / 2, Y + Height / 2);

    public bool Overlaps(Room other) =>
        X <= other.X + other.Width  && X + Width  >= other.X &&
        Y <= other.Y + other.Height && Y + Height >= other.Y;

    public bool Contains(Point p) =>
        p.X >= X && p.X < X + Width &&
        p.Y >= Y && p.Y < Y + Height;
}
```

**Step 4: Create `src/Map/Map.cs`**
```csharp
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
```

**Step 5: Build and verify**
```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 3: Entity System

**Files:**
- Create: `src/Entities/Entity.cs`
- Create: `src/Entities/Player.cs`
- Create: `src/Entities/Monster.cs`
- Create: `src/Entities/MonsterTemplate.cs`

**Step 1: Create `src/Entities/Entity.cs`**
```csharp
namespace DungeonDescent;

abstract class Entity
{
    public Point Position { get; set; }
    public string Name    { get; init; } = "";
    public char   Glyph   { get; init; }
    public string Color   { get; init; } = GameColors.White;

    public int Hp    { get; set; }
    public int MaxHp { get; set; }
    public int Attack  { get; set; }
    public int Defense { get; set; }

    public bool IsAlive => Hp > 0;

    public int TakeDamage(int rawDamage)
    {
        int dmg = Math.Max(1, rawDamage - Defense);
        Hp = Math.Max(0, Hp - dmg);
        return dmg;
    }
}
```

**Step 2: Create `src/Entities/Player.cs`**
```csharp
namespace DungeonDescent;

class Player : Entity
{
    public int Level    { get; private set; } = 1;
    public int Exp      { get; private set; }
    public int ExpNext  => Level * 20;
    public int Gold     { get; set; }
    public int Score    { get; set; }

    public List<Item> Inventory { get; } = [];
    public const int MaxInventory = 10;

    public Player(Point start)
    {
        Name     = "Hero";
        Glyph    = '@';
        Color    = GameColors.Yellow;
        Position = start;
        MaxHp    = 100;
        Hp       = 100;
        Attack   = 8;
        Defense  = 2;
    }

    public string GainExp(int amount)
    {
        Exp += amount;
        if (Exp >= ExpNext)
        {
            Exp -= ExpNext;
            Level++;
            MaxHp   += 10;
            Hp       = Math.Min(Hp + 10, MaxHp);
            Attack  += 1;
            return $"Level up! Now level {Level}. HP +10, ATK +1.";
        }
        return "";
    }

    public bool TryPickup(Item item, out string msg)
    {
        if (Inventory.Count >= MaxInventory)
        {
            msg = "Inventory full!";
            return false;
        }
        Inventory.Add(item);
        msg = $"Picked up {item.Name}.";
        return true;
    }

    public string UseItem(int index)
    {
        if (index < 0 || index >= Inventory.Count) return "Invalid slot.";
        var item = Inventory[index];
        var result = item.Apply(this);
        Inventory.RemoveAt(index);
        return result;
    }
}
```

**Step 3: Create `src/Entities/MonsterTemplate.cs`**
```csharp
namespace DungeonDescent;

record MonsterTemplate(
    string Name,
    char   Glyph,
    string Color,
    int    Hp,
    int    Attack,
    int    Defense,
    int    ExpReward,
    int    MoveInterval  // moves every N player turns (1=normal, 2=slow)
);

static class MonsterTemplates
{
    public static readonly MonsterTemplate Rat = new(
        "Rat",    'r', GameColors.Gray,    12,  4, 0,  5, 1);
    public static readonly MonsterTemplate Goblin = new(
        "Goblin", 'g', GameColors.Green,   25,  7, 1, 12, 1);
    public static readonly MonsterTemplate Troll = new(
        "Troll",  'T', GameColors.DarkRed, 60, 10, 3, 30, 2);
    public static readonly MonsterTemplate Dragon = new(
        "Dragon", 'D', GameColors.Red,    150, 18, 5, 200, 1);
}
```

**Step 4: Create `src/Entities/Monster.cs`**
```csharp
namespace DungeonDescent;

class Monster : Entity
{
    public int ExpReward    { get; init; }
    public int MoveInterval { get; init; }
    public int TurnCounter  { get; set; }

    public static Monster From(MonsterTemplate t, Point pos) => new()
    {
        Name         = t.Name,
        Glyph        = t.Glyph,
        Color        = t.Color,
        MaxHp        = t.Hp,
        Hp           = t.Hp,
        Attack       = t.Attack,
        Defense      = t.Defense,
        ExpReward    = t.ExpReward,
        MoveInterval = t.MoveInterval,
        Position     = pos,
    };
}
```

**Step 5: Build and verify**
```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 4: Item System

**Files:**
- Create: `src/Items/ItemType.cs`
- Create: `src/Items/Item.cs`

**Step 1: Create `src/Items/ItemType.cs`**
```csharp
namespace DungeonDescent;

enum ItemType { Potion, Sword, Armor, Gold }
```

**Step 2: Create `src/Items/Item.cs`**
```csharp
namespace DungeonDescent;

class Item
{
    public string   Name     { get; init; } = "";
    public char     Glyph    { get; init; }
    public string   Color    { get; init; } = GameColors.White;
    public ItemType Type     { get; init; }

    public Func<Player, string> Apply { get; init; } = _ => "";

    public static Item Potion() => new()
    {
        Name  = "Health Potion",
        Glyph = '!',
        Color = GameColors.Magenta,
        Type  = ItemType.Potion,
        Apply = p =>
        {
            int healed = Math.Min(30, p.MaxHp - p.Hp);
            p.Hp += healed;
            return $"You drink the potion. Restored {healed} HP.";
        }
    };

    public static Item Sword() => new()
    {
        Name  = "Iron Sword",
        Glyph = '+',
        Color = GameColors.Cyan,
        Type  = ItemType.Sword,
        Apply = p =>
        {
            p.Attack += 3;
            return "You equip the sword. ATK +3.";
        }
    };

    public static Item Armor() => new()
    {
        Name  = "Leather Armor",
        Glyph = '[',
        Color = GameColors.Blue,
        Type  = ItemType.Armor,
        Apply = p =>
        {
            p.Defense += 2;
            return "You put on armor. DEF +2.";
        }
    };

    public static Item GoldPile(int amount) => new()
    {
        Name  = $"{amount} Gold",
        Glyph = '$',
        Color = GameColors.Yellow,
        Type  = ItemType.Gold,
        Apply = p =>
        {
            p.Gold  += amount;
            p.Score += amount;
            return $"You collect {amount} gold.";
        }
    };
}
```

**Step 3: Build and verify**
```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 5: Message Log

**Files:**
- Create: `src/UI/MessageLog.cs`

**Step 1: Create `src/UI/MessageLog.cs`**
```csharp
namespace DungeonDescent;

class MessageLog
{
    private readonly Queue<string> _messages = new();
    private const int Capacity = 3;

    public void Add(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        _messages.Enqueue(msg);
        while (_messages.Count > Capacity)
            _messages.Dequeue();
    }

    public IReadOnlyList<string> Lines => _messages.ToArray();
}
```

**Step 2: Build and verify**
```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 6: Game State

**Files:**
- Create: `src/Game.cs`

**Step 1: Create `src/Game.cs`**
```csharp
namespace DungeonDescent;

enum GameStatus { Playing, Won, Dead }

class Game
{
    public Map         Map     { get; private set; }
    public Player      Player  { get; }
    public MessageLog  Log     { get; } = new();
    public List<Monster> Monsters { get; private set; } = [];
    public List<Item>    Items    { get; private set; } = [];
    public int         Floor   { get; private set; } = 1;
    public GameStatus  Status  { get; private set; } = GameStatus.Playing;

    private readonly Random _rng = new();

    public Game()
    {
        Map    = new Map(_rng.Next());
        Player = new Player(Map.Rooms[0].Center);
        Map.UpdateFov(Player.Position);
        SpawnEntities();
        Log.Add("Welcome to Dungeon Descent! Use WASD or arrow keys to move.");
        Log.Add("Press [i] for inventory, [?] for help, [q] to quit.");
    }

    // ── Input handling ────────────────────────────────────────────────────────

    public void HandleKey(ConsoleKeyInfo key)
    {
        if (Status != GameStatus.Playing) return;

        Point? move = key.Key switch
        {
            ConsoleKey.W or ConsoleKey.UpArrow    => Direction.Up,
            ConsoleKey.S or ConsoleKey.DownArrow  => Direction.Down,
            ConsoleKey.A or ConsoleKey.LeftArrow  => Direction.Left,
            ConsoleKey.D or ConsoleKey.RightArrow => Direction.Right,
            _ => null
        };

        if (move.HasValue)
        {
            TryMovePlayer(move.Value);
            return;
        }

        switch (char.ToLower(key.KeyChar))
        {
            case '>': TryDescend(); break;
            case '<': TryAscend();  break;
            case '.': PassTurn();   break; // wait
        }
    }

    private void TryMovePlayer(Point dir)
    {
        var dest = Player.Position.Add(dir);
        if (!Map.InBounds(dest)) return;

        var monster = MonsterAt(dest);
        if (monster != null)
        {
            AttackMonster(Player, monster);
            EndPlayerTurn();
            return;
        }

        if (!Map[dest].IsWalkable) return;

        Player.Position = dest;
        Map.UpdateFov(Player.Position);

        // Auto-pickup items
        var item = ItemAt(dest);
        if (item != null)
        {
            Items.Remove(item);
            if (item.Type == ItemType.Gold)
            {
                Log.Add(item.Apply(Player));
            }
            else if (Player.TryPickup(item, out var pickupMsg))
            {
                Log.Add(pickupMsg);
            }
            else
            {
                Log.Add(pickupMsg); // "Inventory full"
                Items.Add(item);    // Put back
            }
        }

        EndPlayerTurn();
    }

    private void TryDescend()
    {
        if (Map[Player.Position].Type != TileType.StairsDown)
        {
            Log.Add("No stairs down here. Stand on '>' to descend.");
            return;
        }
        if (Floor >= 5)
        {
            Log.Add("There is no deeper level. Escape through the stairs up!");
            return;
        }
        NextFloor();
    }

    private void TryAscend()
    {
        if (Map[Player.Position].Type != TileType.StairsUp)
        {
            Log.Add("No stairs up here.");
            return;
        }
        if (Floor == 1)
        {
            Log.Add("You escape the dungeon!");
            Player.Score += Player.Gold * 2 + Player.Level * 50;
            Status = GameStatus.Won;
            return;
        }
        PrevFloor();
    }

    private void PassTurn() => EndPlayerTurn();

    // ── Turn logic ────────────────────────────────────────────────────────────

    private void EndPlayerTurn()
    {
        foreach (var m in Monsters.Where(m => m.IsAlive && Map[m.Position].IsVisible).ToList())
        {
            m.TurnCounter++;
            if (m.TurnCounter % m.MoveInterval != 0) continue;

            var next = Map.BfsNextStep(m.Position, Player.Position);
            if (next == null) continue;

            if (next == Player.Position)
                AttackPlayer(m);
            else if (MonsterAt(next) == null)
                m.Position = next;
        }

        Monsters.RemoveAll(m => !m.IsAlive);

        if (!Player.IsAlive)
            Status = GameStatus.Dead;
    }

    private void AttackMonster(Player attacker, Monster target)
    {
        int dmg = target.TakeDamage(attacker.Attack);
        Log.Add($"You hit {target.Name} for {dmg} damage. ({target.Hp}/{target.MaxHp} HP)");
        if (!target.IsAlive)
        {
            Log.Add($"You slay the {target.Name}!");
            var lvlMsg = attacker.GainExp(target.ExpReward);
            if (lvlMsg != "") Log.Add(lvlMsg);
            Player.Score += target.ExpReward;

            if (target.Name == "Dragon")
            {
                Log.Add("The Dragon is dead! Find the stairs up to escape!");
            }
        }
    }

    private void AttackPlayer(Monster attacker)
    {
        int dmg = Player.TakeDamage(attacker.Attack);
        Log.Add($"{attacker.Name} hits you for {dmg} damage! ({Player.Hp}/{Player.MaxHp} HP)");
    }

    // ── Inventory ─────────────────────────────────────────────────────────────

    public string UseInventoryItem(int index) => Player.UseItem(index);

    // ── Floor management ─────────────────────────────────────────────────────

    private void NextFloor()
    {
        Floor++;
        Log.Add($"You descend to floor {Floor}...");
        var prevEntrance = Map.Rooms[0].Center;
        Map = new Map(_rng.Next());
        Map.PlaceUpStairs(Map.Rooms[0].Center);
        Player.Position = Map.Rooms[0].Center;
        Map.UpdateFov(Player.Position);
        Monsters = [];
        Items    = [];
        SpawnEntities();
    }

    private void PrevFloor()
    {
        Floor--;
        Log.Add($"You ascend to floor {Floor}.");
        Map = new Map(_rng.Next());
        Player.Position = Map.Rooms[^1].Center;
        Map.UpdateFov(Player.Position);
        Monsters = [];
        Items    = [];
        SpawnEntities();
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private void SpawnEntities()
    {
        int monsterCount = 3 + Floor * 2;
        int itemCount    = 3 + Floor;

        var templates = Floor switch
        {
            1 => new[] { MonsterTemplates.Rat },
            2 => new[] { MonsterTemplates.Rat, MonsterTemplates.Goblin },
            3 => new[] { MonsterTemplates.Goblin, MonsterTemplates.Troll },
            4 => new[] { MonsterTemplates.Goblin, MonsterTemplates.Troll },
            _ => new[] { MonsterTemplates.Troll }
        };

        // Spawn monsters in rooms (skip room 0 = start room)
        for (int i = 0; i < monsterCount; i++)
        {
            var room = Map.Rooms[_rng.Next(1, Map.Rooms.Count)];
            var pos  = RandomFloorInRoom(room);
            if (pos == null) continue;
            var template = templates[_rng.Next(templates.Length)];
            Monsters.Add(Monster.From(template, pos.Value));
        }

        // Boss on floor 5
        if (Floor == 5)
        {
            var bossRoom = Map.Rooms[^1];
            Monsters.Add(Monster.From(MonsterTemplates.Dragon, bossRoom.Center));
            Log.Add("You sense a terrifying presence nearby...");
        }

        // Spawn items
        var itemFactories = new Func<Item>[]
        {
            Item.Potion,
            Item.Sword,
            Item.Armor,
            () => Item.GoldPile(_rng.Next(5, 30))
        };
        for (int i = 0; i < itemCount; i++)
        {
            var room = Map.Rooms[_rng.Next(Map.Rooms.Count)];
            var pos  = RandomFloorInRoom(room);
            if (pos == null) continue;
            Items.Add(itemFactories[_rng.Next(itemFactories.Length)]());
            Items[^1] = Items[^1] with { };  // no-op, position set below
            // Re-create with position via reflection workaround — just set field:
            var item = itemFactories[_rng.Next(itemFactories.Length)]();
            Items[^1] = new PositionedItem(item, pos.Value);
        }
    }

    private Point? RandomFloorInRoom(Room room)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var p = new Point(
                _rng.Next(room.X, room.X + room.Width),
                _rng.Next(room.Y, room.Y + room.Height));
            if (Map[p].IsWalkable && MonsterAt(p) == null && ItemAt(p) == null && p != Player.Position)
                return p;
        }
        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public Monster? MonsterAt(Point p) =>
        Monsters.FirstOrDefault(m => m.IsAlive && m.Position == p);

    public Item? ItemAt(Point p) =>
        Items.FirstOrDefault(i => i is PositionedItem pi && pi.Position == p);
}
```

> **Note:** The design uses `PositionedItem` to attach a position to items. See Task 7 for its definition.

**Step 2: Build - expect errors about PositionedItem. Proceed to Task 7.**

---

### Task 7: PositionedItem (fix item positions)

**Files:**
- Modify: `src/Items/Item.cs` — add PositionedItem class and fix spawn logic in Game.cs

**Step 1: Add `PositionedItem` to bottom of `src/Items/Item.cs`**
```csharp
// Append to Item.cs after the Item class:

class PositionedItem : Item
{
    public Point Position { get; }

    public PositionedItem(Item source, Point position) : base()
    {
        // Copy fields
        Name  = source.Name;
        Glyph = source.Glyph;
        Color = source.Color;
        Type  = source.Type;
        Apply = source.Apply;
        Position = position;
    }
}
```

**Step 2: Item class needs settable properties — change `init` to `set` in `src/Items/Item.cs`**

Change all `{ get; init; }` to `{ get; set; }` in Item.cs.

**Step 3: Fix SpawnEntities in `src/Game.cs` — simplify item spawning**

Replace the entire item spawning loop (the `for (int i = 0; i < itemCount; i++)` block):
```csharp
        var itemFactories = new Func<Item>[]
        {
            Item.Potion,
            Item.Sword,
            Item.Armor,
            () => Item.GoldPile(_rng.Next(5, 30))
        };
        for (int i = 0; i < itemCount; i++)
        {
            var room = Map.Rooms[_rng.Next(Map.Rooms.Count)];
            var pos  = RandomFloorInRoom(room);
            if (pos == null) continue;
            var item = itemFactories[_rng.Next(itemFactories.Length)]();
            Items.Add(new PositionedItem(item, pos.Value));
        }
```

**Step 4: Fix `ItemAt` in Game.cs**
```csharp
    public Item? ItemAt(Point p) =>
        Items.OfType<PositionedItem>().FirstOrDefault(i => i.Position == p);
```

**Step 5: Build and verify**
```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 8: Renderer

**Files:**
- Create: `src/UI/Renderer.cs`

**Step 1: Create `src/UI/Renderer.cs`**
```csharp
namespace DungeonDescent;

static class Renderer
{
    private const int MapOffsetX = 0;
    private const int MapOffsetY = 1; // row 0 = title bar

    public static void DrawAll(Game game)
    {
        Console.Clear();
        DrawTitle(game);
        DrawMap(game);
        DrawStatusBar(game);
        DrawMessageLog(game);
        Console.SetCursorPosition(0, MapOffsetY + Map.Height + 3);
    }

    private static void DrawTitle(Game game)
    {
        Console.SetCursorPosition(0, 0);
        string title = $" Dungeon Descent — Floor {game.Floor}/5 ";
        Console.Write($"{GameColors.Bold}{GameColors.Yellow}{title.PadRight(Map.Width)}{GameColors.Reset}");
    }

    private static void DrawMap(Game game)
    {
        var map    = game.Map;
        var player = game.Player;

        for (int y = 0; y < Map.Height; y++)
        {
            Console.SetCursorPosition(MapOffsetX, MapOffsetY + y);
            for (int x = 0; x < Map.Width; x++)
            {
                var tile = map[x, y];
                var p    = new Point(x, y);

                if (!tile.IsExplored)
                {
                    Console.Write(' ');
                    continue;
                }

                // Player
                if (p == player.Position)
                {
                    Console.Write($"{GameColors.Bold}{player.Color}{player.Glyph}{GameColors.Reset}");
                    continue;
                }

                // Monster (only if visible)
                var monster = tile.IsVisible ? game.MonsterAt(p) : null;
                if (monster != null)
                {
                    Console.Write($"{monster.Color}{monster.Glyph}{GameColors.Reset}");
                    continue;
                }

                // Item (only if visible)
                var item = tile.IsVisible ? game.ItemAt(p) : null;
                if (item != null)
                {
                    Console.Write($"{item.Color}{item.Glyph}{GameColors.Reset}");
                    continue;
                }

                // Tile
                if (!tile.IsVisible)
                {
                    // Explored but not currently visible → dim
                    (string color, char glyph) = tile.Type switch
                    {
                        TileType.Floor      => (GameColors.Gray, '.'),
                        TileType.StairsDown => (GameColors.Gray, '>'),
                        TileType.StairsUp   => (GameColors.Gray, '<'),
                        _                   => (GameColors.Gray, '#'),
                    };
                    Console.Write($"{color}{glyph}{GameColors.Reset}");
                }
                else
                {
                    (string color, char glyph) = tile.Type switch
                    {
                        TileType.Floor      => (GameColors.White, '.'),
                        TileType.StairsDown => (GameColors.Cyan,  '>'),
                        TileType.StairsUp   => (GameColors.Cyan,  '<'),
                        _                   => (GameColors.White, '#'),
                    };
                    Console.Write($"{color}{glyph}{GameColors.Reset}");
                }
            }
        }
    }

    private static void DrawStatusBar(Game game)
    {
        var p = game.Player;
        int row = MapOffsetY + Map.Height;

        Console.SetCursorPosition(0, row);
        string hpColor = p.Hp < p.MaxHp / 3 ? GameColors.Red : GameColors.Green;
        Console.Write(
            $"{hpColor}HP:{p.Hp}/{p.MaxHp}{GameColors.Reset}  " +
            $"{GameColors.Cyan}ATK:{p.Attack}{GameColors.Reset}  " +
            $"{GameColors.Blue}DEF:{p.Defense}{GameColors.Reset}  " +
            $"{GameColors.Magenta}LV:{p.Level}{GameColors.Reset}  " +
            $"{GameColors.Yellow}EXP:{p.Exp}/{p.ExpNext}{GameColors.Reset}  " +
            $"{GameColors.Yellow}Gold:{p.Gold}{GameColors.Reset}  " +
            $"Score:{p.Score}");

        Console.SetCursorPosition(0, row + 1);
        Console.Write($"{GameColors.Gray}[WASD/Arrows] Move  [>] Descend  [<] Ascend  [i] Inventory  [.] Wait  [q] Quit{GameColors.Reset}");
    }

    private static void DrawMessageLog(Game game)
    {
        int row = MapOffsetY + Map.Height + 2;
        var lines = game.Log.Lines;
        for (int i = 0; i < 3; i++)
        {
            Console.SetCursorPosition(0, row + i);
            string text = i < lines.Count ? lines[i] : "";
            Console.Write(text.PadRight(Map.Width));
        }
    }

    // ── Overlay screens ───────────────────────────────────────────────────────

    public static void DrawInventory(Game game)
    {
        Console.Clear();
        Console.WriteLine($"{GameColors.Bold}{GameColors.Yellow}=== INVENTORY ==={GameColors.Reset}");
        Console.WriteLine($"Carry: {game.Player.Inventory.Count}/{Player.MaxInventory}");
        Console.WriteLine();

        var inv = game.Player.Inventory;
        if (inv.Count == 0)
        {
            Console.WriteLine("(empty)");
        }
        else
        {
            for (int i = 0; i < inv.Count; i++)
            {
                var item = inv[i];
                Console.WriteLine($"  [{i + 1}] {item.Color}{item.Glyph}{GameColors.Reset} {item.Name}");
            }
            Console.WriteLine();
            Console.WriteLine("Enter number to use item, or [Esc] to cancel:");
        }
    }

    public static void DrawGameOver(Game game)
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine($"{GameColors.Bold}{GameColors.Red}  ══════════════════════════════{GameColors.Reset}");
        Console.WriteLine($"{GameColors.Bold}{GameColors.Red}           YOU DIED            {GameColors.Reset}");
        Console.WriteLine($"{GameColors.Bold}{GameColors.Red}  ══════════════════════════════{GameColors.Reset}");
        Console.WriteLine();
        Console.WriteLine($"  Floor reached : {game.Floor}");
        Console.WriteLine($"  Level         : {game.Player.Level}");
        Console.WriteLine($"  Gold          : {game.Player.Gold}");
        Console.WriteLine($"  Final Score   : {game.Player.Score}");
        Console.WriteLine();
        Console.WriteLine($"  {GameColors.Gray}Press any key to exit...{GameColors.Reset}");
    }

    public static void DrawVictory(Game game)
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine($"{GameColors.Bold}{GameColors.Yellow}  ══════════════════════════════{GameColors.Reset}");
        Console.WriteLine($"{GameColors.Bold}{GameColors.Yellow}     YOU ESCAPED THE DUNGEON!  {GameColors.Reset}");
        Console.WriteLine($"{GameColors.Bold}{GameColors.Yellow}  ══════════════════════════════{GameColors.Reset}");
        Console.WriteLine();
        Console.WriteLine($"  Level         : {game.Player.Level}");
        Console.WriteLine($"  Gold          : {game.Player.Gold}");
        Console.WriteLine($"  Final Score   : {game.Player.Score}");
        Console.WriteLine();
        Console.WriteLine($"  {GameColors.Gray}Press any key to exit...{GameColors.Reset}");
    }

    public static void DrawHelp()
    {
        Console.Clear();
        Console.WriteLine($"{GameColors.Bold}{GameColors.Cyan}=== HELP ==={GameColors.Reset}");
        Console.WriteLine();
        Console.WriteLine("  Movement  : WASD or Arrow Keys");
        Console.WriteLine("  Descend   : > (stand on >)");
        Console.WriteLine("  Ascend    : < (stand on <)");
        Console.WriteLine("  Wait      : . (pass turn)");
        Console.WriteLine("  Inventory : i");
        Console.WriteLine("  Help      : ?");
        Console.WriteLine("  Quit      : q");
        Console.WriteLine();
        Console.WriteLine("  Map symbols:");
        Console.WriteLine("    @ = You        # = Wall       . = Floor");
        Console.WriteLine("    > = Stairs dn  < = Stairs up");
        Console.WriteLine("    r = Rat        g = Goblin     T = Troll   D = Dragon");
        Console.WriteLine("    ! = Potion     + = Sword      [ = Armor   $ = Gold");
        Console.WriteLine();
        Console.WriteLine($"  {GameColors.Gray}Press any key to return...{GameColors.Reset}");
    }
}
```

**Step 2: Build and verify**
```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 9: Main Game Loop

**Files:**
- Modify: `Program.cs`

**Step 1: Replace `Program.cs` with full game loop**
```csharp
using DungeonDescent;

// Enable ANSI escape codes on Windows (no-op on Linux)
Console.OutputEncoding = System.Text.Encoding.UTF8;

var game = new Game();
bool running = true;

while (running && game.Status == GameStatus.Playing)
{
    Renderer.DrawAll(game);

    var key = Console.ReadKey(intercept: true);

    switch (char.ToLower(key.KeyChar))
    {
        case 'q':
            running = false;
            break;

        case 'i':
            HandleInventory(game);
            break;

        case '?':
            Renderer.DrawHelp();
            Console.ReadKey(intercept: true);
            break;

        default:
            game.HandleKey(key);
            break;
    }
}

// End screens
Console.CursorVisible = true;
if (game.Status == GameStatus.Dead)
{
    Renderer.DrawGameOver(game);
    Console.ReadKey(intercept: true);
}
else if (game.Status == GameStatus.Won)
{
    Renderer.DrawVictory(game);
    Console.ReadKey(intercept: true);
}

static void HandleInventory(Game game)
{
    while (true)
    {
        Renderer.DrawInventory(game);

        if (game.Player.Inventory.Count == 0)
        {
            Console.ReadKey(intercept: true);
            return;
        }

        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape) return;

        if (key.KeyChar >= '1' && key.KeyChar <= '9')
        {
            int index = key.KeyChar - '1';
            var msg = game.UseInventoryItem(index);
            game.Log.Add(msg);
            return;
        }
    }
}
```

**Step 2: Build and verify**
```bash
dotnet build
```
Expected: Build succeeded, 0 errors.

---

### Task 10: Run and Play-Test

**Step 1: Run the game**
```bash
cd /home/kenspc/projects/DungeonDescent
dotnet run
```
Expected: Game starts, shows dungeon map, title bar, status bar, and message log.

**Step 2: Verify checklist**
- [ ] Player `@` visible in first room
- [ ] WASD/arrows move the player
- [ ] Walking into a monster attacks it
- [ ] Message log updates each turn
- [ ] `>` on stairs descends floor
- [ ] `i` opens inventory
- [ ] Picking up items shows message
- [ ] Dying shows Game Over screen
- [ ] Winning shows Victory screen

**Step 3: Fix any issues found during play-test before marking complete.**

---

### Task 11: Polish — Console Setup

**Files:**
- Modify: `Program.cs`

**Step 1: Add console setup at top of Program.cs (before `var game = new Game();`)**
```csharp
Console.CursorVisible = false;
Console.Title = "Dungeon Descent";
// Ensure terminal is large enough
if (Console.WindowWidth < 62 || Console.WindowHeight < 27)
{
    Console.WriteLine("Please resize your terminal to at least 62×27 and restart.");
    return;
}
```

**Step 2: Final build and run**
```bash
dotnet run
```
Expected: Polished start, cursor hidden, title set.
