# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build          # compile
dotnet run            # build + run (opens a GUI window)
dotnet run --no-build # run without rebuilding
dotnet watch run      # rebuild + restart on file save (for development)
```

No test project exists. Verification is done by building and running manually.

**Runtime requirement:** the game opens a GUI window via SadConsole + MonoGame
DesktopGL (SDL2). It needs a display server: Windows native, WSL2 with WSLg
(Windows 11), or Linux/macOS with an active display. There is no longer a
text-mode fallback or terminal-size check.

## Architecture

**Dungeon Descent** is a turn-based ASCII-style roguelike rendered with
SadConsole 10.x on top of MonoGame DesktopGL. All code is in the single
`DungeonDescent` namespace (no sub-namespaces). All files use implicit
`using` — no `using DungeonDescent;` needed within the project.

### Turn flow

```
Program.cs → SadConsole.Game.Create / Run (MonoGame loop)
  → RootScreen.ProcessKeyboard()         — captures input each frame
  → SadConsoleKeyAdapter.ToConsoleKeyInfo — translates AsciiKey
  → Game.HandleKey()                      — resolves player action
  → Game.EndPlayerTurn()                  — monsters move/attack via BFS
  → RootScreen.Refresh()                  — redraw four sub-surfaces
  → RootScreen.Update() each frame        — auto-promote to GameOver /
                                            Victory when Game.Status changes
```

### Key design decisions

- **`Game.cs`** is the central authority: holds `Map`, `Player`, `List<Monster>`, `List<Item>`, `Floor`, and `Status`. All turn logic lives here. `EndPlayerTurn()` is `public` so input handlers can call it after inventory use.
- **`SadConsoleRenderer.cs`** is purely presentational — it reads game state and writes to a `IScreenSurface`, never mutates. The renderer exposes `RenderTitle / RenderMap / RenderStatus / RenderLog` plus `DrawInventory / DrawHelp / DrawGameOver / DrawVictory` for overlays. `Palette.cs` (in `src/Core/`) holds the 9-color foreground palette used everywhere.
- **`RootScreen.cs`** owns the four child surfaces (title 60×1, map 60×20, status 60×2, log 60×3) and the keyboard. `OpenOverlay` / `CloseOverlay` swap to a full-window 60×26 overlay (`InventoryScreen`, `HelpScreen`, `GameOverScreen`, `VictoryScreen`).
- **`Item`** on the map is always a `PositionedItem` (subclass of `Item` with a `Position` field). `Game.ItemAt(Point)` uses `OfType<PositionedItem>()` to find items. Inventory items are plain `Item` instances.
- **`Map`** generates rooms procedurally per floor using random placement + overlap rejection, connects them with L-shaped corridors, and places `StairsDown` at the last room's center. `NextFloor()` calls `PlaceUpStairs()` on the new map; `PrevFloor()` also calls `PlaceUpStairs()` so the player can keep ascending.
- **Monster AI:** each monster calls `Map.BfsNextStep()` toward the player every turn. Troll has `MoveInterval = 2` (moves every other turn).
- **FOV:** Manhattan-distance diamond, radius 8. `Map.UpdateFov()` is called after every player move.

### Adding a new monster

1. Add a `MonsterTemplate` entry in `src/Entities/MonsterTemplate.cs` (static field on `MonsterTemplates`). The `Color` argument is a `SadRogue.Primitives.Color` value, sourced from `Palette`.
2. Add it to the `templates` switch in `Game.SpawnEntities()` for the appropriate floors.
3. No other files need changing.

### Adding a new item type

1. Add a value to `ItemType` in `src/Items/ItemType.cs`.
2. Add a static factory method on `Item` in `src/Items/Item.cs` that returns a configured `Item` with an `Apply` lambda. The `Color` field is a `SadRogue.Primitives.Color` (use `Palette.X`).
3. Add the factory to the `itemFactories` array in `Game.SpawnEntities()`.
