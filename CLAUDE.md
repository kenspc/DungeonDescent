# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build          # compile
dotnet run            # build + run (requires terminal ≥ 62×27)
dotnet run --no-build # run without rebuilding
```

No test project exists. Verification is done by building and running manually.

## Architecture

**Dungeon Descent** is a turn-based ASCII roguelike. All code is in the single `DungeonDescent` namespace with no third-party dependencies.

### Turn flow

```
Program.cs (main loop)
  → Renderer.DrawAll()      — redraw entire screen each turn
  → Console.ReadKey()       — block for input
  → Game.HandleKey()        — resolve player action
  → Game.EndPlayerTurn()    — all monsters move/attack via BFS
  → check Game.Status       — Playing / Won / Dead
```

### Key design decisions

- **`Game.cs`** is the central authority: holds `Map`, `Player`, `List<Monster>`, `List<Item>`, `Floor`, and `Status`. All turn logic lives here. `EndPlayerTurn()` is `public` so `Program.cs` can call it after inventory use.
- **`Renderer.cs`** is purely presentational — it reads game state and writes to console, never mutates.
- **`Item`** on the map is always a `PositionedItem` (subclass of `Item` with a `Position` field). `Game.ItemAt(Point)` uses `OfType<PositionedItem>()` to find items. Inventory items are plain `Item` instances.
- **`Map`** generates rooms procedurally per floor using random placement + overlap rejection, connects them with L-shaped corridors, and places `StairsDown` at the last room's center. `NextFloor()` calls `PlaceUpStairs()` on the new map; `PrevFloor()` also calls `PlaceUpStairs()` so the player can keep ascending.
- **Monster AI:** each monster calls `Map.BfsNextStep()` toward the player every turn. Troll has `MoveInterval = 2` (moves every other turn).
- **FOV:** Manhattan-distance diamond, radius 8. `Map.UpdateFov()` is called after every player move.

### Adding a new monster

1. Add a `MonsterTemplate` entry in `src/Entities/MonsterTemplate.cs` (static field on `MonsterTemplates`).
2. Add it to the `templates` switch in `Game.SpawnEntities()` for the appropriate floors.
3. No other files need changing.

### Adding a new item type

1. Add a value to `ItemType` in `src/Items/ItemType.cs`.
2. Add a static factory method on `Item` in `src/Items/Item.cs` that returns a configured `Item` with an `Apply` lambda.
3. Add the factory to the `itemFactories` array in `Game.SpawnEntities()`.
