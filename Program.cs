using SadConsole;
using SadGame = SadConsole.Game;

Settings.WindowTitle = "Dungeon Descent";

SadGame.Create(DungeonDescent.Layout.WindowWidth, DungeonDescent.Layout.WindowHeight, (_, _) =>
{
    var game = new DungeonDescent.Game();
    var root = new DungeonDescent.RootScreen(game);
    SadGame.Instance.Screen = root;
    SadGame.Instance.DestroyDefaultStartingConsole();
});
SadGame.Instance.Run();
SadGame.Instance.Dispose();
