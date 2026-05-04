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
        Map = new Map(_rng.Next());
        // Regenerate if map generation failed to produce rooms. The
        // attempt cap prevents an infinite loop in the unlikely case
        // that the seed space cannot produce a usable layout.
        const int MaxMapAttempts = 100;
        int attempts = 1;
        while (Map.Rooms.Count < 2 && attempts < MaxMapAttempts)
        {
            Map = new Map(_rng.Next());
            attempts++;
        }
        if (Map.Rooms.Count < 2)
            throw new InvalidOperationException(
                $"Failed to generate a map with at least two rooms after {MaxMapAttempts} attempts.");
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
            case '.': PassTurn();   break;
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
                Log.Add(pickupMsg);
                Items.Add(item);
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

    public void EndPlayerTurn()
    {
        foreach (var m in Monsters.Where(m => m.IsAlive).ToList())
        {
            m.TurnCounter++;
            if (m.TurnCounter % m.MoveInterval != 0) continue;

            var next = Map.BfsNextStep(m.Position, Player.Position);
            if (next == null) continue;

            if (next.Value == Player.Position)
                AttackPlayer(m);
            else if (MonsterAt(next.Value) == null)
                m.Position = next.Value;

            // Stop the round as soon as the player falls so remaining
            // monsters do not pile cosmetic "hits you for X damage" lines
            // onto a corpse.
            if (!Player.IsAlive) break;
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
                Log.Add("The Dragon is dead! You have conquered the dungeon!");
                Player.Score += Player.Gold * 2 + Player.Level * 50;
                Status = GameStatus.Won;
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
        Map.PlaceUpStairs(Map.Rooms[0].Center);  // Place StairsUp so player can keep ascending
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
            var bossPos = RandomFloorInRoom(bossRoom) ?? bossRoom.Center;
            Monsters.Add(Monster.From(MonsterTemplates.Dragon, bossPos));
            Log.Add("You sense a terrifying presence nearby...");
        }

        // Spawn items (skip room 0 = start room, consistent with monster spawning)
        var itemFactories = new Func<Item>[]
        {
            Item.Potion,
            Item.Sword,
            Item.Armor,
            () => Item.GoldPile(_rng.Next(5, 30))
        };
        for (int i = 0; i < itemCount; i++)
        {
            var room = Map.Rooms[_rng.Next(1, Map.Rooms.Count)];
            var pos  = RandomFloorInRoom(room);
            if (pos == null) continue;
            var item = itemFactories[_rng.Next(itemFactories.Length)]();
            Items.Add(new PositionedItem(item, pos.Value));
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
        Items.OfType<PositionedItem>().FirstOrDefault(i => i.Position == p);
}
