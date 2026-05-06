using SadConsole;
using SadRogue.Primitives;

namespace DungeonDescent;

// SadConsole-flavoured replacement for the legacy Renderer.cs. After
// Task 8 the main game screen is split into four surfaces (title, map,
// status, log) so each Render* method targets a single dedicated surface
// whose origin is (0, 0). After Task 13 entity Color fields are typed as
// SadRogue.Primitives.Color directly, so the legacy AnsiToColor bridge
// is gone.
static class SadConsoleRenderer
{
    public static void RenderTitle(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        string title = $" Dungeon Descent - Floor {game.Floor}/5 ";
        if (title.Length < Map.Width)
            title = title.PadRight(Map.Width);
        surface.Surface.Print(0, 0, title, Palette.UiTitle);
    }

    public static void RenderMap(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        var map    = game.Map;
        var player = game.Player;

        for (int y = 0; y < Map.Height; y++)
        {
            for (int x = 0; x < Map.Width; x++)
            {
                var tile = map[x, y];
                var p    = new Point(x, y);

                // Surface.Clear() already blanked every cell, so unexplored
                // tiles can simply be skipped.
                if (!tile.IsExplored) continue;

                // Player
                if (p == player.Position)
                {
                    surface.Surface.SetGlyph(x, y, player.Glyph, player.Color);
                    continue;
                }

                // Monster (only if visible)
                var monster = tile.IsVisible ? game.MonsterAt(p) : null;
                if (monster != null)
                {
                    surface.Surface.SetGlyph(x, y, monster.Glyph, monster.Color);
                    continue;
                }

                // Item (only if visible)
                var item = tile.IsVisible ? game.ItemAt(p) : null;
                if (item != null)
                {
                    surface.Surface.SetGlyph(x, y, item.Glyph, item.Color);
                    continue;
                }

                // Tile. Both switches enumerate every named TileType so that
                // adding a new enum value triggers CS8509 (missing case)
                // rather than silently rendering as a wall. CS8524 (warning
                // about hypothetical out-of-range int casts to TileType) is
                // suppressed locally because the only producer of TileType
                // values is Map generation, which never invents unnamed
                // values; we explicitly want to keep the `_ =>` arm absent
                // so the CS8509 trip-wire remains armed for future enum
                // growth.
#pragma warning disable CS8524
                if (!tile.IsVisible)
                {
                    // Remembered (out-of-FOV but explored) tile — keep the
                    // hue, drop the brightness via Palette.Dim() so memory
                    // tiles still convey type, not just "explored".
                    (Color color, char glyph) = tile.Type switch
                    {
                        TileType.Wall         => (Palette.Dim(Palette.WallStone),    '#'),
                        TileType.Floor        => (Palette.Dim(Palette.FloorBase),    '.'),
                        TileType.FloorMossy   => (Palette.Dim(Palette.FloorMossy),   ','),
                        TileType.FloorCracked => (Palette.Dim(Palette.FloorCracked), '\''),
                        TileType.StairsDown   => (Palette.Dim(Palette.UiAccent),     '>'),
                        TileType.StairsUp     => (Palette.Dim(Palette.UiAccent),     '<'),
                    };
                    surface.Surface.SetGlyph(x, y, glyph, color);
                }
                else
                {
                    (Color color, char glyph) = tile.Type switch
                    {
                        TileType.Wall         => (Palette.WallStone,    '#'),
                        TileType.Floor        => (Palette.FloorBase,    '.'),
                        TileType.FloorMossy   => (Palette.FloorMossy,   ','),
                        TileType.FloorCracked => (Palette.FloorCracked, '\''),
                        TileType.StairsDown   => (Palette.UiAccent,     '>'),
                        TileType.StairsUp     => (Palette.UiAccent,     '<'),
                    };
                    surface.Surface.SetGlyph(x, y, glyph, color);
                }
#pragma warning restore CS8524
            }
        }
    }

    public static void RenderStatus(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        var p = game.Player;

        // Row 0 — colored stat segments printed left-to-right. Single-space
        // separators and abbreviated Gold/Score labels keep the worst-case
        // line under the 60-column surface width even at maxed-out values.
        int col = 0;
        Color hpColor = p.Hp < p.MaxHp / 3 ? Palette.EffectPoison : Palette.EffectHealth;
        col = PrintSegment(surface, col, 0, $"HP:{p.Hp}/{p.MaxHp}", hpColor);
        col = PrintSegment(surface, col, 0, " ", Palette.UiText);
        col = PrintSegment(surface, col, 0, $"ATK:{p.Attack}", Palette.UiAccent);
        col = PrintSegment(surface, col, 0, " ", Palette.UiText);
        col = PrintSegment(surface, col, 0, $"DEF:{p.Defense}", Palette.UiAccent);
        col = PrintSegment(surface, col, 0, " ", Palette.UiText);
        col = PrintSegment(surface, col, 0, $"LV:{p.Level}", Palette.UiAccent);
        col = PrintSegment(surface, col, 0, " ", Palette.UiText);
        col = PrintSegment(surface, col, 0, $"EXP:{p.Exp}/{p.ExpNext}", Palette.UiAccent);
        col = PrintSegment(surface, col, 0, " ", Palette.UiText);
        col = PrintSegment(surface, col, 0, $"G:{p.Gold}", Palette.UiAccent);
        col = PrintSegment(surface, col, 0, " ", Palette.UiText);
        PrintSegment(surface, col, 0, $"Sc:{p.Score}", Palette.UiText);

        // Hint must fit the 60-column status surface; longer strings are
        // silently truncated by SadConsole's Print, hiding the trailing keys.
        surface.Surface.Print(0, 1,
            "[WASD] Move [>/<] Stairs [i] Inv [?] Help [.] Wait [q] Quit",
            Palette.UiDim);
    }

