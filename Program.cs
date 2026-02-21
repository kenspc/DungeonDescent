using DungeonDescent;

// Console setup
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.CursorVisible = false;
Console.Title = "Dungeon Descent";

// Ensure terminal is large enough
if (Console.WindowWidth < 62 || Console.WindowHeight < 27)
{
    Console.CursorVisible = true;
    Console.WriteLine("Please resize your terminal to at least 62×27 and restart.");
    return;
}

var game = new Game();
bool running = true;

while (running && game.Status == GameStatus.Playing)
{
    Renderer.DrawAll(game);

    var key = Console.ReadKey(intercept: true);

    switch (char.ToLower(key.KeyChar))
    {
        case 'q':
            running = false;
            break;

        case 'i':
            HandleInventory(game);
            break;

        case '?':
            Renderer.DrawHelp();
            Console.ReadKey(intercept: true);
            break;

        default:
            game.HandleKey(key);
            break;
    }
}

// End screens
Console.CursorVisible = true;
if (game.Status == GameStatus.Dead)
{
    Renderer.DrawGameOver(game);
    Console.ReadKey(intercept: true);
}
else if (game.Status == GameStatus.Won)
{
    Renderer.DrawVictory(game);
    Console.ReadKey(intercept: true);
}

static void HandleInventory(Game game)
{
    while (true)
    {
        Renderer.DrawInventory(game);

        if (game.Player.Inventory.Count == 0)
        {
            Console.ReadKey(intercept: true);
            return;
        }

        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape) return;

        if (key.KeyChar >= '1' && key.KeyChar <= '9')
        {
            int index = key.KeyChar - '1';
            var msg = game.UseInventoryItem(index);
            game.Log.Add(msg);
            game.EndPlayerTurn();  // Using an item costs a turn
            return;
        }
    }
}
