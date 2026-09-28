# Design system

The project has a UI: a desktop GUI window, running on Windows, on WSL2 with WSLg (Windows 11), and on Linux or macOS with a display server. The whole interface is a grid of character cells drawn with SadConsole.

This document says how the design tokens are used and repeats no hex value. Tokens live in code: `src/Core/Palette.cs` (colors), `src/Core/Layout.cs` (the grid and its regions), `assets/fonts/px437-fmtowns-re/` (the font)

## Visual style

Brogue style, with Brogue 1.7.5 as the reference: character cells, one pixel font, and a tuned palette on a black background. Tileset and sprite rendering are permanently excluded.

## Color roles

A color marks a semantic family, not an individual; the glyph tells members of a family apart. Goblin and Troll share `EntityHumanoid`, and Sword and Armor share `ItemEquipment`.

- **Architecture** — `WallStone` for walls, `FloorBase` for floor, `FloorMossy` and `FloorCracked` for the decorative floor variants.
- **Entity** — `EntityPlayer` for the player (locked to white), `EntityHumanoid` for goblins and trolls, `EntityBeast` for rats, `EntityMagical` for the dragon.
- **Item** — `ItemConsumable` for potions, `ItemEquipment` for weapons and armor, `ItemTreasure` for gold; `ItemStaff` is reserved for a future staff or wand.
- **Effect** — `EffectHealth` for HP at or above a third of its maximum. `EffectPoison` also stands for danger until a dedicated danger slot exists: HP below a third, and the YOU DIED banner. `EffectFire` and `EffectIce` are reserved.
- **UiChrome** — `UiTitle` for title bars and banners, `UiText` for body text, `UiAccent` for stairs and stat labels, `UiDim` for hints such as "Press any key".
- **Remembered tiles** — a tile explored earlier but out of view takes its color through `Palette.Dim()`, and a remembered floor variant is drawn as plain floor.

The guardrails in the header of `Palette.cs` hold for every change: the floor variants stay within 30 per channel of `FloorBase`, and the critical pairs — `EntityPlayer` against every other entity, `EffectHealth` against `EffectPoison`, and each floor variant against every entity — stay at least 80 apart in RGB distance.

## Typography

One font at runtime: Px437 "FM Towns re.", a 16×16 glyph table covering codepoints 0x00–0xFF, drawn at 32×32 px per cell (`IFont.Sizes.Two`). There is no type scale: every cell is the same size, and emphasis comes from color alone. `--font <path>` swaps the font for a run.

## Components

No widget library is used. The screen is four surfaces drawn by `SadConsoleRenderer` — title, map, status, and log — and four overlays: `InventoryScreen`, `HelpScreen`, `GameOverScreen`, and `VictoryScreen`. Only the renderer draws; it reads game state and never changes it.

## Layout

A fixed grid of 60×26 cells. The title (1 row), map (20 rows), status (2 rows), and log (3 rows) are stacked in that order with no gaps; the heights in `Layout` must sum to the window height, because `RootScreen` positions each surface by adding up the heights above it.

## Elevation

None: one layer at a time. An overlay covers the whole window in place of the four game surfaces rather than floating above them.

## Do and don't

- Do take every color from a `Palette` slot; no RGB value is written outside `Palette.cs`.
- Do give a new creature or item its family's color and a glyph of its own; don't give it a color of its own to tell it apart.
- Do check a new color against the guardrails above before adding it.
- Don't add tileset or sprite rendering.

## Responsive behavior

None: the window is a fixed 60×26 cells of 32×32 px, sized in cells from `Layout` at startup, on every platform.
