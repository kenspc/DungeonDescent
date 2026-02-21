# Dungeon Descent — Design Document

Date: 2026-02-21
Platform: .NET 8 Console

## Concept

ASCII Roguelike dungeon crawler. Player controls hero `@`, explores randomly
generated dungeons, fights monsters, collects items, descends 5 floors and
defeats the final Boss.

## Map / Levels

- Procedurally generated rooms + corridors (BSP or random room placement)
- 5 dungeon floors, increasing difficulty per floor
- `>` = stairs down, `<` = stairs up
- Fog of war: only explored tiles shown

## Player

- Stats: HP, MaxHP, Attack, Defense, Level, EXP
- Level-up on EXP threshold, gains +5 HP, +1 ATK or DEF (alternating)
- Inventory: up to 10 items

## Monsters

| Symbol | Name   | Notes                  |
|--------|--------|------------------------|
| `r`    | Rat    | Weak, appears in packs |
| `g`    | Goblin | Balanced               |
| `T`    | Troll  | High HP, slow          |
| `D`    | Dragon | Final Boss, Floor 5    |

## Items

| Symbol | Name   | Effect              |
|--------|--------|---------------------|
| `!`    | Potion | Restore 30 HP       |
| `+`    | Sword  | +3 Attack           |
| `[`    | Armor  | +2 Defense          |
| `$`    | Gold   | +10 score           |

## Combat

- Turn-based: player move = all monsters move one step
- Step onto monster tile = attack
- Damage = max(1, attacker.Attack - defender.Defense)
- Monsters use simple pathfinding (BFS toward player if in sight)

## UI Layout

```
┌─────────────── Dungeon Descent ──────────────┐
│  Map area (60×20)                             │
├──────────────────────────────────────────────┤
│ HP: 80/100  ATK: 12  DEF: 5  LV: 3  Floor: 2│
│ [i] Inventory  [?] Help  [q] Quit  WASD/Arrows│
├──────────────────────────────────────────────┤
│ Message log (last 3 lines)                    │
└──────────────────────────────────────────────┘
```

## Technical

- `System.Console` only (no third-party libraries)
- `Console.ReadKey(intercept: true)` for input
- ANSI color codes for visual distinction
- Project structure:
  - `Program.cs` — entry point, game loop
  - `Game.cs` — main game state, turn logic
  - `Map.cs` — dungeon generation, tile types
  - `Entity.cs` — base class for Player and Monster
  - `Player.cs` — player stats, inventory, input handling
  - `Monster.cs` — monster AI, pathfinding
  - `Item.cs` — item types and effects
  - `Renderer.cs` — all console drawing logic
  - `MessageLog.cs` — scrolling message log
