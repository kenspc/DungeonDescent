namespace DungeonDescent;

static class Renderer
{
    private const int MapOffsetX = 0;
    private const int MapOffsetY = 1; // row 0 = title bar

    public static void DrawAll(Game game)
    {
        Console.Clear();
        DrawTitle(game);
        DrawMap(game);
        DrawStatusBar(game);
        DrawMessageLog(game);
        Console.SetCursorPosition(0, MapOffsetY + Map.Height + 3);
    }

    private static void DrawTitle(Game game)
    {
        Console.SetCursorPosition(0, 0);
        string title = $" Dungeon Descent — Floor {game.Floor}/5 ";
        Console.Write($"{GameColors.Bold}{GameColors.Yellow}{title.PadRight(Map.Width)}{GameColors.Reset}");
    }

    private static void DrawMap(Game game)
    {
        var map    = game.Map;
        var player = game.Player;

        for (int y = 0; y < Map.Height; y++)
        {
            Console.SetCursorPosition(MapOffsetX, MapOffsetY + y);
            for (int x = 0; x < Map.Width; x++)
            {
                var tile = map[x, y];
                var p    = new Point(x, y);

                if (!tile.IsExplored)
                {
                    Console.Write(' ');
                    continue;
                }

                // Player
                if (p == player.Position)
                {
                    Console.Write($"{GameColors.Bold}{player.Color}{player.Glyph}{GameColors.Reset}");
                    continue;
                }

                // Monster (only if visible)
                var monster = tile.IsVisible ? game.MonsterAt(p) : null;
                if (monster != null)
                {
                    Console.Write($"{monster.Color}{monster.Glyph}{GameColors.Reset}");
                    continue;
                }

                // Item (only if visible)
                var item = tile.IsVisible ? game.ItemAt(p) : null;
                if (item != null)
                {
                    Console.Write($"{item.Color}{item.Glyph}{GameColors.Reset}");
                    continue;
                }

                // Tile
                if (!tile.IsVisible)
                {
                    (string color, char glyph) = tile.Type switch
                    {
                        TileType.Floor      => (GameColors.Gray, '.'),
                        TileType.StairsDown => (GameColors.Gray, '>'),
                        TileType.StairsUp   => (GameColors.Gray, '<'),
                        _                   => (GameColors.Gray, '#'),
                    };
                    Console.Write($"{color}{glyph}{GameColors.Reset}");
                }
                else
                {
                    (string color, char glyph) = tile.Type switch
                    {
                        TileType.Floor      => (GameColors.White, '.'),
                        TileType.StairsDown => (GameColors.Cyan,  '>'),
                        TileType.StairsUp   => (GameColors.Cyan,  '<'),
                        _                   => (GameColors.White, '#'),
                    };
                    Console.Write($"{color}{glyph}{GameColors.Reset}");
                }
            }
        }
    }

    private static void DrawStatusBar(Game game)
    {
        var p = game.Player;
        int row = MapOffsetY + Map.Height;

        Console.SetCursorPosition(0, row);
        string hpColor = p.Hp < p.MaxHp / 3 ? GameColors.Red : GameColors.Green;
        Console.Write(
            $"{hpColor}HP:{p.Hp}/{p.MaxHp}{GameColors.Reset}  " +
            $"{GameColors.Cyan}ATK:{p.Attack}{GameColors.Reset}  " +
            $"{GameColors.Blue}DEF:{p.Defense}{GameColors.Reset}  " +
            $"{GameColors.Magenta}LV:{p.Level}{GameColors.Reset}  " +
            $"{GameColors.Yellow}EXP:{p.Exp}/{p.ExpNext}{GameColors.Reset}  " +
            $"{GameColors.Yellow}Gold:{p.Gold}{GameColors.Reset}  " +
            $"Score:{p.Score}");

        Console.SetCursorPosition(0, row + 1);
        Console.Write($"{GameColors.Gray}[WASD/Arrows] Move  [>] Descend  [<] Ascend  [i] Inventory  [.] Wait  [q] Quit{GameColors.Reset}");
    }

