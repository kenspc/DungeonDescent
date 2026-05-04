using SadConsole;
using SadGame = SadConsole.Game;

Settings.WindowTitle = "Dungeon Descent";

SadGame.Create(60, 26, (_, _) =>
{
    var game = new DungeonDescent.Game();
    var root = new DungeonDescent.RootScreen(game);
    SadGame.Instance.Screen = root;
    SadGame.Instance.DestroyDefaultStartingConsole();
});
SadGame.Instance.Run();
SadGame.Instance.Dispose();
