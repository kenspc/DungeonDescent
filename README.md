# Dungeon Descent

A turn-based ASCII roguelike dungeon crawler built with .NET 8 Console. No third-party libraries — pure `System.Console` with ANSI color rendering.

```
 Dungeon Descent — Floor 3/5

        ############
        #..........#      #########
        #....@.....#──────#.......#
        #..........#      #...r...#
        ############      #.......#
                          ###.#####
                            #.#
                    #########.#########
                    #.................#
                    #......[..........#
                    #.................>
                    ###################

HP:72/100  ATK:11  DEF:4  LV:2  EXP:8/40  Gold:35  Score:89
[WASD/Arrows] Move  [>] Descend  [<] Ascend  [i] Inventory  [.] Wait  [q] Quit
Rat hits you for 4 damage! (72/100 HP)
You slay the Goblin! Level up! Now level 2. HP +10, ATK +1.
Picked up Leather Armor.
```

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Terminal: minimum **62 columns × 27 rows**
- ANSI color support (Windows Terminal, macOS Terminal, any Linux terminal)

## Running the Game

```bash
git clone <repo>
cd DungeonDescent
dotnet run
```

Or build and run separately:

```bash
dotnet build
dotnet run --no-build
```

## How to Play

### Objective

Descend 5 floors of a randomly generated dungeon, slay the **Dragon** boss on Floor 5, then climb back up and escape through the entrance on Floor 1 to win.

### Controls

| Key | Action |
|-----|--------|
| `W` / `↑` | Move up |
| `S` / `↓` | Move down |
| `A` / `←` | Move left |
| `D` / `→` | Move right |
| `>` | Descend stairs (stand on `>` first) |
| `<` | Ascend stairs (stand on `<` first) |
| `.` | Wait one turn |
| `i` | Open inventory |
| `?` | Help screen |
| `q` | Quit |

### Combat

The game is **turn-based**: every time you move, all monsters take one step.

- Walk into a monster to attack it
- Monsters adjacent to you will attack back on their turn
- **Damage formula:** `max(1, attacker.Attack − defender.Defense)`
- Killing monsters grants EXP; enough EXP levels you up

### Map Symbols

| Symbol | Meaning |
|--------|---------|
| `@` | You (yellow) |
| `#` | Wall |
| `.` | Floor |
| `>` | Stairs down (cyan) |
| `<` | Stairs up (cyan) |
| `r` | Rat (gray) |
| `g` | Goblin (green) |
| `T` | Troll (dark red) |
| `D` | Dragon — Boss (red) |
| `!` | Health Potion (magenta) |
| `+` | Iron Sword (cyan) |
| `[` | Leather Armor (blue) |
| `$` | Gold (yellow) |

### Fog of War

Only explored tiles are shown. Tiles in your current field of view (radius 8, diamond shape) are shown bright. Previously explored but currently out-of-sight tiles are shown dim.

## Game Systems

### Player Stats

| Stat | Starting Value | Description |
|------|---------------|-------------|
| HP | 100 | Current / max health |
| Attack | 8 | Base damage dealt |
| Defense | 2 | Damage reduction |
| Level | 1 | Increases via EXP |
| EXP needed | Level × 20 | EXP required for next level |

**Level-up bonus:** +10 Max HP, +10 current HP, +1 Attack.

### Monsters

| Symbol | Name | HP | ATK | DEF | EXP | Speed | Floors |
|--------|------|----|-----|-----|-----|-------|--------|
| `r` | Rat | 12 | 4 | 0 | 5 | Normal | 1–2 |
| `g` | Goblin | 25 | 7 | 1 | 12 | Normal | 2–4 |
| `T` | Troll | 60 | 10 | 3 | 30 | Slow (every 2 turns) | 3–4 |
| `D` | Dragon | 150 | 18 | 5 | 200 | Normal | 5 only (Boss) |

Monster count per floor: `3 + floor × 2` (e.g., Floor 1 = 5 monsters, Floor 5 = 13 + Dragon boss).

Monsters use **BFS pathfinding** — they always take the shortest walkable route toward you.

### Items

Items are **auto-picked up** when you walk onto them. Gold is collected immediately; other items go to your inventory (max 10 slots).