    private static void DrawMessageLog(Game game)
    {
        int row = MapOffsetY + Map.Height + 2;
        var lines = game.Log.Lines;
        for (int i = 0; i < 3; i++)
        {
            Console.SetCursorPosition(0, row + i);
            string text = i < lines.Count ? lines[i] : "";
            Console.Write(text.PadRight(Map.Width));
        }
    }

    // ── Overlay screens ───────────────────────────────────────────────────────

    public static void DrawInventory(Game game)
    {
        Console.Clear();
        Console.WriteLine($"{GameColors.Bold}{GameColors.Yellow}=== INVENTORY ==={GameColors.Reset}");
        Console.WriteLine($"Carry: {game.Player.Inventory.Count}/{Player.MaxInventory}");
        Console.WriteLine();

        var inv = game.Player.Inventory;
        if (inv.Count == 0)
        {
            Console.WriteLine("(empty)");
        }
        else
        {
            for (int i = 0; i < inv.Count; i++)
            {
                var item = inv[i];
                Console.WriteLine($"  [{i + 1}] {item.Color}{item.Glyph}{GameColors.Reset} {item.Name}");
            }
            Console.WriteLine();
            Console.WriteLine("Enter number to use item, or [Esc] to cancel:");
        }
    }

    public static void DrawGameOver(Game game)
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine($"{GameColors.Bold}{GameColors.Red}  ══════════════════════════════{GameColors.Reset}");
        Console.WriteLine($"{GameColors.Bold}{GameColors.Red}           YOU DIED            {GameColors.Reset}");
        Console.WriteLine($"{GameColors.Bold}{GameColors.Red}  ══════════════════════════════{GameColors.Reset}");
        Console.WriteLine();
        Console.WriteLine($"  Floor reached : {game.Floor}");
        Console.WriteLine($"  Level         : {game.Player.Level}");
        Console.WriteLine($"  Gold          : {game.Player.Gold}");
        Console.WriteLine($"  Final Score   : {game.Player.Score}");
        Console.WriteLine();
        Console.WriteLine($"  {GameColors.Gray}Press any key to exit...{GameColors.Reset}");
    }

    public static void DrawVictory(Game game)
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine($"{GameColors.Bold}{GameColors.Yellow}  ══════════════════════════════{GameColors.Reset}");
        Console.WriteLine($"{GameColors.Bold}{GameColors.Yellow}     YOU ESCAPED THE DUNGEON!  {GameColors.Reset}");
        Console.WriteLine($"{GameColors.Bold}{GameColors.Yellow}  ══════════════════════════════{GameColors.Reset}");
        Console.WriteLine();
        Console.WriteLine($"  Level         : {game.Player.Level}");
        Console.WriteLine($"  Gold          : {game.Player.Gold}");
        Console.WriteLine($"  Final Score   : {game.Player.Score}");
        Console.WriteLine();
        Console.WriteLine($"  {GameColors.Gray}Press any key to exit...{GameColors.Reset}");
    }

    public static void DrawHelp()
    {
        Console.Clear();
        Console.WriteLine($"{GameColors.Bold}{GameColors.Cyan}=== HELP ==={GameColors.Reset}");
        Console.WriteLine();
        Console.WriteLine("  Movement  : WASD or Arrow Keys");
        Console.WriteLine("  Descend   : > (stand on >)");
        Console.WriteLine("  Ascend    : < (stand on <)");
        Console.WriteLine("  Wait      : . (pass turn)");
        Console.WriteLine("  Inventory : i");
        Console.WriteLine("  Help      : ?");
        Console.WriteLine("  Quit      : q");
        Console.WriteLine();
        Console.WriteLine("  Map symbols:");
        Console.WriteLine("    @ = You        # = Wall       . = Floor");
        Console.WriteLine("    > = Stairs dn  < = Stairs up");
        Console.WriteLine("    r = Rat        g = Goblin     T = Troll   D = Dragon");
        Console.WriteLine("    ! = Potion     + = Sword      [ = Armor   $ = Gold");
        Console.WriteLine();
        Console.WriteLine($"  {GameColors.Gray}Press any key to return...{GameColors.Reset}");
    }
}