    public static void RenderLog(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        var lines = game.Log.Lines;
        for (int i = 0; i < MessageLog.Capacity; i++)
        {
            string text = i < lines.Count ? lines[i] : "";
            surface.Surface.Print(0, i, text.PadRight(Map.Width), Palette.UiText);
        }
    }

    // ── Overlay screens ───────────────────────────────────────────────────────

    public static void DrawInventory(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        surface.Surface.Print(0, 0, "=== INVENTORY ===", Palette.UiTitle);

        var p = game.Player;
        Color hpColor = p.Hp < p.MaxHp / 3 ? Palette.EffectPoison : Palette.EffectHealth;
        int col = 0;
        col = PrintSegment(surface, col, 1, "HP: ", Palette.UiText);
        col = PrintSegment(surface, col, 1, $"{p.Hp}/{p.MaxHp}", hpColor);
        PrintSegment(surface, col, 1, $"  Carry: {p.Inventory.Count}/{Player.MaxInventory}", Palette.UiText);

        var inv = game.Player.Inventory;
        int promptRow;
        if (inv.Count == 0)
        {
            surface.Surface.Print(0, 3, "(empty)", Palette.UiText);
            promptRow = 5;
        }
        else
        {
            for (int i = 0; i < inv.Count; i++)
            {
                var item = inv[i];
                int row = 3 + i;
                int c = 0;
                c = PrintSegment(surface, c, row, $"  [{i + 1}] ", Palette.UiText);
                c = PrintSegment(surface, c, row, item.Glyph.ToString(), item.Color);
                PrintSegment(surface, c, row, $" {item.Name}", Palette.UiText);
            }
            promptRow = 4 + inv.Count;
        }

        // Always show how to leave the screen, even if the inventory is
        // empty, so users are not stuck wondering how to escape.
        string prompt = inv.Count == 0
            ? "Press [Esc] to cancel."
            : "Enter number to use item, or [Esc] to cancel:";
        surface.Surface.Print(0, promptRow, prompt, Palette.UiText);
    }

    public static void DrawHelp(IScreenSurface surface)
    {
        surface.Surface.Clear();
        surface.Surface.Print(0, 0, "=== HELP ===", Palette.UiTitle);
        surface.Surface.Print(0, 2, "  Movement  : WASD or Arrow Keys", Palette.UiText);
        surface.Surface.Print(0, 3, "  Descend   : > (stand on >)",     Palette.UiText);
        surface.Surface.Print(0, 4, "  Ascend    : < (stand on <)",     Palette.UiText);
        surface.Surface.Print(0, 5, "  Wait      : . (pass turn)",      Palette.UiText);
        surface.Surface.Print(0, 6, "  Inventory : i",                  Palette.UiText);
        surface.Surface.Print(0, 7, "  Help      : ?",                  Palette.UiText);
        surface.Surface.Print(0, 8, "  Quit      : q",                  Palette.UiText);
        surface.Surface.Print(0, 10, "  Map symbols:",                  Palette.UiText);
        surface.Surface.Print(0, 11, "    @ = You        # = Wall       . = Floor",         Palette.UiText);
        surface.Surface.Print(0, 12, "    > = Stairs dn  < = Stairs up",                    Palette.UiText);
        surface.Surface.Print(0, 13, "    r = Rat        g = Goblin     T = Troll   D = Dragon", Palette.UiText);
        surface.Surface.Print(0, 14, "    ! = Potion     + = Sword      [ = Armor   $ = Gold",  Palette.UiText);
        surface.Surface.Print(0, 16, "  Press any key to return...", Palette.UiDim);
    }

    public static void DrawGameOver(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        // Banner is 30 cols wide; center it inside the 60-col window so
        // the right side is not visually empty.
        const string banner = "==============================";
        const string title  = "          YOU DIED            ";
        int col = (Layout.WindowWidth - banner.Length) / 2;
        surface.Surface.Print(col, 1, banner, Palette.EffectPoison);
        surface.Surface.Print(col, 2, title,  Palette.EffectPoison);
        surface.Surface.Print(col, 3, banner, Palette.EffectPoison);
        surface.Surface.Print(col, 5, $"Floor reached : {game.Floor}",        Palette.UiText);
        surface.Surface.Print(col, 6, $"Level         : {game.Player.Level}", Palette.UiText);
        surface.Surface.Print(col, 7, $"Gold          : {game.Player.Gold}",  Palette.UiText);
        surface.Surface.Print(col, 8, $"Final Score   : {game.Player.Score}", Palette.UiText);
        surface.Surface.Print(col, 10, "Press any key to exit...", Palette.UiDim);
    }

    public static void DrawVictory(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        const string banner = "==============================";
        const string title  = "    YOU ESCAPED THE DUNGEON!  ";
        int col = (Layout.WindowWidth - banner.Length) / 2;
        surface.Surface.Print(col, 1, banner, Palette.UiTitle);
        surface.Surface.Print(col, 2, title,  Palette.UiTitle);
        surface.Surface.Print(col, 3, banner, Palette.UiTitle);
        surface.Surface.Print(col, 5, $"Level         : {game.Player.Level}", Palette.UiText);
        surface.Surface.Print(col, 6, $"Gold          : {game.Player.Gold}",  Palette.UiText);
        surface.Surface.Print(col, 7, $"Final Score   : {game.Player.Score}", Palette.UiText);
        surface.Surface.Print(col, 9, "Press any key to exit...", Palette.UiDim);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int PrintSegment(IScreenSurface surface, int col, int row, string text, Color color)
    {
        surface.Surface.Print(col, row, text, color);
        return col + text.Length;
    }
}