| Symbol | Item | Effect |
|--------|------|--------|
| `!` | Health Potion | Restore up to 30 HP |
| `+` | Iron Sword | +3 Attack (permanent) |
| `[` | Leather Armor | +2 Defense (permanent) |
| `$` | Gold (5–29) | Added to gold count and score |

**Using inventory items costs one turn** — monsters will respond.

### Scoring

| Source | Points |
|--------|--------|
| Killing a monster | Monster's EXP value |
| Collecting gold | Gold amount |
| Winning the game | `Gold × 2 + Level × 50` bonus |

### Floor Transitions

Each floor is **procedurally generated** with random rooms and corridors.

- `>` Stairs down — descend to the next floor (Floors 1→5)
- `<` Stairs up — ascend to the previous floor, or escape on Floor 1 (win condition)
- You keep all stats, inventory, and gold between floors
- Monsters and items are freshly spawned on each floor

## Strategy Tips

- **Floor 1:** Rats are weak. Farm EXP and collect items before descending.
- **Trolls are slow** — attack once, step back, let them waste their turn catching up.
- **Stock potions** before Floor 5. The Dragon deals 13 damage per hit at minimum.
- **Swords stack** — equipping multiple swords keeps adding +3 ATK each time.
- **You can go back up** if you're low on health; floors respawn fresh enemies and items.
- Watch your HP color: it turns **red** when below 1/3 of max.

## Project Structure

```
DungeonDescent/
├── Program.cs                 # Entry point, main game loop, inventory handler
├── DungeonDescent.csproj      # .NET 8 project file
├── docs/
│   └── plans/                 # Design and implementation documents
└── src/
    ├── Core/
    │   ├── Point.cs           # 2D coordinate value type (record struct)
    │   ├── Direction.cs       # Cardinal direction constants (Up/Down/Left/Right)
    │   └── GameColors.cs      # ANSI escape code color constants
    ├── Map/
    │   ├── TileType.cs        # Tile type enum (Wall, Floor, StairsDown, StairsUp)
    │   ├── Tile.cs            # Tile state (type, explored, visible)
    │   ├── Room.cs            # Room data with overlap/contains helpers
    │   └── Map.cs             # Dungeon generation, BFS pathfinding, FOV
    ├── Entities/
    │   ├── Entity.cs          # Abstract base: position, stats, TakeDamage()
    │   ├── Player.cs          # Player: leveling, inventory, EXP system
    │   ├── Monster.cs         # Monster: turn counter, speed interval, factory
    │   └── MonsterTemplate.cs # Immutable monster stat definitions (Rat/Goblin/Troll/Dragon)
    ├── Items/
    │   ├── ItemType.cs        # Item type enum (Potion, Sword, Armor, Gold)
    │   └── Item.cs            # Item factory methods + PositionedItem (world placement)
    ├── UI/
    │   ├── MessageLog.cs      # Scrolling 3-line message queue
    │   └── Renderer.cs        # All Console drawing (map, HUD, overlays)
    └── Game.cs                # Game state, turn logic, floor management, spawning
```

## Architecture

The game follows a simple layered design:

```
Program.cs  ──▶  Game.cs  ──▶  Map / Entities / Items
               (turn logic)
                    │
                    ▼
             Renderer.cs
           (draw after each turn)
```

**Turn flow:**
1. `Renderer.DrawAll()` — redraw screen
2. `Console.ReadKey()` — wait for player input
3. `Game.HandleKey()` — resolve player action (move, attack, use stairs)
4. `Game.EndPlayerTurn()` — all monsters take one step (BFS toward player)
5. Check win/lose condition → repeat or show end screen

**No third-party libraries.** Rendering uses raw ANSI escape sequences via `Console.Write`. Input is captured with `Console.ReadKey(intercept: true)` so keypresses are not echoed.

## Technical Notes

- **Map generation:** Random room placement with overlap rejection (60 attempts per floor), rooms connected with L-shaped corridors via horizontal-then-vertical carving.
- **Pathfinding:** BFS on walkable tiles, returns the first step toward the target. Runs per-monster per-turn; acceptable for maps of 60×20.
- **FOV:** Manhattan-distance diamond with radius 8. All tiles within range are marked visible and explored.
- **Randomness:** Each floor uses a fresh `Random` seed so maps are non-repeatable across sessions.
