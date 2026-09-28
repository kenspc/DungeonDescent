# Architecture overview

The code is the source of truth for structure and dependencies; this document records what the code does not say.

## Stack

One app at the repository root: C# on .NET 8, with the SDK pinned by `global.json` to 8.0.125 and rolling forward within the latest feature band; SadConsole 10.9 with its MonoGame host; MonoGame DesktopGL 3.8.4.1, which brings SDL2 and OpenAL native libraries into the build output. The glyphs come from the Px437 "FM Towns re." pixel font, a vendored asset under CC BY-SA 4.0.

## Components and boundaries

- **`Program.cs`** — parses the command-line flags, loads the font, and starts SadConsole with `RootScreen`. `--probe-seed` and `--help` exit before any window is created.
- **`Game`** — the central authority: it holds the map, the player, the monsters, the items, the floor number, and the game status, and all turn logic lives in it.
- **`RootScreen`** — owns the keyboard and the four game surfaces (title, map, status, log), swaps a full-window overlay in and out, and promotes to the game-over or victory screen when `Game.Status` changes.
- **`SadConsoleRenderer`** — purely presentational: it reads game state and writes to surfaces, and never mutates the game.
- **`SadConsoleKeyAdapter`** — translates SadConsole key presses into `ConsoleKeyInfo`, so `Game.HandleKey` keeps the signature it had when the game ran in a terminal.
- **`Map`** — floor generation, BFS pathfinding, and field of view.

The boundary: the game layer (`Game`, `Map`, entities, items) has no dependency on SadConsole; it uses `SadRogue.Primitives` only for the `Color` type its entities and items carry. Everything that draws or reads input lives under `src/UI/`.

## Data

All state lives in memory for one run, owned by `Game`. There is no save file and no database. A floor is regenerated, with fresh monsters and items, each time the player arrives on it; the player's stats, inventory, and gold carry across floors.

## External integrations

None: the game uses no network or outside service. The font is a vendored asset; its source, license, and regeneration steps are in `assets/fonts/README.md`.

## Key decisions

- **SadConsole on MonoGame for rendering**, over staying in the terminal and over bare MonoGame or Raylib-cs. The terminal gave too little learning payoff and no control over the font across platforms; bare MonoGame was about five times the work, and its first job — drawing a grid of character cells — is what SadConsole already does. Source: `docs/briefs/pixel-ui-rewrite.md`.
- **The Brogue visual anchor, with tileset rendering permanently excluded.** It keeps the turn-based grid genre intact; a tileset or sprite world would rewrite the turn loop. Source: `docs/briefs/pixel-ui-rewrite.md`, `docs/briefs/palette-brogue-pass.md`.
- **A 20-slot semantic palette sampled from Brogue 1.7.5**, where a color marks a family and the glyph tells its members apart. Source: `src/Core/Palette.cs`, `docs/briefs/palette-brogue-pass.md`.
- **The Px437 "FM Towns re." font**, chosen after a four-candidate audit. Its CC BY-SA 4.0 license was accepted because the project does not distribute; distributing would require attribution. Source: `assets/fonts/README.md`, `docs/screenshots/font-pass/decision.md`.
- **Visual passes leave the mechanics untouched.** The UI rewrite, the palette pass, and the font pass each excluded changes to `Game`, `Map`, entities, and items, which is why input is adapted to `ConsoleKeyInfo` rather than rewritten. Source: the briefs in `docs/briefs/`, `docs/plans/pixel-ui-rewrite.md`.
- **A fresh random seed per floor, and a deterministic `Map(seed)` constructor** behind `--probe-seed`, so maps differ between sessions while generation can still be audited headless. Source: `README.md`.
