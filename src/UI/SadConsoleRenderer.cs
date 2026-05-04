using SadConsole;
using SadRogue.Primitives;

namespace DungeonDescent;

// SadConsole-flavoured replacement for the legacy Renderer.cs. All entity
// `Color` fields are still ANSI escape strings during M2-M4; AnsiToColor is
// the bridge until M5 retypes them to SadRogue.Primitives.Color.
static class SadConsoleRenderer
{
    private const int MapOffsetX = 0;
    private const int MapOffsetY = 1; // row 0 = title bar

    public static void RenderAll(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        DrawTitle(game, surface);
        RenderMap(game, surface);
        DrawStatusBar(game, surface);
        DrawMessageLog(game, surface);
    }

    public static void RenderMap(Game game, IScreenSurface surface)
    {
        var map    = game.Map;
        var player = game.Player;

        for (int y = 0; y < Map.Height; y++)
        {
            for (int x = 0; x < Map.Width; x++)
            {
                var tile = map[x, y];
                var p    = new Point(x, y);
                int sx   = MapOffsetX + x;
                int sy   = MapOffsetY + y;

                if (!tile.IsExplored)
                {
                    surface.Surface.SetGlyph(sx, sy, ' ', Palette.White);
                    continue;
                }

                // Player
                if (p == player.Position)
                {
                    surface.Surface.SetGlyph(sx, sy, player.Glyph, AnsiToColor(player.Color));
                    continue;
                }

                // Monster (only if visible)
                var monster = tile.IsVisible ? game.MonsterAt(p) : null;
                if (monster != null)
                {
                    surface.Surface.SetGlyph(sx, sy, monster.Glyph, AnsiToColor(monster.Color));
                    continue;
                }

                // Item (only if visible)
                var item = tile.IsVisible ? game.ItemAt(p) : null;
                if (item != null)
                {
                    surface.Surface.SetGlyph(sx, sy, item.Glyph, AnsiToColor(item.Color));
                    continue;
                }

                // Tile
                if (!tile.IsVisible)
                {
                    (Color color, char glyph) = tile.Type switch
                    {
                        TileType.Floor      => (Palette.Gray, '.'),
                        TileType.StairsDown => (Palette.Gray, '>'),
                        TileType.StairsUp   => (Palette.Gray, '<'),
                        _                   => (Palette.Gray, '#'),
                    };
                    surface.Surface.SetGlyph(sx, sy, glyph, color);
                }
                else
                {
                    (Color color, char glyph) = tile.Type switch
                    {
                        TileType.Floor      => (Palette.White, '.'),
                        TileType.StairsDown => (Palette.Cyan,  '>'),
                        TileType.StairsUp   => (Palette.Cyan,  '<'),
                        _                   => (Palette.White, '#'),
                    };
                    surface.Surface.SetGlyph(sx, sy, glyph, color);
                }
            }
        }
    }

    private static void DrawTitle(Game game, IScreenSurface surface)
    {
        string title = $" Dungeon Descent - Floor {game.Floor}/5 ";
        if (title.Length < Map.Width)
            title = title.PadRight(Map.Width);
        surface.Surface.Print(0, 0, title, Palette.Yellow);
    }

    private static void DrawStatusBar(Game game, IScreenSurface surface)
    {
        var p = game.Player;
        int row = MapOffsetY + Map.Height;

        // First line — colored stat segments printed left-to-right.
        int col = 0;
        Color hpColor = p.Hp < p.MaxHp / 3 ? Palette.Red : Palette.Green;
        col = PrintSegment(surface, col, row, $"HP:{p.Hp}/{p.MaxHp}", hpColor);
        col = PrintSegment(surface, col, row, "  ", Palette.White);
        col = PrintSegment(surface, col, row, $"ATK:{p.Attack}", Palette.Cyan);
        col = PrintSegment(surface, col, row, "  ", Palette.White);
        col = PrintSegment(surface, col, row, $"DEF:{p.Defense}", Palette.Blue);
        col = PrintSegment(surface, col, row, "  ", Palette.White);
        col = PrintSegment(surface, col, row, $"LV:{p.Level}", Palette.Magenta);
        col = PrintSegment(surface, col, row, "  ", Palette.White);
        col = PrintSegment(surface, col, row, $"EXP:{p.Exp}/{p.ExpNext}", Palette.Yellow);
        col = PrintSegment(surface, col, row, "  ", Palette.White);
        col = PrintSegment(surface, col, row, $"Gold:{p.Gold}", Palette.Yellow);
        col = PrintSegment(surface, col, row, "  ", Palette.White);
        PrintSegment(surface, col, row, $"Score:{p.Score}", Palette.White);

        surface.Surface.Print(0, row + 1,
            "[WASD/Arrows] Move  [>] Descend  [<] Ascend  [i] Inventory  [.] Wait  [q] Quit",
            Palette.Gray);
    }

    private static void DrawMessageLog(Game game, IScreenSurface surface)
    {
        int row = MapOffsetY + Map.Height + 2;
        var lines = game.Log.Lines;
        for (int i = 0; i < 3; i++)
        {
            string text = i < lines.Count ? lines[i] : "";
            surface.Surface.Print(0, row + i, text.PadRight(Map.Width), Palette.White);
        }
    }

    // ── Overlay screens ───────────────────────────────────────────────────────

