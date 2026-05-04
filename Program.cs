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

// try/finally guarantees Dispose runs even if Run throws, so SDL /
// MonoGame native handles release before the process exits.
try
{
    SadGame.Instance.Run();
}
finally
{
    SadGame.Instance.Dispose();
}
