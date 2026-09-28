# Product

## Purpose

Dungeon Descent exists for two reasons: to be a roguelike that looks the way its author wants — Brogue style, with character cells, a pixel font, and a tuned palette — and to be a vehicle for learning how games are rendered on .NET. Being shareable with others is a minor, secondary reason.

## Users

Mainly the author, who plays it and builds it to learn. Sharing it with others is a small part of the motivation, and no distribution has happened yet.

## Scope

- A five-floor dungeon; each floor is generated procedurally from rooms and L-shaped corridors, and regenerated on every visit.
- Turn-based combat: walking into a monster attacks it, and after every player turn each monster steps toward the player along a BFS path.
- Monsters (Rat, Goblin, Troll, and the Dragon boss on floor 5), items (potion, sword, armor, gold), and an inventory.
- Fog of war: a Manhattan-diamond field of view, with explored tiles remembered and drawn dimmed.
- The win condition: defeat the Dragon on floor 5, then climb back up and escape through floor 1.
- A desktop GUI window rendered with SadConsole on MonoGame, with the command-line options `--font`, `--probe-seed`, and `--help`.

## Non-goals

- Tileset or sprite rendering — permanently excluded; it conflicts with the Brogue-style visual anchor.
- Leaving the turn-based grid roguelike genre, for example for a sprite world in the style of Stardew Valley.

## Terms

- **Floor** — one dungeon level, numbered 1 to 5 from the top.
- **Brogue anchor** — the visual reference the project follows: Brogue 1.7.5's cell-based look, pixel font, and semantic palette.
- **Remembered tile** — a tile explored earlier but outside the current field of view; drawn dimmed.
- **Floor variant** — a mossy (`,`) or cracked (`'`) floor tile; decorative only, walkable and see-through like plain floor.
- **Overlay** — a full-window screen (inventory, help, game over, victory) shown in place of the four game surfaces.
- **Pass** — one scoped iteration on a single aspect of the game (the palette pass, the font pass), carried through a brief, a plan, and a task document.