    public static void DrawInventory(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        surface.Surface.Print(0, 0, "=== INVENTORY ===", Palette.Yellow);

        var p = game.Player;
        Color hpColor = p.Hp < p.MaxHp / 3 ? Palette.Red : Palette.Green;
        int col = 0;
        col = PrintSegment(surface, col, 1, "HP: ", Palette.White);
        col = PrintSegment(surface, col, 1, $"{p.Hp}/{p.MaxHp}", hpColor);
        PrintSegment(surface, col, 1, $"  Carry: {p.Inventory.Count}/{Player.MaxInventory}", Palette.White);

        var inv = game.Player.Inventory;
        if (inv.Count == 0)
        {
            surface.Surface.Print(0, 3, "(empty)", Palette.White);
        }
        else
        {
            for (int i = 0; i < inv.Count; i++)
            {
                var item = inv[i];
                int row = 3 + i;
                int c = 0;
                c = PrintSegment(surface, c, row, $"  [{i + 1}] ", Palette.White);
                c = PrintSegment(surface, c, row, item.Glyph.ToString(), AnsiToColor(item.Color));
                PrintSegment(surface, c, row, $" {item.Name}", Palette.White);
            }
            surface.Surface.Print(0, 4 + inv.Count,
                "Enter number to use item, or [Esc] to cancel:", Palette.White);
        }
    }

    public static void DrawHelp(IScreenSurface surface)
    {
        surface.Surface.Clear();
        surface.Surface.Print(0, 0, "=== HELP ===", Palette.Cyan);
        surface.Surface.Print(0, 2, "  Movement  : WASD or Arrow Keys", Palette.White);
        surface.Surface.Print(0, 3, "  Descend   : > (stand on >)",     Palette.White);
        surface.Surface.Print(0, 4, "  Ascend    : < (stand on <)",     Palette.White);
        surface.Surface.Print(0, 5, "  Wait      : . (pass turn)",      Palette.White);
        surface.Surface.Print(0, 6, "  Inventory : i",                  Palette.White);
        surface.Surface.Print(0, 7, "  Help      : ?",                  Palette.White);
        surface.Surface.Print(0, 8, "  Quit      : q",                  Palette.White);
        surface.Surface.Print(0, 10, "  Map symbols:",                  Palette.White);
        surface.Surface.Print(0, 11, "    @ = You        # = Wall       . = Floor",         Palette.White);
        surface.Surface.Print(0, 12, "    > = Stairs dn  < = Stairs up",                    Palette.White);
        surface.Surface.Print(0, 13, "    r = Rat        g = Goblin     T = Troll   D = Dragon", Palette.White);
        surface.Surface.Print(0, 14, "    ! = Potion     + = Sword      [ = Armor   $ = Gold",  Palette.White);
        surface.Surface.Print(0, 16, "  Press any key to return...", Palette.Gray);
    }

    public static void DrawGameOver(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        surface.Surface.Print(2, 1, "  ==============================", Palette.Red);
        surface.Surface.Print(2, 2, "           YOU DIED            ",   Palette.Red);
        surface.Surface.Print(2, 3, "  ==============================", Palette.Red);
        surface.Surface.Print(2, 5, $"  Floor reached : {game.Floor}",        Palette.White);
        surface.Surface.Print(2, 6, $"  Level         : {game.Player.Level}", Palette.White);
        surface.Surface.Print(2, 7, $"  Gold          : {game.Player.Gold}",  Palette.White);
        surface.Surface.Print(2, 8, $"  Final Score   : {game.Player.Score}", Palette.White);
        surface.Surface.Print(2, 10, "  Press any key to exit...", Palette.Gray);
    }

    public static void DrawVictory(Game game, IScreenSurface surface)
    {
        surface.Surface.Clear();
        surface.Surface.Print(2, 1, "  ==============================", Palette.Yellow);
        surface.Surface.Print(2, 2, "     YOU ESCAPED THE DUNGEON!  ",   Palette.Yellow);
        surface.Surface.Print(2, 3, "  ==============================", Palette.Yellow);
        surface.Surface.Print(2, 5, $"  Level         : {game.Player.Level}", Palette.White);
        surface.Surface.Print(2, 6, $"  Gold          : {game.Player.Gold}",  Palette.White);
        surface.Surface.Print(2, 7, $"  Final Score   : {game.Player.Score}", Palette.White);
        surface.Surface.Print(2, 9, "  Press any key to exit...", Palette.Gray);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int PrintSegment(IScreenSurface surface, int col, int row, string text, Color color)
    {
        surface.Surface.Print(col, row, text, color);
        return col + text.Length;
    }

    // Reverse maps the 9 foreground ANSI strings used by entities back to a
    // SadRogue.Primitives.Color. M5 will delete this once entity types switch
    // to Color directly.
    private static Color AnsiToColor(string ansi) => ansi switch
    {
        GameColors.White   => Palette.White,
        GameColors.Yellow  => Palette.Yellow,
        GameColors.Green   => Palette.Green,
        GameColors.Red     => Palette.Red,
        GameColors.Cyan    => Palette.Cyan,
        GameColors.Magenta => Palette.Magenta,
        GameColors.Blue    => Palette.Blue,
        GameColors.Gray    => Palette.Gray,
        GameColors.DarkRed => Palette.DarkRed,
        _                  => Palette.White,
    };
}
